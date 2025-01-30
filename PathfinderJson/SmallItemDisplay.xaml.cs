using SolidShineUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PathfinderJson
{
    /// <summary>
    /// A smaller display for <see cref="Equipment"/>, with less editing options than <see cref="ItemEditor"/>.
    /// </summary>
    public partial class SmallItemDisplay : SelectableUserControl
    {
        public SmallItemDisplay()
        {
            InitializeComponent();
        }
    }
}
