using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MarbleBag<T>
{
    private IEnumerable<T> fullListing;
    private List<T> current = new List<T>();

    public MarbleBag(IEnumerable<T> contents)
    {
        fullListing = contents;
    }

    public void Reset()
    {
        current.Clear();
        foreach (T t in fullListing)
        {
            current.Insert(Random.Range(0, current.Count + 1), t);
        }
    }

    public T Next
    {
        get
        {
            if (current.Count == 0)
            {
                Reset();
            }

            T result = current[current.Count - 1];
            current.RemoveAt(current.Count - 1);

            return result;
        }
    }
}

/* Example:
 * Initialize a Critical hit bag for 25% chance to critical hit
 * private static readonly bool[] CriticalHitBagContents = { true, true, false, false, false, false, false };
 * private MarbleBag<bool> CriticalHitBag = new MarbleBag<bool>(CriticalHitBagContents);
 * 
 * private bool IsCriticalHit()
 * {
 *      return CriticalHitBag.Next;
 * } 
*/
