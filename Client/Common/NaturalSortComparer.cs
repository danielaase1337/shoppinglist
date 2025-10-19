using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BlazorApp.Client.Common
{
    /// <summary>
    /// Natural alphanumeric sorting comparer that sorts strings with numbers correctly.
    /// Example: "Uke 1", "Uke 2", "Uke 10", "Uke 20" instead of "Uke 1", "Uke 10", "Uke 2", "Uke 20"
    /// </summary>
    public class NaturalSortComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var regex = new Regex(@"(\d+)|(\D+)");
            
            var xParts = regex.Matches(x);
            var yParts = regex.Matches(y);

            for (int i = 0; i < Math.Min(xParts.Count, yParts.Count); i++)
            {
                var xPart = xParts[i].Value;
                var yPart = yParts[i].Value;

                // Both are numbers - compare numerically
                if (int.TryParse(xPart, out int xNum) && int.TryParse(yPart, out int yNum))
                {
                    int numCompare = xNum.CompareTo(yNum);
                    if (numCompare != 0) return numCompare;
                }
                // At least one is text - compare alphabetically
                else
                {
                    int stringCompare = string.Compare(xPart, yPart, StringComparison.OrdinalIgnoreCase);
                    if (stringCompare != 0) return stringCompare;
                }
            }

            // If all parts matched, the shorter string comes first
            return xParts.Count.CompareTo(yParts.Count);
        }
    }
}
