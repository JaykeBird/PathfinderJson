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
                // See https://stackoverflow.com/a/6040946/44360 for why this is required
                url = System.Text.RegularExpressions.Regex.Replace(url, @"(\\*)" + "\"", @"$1$1\" + "\"");
                url = System.Text.RegularExpressions.Regex.Replace(url, @"(\\+)$", @"$1$1");
                Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{url}\"") { CreateNoWindow = true });
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

        public static int ParseStringAsInt(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }
            else
            {
                try
                {
                    return int.Parse(value);
                }
                catch (FormatException)
                {
                    throw;
                }
            }
        }
        
        public static string CalculateModifier(int score)
        {
            int r = (int)Math.Floor((score - 10) / 2d);
            if (r >= 0) return "+" + r.ToString(); else return r.ToString();
        }
    }
}
