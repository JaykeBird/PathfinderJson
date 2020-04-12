using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Diagnostics;
using System.Linq;

namespace UiCore.Keyboard
{
    /// <summary>
    /// A class to list and manage methods available to use with keyboard shortcuts. 
    /// </summary>
    public class RoutedMethodRegistry
    {
        public Dictionary<string, (string, RoutedEventHandler, MenuItem?)> RegisteredMethods { get; } = new Dictionary<string, (string, RoutedEventHandler, MenuItem?)>();

        public void Add(string methodId, RoutedEventHandler method, MenuItem? methodHandler)
        {
            RegisteredMethods[methodId] = (methodId, method, methodHandler);
        }

        public void FillFromMenu(System.Windows.Controls.Menu menu)
        {
            foreach (MenuItem? menuItem in menu.Items)
            {
                if (menuItem != null)
                {
                    FillFromMenuItem(menuItem);
                }
            }
        }

        private void FillFromMenuItem(MenuItem mi)
        {
            foreach (object? o in mi.Items)
            {
                if (o is MenuItem item)
                {
                    if (item != null)
                    {
                        // source: https://stackoverflow.com/questions/982709/removing-routed-event-handlers-through-reflection
                        var eventInfo = item.GetType().GetEvent("Click", BindingFlags.Public | BindingFlags.FlattenHierarchy);
                        TypeInfo t = item.GetType().GetTypeInfo();
                        var clickEvent = t.DeclaredFields.Where((ei) => { return ei.Name == "ClickEvent"; }).First();
                        RoutedEvent re = (RoutedEvent)clickEvent.GetValue(item)!;

                        PropertyInfo pi = t.GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic)!;
                        var ehs = pi.GetValue(item);
                        if (ehs != null)
                        {
                            var delegates = (RoutedEventHandlerInfo[]?)ehs.GetType().GetMethod("GetRoutedEventHandlers")!.Invoke(ehs, new object[] { MenuItem.ClickEvent });
                            if (delegates != null)
                            {
                                if (delegates.Length > 0)
                                {
                                    var dele = (delegates[0]).Handler;
                                    var handler = (RoutedEventHandler)(delegates[0]).Handler;
                                    Add(item.Name, handler, item);
                                }
                            }
                        }

                        if (item.Items.Count > 0)
                        {
                            FillFromMenuItem(item);
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            RegisteredMethods.Clear();
        }

        public bool Contains(string methodId)
        {
            return RegisteredMethods.ContainsKey(methodId);
        }

        public (string methodId, RoutedEventHandler handler, MenuItem? menuItem) Get(string methodId)
        {
            return RegisteredMethods[methodId];
        }

        public (string methodId, RoutedEventHandler handler, MenuItem? menuItem) this[string methodId] { get => RegisteredMethods[methodId]; }

        public int Count { get => RegisteredMethods.Count; }
    }
}
