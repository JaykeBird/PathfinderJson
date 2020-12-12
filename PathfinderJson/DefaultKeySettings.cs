using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using SolidShineUi.Keyboard;

namespace PathfinderJson
{
    public static class DefaultKeySettings
    {

        public static List<KeyboardShortcut> CreateDefaultShortcuts(RoutedMethodRegistry mr)
        {
            List<KeyboardShortcut> ks = new List<KeyboardShortcut>();

            AddShortcut(ref ks, KeyboardCombination.Ctrl, Key.N, "mnuNew", mr);
            AddShortcut(ref ks, KeyboardCombination.CtrlShift, Key.N, "mnuNewWindow", mr);
            AddShortcut(ref ks, KeyboardCombination.Ctrl, Key.O, "mnuOpen", mr);
            AddShortcut(ref ks, KeyboardCombination.Ctrl, Key.W, "mnuClose", mr);
            AddShortcut(ref ks, KeyboardCombination.Ctrl, Key.S, "mnuSave", mr);
            AddShortcut(ref ks, KeyboardCombination.None, Key.F12, "mnuSaveAs", mr);
            AddShortcut(ref ks, KeyboardCombination.CtrlAlt, Key.S, "mnuSaveAs", mr);
            AddShortcut(ref ks, KeyboardCombination.CtrlAlt, Key.R, "mnuRevert", mr);
            AddShortcut(ref ks, KeyboardCombination.None, Key.F5, "mnuUpdate", mr);
            AddShortcut(ref ks, KeyboardCombination.Ctrl, Key.K, "mnuUpdate", mr);
            AddShortcut(ref ks, KeyboardCombination.Ctrl, Key.E, "mnuRawJson", mr);
            AddShortcut(ref ks, KeyboardCombination.Ctrl, Key.D1, "mnuScroll", mr);
            AddShortcut(ref ks, KeyboardCombination.Ctrl, Key.D2, "mnuTabs", mr);
            AddShortcut(ref ks, KeyboardCombination.Ctrl, Key.D3, "mnuRawJson", mr);
            AddShortcut(ref ks, KeyboardCombination.None, Key.F8, "mnuToolbar", mr);
            AddShortcut(ref ks, KeyboardCombination.Ctrl, Key.D, "mnuDiceRoll", mr);
            AddShortcut(ref ks, KeyboardCombination.CtrlAlt, Key.F1, "mnuAbout", mr);
            AddShortcut(ref ks, KeyboardCombination.Ctrl, Key.F1, "mnuFeedback", mr);

            return ks;
        }

        private static void AddShortcut(ref List<KeyboardShortcut> list, KeyboardCombination combination, Key key, string method, RoutedMethodRegistry mr)
        {
            try
            {
                var res = mr[method];

                KeyboardShortcut ks = new KeyboardShortcut(combination, key, res.handler, res.methodId, res.menuItem);
                list.Add(ks);
            }
            catch (KeyNotFoundException) { }
        }
    }
}
