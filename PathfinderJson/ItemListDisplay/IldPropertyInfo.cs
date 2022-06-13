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

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public IldType IldType { get; set; }

        public int? MinValue { get; set; }

        public int? MaxValue { get; set; }

    }

    public class IldPropertyFilter
    {
        public IldPropertyFilter(string name, string filterType, string value)
        {
            Name = name;
            FilterType = filterType;
            Value = value;
        }

        public string Name { get; set; }

        public string FilterType { get; set; }

        public string Value { get; set; }
    }

    public enum IldType
    {
        String = 0,
        Integer = 1,
        Double = 2,
        Boolean = 3
    }
}
