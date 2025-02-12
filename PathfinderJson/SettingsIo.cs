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
using System.Security;
using System.Diagnostics.CodeAnalysis;

namespace PathfinderJson
{
    /// <summary>
    /// A helper class for JaykeBird programs, for organizing app settings and setting up working directories.
    /// </summary>
    public static class SettingsIo
    {
        // note that this class includes some PathfinderJson-specific code and scenarios. Review the code when adapting for use in another program

        private const string appName = "PathfinderJson";
        private const string appComponentName = "pathfinderJson";
        private const int version = 1_03_00_0;

        #region Read-only Properties / Constants

        /// <summary>
        /// Get if this program is running as a portable program.
        /// A portable program isn't installed on a user's computer (and thus can be transferred between devices), and so settings 
        /// should be stored alongside the program's executable.
        /// </summary>
        public static bool IsPortable { get; } = true;

        /// <summary>
        /// Get if this program can be run as a portable version. If <c>false</c>, then <see cref="IsPortable"/> should always be <c>false</c>.
        /// </summary>
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
        /// The base JaykeBird application data directory, which all other directories should be stored within.
        /// </summary>
        /// <remarks>
        /// Data should not be stored in this directory itself, but instead in appropriate subdirectories for this application.
        /// Use the other properties (such as <see cref="AppCacheDirectory"/> and <see cref="SettingsDirectory"/>) for locating the folders
        /// to store data within.
        /// </remarks>
        public static string BaseAppDataDirectory { get; private set; } = "";

        /// <summary>
        /// Get the directory for storing working files and data for the application.
        /// This directory is not managed by Qhuill cache management.
        /// </summary>
        /// <remarks>
        /// This folder should not store user settings, but instead store files and data for the application to perform its current
        /// tasks as a working space. This can also be used for storing long-term data, but data that is meant to be cached and shared
        /// should instead be stored in the <see cref="AppCacheDirectory"/> so that the cache can be managed by Qhuill.
        /// This directory is shared between all versions of the program, and note that multiple versions could be running at the same time.
        /// </remarks>
        public static string AppDataDirectory { get; private set; } = "";

        /// <summary>
        /// Get the directory for storing data that can be referred to by the application over multiple runs/instances.
        /// This directory is managed by Qhuill cache management.
        /// </summary>
        /// <remarks>
        /// This folder should store files and data that can be easily referenced by the program during this instance or in future
        /// instances, especially to avoid future extra work needing to be done.
        /// While this is meant as semi-permanent storage, this folder can be emptied out at any time if the user directs Qhuill
        /// to clear the cache.
        /// This directory is shared between all versions of the program, and note that multiple versions could be running at the same time.
        /// </remarks>
        public static string AppCacheDirectory { get; private set; } = "";

        /// <summary>
        /// Get the directory that error logs should be stored in. Error logs are helpful for explaining the program's status at the time that it crashed.
        /// </summary>
        /// <remarks>
        /// This directory is shared by all JaykeBird programs, so any files added to this directory should also include the name of the program.
        /// </remarks>
        public static string ErrorLogDirectory { get; private set; } = "";

        /// <summary>
        /// Get the name of this program, by the Qhuill component that it is packaged in.
        /// </summary>
        public static string ComponentName { get; } = appComponentName;

        /// <summary>
        /// Get the version number for this particular version of this component.
        /// </summary>
        public static int Version { get; } = version;

        private static Encoding UTF8Encoding = new UTF8Encoding(false);

        private const string Settings_Redirect_Filename = "settings_redir";
        #endregion

        #region Directory Setup

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LocateAppDataFolder()
        {
            string dataFolder = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JaykeBird");
            }
            else
            {
                // check the Home environment variable
                string? homeDir = Environment.GetEnvironmentVariable("HOME");
                if (homeDir != null)
                {
                    dataFolder = Path.Combine(homeDir, ".local", "jaykebird");
                }
                else
                {
                    // if it is null, well... we tried
                    // let's assume a standard location
                    dataFolder = "~/.local/jaykebird";
                }
            }

            BaseAppDataDirectory = dataFolder;
        }

        /// <summary>
        /// This method sets up all of the main directories, and also initializes the <see cref="SettingsDirectory"/> variable. This does NOT create the SettingsDirectory.
        /// </summary>
        public static void SetupBaseDirectories()
        {
            LocateAppDataFolder();

            // creating the AppData folder and its subfolders
            if (!string.IsNullOrEmpty(BaseAppDataDirectory))
            {
                Directory.CreateDirectory(BaseAppDataDirectory);
                AppDataDirectory = Directory.CreateDirectory(Path.Combine(BaseAppDataDirectory, appName)).FullName;
                AppCacheDirectory = Directory.CreateDirectory(Path.Combine(BaseAppDataDirectory, "cache", appName)).FullName;
                ErrorLogDirectory = Directory.CreateDirectory(Path.Combine(BaseAppDataDirectory, "errorLogs")).FullName;

                // PathfinderJson-specific
                Directory.CreateDirectory(Path.Combine(AppCacheDirectory, "Optimization"));
            }

            if (!IsPortable)
            {
                Directory.CreateDirectory(Path.Combine(BaseAppDataDirectory, "settings", appComponentName));
            }

            SettingsDirectory = GetSettingsFolder();
        }

