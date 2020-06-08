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
using System.Text;

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

            Directory.CreateDirectory(Path.Combine(appDataPath, "Optimization"));
            Directory.CreateDirectory(Path.Combine(appDataPath, "ErrorLogs"));

            if (Directory.Exists(appDataPath))
            {
                try
                {
                    Settings = Settings.LoadSettings(Path.Combine(appDataPath, "settings.json"));
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("The settings file for PathfinderJson could not be found or accessed. PathfinderJson will continue with default settings. Please check the permissions for your AppData folder.", 
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Settings = new Settings();
                }
                catch (System.Security.SecurityException)
                {
                    MessageBox.Show("The settings file for PathfinderJson could not be found or accessed. PathfinderJson will continue with default settings. Please check the permissions for your AppData folder.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Settings = new Settings();
                }
                catch (IOException)
                {
                    MessageBox.Show("The settings file for PathfinderJson could not be found or accessed. PathfinderJson will continue with default settings. Please check the permissions for your AppData folder.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Settings = new Settings();
                }
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(appDataPath);
                    Directory.CreateDirectory(Path.Combine(appDataPath, "Optimization"));
                    Directory.CreateDirectory(Path.Combine(appDataPath, "ErrorLogs"));
                    Settings.Save(Path.Combine(appDataPath, "settings.json"));
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("The settings file could not be created for PathfinderJson. Please check the permissions for your AppData folder.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (System.Security.SecurityException)
                {
                    MessageBox.Show("The settings file could not be created for PathfinderJson. Please check the permissions for your AppData folder.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (IOException)
                {
                    MessageBox.Show("The settings file could not be created for PathfinderJson. Please check the permissions for your AppData folder.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (Settings.UseStartupOptimization)
            {
                // more info: https://docs.microsoft.com/en-us/dotnet/api/system.runtime.profileoptimization?view=netcore-3.1
                ProfileOptimization.SetProfileRoot(Path.Combine(appDataPath, "Optimization"));
                ProfileOptimization.StartProfile("Startup.profile");
            }

            Newtonsoft.Json.JsonConvert.DefaultSettings = () => new Newtonsoft.Json.JsonSerializerSettings { 
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore 
            };
        }

        public static ColorScheme ColorScheme { get; set; } = new ColorScheme(Colors.Peru);

        public static Settings Settings { get; set; } = new Settings();

        #region Constants
        public static Version AppVersion = new Version("1.1.2");
        
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
                                // white on black
                                Settings.HighContrastTheme = "1";
                            }
                        }
                        else
                        {
                            ncs = ColorScheme.GetHighContrastScheme(HighContrastOption.GreenOnBlack);
                            md.ColorScheme = ncs;

                            if (md.ShowDialog() == MessageDialogResult.OK)
                            {
                                // green on black
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
                            // black on white
                            Settings.HighContrastTheme = "3";
                        }
                    }

                    try
                    {
                        Settings.Save(Path.Combine(appDataPath, "settings.json"));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        MessageBox.Show("The settings file for PathfinderJson could not be saved. Please check the permissions for your AppData folder.",
                            "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (System.Security.SecurityException)
                    {
                        MessageBox.Show("The settings file for PathfinderJson could not be saved. Please check the permissions for your AppData folder.",
                            "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (IOException)
                    {
                        MessageBox.Show("The settings file for PathfinderJson could not be saved. Please check the permissions for your AppData folder.",
                            "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            MainWindow mw = new MainWindow();
            MainWindow = mw;
            if (file != "") mw.OpenFile(file);
            mw.Show();
        }

        private async void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // save this to the crash logs
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PathfinderJson");
            string errorLogPath = Path.Combine(appDataPath, "ErrorLogs");

            Exception ex = e.Exception;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(DateTime.UtcNow.ToString("yyyyMMddTHH:mm:ssZ"));
            sb.AppendLine("VERSION " + AppVersion.ToString());
            sb.AppendLine("--------------------------------------");
            sb.AppendLine(ex.GetType().FullName);
            sb.AppendLine(ex.Message);
            sb.AppendLine(ex.ToString());

            if (ex.InnerException != null)
            {
                sb.AppendLine("INNER EXCEPTION:");
                sb.AppendLine(ex.InnerException.GetType().FullName);
                sb.AppendLine(ex.InnerException.Message);
                sb.AppendLine(ex.InnerException.ToString());
            }

            sb.AppendLine("END FILE");

            await File.WriteAllTextAsync(Path.Combine(errorLogPath, DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ")), sb.ToString(), Encoding.UTF8);

            MessageBox.Show("An error has occurred and PathfinderJSON may not be able to continue.\n\n" +
                "An error log file was created.\n\n" +
                "Please go to PathfinderJson GitHub website to report the error.",
                "PathfinderJSON Error Occurred", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
