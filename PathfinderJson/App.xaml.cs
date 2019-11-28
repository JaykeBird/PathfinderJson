using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using UiCore;
using System.Runtime;
using System.IO;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PathfinderJson");

            if (Directory.Exists(appDataPath))
            {
                Settings = Settings.LoadSettings(Path.Combine(appDataPath, "settings.json"));
            }
            else
            {
                Directory.CreateDirectory(appDataPath);
                Directory.CreateDirectory(Path.Combine(appDataPath, "Optimization"));
                Settings.Save(Path.Combine(appDataPath, "settings.json"));
            }

            ProfileOptimization.SetProfileRoot(Path.Combine(appDataPath, "Optimization"));
            ProfileOptimization.StartProfile("Startup.profile");
        }

        public static ColorScheme ColorScheme { get; set; } = new ColorScheme(Colors.Peru);

        public static Settings Settings { get; set; } = new Settings();

        public static Version AppVersion = new Version("0.9.2.1");
    }
}