        /// <summary>
        /// Get the path for the folder that settings should be stored in.
        /// </summary>
        public static string GetSettingsFolder()
        {
            string mainSettingsDir = Path.Combine(BaseAppDataDirectory, "Settings", appComponentName);

            if (CanBePortable && IsPortable)
            {
                return Path.Combine(AppContext.BaseDirectory, appComponentName + "-settings");
            }
            else
            {
                string currentDir = Path.Combine(mainSettingsDir, App.VersionString);

                // okay, is there a redirect file?
                if (File.Exists(Path.Combine(currentDir, Settings_Redirect_Filename)))
                {
                    // settings has been redirected to another directory
                    try
                    {
                        string newDir = File.ReadAllText(Path.Combine(currentDir, Settings_Redirect_Filename), UTF8Encoding);
                        if (IsValidPath(newDir))
                        {
                            return newDir;
                        }
                    }
                    catch (IOException) { }
                    catch (ArgumentException) { }
                    catch (UnauthorizedAccessException) { }
                    catch (NotSupportedException) { }
                }

                // current directory is here, let's load in from this directory
                return currentDir;
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
        /// Get if a particular path string is actually a valid directory or file path.
        /// </summary>
        /// <param name="path">the path string to check</param>
        /// <returns><c>true</c> if a valid path, <c>false</c> if this path contains invalid characters or is just whitespace</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidPath([NotNullWhen(true)] string? path)
        {
            return !(string.IsNullOrWhiteSpace(path) || path.Any(c => Path.GetInvalidPathChars().Contains(c)));
        }

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
        /// The folder where the most recent user settings have been found (or <c>null</c> if no folder could be found).
        /// Also returns if there is an upgrade path available that the user should be asked about.
        /// </returns>
        public static (string? dirPath, bool canRequestUpgrade) LocateLatestSettings()
        {
            // first, some setup
            string mainSettingsDir = Path.Combine(BaseAppDataDirectory, "Settings", appComponentName); // this is the directory settings should be stored in

            // if we're using the portable version, then we'll pull the portable directory first
            if (IsPortable && Directory.Exists(Path.Combine(AppContext.BaseDirectory, appComponentName + "-settings")))
            {
                // we found the portable settings directory
                // no need to do an upgrade
                return (Path.Combine(AppContext.BaseDirectory, appComponentName + "-settings"), false);
            }
            // otherwise, let's look for the main settings directory
            else if (Directory.Exists(mainSettingsDir) && Directory.EnumerateDirectories(mainSettingsDir).Any()) // check if main settings directory exists (and make sure it has subdirectories)
            {
                // okay, cool... does the settings directory for this version exist?
                string currentDir = Path.Combine(mainSettingsDir, App.VersionString);
                if (Directory.Exists(currentDir))
                {
                    // okay, it does... is there a redirect file?
                    if (File.Exists(Path.Combine(currentDir, Settings_Redirect_Filename)))
                    {
                        // settings has been redirected to another directory
                        try
                        {
                            string newDir = File.ReadAllText(Path.Combine(currentDir, Settings_Redirect_Filename), UTF8Encoding);
                            if (Directory.Exists(newDir))
                            {
                                return (newDir, false);
                            }
                        }
                        catch (IOException) { }
                        catch (ArgumentException) { }
                        catch (UnauthorizedAccessException) { }
                        catch (NotSupportedException) { }
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
                                    // so we'll return that redirected directory
                                    return (newDir, true);
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
                                // I could in theory check for any other folders, but that's a lot of code to write for what will be a fairly unlikely scenario
                                // instead, let's just return null and go from there
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
                        // there may be folders that are greater than the current version, but I'm not going to look for those
                        // let's just return null and go from there
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
            SetupBaseDirectories(); // this does not create the actual settings directory, but does set where it should be

            if (Directory.Exists(SettingsDirectory))
            {
                // if the directory we need already exists, no point in asking about it
                return;
            }

            (string? oldDir, bool canUpdate) = LocateLatestSettings();

            // now the actual settings directory is created (could not be created prior to LocateLatestSettings to prevent a false positive)
            Directory.CreateDirectory(SettingsDirectory);

            if (canUpdate && oldDir != null)
            {
                // settings stored in old directory, ask user to update
                string pStr = IsPortable ? "(or non-portable) " : "";
                var result = MessageBox.Show($"Settings for a previous {pStr}version of {appName} was located. Do you want to transfer these settings to this version?",
                                 "Old Settings Found", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);

                if (result == MessageBoxResult.Yes)
                {
                    // transfer from old to new
                    foreach (string sff in Directory.GetFiles(oldDir))
                    {
                        try
                        {
                            File.Copy(sff, Path.Combine(SettingsDirectory, Path.GetFileName(sff)), true);
                        }
                        catch (IOException) { }
                        catch (SecurityException) { }
                        catch (UnauthorizedAccessException) { }
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