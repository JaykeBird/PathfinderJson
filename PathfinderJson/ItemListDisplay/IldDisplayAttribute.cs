using System;
using System.Collections.Generic;
using System.Text;

namespace PathfinderJson.Ild
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class IldDisplayAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236

        // This is a positional argument
        public IldDisplayAttribute() { }

        public string? Name { get; set; } = null;

        public bool Ignore { get; set; } = false;

        public int? MinValue { get; set; } = null;
        public int? MaxValue { get; set; } = null;

        public bool HandleAsInt { get; set; } = false;
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class IldLinkAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236

        // This is a positional argument
        public IldLinkAttribute(string? baseName) { BaseName = baseName; }

        public string? BaseName { get; } = null;
    }
}
