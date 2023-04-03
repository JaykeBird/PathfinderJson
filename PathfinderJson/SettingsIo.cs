using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows;
using Newtonsoft.Json;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using System.Runtime.CompilerServices;

namespace PathfinderJson
{
    /// <summary>
    /// A helper class for JaykeBird programs, for organizing app settings and setting up working directories.
    /// </summary>
    public static class SettingsIo
    {
        // note that this class includes some PathfinderJson-specific code and scenarios. Review the code when adapting for use in another program

        /// <summary>
        /// Get if this program is running as a portable program.
        /// A portable program isn't installed on a user's computer (and thus can be transferred between devices), and so settings should be stored alongside the program's executable.
        /// </summary>
        public static bool IsPortable { get; } = true;

        /// <summary>
        /// Get the directory that user settings files should be stored into.
        /// </summary>
        /// <remarks>
        /// This folder should store settings that users have access to changing, and that aren't vital for the program running by itself.
        /// Note that the settings directory is unique to every version of the program, so each version can have their own settings.
        /// </remarks>
        public static string SettingsDirectory { get; private set; } = "";

        /// <summary>
        /// Get the directory for storing any data or working files, that the application uses for performing its functions.
        /// </summary>
        /// <remarks>
        /// These files should not be user settings, and should instead be files or things that the application creates, manages, and deletes throughout the course of its lifecycle.
        /// More permanently stored things can be present here as well, but note that this directory could be cleared out at any time between executions.
        /// This directory is shared between all versions of the program, and note that multiple versions could be running at the same time.
        /// </remarks>
        public static string AppDataDirectory { get; private set; } = "";

        /// <summary>
        /// Get the directory that error logs should be stored in. Error logs are helpful for explaining the program's status at the time that it crashed.
        /// </summary>
        public static string ErrorLogDirectory { get; private set; } = "";

        private const string appName = "PathfinderJson";
        private const string appSettingsName = "pathfinderJson";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string LocateAppDataFolder()
        {
            string AppDataFolder = "";

            AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JaykeBird");

            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //{
            //    AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JaykeBird");
            //}
            //else
            //{
            //    // check the Home environment variable
            //    string? homeDir = Environment.GetEnvironmentVariable("HOME");
            //    if (homeDir != null) // if it is null, then... well, we tried
            //    {
            //        AppDataFolder = Path.Combine(homeDir, ".jaykebird");
            //    }
            //}

            return AppDataFolder;
        }

        /// <summary>
        /// This method sets up all of the main directories, and also initializes the <see cref="SettingsDirectory"/> variable. This does NOT create the SettingsDirectory.
        /// </summary>
        public static void SetupBaseDirectories()
        {
            string AppDataFolder = LocateAppDataFolder();

            if (!string.IsNullOrEmpty(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
                AppDataDirectory = Directory.CreateDirectory(Path.Combine(AppDataFolder, appName)).FullName;
                Directory.CreateDirectory(Path.Combine(AppDataFolder, appName, "Optimization"));
                Directory.CreateDirectory(Path.Combine(AppDataFolder, appName, "ErrorLogs"));
            }

            if (IsPortable)
            {
                string? filename = Process.GetCurrentProcess().MainModule?.FileName;
                if (filename == null)
                {
                    Directory.CreateDirectory(Path.Combine(AppDataFolder, "Settings", appSettingsName));
                    Directory.CreateDirectory(Path.Combine(AppDataFolder, appName));

                    // don't create the directory for this version yet, as a detection method occurs later
                    SettingsDirectory = Path.Combine(AppDataFolder, "Settings", appSettingsName, App.VersionString); //Directory.CreateDirectory().FullName;
                }
                else
                {
                    string parentPath = Directory.GetParent(filename)?.FullName ?? filename.Replace(".exe", "");
                    SettingsDirectory = Path.Combine(parentPath, "Settings");
                }
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(AppDataFolder, "Settings", appSettingsName));
                Directory.CreateDirectory(Path.Combine(AppDataFolder, appName));

                // don't create the directory for this version yet, as a detection method occurs later
                SettingsDirectory = Path.Combine(AppDataFolder, "Settings", appSettingsName, App.VersionString); // Directory.CreateDirectory().FullName;
            }
        }

