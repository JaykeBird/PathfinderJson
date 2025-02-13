using System;
using System.Collections.Generic;
using System.Text;

namespace PathfinderJson.Ild
{
    /// <summary>
    /// Set how a property of a class is displayed in an ItemListDisplay. This should be applied to the actual type that contains the data, not the UI element's type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class IldDisplayAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236

        // This is a positional argument
        public IldDisplayAttribute() { }

        /// <summary>
        /// The name to use for displaying this property in the UI. If this is null, then the property's actual name is used.
        /// </summary>
        public string? Name { get; set; } = null;

        /// <summary>
        /// Whether this property should be ignored by the ItemListDisplay.
        /// </summary>
        public bool Ignore { get; set; } = false;

        /// <summary>
        /// The minimum value that is allowed for numeric properties.
        /// </summary>
        public int? MinValue { get; set; } = null;

        /// <summary>
        /// The maximum value that is allowed for numeric properties.
        /// </summary>
        public int? MaxValue { get; set; } = null;

        /// <summary>
        /// Set if this property should be searched against if a search term is entered into the ItemListDisplay.
        /// </summary>
        public bool Searchable { get; set; } = false;
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class IldLinkAttribute : Attribute
    {
        // This is a positional argument
        public IldLinkAttribute(string? baseName) { BaseName = baseName; }

        public string? BaseName { get; } = null;
    }
}
