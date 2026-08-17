#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace Repetitionless.Editor.Updating
{
    using Config;

    internal static class SaleChecker
    {
        public enum ESalePlatform
        {
            All,
            Unity,
            Itch
        }

        [Serializable]
        public struct SaleInfo
        {
            public ESalePlatform Platform;
            public int PercentOff;
            public DateTime StartDate;
            public DateTime EndDate;
        }

        [Serializable]
        private struct SaleInfoJson
        {
            public int platform;
            public int percentOff;
            public string startDate;
            public string endDate;
        }

        [Serializable]
        private struct SaleInfoJsonArray
        {
            public SaleInfoJson[] items;
        }

        private const string SALES_FILE_URL = "https://data.wilschack.dev/repetitionless/sales.json";
        private const string CACHE_FILE_PATH = Constants.LIBRARY_PATH + "/sales.json";
        private const string DATE_TIME_FORMAT = "dd-MM-yyyy";

        private static List<SaleInfo> _sales = null;

        private static HttpClient _client = new HttpClient();

        private static FileInfo GetCacheFileInfo()
        {
            FileInfo prefsFileInfo = new FileInfo(CACHE_FILE_PATH);
            if (!prefsFileInfo.Exists)
                CreateCache();

            return prefsFileInfo;
        }

        private static void CreateCache()
        {
            FileInfo prefsFileInfo = new FileInfo(CACHE_FILE_PATH);
            if (prefsFileInfo.Exists) return;

            string parentDir = prefsFileInfo.DirectoryName;
            if (!Directory.Exists(parentDir))
                Directory.CreateDirectory(parentDir);

            string contents = "[]";
            File.WriteAllText(prefsFileInfo.FullName, contents);
        }

        public static void ReadCache(bool force = false)
        {
            // Only read the cache if required
            if (_sales != null && !force)
                return;

            if (_sales == null)
                _sales = new List<SaleInfo>();
            else
                _sales.Clear();

            FileInfo cacheFileInfo = GetCacheFileInfo();
            string json = File.ReadAllText(cacheFileInfo.FullName);

            // JSONUtility cannot read root json arrays, put the array in an object
            string wrappedJson = $"{{\"items\":{json}}}";
            SaleInfoJsonArray responseJson = JsonUtility.FromJson<SaleInfoJsonArray>(wrappedJson);

            foreach(SaleInfoJson saleJson in responseJson.items) {
                SaleInfo sale;
                sale.Platform = (ESalePlatform)saleJson.platform;
                sale.PercentOff = saleJson.percentOff;
                sale.StartDate = DateTime.ParseExact(saleJson.startDate, DATE_TIME_FORMAT, null);
                sale.EndDate = DateTime.ParseExact(saleJson.endDate, DATE_TIME_FORMAT, null);

                _sales.Add(sale);
            }
        }

        public static void FetchSalesAndUpdateCache()
        {
            if (!RepetitionlessPrefs.Data.CheckForSales)
                return;

            // Get sales data
            HttpRequestMessage request = new HttpRequestMessage() {
                RequestUri = new Uri(SALES_FILE_URL),
                Method = HttpMethod.Get
            };

            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Mozilla", "5.0"));

            Task<HttpResponseMessage> getTask;
            try {
                getTask = _client.SendAsync(request);
                getTask.Wait();
            } catch (Exception) {
                return;
            }

            HttpResponseMessage response = getTask.Result;
            Task<string> getContentTask = response.Content.ReadAsStringAsync();
            getContentTask.Wait();

            // Save to cache
            FileInfo cacheFileInfo = GetCacheFileInfo();
            File.WriteAllText(cacheFileInfo.FullName, getContentTask.Result);

            ReadCache(true);
        }

        // Returns empty SaleInfo if no sale is active
        public static SaleInfo GetActiveSale()
        {
            if (!RepetitionlessPrefs.Data.CheckForSales)
                return new SaleInfo();

            ReadCache();

            if (_sales.Count == 0)
                return new SaleInfo();

            // Most recent sale is stored at index 0, assuming there isnt any multiple overlapping
            SaleInfo mostRecentSale = _sales[0];

            // Check if in the start - end date
            DateTime now = DateTime.Now;
            if (now.Ticks <= mostRecentSale.StartDate.Ticks || now.Ticks >= mostRecentSale.EndDate.Ticks)
                return new SaleInfo();

            // Only return the sale if its on this platform
            ESalePlatform platform = mostRecentSale.Platform;
            switch (RepetitionlessPackageInfo.PackageSource) {
                case RepetitionlessPackageInfo.EPackageSource.AssetStore:
                    if (platform == ESalePlatform.All || platform == ESalePlatform.Unity)
                        return mostRecentSale;
                    break;
                case RepetitionlessPackageInfo.EPackageSource.Itch:
                    if (platform == ESalePlatform.All || platform == ESalePlatform.Itch)
                        return mostRecentSale;
                    break;
                case RepetitionlessPackageInfo.EPackageSource.Unknown:
                    return mostRecentSale;
            }

            return new SaleInfo();
        }

        public static bool SaleActive()
        {
            SaleInfo saleInfo = GetActiveSale();
            return saleInfo.PercentOff != 0;
        }

        public static string GetSaleText(SaleInfo sale)
        {
            TimeSpan timeLeft = sale.EndDate.Subtract(DateTime.Now);
            int daysLeft = timeLeft.Days;

            return $"Get the full version for {sale.PercentOff}% Off! ({daysLeft} Day{(daysLeft == 1 ? "" : "s")})";
        }
    }
}
#endif