using System;
using System.Collections.Generic;
using System.Text;

namespace PathfinderJson
{
    public static class CoreUtils
    {




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
    }
}
