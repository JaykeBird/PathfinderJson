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

            Newtonsoft.Json.JsonConvert.DefaultSettings = () => new Newtonsoft.Json.JsonSerializerSettings { 
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore 
            };
        }

        public static ColorScheme ColorScheme { get; set; } = new ColorScheme(Colors.Peru);

        public static Settings Settings { get; set; } = new Settings();

        #region Constants
        public static Version AppVersion = new Version("0.9.4");
        
        public const string NO_HIGH_CONTRAST = "0";

        public const string TABS_VIEW = "tabs";
        public const string CONTINUOUS_VIEW = "continuous";
        public const string RAWJSON_VIEW = "rawjson";
        #endregion

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

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PathfinderJson");

            string file = "";

            if (e.Args.Length > 0)
            {
                if (File.Exists(e.Args[0]))
                {
                    file = e.Args[0];
                }
            }

            if (SystemParameters.HighContrast)
            {
                if (Settings.HighContrastTheme == NO_HIGH_CONTRAST)
                {
                    ColorScheme ncs;
                    MessageDialog md = new MessageDialog();
                    md.Message = "It appears that you have Windows High Contrast mode activated. Did you want to activate High Contrast mode in PathfinderJSON as well?";
                    md.OkButtonText = "Yes";
                    md.CancelButtonText = "No";
                    md.Image = MessageDialogImage.Question;
                    md.Title = "PathfinderJSON - High Contrast Mode";

                    // check the control color
                    if (SystemColors.ControlColor == Colors.Black)
                    {
                        // black color scheme?
                        if (SystemColors.WindowTextColor == Colors.White)
                        {
                            ncs = ColorScheme.GetHighContrastScheme(HighContrastOption.WhiteOnBlack);
                            md.ColorScheme = ncs;

                            if (md.ShowDialog() == MessageDialogResult.OK)
                            {
                                Settings.HighContrastTheme = "1";
                            }
                        }
                        else
                        {
                            ncs = ColorScheme.GetHighContrastScheme(HighContrastOption.GreenOnBlack);
                            md.ColorScheme = ncs;

                            if (md.ShowDialog() == MessageDialogResult.OK)
                            {
                                Settings.HighContrastTheme = "2";
                            }
                        }
                    }
                    else
                    {
                        ncs = ColorScheme.GetHighContrastScheme(HighContrastOption.BlackOnWhite);
                        md.ColorScheme = ncs;

                        if (md.ShowDialog() == MessageDialogResult.OK)
                        {
                            Settings.HighContrastTheme = "3";
                        }
                    }

                    Settings.Save(Path.Combine(appDataPath, "settings.json"));
                }
            }

            MainWindow mw = new MainWindow();
            MainWindow = mw;
            if (file != "") mw.OpenFile(file);
            mw.Show();
        }
    }
}
