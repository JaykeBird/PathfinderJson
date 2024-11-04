using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PathfinderJson;
using SolidShineUi;

namespace PathfinderJson
{
    public class DetailsButton : FlatButton
    {

        TextBlock tb = new TextBlock();
        ThemedImage ti = new ThemedImage();

        bool init = true;

        public DetailsButton()
        {
            SelectOnClick = true;

            ColorSchemeChanged += detailsButton_ColorSchemeChanged;
            IsSelectedChanged += detailsButton_IsSelectedChanged;

            SetupUI();

            init = false;
        }

        private void detailsButton_IsSelectedChanged(object sender, ItemSelectionChangedEventArgs e)
        {
            if (init) return;

            if (IsSelected)
            {
                ti.ImageName = "UpArrow";
            }
            else
            {
                ti.ImageName = "DownArrow";
            }
        }

        private void detailsButton_ColorSchemeChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (init) return;

            ti.ColorScheme = ColorScheme;
        }

        public string DetailsText
        {
            get => tb.Text;
            set => tb.Text = value;
        }

        void SetupUI()
        {
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;

            //   <local:ThemedImage x:Name="imgDetails" ImageName="DownArrow" Width="16" Height="16" />
            //   <TextBlock Text = "Details" Margin = "3,0" />

            IsSelected = false;

            ti.ImageName = "DownArrow";
            ti.ColorScheme = ColorScheme;
            ti.Width = 16;
            ti.Height = 16;

            tb.Text = "Details";
            tb.Margin = new Thickness(3, 0, 3, 0);

            sp.Children.Add(ti);
            sp.Children.Add(tb);

            base.TransparentBack = true;
            base.BorderSelectionThickness = new Thickness(1);
            base.Content = sp;
        }

    }
}
