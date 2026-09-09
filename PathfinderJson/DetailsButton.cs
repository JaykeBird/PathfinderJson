using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using SolidShineUi;

namespace PathfinderJson
{
    public class DetailsButton : FlatButton
    {

        TextBlock tb = new TextBlock();
        Border ti = new Border();
        Path p = new Path();

        static readonly Geometry closedPath = Geometry.Parse("F1 M 1.22334,10L 7,5L 1.5,0L 0,1.6667L 4,5L 0,8.3333L 1.5,10 Z");
        static readonly Geometry openPath = Geometry.Parse("F1 M 10,1.22334L 5,7L 0,1.5L 1.6667,0L 5,4L 8.3333,0L 10,1.5 Z");

        bool init = false;

        public DetailsButton()
        {
            init = true;

            SelectOnClick = true;
            IsSelectedChanged += detailsButton_IsSelectedChanged;

            SetupUI();

            init = false;
        }

        private void detailsButton_IsSelectedChanged(object sender, ItemSelectionChangedEventArgs e)
        {
            if (init) return;

            if (IsSelected)
            {
                //ti.ImageName = "UpArrow";
                p.Data = openPath;
            }
            else
            {
                //ti.ImageName = "DownArrow";
                p.Data = closedPath;
            }
        }

        /// <summary>
        /// The text to display within the button.
        /// </summary>
        public string DetailsText { get => (string)GetValue(DetailsTextProperty); set => SetValue(DetailsTextProperty, value); }

        /// <summary>The backing dependency property for <see cref="DetailsText"/>. See the related property for details.</summary>
        public static readonly DependencyProperty DetailsTextProperty
            = DependencyProperty.Register(nameof(DetailsText), typeof(string), typeof(DetailsButton),
            new FrameworkPropertyMetadata("Details"));


        void SetupUI()
        {
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;

            //   <local:ThemedImage x:Name="imgDetails" ImageName="DownArrow" Width="16" Height="16" />
            //   <TextBlock Text = "Details" Margin = "3,0" />

            IsSelected = false;

            //ti.ImageName = "DownArrow";
            //ti.ColorScheme = ColorScheme;
            ti.Width = 16;
            ti.Height = 16;

            p.SetBinding(Shape.FillProperty, new Binding("Foreground") { Source = this });
            p.Data = closedPath;
            p.VerticalAlignment = VerticalAlignment.Center;
            p.HorizontalAlignment = HorizontalAlignment.Center;

            ti.Child = p;

            tb.SetBinding(TextBlock.TextProperty, new Binding(nameof(DetailsText)) { Source = this });
            tb.Margin = new Thickness(3, 0, 3, 0);

            sp.Children.Add(ti);
            sp.Children.Add(tb);

            base.TransparentBack = true;
            base.BorderSelectionThickness = new Thickness(1);
            base.Content = sp;
        }

    }
}
