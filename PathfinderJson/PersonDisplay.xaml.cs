using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SolidShineUi;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for PersonDisplay.xaml
    /// </summary>
    public partial class PersonDisplay : UserControl
    {
        public PersonDisplay()
        {
            InitializeComponent();
        }

        public ColorScheme ColorScheme { get => (ColorScheme)GetValue(ColorSchemeProperty); set => SetValue(ColorSchemeProperty, value); }

        /// <summary>The backing dependency property for <see cref="ColorScheme"/>. See the related property for details.</summary>
        public static DependencyProperty ColorSchemeProperty
            = DependencyProperty.Register(nameof(ColorScheme), typeof(ColorScheme), typeof(PersonDisplay),
            new FrameworkPropertyMetadata(new ColorScheme()));

    }
}
