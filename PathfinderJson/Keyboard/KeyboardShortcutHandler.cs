using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using KeyBoard = System.Windows.Input.Keyboard;

namespace UiCore.Keyboard
{
    public class KeyboardShortcutHandler
    {
        public KeyboardShortcutHandler(Window w)
        {
            Window = w;

            w.KeyDown += window_KeyDown;
            w.KeyUp += window_KeyUp;

            keyCheck = new DispatcherTimer(new TimeSpan(400), DispatcherPriority.Input, KeyModifierCheck, w.Dispatcher);
        }

        public Window Window { get; set; }
        public KeyRegistry KeyRegistry { get; } = new KeyRegistry();

        private DispatcherTimer keyCheck;

        bool CtrlPressed = false;
        bool ShiftPressed = false;
        bool AltPressed = false;

        public void LoadShortcutsFromList(List<KeyboardShortcut> ks)
        {
            foreach (KeyboardShortcut item in ks)
            {
                KeyRegistry.RegisterKeyShortcut(item);
            }
        }

        public void LoadShortcutsFromFile(string file, RoutedMethodRegistry? methodList)
        {
            var list = KeyboardShortcutsIo.LoadFromFile(file, methodList);
            foreach (KeyboardShortcut item in list)
            {
                KeyRegistry.RegisterKeyShortcut(item);
            }
        }

        public async void WriteShortcutsToFileAsync(string file)
        {
            await KeyboardShortcutsIo.WriteToFile(KeyRegistry, file);
        }

        /// <summary>
        /// Set if menu items should display the keyboard shortcut combinations directly in the user interface. 
        /// </summary>
        /// <param name="display">True to display keyboard shortcut combinations, false to not have them displayed.</param>
        public void ApplyDisplaySettings(bool display)
        {
            KeyRegistry.ApplyDisplaySettings(display);
        }

        private void window_KeyDown(object sender, KeyEventArgs e)
        {
            // first, check for modifier key changes

            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                CtrlPressed = true;
                keyCheck.Start();
                return;
            }

            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                ShiftPressed = true;
                keyCheck.Start();
                return;
            }

            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt || e.Key == Key.System)
            {
                AltPressed = true;
                keyCheck.Start();

                //if (!gwi.MenuBarVisible && !CtrlPressed && !ShiftPressed)
                //{
                //    txtTest.Text = "Alt pressed, Showing menu bar";

                //    firstMenuShow = true;
                //    txtMnu.Text = "true";
                //    ShowMenuBarTemporarily();
                //}

                //menu.Focus();

                return;
            }

            // secondly, check for keyboard shortcuts!

            (RoutedEventHandler? m, string s) = KeyRegistry.GetMethodForKey(e.Key, ShiftPressed, AltPressed, CtrlPressed);

            if (m != null)
            {
                m.Invoke(Window, new RoutedEventArgs(UIElement.KeyDownEvent, this));
            }

            return;
        }

        private void window_KeyUp(object sender, KeyEventArgs e)
        {
            // use to monitor modifier key changes

            CtrlPressed &= (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl);

            ShiftPressed &= (e.Key != Key.LeftShift && e.Key != Key.RightShift);

            AltPressed &= (e.Key != Key.LeftAlt && e.Key != Key.RightAlt && e.Key != Key.System);
        }

        private void KeyModifierCheck(object? sender, EventArgs e)
        {
            if (CtrlPressed)
            {
                if (!KeyBoard.IsKeyDown(Key.LeftCtrl) && !KeyBoard.IsKeyDown(Key.RightCtrl))
                {
                    CtrlPressed = false;
                }
            }

            if (ShiftPressed)
            {
                if (!KeyBoard.IsKeyDown(Key.LeftShift) && !KeyBoard.IsKeyDown(Key.RightShift))
                {
                    ShiftPressed = false;
                }
            }

            if (AltPressed)
            {
                if (!KeyBoard.IsKeyDown(Key.LeftAlt) && !KeyBoard.IsKeyDown(Key.RightAlt) && !KeyBoard.IsKeyDown(Key.System))
                {
                    AltPressed = false;
                }
            }

            if (!CtrlPressed && !ShiftPressed && !AltPressed)
            {
                keyCheck.Stop();
            }
        }

    }
}
