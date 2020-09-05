using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace SolidShineUi.Keyboard
{
    public class MenuItemList
    {

        public Dictionary<string, List<MenuItem>> MenuItems { get; private set; } = new Dictionary<string, List<MenuItem>>();

        public void Load(Menu menu)
        {
            foreach (object? menuItem in menu.Items)
            {
                if (menuItem is MenuItem mi)
                {
                    List<MenuItem> mil = new List<MenuItem>();
                    string title = mi.Header.ToString() ?? "";

                    if (MenuItems.ContainsKey(title))
                    {
                        // begin iterating on numbers
                        int a = 2;
                        bool c = true;

                        while (c)
                        {
                            if (MenuItems.ContainsKey(title + a.ToString()))
                            {
                                a++;
                            }
                            else
                            {
                                c = false;
                                title = title + a.ToString();
                            }
                        }
                    }

                    LoadMenuItem(mi, ref mil);

                    MenuItems.Add(title, mil);
                }
            }
        }

        private void LoadMenuItem(MenuItem item, ref List<MenuItem> items)
        {
            foreach (object? o in item.Items)
            {
                if (o is MenuItem mi)
                {
                    if (mi != null)
                    {
                        if (string.IsNullOrEmpty(mi.Name))
                        {
                            continue;
                        }

                        if (mi.Items.Count > 0)
                        {
                            LoadMenuItem(mi, ref items);
                        }
                        else
                        {
                            items.Add(mi);
                        }
                    }
                }
            }
        }
    }
}
