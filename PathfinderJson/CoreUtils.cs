using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PathfinderJson
{
    public static class CoreUtils
    {

        /// <summary>
        /// Opens the user's default browser to a certain URL. Works on Windows, Linux, and OS X.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        /// <returns></returns>
        public static bool OpenBrowser(string url)
        {
            // See https://github.com/dotnet/corefx/issues/10361
            // This is best-effort only, but should work most of the time.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                return true;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
                return true;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
                return true;
            }
            return false;
        }

        public static string? GetStringOrNull(string? value, bool zeroAsNull = false)
        {
            if (zeroAsNull)
            {
                if (value == "0") value = null;
            }
            return string.IsNullOrEmpty(value) ? null : value;
        }

        public static string GetStringOrNull(string? value, string defaultValue, bool zeroAsNull = false)
        {
            if (zeroAsNull)
            {
                if (value == "0") value = null;
            }

            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        public static int ParseStringAsInt(string? value)
        {
            return ParseStringAsInt(value, 0);
        }

        public static int ParseStringAsInt(string? value, int defaultValue)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            else
            {
                return int.TryParse(value, out int r) ? r : defaultValue;
            }
        }

        public static int? ParseStringAsIntNullable(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            else
            {
                return int.TryParse(value, out int r) ? r : null;
            }
        }

        public static double ParseStringAsDouble(string? value)
        {
            return ParseStringAsDouble(value, 0);
        }

        public static double ParseStringAsDouble(string? value, double defaultValue)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            else
            {
                return double.TryParse(value, out double r) ? r : defaultValue;
            }
        }

        public static double? ParseStringAsDoubleNullable(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            else
            {
                return double.TryParse(value, out double r) ? r : null;
            }
        }

        public static int CalculateModifierInt(int score)
        {
            return (int)Math.Floor((score - 10) / 2d);
        }

        public static string CalculateModifier(int score)
        {
            int r = CalculateModifierInt(score);
            if (r >= 0) return "+" + r.ToString(); else return r.ToString();
        }

        public static string DisplayModifier(int mod)
        {
            if (mod >= 0) return "+" + mod; else return mod.ToString();
        }

        public static IEnumerable<string> GetStringListFromDictionary(Dictionary<string, string> vals)
        {
            foreach (KeyValuePair<string, string> item in vals)
            {
                yield return item.Key + "," + item.Value;
            }
        }

        //public static IEnumerable<string> GetStringListFromDictionary(Dictionary<string, string?> vals)
        //{
        //    foreach (KeyValuePair<string, string?> item in vals)
        //    {
        //        yield return item.Key + "," + item.Value ?? "";
        //    }
        //}
    }
}