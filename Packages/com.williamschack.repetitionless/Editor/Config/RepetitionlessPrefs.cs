#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace Repetitionless.Editor.Config
{
    /// <summary>
    /// Used to update repetitionless prefs<br />
    /// Prefs are project relative and Stored in "Library/com.williamschack.repetitionless/prefs.json"
    /// </summary>
    internal static class RepetitionlessPrefs
    {
        private const string PREFS_FILE_PATH = Constants.LIBRARY_PATH + "/prefs.json";

        internal class Prefs
        {
            public bool WelcomeWindowShown = false;
            public bool OpenWindowOnUpdate = true;

            public string LastProcessedVersion = "0.0.0";
            public bool LiteMode = true;

            public bool URPActive = false;
            public bool HDRPActive = false;
            public bool HasNewHDRPSupport = true; // If the last processed unity version was 6.3+

            public bool CheckForSales = true;

            public long LastSessionId = 0;
        }

        private static Prefs _prefsCache;
        public static Prefs Data => _prefsCache ??= LoadPrefs();

        private static FileInfo GetPrefsFileInfo()
        {
            FileInfo prefsFileInfo = new FileInfo(PREFS_FILE_PATH);
            if (!prefsFileInfo.Exists)
                CreatePrefs();

            return prefsFileInfo;
        }

        private static void CreatePrefs()
        {
            FileInfo prefsFileInfo = new FileInfo(PREFS_FILE_PATH);
            if (prefsFileInfo.Exists) return;

            string parentDir = prefsFileInfo.DirectoryName;
            if (!Directory.Exists(parentDir))
                Directory.CreateDirectory(parentDir);

            Prefs prefs = new Prefs();
            string prefsJson = JsonUtility.ToJson(prefs);

            File.WriteAllText(prefsFileInfo.FullName, prefsJson);
        }

        private static Prefs LoadPrefs()
        {
            FileInfo prefsFileInfo = GetPrefsFileInfo();

            string prefsJson = File.ReadAllText(prefsFileInfo.FullName);
            return JsonUtility.FromJson<Prefs>(prefsJson);
        }

        private static void WritePrefs(Prefs prefs)
        {
            FileInfo prefsFileInfo = GetPrefsFileInfo();
            
            string prefsJson = JsonUtility.ToJson(prefs);
            File.WriteAllText(prefsFileInfo.FullName, prefsJson);
        }

        /// <summary>
        /// Writes the prefs after calling the updater action
        /// </summary>
        /// <param name="updater">
        /// The action used to modify the prefs before writing them
        /// </param>
        public static void UpdatePrefs(Action<Prefs> updater)
        {
            updater(Data);
            WritePrefs(Data);
        }
    }
}
#endif
