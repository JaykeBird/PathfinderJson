using System;
using System.Collections.Generic;
using System.Text;

namespace PathfinderJson.Ild
{
    public class IldPropertyInfo
    {
        public IldPropertyInfo(string name, IldType type, string? displayName = null)
        {
            Name = name;
            IldType = type;
            if (displayName != null) DisplayName = displayName; else DisplayName = name;
        }

        /// <summary>
        /// The internal name of this property
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The name of this property, meant for displaying in UI
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The data type of this property
        /// </summary>
        public IldType IldType { get; set; }

        /// <summary>
        /// The minimum value that is allowed for this property (only applies if <see cref="IldType"/> is <see cref="IldType.Integer"/> or <see cref="IldType.Double"/>)
        /// </summary>
        public int? MinValue { get; set; }

        /// <summary>
        /// The maximum value that is allowed for this property (only applies if <see cref="IldType"/> is <see cref="IldType.Integer"/> or <see cref="IldType.Double"/>)
        /// </summary>
        public int? MaxValue { get; set; }

        /// <summary>
        /// The filter to apply upon this property, to only display elements where its values passes this property's filter. Set to <c>null</c> to not have a filter.
        /// </summary>
        public IldPropertyFilter? Filter { get; set; } = null;

        /// <summary>
        /// Compare a value to this property's filter, and check if passes the filter.
        /// </summary>
        /// <param name="value">The value to check against the filter</param>
        /// <returns><c>true</c> if the <paramref name="value"/> passes, or <c>false</c> if it does not</returns>
        public bool CompareToFilter(object? value)
        {
            if (Filter == null) return true;
            FilterType ft = Filter.FilterType;

            switch (IldType)
            {
                case IldType.String:
                    if (value is string s)
                    {
                        if (ft == FilterType.STRING_CONTAINS)
                        {
                            return s.Contains(Filter.Value);
                        }
                        else if (ft == FilterType.STRING_MATCHES)
                        {
                            return s == Filter.Value;
                        }
                        else if (ft == FilterType.STRING_STARTS_WITH)
                        {
                            return s.StartsWith(Filter.Value);
                        }
                        else if (ft == FilterType.STRING_NOT_CONTAINS)
                        {
                            return !s.Contains(Filter.Value);
                        }
                        else
                        {
                            return true;
                        }
                    }
                    break;
                case IldType.Integer:
                    if (value is int i)
                    {
                        if (ft == FilterType.NUMBER_EQUALS)
                        {
                            return i == Filter.ValueAsInt();
                        }
                        else if (ft == FilterType.NUMBER_BETWEEN)
                        {
                            (int? min, int? max) = Filter.ValueAsBetweenInt();
                            return min == null || max == null || min < i && i < max;
                            // if min or max are null, this means that the filter value is not set properly - just return "true" to allow it through
                        }
                        else if (ft == FilterType.NUMBER_NOT_BETWEEN)
                        {
                            (int? min, int? max) = Filter.ValueAsBetweenInt();
                            return min == null || max == null || i < min || i > max;
                        }
                        else
                        {
                            // wrong filter type applied here
                            return true;
                        }
                    }
                    break;
                case IldType.Double:
                    if (value is double d)
                    {
                        if (ft == FilterType.NUMBER_EQUALS)
                        {
                            return d == Filter.ValueAsDouble();
                        }
                        else if (ft == FilterType.NUMBER_BETWEEN)
                        {
                            (double? min, double? max) = Filter.ValueAsBetweenDouble();
                            return min == null || max == null || min < d && d < max;
                            // if min or max are null, this means that the filter value is not set properly - just return "true" to allow it through
                        }
                        else if (ft == FilterType.NUMBER_NOT_BETWEEN)
                        {
                            (double? min, double? max) = Filter.ValueAsBetweenDouble();
                            return min == null || max == null || d < min || d > max;
                        }
                        else
                        {
                            // wrong filter type applied here
                            return true;
                        }
                    }
                    break;
                case IldType.Boolean:
                    if (value is bool b)
                    {
                        if (ft == FilterType.BOOLEAN_FALSE)
                        {
                            return b == false;
                        }
                        else if (ft == FilterType.BOOLEAN_TRUE)
                        {
                            return b == true;
                        }
                        else
                        {
                            // wrong filter type applied here
                            return true;
                        }
                    }
                    break;
                default:
                    break;
            }

            // this is a type that ILD doesn't support, just return true
            return true;
        }

    }

    public class IldPropertyFilter
    {
        public IldPropertyFilter(FilterType filterType, string value)
        {
            FilterType = filterType;
            Value = value;
        }

        /// <summary>
        /// The type of filter to apply.
        /// </summary>
        public FilterType FilterType { get; set; }

        /// <summary>
        /// The value to be filtering against. Use the helper <c>ValueAs</c>... functions to assist with getting non-string values.
        /// </summary>
        public string Value { get; set; }

        public int? ValueAsInt()
        {
            return int.TryParse(Value, out int i) ? i : null;
        }

        public double? ValueAsDouble()
        {
            return double.TryParse(Value, out double d) ? d : null;
        }

        public (int? min, int? max) ValueAsBetweenInt()
        {
            string[] vals = Value.Split(Value, '-');
            if (vals.Length < 2) return (null, null);
            int? val0 = CoreUtils.ParseStringAsIntNullable(vals[0]);
            int? val1 = CoreUtils.ParseStringAsIntNullable(vals[1]);

            if (val0 > val1)
            {
                (val0, val1) = (val1, val0);
            }

            return (val0, val1);
        }

        public (double? min, double? max) ValueAsBetweenDouble()
        {
            string[] vals = Value.Split(Value, '-');
            if (vals.Length < 2) return (null, null);
            double? val0 = CoreUtils.ParseStringAsDoubleNullable(vals[0]);
            double? val1 = CoreUtils.ParseStringAsDoubleNullable(vals[1]);

            if (val0 > val1)
            {
                (val0, val1) = (val1, val0);
            }

            return (val0, val1);
        }
    }

    public enum FilterType
    {
        STRING_CONTAINS = 0,
        STRING_NOT_CONTAINS = 1,
        STRING_STARTS_WITH = 2,
        STRING_MATCHES = 3,
        BOOLEAN_TRUE = 4,
        BOOLEAN_FALSE = 5,
        NUMBER_EQUALS = 6,
        NUMBER_BETWEEN = 7,
        NUMBER_NOT_BETWEEN = 8
    }

    public enum IldType
    {
        String = 0,
        Integer = 1,
        Double = 2,
        Boolean = 3
    }
}
