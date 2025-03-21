﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using SolidShineUi;
using System.Runtime;
using System.IO;
using System.Windows.Media.Imaging;
using System.Text;
using System.Windows.Shell;
using System.Runtime.InteropServices;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {

        }

        public static ColorScheme ColorScheme { get; set; } = new ColorScheme(Colors.Peru);

        public static Settings Settings { get; set; } = new Settings();

        #region Constants
        public static Version AppVersion { get; } = new Version("1.2.3");
        public const string VersionString = "102030";
        public const int VersionInt = 1_02_03_0;

        public const string NO_HIGH_CONTRAST = "0";

        public const string TABS_VIEW = "tabs";
        public const string CONTINUOUS_VIEW = "continuous";
        public const string RAWJSON_VIEW = "rawjson";

        public const int TABLE_LIST_VIEW = 0;
        public const int ICON_SPECIAL_VIEW = 1;
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

        public static BitmapImage GetResourcesImage(string name, ColorScheme cs)
        {
            if (!name.EndsWith(".png"))
            {
                name += ".png";
            }

            ImageColor _theme = ImageColor.Color;

            if (cs.IsHighContrast)
            {
                if (cs.BackgroundColor == Colors.Black)
                {
                    _theme = ImageColor.White;
                }
                else
                {
                    _theme = ImageColor.Black;
                }
            }
            else
            {
                if (cs.BackgroundColor == Colors.Black)
                {
                    _theme = ImageColor.White;
                }
                else if (cs.BackgroundColor == Colors.White)
                {
                    _theme = ImageColor.Black;
                }
                else
                {
                    _theme = ImageColor.Color;
                }
            }

            return new BitmapImage(new Uri("pack://application:,,,/Images/" + _theme.ToString("g") + "/" + name, UriKind.RelativeOrAbsolute));
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // let's put this right at the top before any JSON deserialization occurs
            Newtonsoft.Json.JsonConvert.DefaultSettings = () => new Newtonsoft.Json.JsonSerializerSettings
            {
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            };

            string settingsFileName = "settings.json";
            string settingsFilePath = Path.Combine(SettingsIo.SettingsDirectory, settingsFileName);
            // this one method does a lot of work behind the scenes to set up everything lol
            Settings = SettingsIo.PrepareSettingsJson<Settings>();

            if (Settings.UseStartupOptimization)
            {
                // more info: https://docs.microsoft.com/en-us/dotnet/api/system.runtime.profileoptimization?view=netcore-3.1
                ProfileOptimization.SetProfileRoot(Path.Combine(SettingsIo.AppDataDirectory, "Optimization"));
                ProfileOptimization.StartProfile(VersionInt + "_Startup.profile");
            }

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
                // if the system is in High Contrast, let's ask the user if they want PathfinderJson in High Contrast mode
                if (Settings.HighContrastTheme == NO_HIGH_CONTRAST)
                {
                    ColorScheme ncs;
                    MessageDialog md = new MessageDialog
                    {
                        Message = "It appears that you have Windows High Contrast mode activated. Did you want to activate High Contrast mode in PathfinderJSON as well?",
                        OkButtonText = "Yes",
                        CancelButtonText = "No",
                        Image = MessageDialogImage.Question,
                        Title = "PathfinderJSON - High Contrast Mode"
                    };

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

                    SettingsIo.SaveSettingsJson(Settings);
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
            //string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PathfinderJson");
            //string errorLogPath = Path.Combine(appDataPath, "ErrorLogs");

            if (Directory.Exists(SettingsIo.ErrorLogDirectory))
            {
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

                await File.WriteAllTextAsync(Path.Combine(SettingsIo.ErrorLogDirectory, DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ") + ".txt"), sb.ToString(), Encoding.UTF8);
            }

            MessageBox.Show("An error has occurred and PathfinderJSON may not be able to continue.\n\n" +
                "An error log file was created.\n\n" +
                "Please go to PathfinderJson GitHub website to report the error.",
                "PathfinderJSON Error Occurred", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #region Jump List Functions
        private void JumpList_JumpItemsRemovedByUser(object sender, JumpItemsRemovedEventArgs e)
        {

        }

        private async void JumpList_JumpItemsRejected(object sender, JumpItemsRejectedEventArgs e)
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PathfinderJson");

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} Jump Items Rejected:\n", e.RejectionReasons.Count);
            for (int i = 0; i < e.RejectionReasons.Count; ++i)
            {
                if (e.RejectedItems[i].GetType() == typeof(JumpPath))
                    sb.AppendFormat("Reason: {0}\tItem: {1}\n", e.RejectionReasons[i], ((JumpPath)e.RejectedItems[i]).Path);
                else
                    sb.AppendFormat("Reason: {0}\tItem: {1}\n", e.RejectionReasons[i], ((JumpTask)e.RejectedItems[i]).ApplicationPath);
            }

            await File.WriteAllTextAsync(Path.Combine(appDataPath, "JumpItemsRejected.txt"), sb.ToString(), Encoding.UTF8);
        }
        #endregion
    }
}