        /// <summary>
        /// Load a Settings object, from a JSON file.
        /// </summary>
        /// <typeparam name="T">The type of the Settings object. This object should be serializable.</typeparam>
        /// <param name="settingsFileName">The JSON file to read from.</param>
        /// <returns>Either the Settings object deserialized from the file, or returns a new instance of <typeparamref name="T"/> if the file could not be found or read.</returns>
        /// <remarks>
        /// Message dialogs will be displayed to the user if the file could not be found or read.
        /// </remarks>
        public static T LoadSettingsJson<T>(string settingsFileName) where T : new()
        {
            if (File.Exists(settingsFileName))
            {
                try
                {
                    using StreamReader file = File.OpenText(settingsFileName);
                    JsonSerializer serializer = new JsonSerializer();

                    T? ss = (T?)serializer.Deserialize(file, typeof(T));
                    if (ss == null) ss = new T();
                    return ss;
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("The settings file for PathfinderJson could not be found or accessed. PathfinderJson will continue with default settings. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new T();
                }
                catch (System.Security.SecurityException)
                {
                    MessageBox.Show("The settings file for PathfinderJson could not be found or accessed. PathfinderJson will continue with default settings. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new T();
                }
                catch (JsonReaderException)
                {
                    MessageBox.Show("The settings file for PathfinderJson was corrupted. PathfinderJson will continue with default settings.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return new T();
                    //T sn = new T();
                    //using StreamWriter file = new StreamWriter(settingsFileName, false, new UTF8Encoding(false));
                    //JsonSerializer serializer = new JsonSerializer();
                    //serializer.Serialize(file, sn);
                    //return sn;
                }
                catch (FileNotFoundException)
                {
                    // somehow the file got deleted???
                    T sn = new T();
                    using StreamWriter file = new StreamWriter(settingsFileName, false, new UTF8Encoding(false));
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, sn);
                    return sn;
                }
                catch (IOException)
                {
                    MessageBox.Show("The settings file for PathfinderJson could not be found or accessed. PathfinderJson will continue with default settings. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new T();
                }
            }
            else
            {
                // there is no settings file here, let's just build a new one
                T settings = new T();
                try
                {
                    using StreamWriter file = new StreamWriter(settingsFileName, false, new UTF8Encoding(false));
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, settings);
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("The settings file could not be created for PathfinderJson. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (System.Security.SecurityException)
                {
                    MessageBox.Show("The settings file could not be created for PathfinderJson. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (IOException)
                {
                    MessageBox.Show("The settings file could not be created for PathfinderJson. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return settings;
            }
        }

        /// <summary>
        /// Load a Settings object, from an XML file.
        /// </summary>
        /// <typeparam name="T">The type of the Settings object. This object should be serializable.</typeparam>
        /// <param name="settingsFileName">The XML file to read from.</param>
        /// <returns>Either the Settings object deserialized from the file, or returns a new instance of <typeparamref name="T"/> if the file could not be found or read.</returns>
        /// <remarks>
        /// For performance reasons, please consider storing a static XmlSerializer for use throughout the life of the program,
        /// and using <see cref="LoadSettingsXml{T}(string, XmlSerializer)"/> to use that static serializer object.
        /// Message dialogs will be displayed to the user if the file could not be found or read.
        /// </remarks>
        public static T LoadSettingsXml<T>(string settingsFileName) where T : new()
        {
            return LoadSettingsXml<T>(settingsFileName, new XmlSerializer(typeof(T)));
        }

        /// <summary>
        /// Load a Settings object, from an XML file.
        /// </summary>
        /// <typeparam name="T">The type of the Settings object. This object should be serializable.</typeparam>
        /// <param name="settingsFileName">The XML file to read from.</param>
        /// <param name="serializer">The XML serializer that should be used to handle deserializing the file. Note that the serializer should be set up to handle <typeparamref name="T"/> objects.</param>
        /// <returns>Either the Settings object deserialized from the file, or returns a new instance of <typeparamref name="T"/> if the file could not be found or read.</returns>
        /// <remarks>
        /// Message dialogs will be displayed to the user if the file could not be found or read.
        /// </remarks>
        public static T LoadSettingsXml<T>(string settingsFileName, XmlSerializer serializer) where T : new()
        {
            if (File.Exists(settingsFileName))
            {
                try
                {
                    using StreamReader file = File.OpenText(settingsFileName);

                    T? ss = (T?)serializer.Deserialize(XmlReader.Create(settingsFileName));
                    if (ss == null) ss = new T();
                    return ss;
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("The settings file for PathfinderJson could not be found or accessed. PathfinderJson will continue with default settings. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new T();
                }
                catch (System.Security.SecurityException)
                {
                    MessageBox.Show("The settings file for PathfinderJson could not be found or accessed. PathfinderJson will continue with default settings. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new T();
                }
                catch (InvalidOperationException)
                {
                    MessageBox.Show("The settings file for PathfinderJson was corrupted. PathfinderJson will continue with default settings.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return new T();
                    //T sn = new T();
                    //using StreamWriter file = new StreamWriter(settingsFileName, false, new UTF8Encoding(false));
                    //JsonSerializer serializer = new JsonSerializer();
                    //serializer.Serialize(file, sn);
                    //return sn;
                }
                catch (FileNotFoundException)
                {
                    // somehow the file got deleted???
                    T sn = new T();
                    using StreamWriter file = new StreamWriter(settingsFileName, false, new UTF8Encoding(false));
                    serializer.Serialize(file, sn);
                    return sn;
                }
                catch (IOException)
                {
                    MessageBox.Show("The settings file for PathfinderJson could not be found or accessed. PathfinderJson will continue with default settings. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new T();
                }
            }
            else
            {
                // there is no settings file here, let's just build a new one
                T settings = new T();
                try
                {
                    using StreamWriter file = new StreamWriter(settingsFileName, false, new UTF8Encoding(false));
                    serializer.Serialize(file, settings);
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("The settings file could not be created for PathfinderJson. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (System.Security.SecurityException)
                {
                    MessageBox.Show("The settings file could not be created for PathfinderJson. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (IOException)
                {
                    MessageBox.Show("The settings file could not be created for PathfinderJson. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return settings;
            }
        }

        /// <summary>
        /// Locate the directory that stores the latest app settings that can be found. Ideally, this should be this version's main settings folder (<see cref="SettingsDirectory"/>),
        /// but otherwise this will attempt to find a folder from a previous version of this program.
        /// </summary>
        /// <remarks>
        /// If settings cannot be found in the main settings folder (<see cref="SettingsDirectory"/>), then this will attempt to search for the folder from the next-closest previous version.
        /// You should check against the resulting string of this method against <see cref="SettingsDirectory"/> to see if settings are indeed present in the main folder or should be upgraded
        /// from a previous version. If settings cannot be found in any folder or location, then <c>null</c> is returned, and it should be assumed this is a new install.
        /// </remarks>
        /// <returns>The folder that stores user settings (either this app version's folder or the folder from next-closest previous version), 
        /// or <c>null</c> if no folder could be found.</returns>
        public static string? FindLatestSettings()
        {
            // first, some setup
            string AppDataFolder = LocateAppDataFolder();
            string mainSettingsDir = Path.Combine(AppDataFolder, "Settings", appSettingsName); // this is the directory settings should be stored in

            if (Directory.Exists(mainSettingsDir) && Directory.GetDirectories(mainSettingsDir).Any()) // check if main settings directory exists (and make sure it has subdirectories)
            {
                // okay, cool... does the settings directory for this version exist?
                string currentDir = Path.Combine(mainSettingsDir, App.VersionString);
                if (Directory.Exists(currentDir))
                {
                    // current directory is here, let's load in from this directory
                    return Path.Combine(mainSettingsDir, App.VersionString);
                }
                else
                {
                    // do directories for other older (or maybe newer?) versions exist?
                    // let's take a look at others
                    string[] dirList = Directory.GetDirectories(mainSettingsDir);
                    IEnumerable<int> names = dirList.Select(s =>
                    {
                        if (int.TryParse(s, out var nn))
                        {
                            return nn;
                        }
                        else
                        {
                            return -1;
                        }
                    });

                    // get the largest version number that's still less than this one
                    // as newer versions may include settings or have a format that this one doesn't support
                    try
                    {
                        int res = names.Where(i => { return i <= App.VersionInt; }).Max();

                        if (res < 0)
                        {
                            // check if this folder has any files (that could in theory be read from)
                            if (Directory.GetFiles(Path.Combine(mainSettingsDir, res.ToString())).Any())
                            {
                                // if so, then let's return this folder (so that the user can be asked if they want to upgrade)
                                return Path.Combine(mainSettingsDir, res.ToString());
                            }
                            else
                            {
                                // I could in theory check any other lower folders, but that's a lot of code to write for what will be a fairly unlikely scenario
                                // instead, let's just return null and then cause a new settings file to be created
                                return null;
                            }
                        }
                        else
                        {
                            // if res is -1, then there are no folders that have an integer-only name
                            // just return nothing
                            return null;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // there are no folders that are less than the current version
                        // there may be folders that are lower than the 
                        return null;
                    }
                }
            }
            else
            {
                // no settings are stored in the main directory

                // check if the old settings directory is being used (Windows only)
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName)))
                {
                    // old version of settings is stored here, probably
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName);
                }
                else
                {
                    // settings is not anywhere, time to start from scratch
                    return null;
                }
            }
        }

    }
}
