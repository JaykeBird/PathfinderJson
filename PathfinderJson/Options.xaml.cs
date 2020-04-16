using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UiCore;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : FlatWindow
    {
        public Options()
        {
            InitializeComponent();

            SetupTabs();
            LoadTab("General");
            selTabs[0].IsSelected = true;
        }

        //public Options(string tab)
        //{
        //    InitializeComponent();

        //    SetupTabs();
        //    LoadTab(tab);
        //}

        void SetupTabs()
        {
            selTabs.AddItem(CreateTab("General"));
            selTabs.AddItem(CreateTab("Saving"));
            selTabs.AddItem(CreateTab("Interface"));
            selTabs.AddItem(CreateTab("Text Editor"));
            selTabs.AddItem(CreateTab("Advanced"));

            SelectableItem CreateTab(string name, ImageSource? image = null)
            {
                SelectableItem si = new SelectableItem
                {
                    Height = 36,
                    Text = name,
                    Indent = 6
                };

                if (image == null)
                {
                    si.ShowImage = false;
                }
                else
                {
                    si.ImageSource = image;
                    si.ShowImage = true;
                }

                si.Click += tabItem_Click;
                return si;
            }
        }

        private void tabItem_Click(object? sender, EventArgs e)
        {
            if (sender == null) return;
            SelectableItem si = (SelectableItem)sender;

            if (si.CanSelect)
            {
                LoadTab(si.Text);
            }
        }

        void LoadTab(string name)
        {
            foreach (Grid? item in grdHolder.Children)
            {
                if (item != null)
                {
                    item.Visibility = Visibility.Collapsed;
                }
            }

            switch (name)
            {
                case "General":
                    tabGeneral.Visibility = Visibility.Visible;
                    break;
                case "Saving":
                    tabSaving.Visibility = Visibility.Visible;
                    break;
                case "Interface":
                    tabInterface.Visibility = Visibility.Visible;
                    break;
                case "Text Editor":
                    tabTextEditor.Visibility = Visibility.Visible;
                    break;
                case "Advanced":
                    tabAdvanced.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
