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


        bool resizing = false;

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // if (e.PreviousSize.Width <= 0) return;
            if (e.WidthChanged == false) return;
            if (resizing) return;

            resizing = true;

            if (e.NewSize.Width < 525)
            {
                if (stkRight.Children.Contains(stkHead)) { resizing = false; return; }

                // mini mode
                // TODO: put in right positions
                stkLeft.Children.Remove(stkHead);
                stkRight.Children.Insert(0, stkHead);

                stkLeft.Children.Remove(stkEyes);
                stkRight.Children.Insert(2, stkEyes);

                stkLeft.Children.Remove(stkChest);
                stkRight.Children.Insert(5, stkChest);

                stkLeft.Children.Remove(stkBody);
                stkRight.Children.Insert(6, stkBody);

                stkLeft.Children.Remove(stkArmor);
                stkRight.Children.Insert(7, stkArmor);

                stkLeft.Children.Remove(stkHand);
                stkRight.Children.Insert(9, stkHand);

                stkLeft.Children.Remove(stkFoot);
                stkRight.Children.Insert(12, stkFoot);

                colLeft.Width = new GridLength(0);
            }
            else
            {
                if (stkLeft.Children.Contains(stkHead)) { resizing = false; return; }

                // regular mode
                stkRight.Children.Remove(stkHead);
                stkLeft.Children.Add(stkHead);

                stkRight.Children.Remove(stkEyes);
                stkLeft.Children.Add(stkEyes);

                stkRight.Children.Remove(stkChest);
                stkLeft.Children.Add(stkChest);

                stkRight.Children.Remove(stkBody);
                stkLeft.Children.Add(stkBody);

                stkRight.Children.Remove(stkArmor);
                stkLeft.Children.Add(stkArmor);

                stkRight.Children.Remove(stkHand);
                stkLeft.Children.Add(stkHand);

                stkRight.Children.Remove(stkFoot);
                stkLeft.Children.Add(stkFoot);

                colLeft.Width = new GridLength(1, GridUnitType.Star);

                // stk Left
                // <local:PersonSlotItemList Title="Head/Crown" SlotColor="SkyBlue" x:Name="stkHead" />
                // <local:PersonSlotItemList Title="Face/Eyes" SlotColor="Moccasin" x:Name="stkEyes" />
                // <local:PersonSlotItemList Title="Chest" SlotColor="OrangeRed" x:Name="stkChest" />
                // <local:PersonSlotItemList Title="Body" SlotColor="MediumSeaGreen" x:Name="stkBody" />
                // <local:PersonSlotItemList Title="Armor" SlotColor="Gold" x:Name="stkArmor" />
                // <local:PersonSlotItemList Title="Hand/Glove" SlotColor="GreenYellow" x:Name="stkHand" />
                // <local:PersonSlotItemList Title="Foot" SlotColor="ForestGreen" x:Name="stkFoot" />

                if (e.NewSize.Width >= 600)
                {
                    // add padding to person image
                    person.Margin = new Thickness(10, 0, 10, 0);
                    personButtonGrid.Margin = new Thickness(10, 0, 10, 0);
                    colPerson.Width = new GridLength(220);
                }
                else
                {
                    person.Margin = new Thickness(0);
                    personButtonGrid.Margin = new Thickness(0);
                    colPerson.Width = new GridLength(200);
                }
            }

            resizing = false;
        }
    }
}
