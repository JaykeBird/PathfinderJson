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
using System.Windows.Media.Imaging;

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

        /// <summary>
        /// Get a particular image from the internal Images folder.
        /// </summary>
        /// <exception cref="IOException">Thrown if there is no image with this name.</exception>
        /// <param name="name">The name for the resource.</param>
        public static BitmapImage GetResourcesImage(string name)
        {
            if (!name.EndsWith(".png"))
            {
                name += ".png";
            }

            return new BitmapImage(new Uri("pack://application:,,,/Images/" + name, UriKind.RelativeOrAbsolute));
        }
    }
}
