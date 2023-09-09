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

        #region Read-only Properties / Constants
        /// <summary>
        /// Get if this program is running as a portable program.
        /// A portable program isn't installed on a user's computer (and thus can be transferred between devices), and so settings should be stored alongside the program's executable.
        /// </summary>
        public static bool IsPortable { get; } = true;

        private const bool CanBePortable = true;

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

        private static Encoding UTF8Encoding = new UTF8Encoding(false);

        private const string appName = "PathfinderJson";
        private const string appSettingsName = "pathfinderJson";

        private const string Settings_Redirect_Filename = "settings_redir";
        private const string Settings_RedirPin_Filename = "pin_version";
        #endregion

        #region Directory Setup

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
            string jaykeBirdAppData = LocateAppDataFolder();

            if (!string.IsNullOrEmpty(jaykeBirdAppData))
            {
                Directory.CreateDirectory(jaykeBirdAppData);
                AppDataDirectory = Directory.CreateDirectory(Path.Combine(jaykeBirdAppData, appName)).FullName;
                Directory.CreateDirectory(Path.Combine(jaykeBirdAppData, appName, "Optimization"));
                Directory.CreateDirectory(Path.Combine(jaykeBirdAppData, appName, "ErrorLogs"));
            }

            if (IsPortable && CanBePortable)
            {
                string? filename = Process.GetCurrentProcess().MainModule?.FileName;
                if (filename == null)
                {
                    Directory.CreateDirectory(Path.Combine(jaykeBirdAppData, "Settings", appSettingsName));
                    Directory.CreateDirectory(Path.Combine(jaykeBirdAppData, appName));

                    // don't create the directory for this version yet, as a detection method occurs later
                    SetSettingsDirectoryVariable(jaykeBirdAppData);
                    //SettingsDirectory = Path.Combine(jaykeBirdAppData, "Settings", appSettingsName, App.VersionString); //Directory.CreateDirectory().FullName;
                }
                else
                {
                    string parentPath = Directory.GetParent(filename)?.FullName ?? filename.Replace(".exe", "");
                    SettingsDirectory = Path.Combine(parentPath, "Settings");
                }
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(jaykeBirdAppData, "Settings", appSettingsName));
                Directory.CreateDirectory(Path.Combine(jaykeBirdAppData, appName));

                // don't create the directory for this version yet, as a detection method occurs later
                SetSettingsDirectoryVariable(jaykeBirdAppData);
                //SettingsDirectory = Path.Combine(AppDataFolder, "Settings", appSettingsName, App.VersionString); // Directory.CreateDirectory().FullName;
            }
        }

        /// <summary>
        /// This sets the value <see cref="SettingsDirectory"/>, by reading the standard settings directory and resolving the redirect file if needed.
        /// </summary>
        /// <param name="appDataFolder">The path to the JaykeBird programs AppData folder.</param>
        static void SetSettingsDirectoryVariable(string appDataFolder)
        {
            // in comparison to FindLatestSettings(), this locates the directory that settings should be placed in
            // which will either be 1) the default directory in AppData, or 2) the directory listed in the redirect file

            // so first, let's check the directory and then go from there
            string settingsDir = Path.Combine(appDataFolder, "Settings", appSettingsName, App.VersionString);
            if (Directory.Exists(settingsDir))
            {
                // okay, so the directory is already here... does that mean settings are here, or a redirect file?

                if (File.Exists(Path.Combine(settingsDir, Settings_Redirect_Filename)))
                {
                    // let's read from the redirect file
                    string newDir = File.ReadAllText(Path.Combine(settingsDir, Settings_Redirect_Filename), UTF8Encoding).Trim();
                    if (Directory.Exists(newDir))
                    {
                        SettingsDirectory = newDir;
                    }
                    else
                    {
                        SettingsDirectory = settingsDir;
                    }
                }
                else
                {
                    SettingsDirectory = settingsDir;
                }
            }
            else
            {
                // okay, no redirect file or anything at all (since the directory doesn't exist yet)
                // so let's return the default directory
                SettingsDirectory = settingsDir;
            }
        }
        #endregion

        #region Deserialization
        /// <summary>
        /// Load a Settings object, from a JSON file.
        /// </summary>
        /// <typeparam name="T">The type of the Settings object. This object should be serializable.</typeparam>
        /// <param name="fileName">The JSON file to read from. Only the file name is needed; this is appended to the <see cref="SettingsDirectory"/> path.</param>
        /// <returns>Either the Settings object deserialized from the file, or returns a new instance of <typeparamref name="T"/> if the file could not be found or read.</returns>
        /// <remarks>
        /// Message dialogs will be displayed to the user if the file could not be found or read.
        /// </remarks>
        public static T LoadSettingsJson<T>(string fileName = "settings.json") where T : new()
        {
            string settingsFile = Path.Combine(SettingsDirectory, fileName);
            if (File.Exists(settingsFile))
            {
                try
                {
                    using StreamReader file = File.OpenText(settingsFile);
                    JsonSerializer serializer = new JsonSerializer();

                    T? ss = (T?)serializer.Deserialize(file, typeof(T)) ?? new T();
                    return ss;
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show($"The settings file for {appName} could not be found or accessed. {appName} will continue with default settings. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new T();
                }
                catch (System.Security.SecurityException)
                {
                    MessageBox.Show($"The settings file for {appName} could not be found or accessed. {appName} will continue with default settings. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new T();
                }
                catch (JsonReaderException)
                {
                    MessageBox.Show($"The settings file for {appName} was corrupted. {appName} will continue with default settings.",
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
                    T sn = new();
                    using StreamWriter file = new StreamWriter(settingsFile, false, new UTF8Encoding(false));
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
                T settings = new();
                try
                {
                    using StreamWriter file = new StreamWriter(settingsFile, false, new UTF8Encoding(false));
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, settings);
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show($"The settings file could not be created for {appName}. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (System.Security.SecurityException)
                {
                    MessageBox.Show($"The settings file could not be created for {appName}. Please check the permissions of the folder where settings is saved.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (IOException)
                {
                    MessageBox.Show($"The settings file could not be created for {appName}. Please check the permissions of the folder where settings is saved.",
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
        public static T LoadSettingsXml<T>(string settingsFileName = "settings.xml") where T : new()
        {
            return LoadSettingsXml<T>(new XmlSerializer(typeof(T)), settingsFileName);
        }

        /// <summary>
        /// Load a Settings object, from an XML file.
        /// </summary>
        /// <typeparam name="T">The type of the Settings object. This object should be serializable.</typeparam>
        /// <param name="fileName">The XML file to read from. </param>
        /// <param name="serializer">The XML serializer that should be used to handle deserializing the file. Note that the serializer should be set up to handle <typeparamref name="T"/> objects.</param>
        /// <returns>Either the Settings object deserialized from the file, or returns a new instance of <typeparamref name="T"/> if the file could not be found or read.</returns>
        /// <remarks>
        /// Message dialogs will be displayed to the user if the file could not be found or read.
        /// </remarks>
        public static T LoadSettingsXml<T>(XmlSerializer serializer, string fileName = "settings.xml") where T : new()
        {
            string settingsFile = Path.Combine(SettingsDirectory, fileName);
            if (File.Exists(settingsFile))
            {
                try
                {
                    using StreamReader file = File.OpenText(settingsFile);

                    T? ss = (T?)serializer.Deserialize(XmlReader.Create(settingsFile)) ?? new T();
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
                    T sn = new();
                    using StreamWriter file = new StreamWriter(settingsFile, false, new UTF8Encoding(false));
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
                T settings = new();
                try
                {
                    using StreamWriter file = new StreamWriter(settingsFile, false, new UTF8Encoding(false));
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
        #endregion

        #region Serialization

        public static void SaveSettingsJson<T>(T settings, string fileName = "settings.json")
        {
            string settingsFile = Path.Combine(SettingsDirectory, fileName);
            try
            {
                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
                }

                using StreamWriter file = new StreamWriter(settingsFile, false, UTF8Encoding);
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, settings);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show($"The settings file for {appName} could not be saved. Please make sure the settings directory is available and accessible.",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (System.Security.SecurityException)
            {
                MessageBox.Show($"The settings file for {appName} could not be saved. Please make sure the settings directory is available and accessible.",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException)
            {
                MessageBox.Show($"The settings file for {appName} could not be saved. Please make sure the settings directory is available and accessible.",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void SaveSettingsXml<T>(T settings, string fileName = "settings.xml")
        {
            SaveSettingsXml(settings, new XmlSerializer(typeof(T)), fileName);
        }

        public static void SaveSettingsXml<T>(T settings, XmlSerializer serializer, string fileName = "settings.xml")
        {
            string settingsFile = Path.Combine(SettingsDirectory, fileName);
            try
            {
                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
                }

                using StreamWriter file = new StreamWriter(settingsFile, false, UTF8Encoding);
                serializer.Serialize(file, settings);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show($"The settings file for {appName} could not be saved. Please make sure the settings directory is available and accessible.",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (System.Security.SecurityException)
            {
                MessageBox.Show($"The settings file for {appName} could not be saved. Please make sure the settings directory is available and accessible.",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException)
            {
                MessageBox.Show($"The settings file for {appName} could not be saved. Please make sure the settings directory is available and accessible.",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Locate Settings
        /// <summary>
        /// Locate the directory that stores the latest app settings that can be found. Ideally, this should be this version's main settings folder (<see cref="SettingsDirectory"/>),
        /// but otherwise this will attempt to find a folder from a previous version of this program.
        /// </summary>
        /// <remarks>
        /// If settings cannot be found in the main settings folder (<see cref="SettingsDirectory"/>), then this will attempt to search for the folder from the next-closest previous version.
        /// This will also check and resolve settings redirect files. If settings cannot be found in any folder or location, then <c>null</c> is returned, and it should be assumed this is a new install.
        /// Note that the directory returned here does not necessarily mean that is the directory to write settings to; instead, settings should always be written to the <see cref="SettingsDirectory"/>.
        /// The second return value, the boolean value, determines if a valid upgrade path is available from this directory to the <see cref="SettingsDirectory"/>. If there is an upgrade path,
        /// you should ask the user if they want to upgrade, and if so, then copy all files from this old folder into the current one.
        /// (If there is not an upgrade path, that means that either the current folder is the one being used, so no upgrade needed, or no folder could be found at all.)
        /// </remarks>
        /// <returns>
        /// The folder where current or older user settings have been found (or <c>null</c> if no folder could be found).
        /// Also returns if there is an upgrade path available that the user should be asked about.
        /// </returns>
        public static (string? dirPath, bool canRequestUpgrade) LocateLatestSettingsDirectory()
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
                    // okay, it does... is there a redirect file?
                    if (File.Exists(Path.Combine(currentDir, Settings_Redirect_Filename)))
                    {
                        // settings has been redirected to another directory
                        string newDir = File.ReadAllText(Path.Combine(currentDir, Settings_Redirect_Filename), UTF8Encoding);
                        if (Directory.Exists(newDir))
                        {
                            return (newDir, false);
                        }
                    }

                    // current directory is here, let's load in from this directory
                    return (Path.Combine(mainSettingsDir, App.VersionString), false);
                }
                else
                {
                    // do directories for other older (or maybe newer?) versions exist?
                    // let's take a look at others
                    string[] dirList = Directory.GetDirectories(mainSettingsDir);
                    IEnumerable<int> names = dirList.Select(s =>
                    {
                        return int.TryParse(s, out var nn) ? nn : -1;
                    });

                    try
                    {
                        // get the largest version number that's still less than this one
                        // as newer versions may include settings or have a format that this one doesn't support

                        // we're only going to look at one folder, for the version nearest to the current (but not newer than the current)
                        // trying to recursively look through older and older folders, I think, will have much more diminishing returns
                        int res = names.Where(i => { return i <= App.VersionInt; }).Max();

                        if (res > 0)
                        {
                            // such a folder for an older version exists
                            // first, let's check if there is a settings redirect file
                            if (File.Exists(Path.Combine(mainSettingsDir, res.ToString(), Settings_Redirect_Filename)))
                            {
                                // settings has been redirected to another directory
                                string newDir = File.ReadAllText(Path.Combine(mainSettingsDir, res.ToString(), Settings_Redirect_Filename), UTF8Encoding).Trim();
                                if (Directory.Exists(newDir))
                                {
                                    // is there a pin file to go along with the settings redirect file?
                                    if (File.Exists(Path.Combine(mainSettingsDir, res.ToString(), Settings_RedirPin_Filename)))
                                    {
                                        // this folder is to be only used for that version
                                        // so we'll upgrade from this folder and then just use this version's standard settings directory
                                        return (newDir, true);
                                    }
                                    else
                                    {
                                        // let's also use the current version's settings in this folder too
                                        // (so, we'll write a new settings redirect file)
                                        string currentnDir = Path.Combine(mainSettingsDir, App.VersionString); // this is the standard settings directory
                                        Directory.CreateDirectory(currentnDir);
                                        File.WriteAllText(Path.Combine(currentnDir, Settings_Redirect_Filename), newDir, UTF8Encoding); // writing redirect file
                                        return (newDir, false);
                                    }
                                }
                            }

                            // okay, no settings redirect file
                            // check if this folder has any files (that could in theory be read from)
                            if (Directory.GetFiles(Path.Combine(mainSettingsDir, res.ToString())).Any())
                            {
                                // if so, then let's return this folder (so that the user can be asked if they want to upgrade)
                                return (Path.Combine(mainSettingsDir, res.ToString()), true);
                            }
                            else
                            {
                                // I could in theory check any other lower folders, but that's a lot of code to write for what will be a fairly unlikely scenario
                                // instead, let's just return null and then cause a new settings file to be created
                                return (null, false);
                            }
                        }
                        else
                        {
                            // if res is -1, then there are no folders that have an integer-only name
                            // just return nothing
                            return (null, false);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // there are no folders that are less than the current version
                        // there may be folders that are lower than the
                        // ... than the what? why didn't I finish writing that comment?
                        return (null, false);
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
                    return (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName), true);
                }
                else
                {
                    // settings is not anywhere, time to start from scratch
                    return (null, false);
                }
            }
        }
        #endregion

        #region Prepare Settings (all in one)

        static void SetupSettingsBase()
        {
            SetupBaseDirectories(); // this does not create the actual settings directory, that is done a few lines later

            (string? settingsDir, bool canUpdate) = LocateLatestSettingsDirectory();
            if (settingsDir == null || (IsPortable && CanBePortable && Directory.Exists(SettingsDirectory)))
            {
                // settingsDir is null if no old settings directories could be found, so we'll just default to the standard directory
                // otherwise, if this is a Portable app and the portable settings app already exists, just use the standard directory
                settingsDir = SettingsDirectory;
            }

            // now the actual settings directory is created (could not be created prior to FindLatestSettings to prevent a false positive)
            Directory.CreateDirectory(SettingsDirectory);

            if (canUpdate)
            {
                // settings stored in old directory, ask user to update
                string pStr = IsPortable ? "(or non-portable) " : "";
                var result = MessageBox.Show($"Settings for a previous {pStr}version of {appName} was located. Do you want to transfer these settings to this version?",
                                 "Old Settings Found", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);

                if (result == MessageBoxResult.Yes)
                {
                    // transfer from old to new
                    foreach (string sff in Directory.GetFiles(settingsDir))
                    {
                        // TODO: add try-catch statements
                        File.Copy(sff, Path.Combine(SettingsDirectory, Path.GetFileName(sff)), true);
                    }
                }
            } // otherwise, settings is in the standard location
        }

        /// <summary>
        /// Set up the settings infrastructure/directories, and then load in settings from the main settings file.
        /// </summary>
        /// <typeparam name="T">The type of the settings object</typeparam>
        /// <param name="settingsFilename">The filename (not full path) of the file that contains the settings.</param>
        /// <remarks>If settings are located in an earlier version's directory and can be upgraded, the user will be asked if they want to upgrade.</remarks>
        /// <returns>Either the settings stored in the file, or a new settings instance.</returns>
        public static T PrepareSettingsJson<T>(string settingsFilename = "settings.json") where T : new()
        {
            SetupSettingsBase();

            return LoadSettingsJson<T>(settingsFilename);
        }

        /// <summary>
        /// Set up the settings infrastructure/directories, and then load in settings from the main settings file.
        /// </summary>
        /// <typeparam name="T">The type of the settings object</typeparam>
        /// <param name="settingsFilename">The filename (not full path) of the file that contains the settings.</param>
        /// <remarks>If settings are located in an earlier version's directory and can be upgraded, the user will be asked if they want to upgrade.</remarks>
        /// <returns>Either the settings stored in the file, or a new settings instance.</returns>
        public static T PrepareSettingsXml<T>(string settingsFilename = "settings.xml") where T : new()
        {
            SetupSettingsBase();

            return LoadSettingsXml<T>(settingsFilename);
        }

        /// <summary>
        /// Set up the settings infrastructure/directories, and then load in settings from the main settings file.
        /// </summary>
        /// <typeparam name="T">The type of the settings object</typeparam>
        /// <param name="serializer">The XML serializer that will be used for loading the settings</param>
        /// <param name="settingsFilename">The filename (not full path) of the file that contains the settings.</param>
        /// <remarks>If settings are located in an earlier version's directory and can be upgraded, the user will be asked if they want to upgrade.</remarks>
        /// <returns>Either the settings stored in the file, or a new settings instance.</returns>
        public static T PrepareSettingsXml<T>(XmlSerializer serializer, string settingsFilename = "settings.xml") where T : new()
        {
            SetupSettingsBase();

            return LoadSettingsXml<T>(serializer, settingsFilename);
        }
        #endregion
    }
}
