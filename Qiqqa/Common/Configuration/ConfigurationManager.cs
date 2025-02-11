﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using Qiqqa.WebBrowsing.GeckoStuff;
using Utilities;
using Utilities.Files;
using Utilities.Internet;
using Utilities.Reflection;
using Utilities.Shutdownable;
using Application = System.Windows.Forms.Application;
using Utilities.Strings;

namespace Qiqqa.Common.Configuration
{
    [Obfuscation(Feature = "properties renaming")]
    public class ConfigurationManager
    {
        public static readonly ConfigurationManager Instance = new ConfigurationManager();

        string user_guid;
        bool is_guest;


        public string TempDirectoryForQiqqa
        {
            get
            {
                return TempFile.TempDirectory + @"Qiqqa\";
            }
        }

        public string StartupDirectoryForQiqqa
        {
            get
            {
                return Application.StartupPath + @"\";
            }
        }
        
        private string base_directory_for_qiqqa = null;
        public string BaseDirectoryForQiqqa
        {
            get
            {
                if (null == base_directory_for_qiqqa)
                {
                    string override_path = RegistrySettings.Instance.Read(RegistrySettings.BaseDataDirectory);
                    if (!String.IsNullOrEmpty(override_path))
                    {
                        override_path = override_path.Trim();
                        if (!String.IsNullOrEmpty(override_path))
                        {
                            // Make sure it ends with a \
                            if (!override_path.EndsWith(@"\"))
                            {
                                override_path = override_path + @"\";
                            }

                            base_directory_for_qiqqa = override_path;

                            // Check that the path is reasonable
                            try
                            {
                                Directory.CreateDirectory(base_directory_for_qiqqa);
                            }
                            catch (Exception ex)
                            {
                                Logging.Error(ex, "There was a problem creating the user-overridden base directory, so reverting to default");
                                base_directory_for_qiqqa = null;
                            }
                        }
                    }

                    // If we get here, use the default path
                    if (null == base_directory_for_qiqqa)
                    {
                        base_directory_for_qiqqa = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Quantisle\Qiqqa\";
                    }
                }

                return base_directory_for_qiqqa;
            }
        }

        public string BaseDirectoryForUser
        {
            get
            {
                return BaseDirectoryForQiqqa + @"\" + user_guid + @"\";
            }
        }

        string ConfigFilenameForUser
        {
            get
            {
                return BaseDirectoryForUser + @"Qiqqa.configuration";
            }
        }

        string SearchHistoryFilename
        {
            get
            {
                return BaseDirectoryForUser + @"Qiqqa.search_history";
            }
        }

        public string Program7ZIP
        {
            get
            {
                return StartupDirectoryForQiqqa + @"\7za.exe";
            }
        }

        public string ProgramHTMLToPDF
        {
            get
            {
                return StartupDirectoryForQiqqa + @"\wkhtmltopdf.exe";
            }
        }

        ConfigurationRecord configuration_record;
        AugmentedBindable<ConfigurationRecord> configuration_record_bindable;

        private ConfigurationManager()
        {
            ShutdownableManager.Instance.Register(Shutdown);
        }

        void Shutdown()
        {
            Logging.Info("ConfigurationManager is saving the configuration at shutdown");
            SaveConfigurationRecord();
            SaveSearchHistory();
        }

        private void ResetConfigurationRecord(string user_guid, bool is_guest)
        {
            Logging.Info("Resetting configuration settings to {0}", user_guid);

            if (null != configuration_record_bindable)
            {
                configuration_record_bindable.PropertyChanged -= configuration_record_bindable_PropertyChanged;
            }

            // Create the new user_guid
            this.user_guid = user_guid;
            this.is_guest = is_guest;

            // Create the base directory in case it doesnt exist
            Directory.CreateDirectory(BaseDirectoryForUser);

            // Try loading any pre-existing config file
            try
            {
                if (File.Exists(ConfigFilenameForUser))
                {
                    Logging.Info("Loading configuration");
                    configuration_record = (ConfigurationRecord)ObjectSerializer.LoadObject(ConfigFilenameForUser);
                    Logging.Info("Loaded configuration");
                }
            }
            catch (Exception ex)
            {
                Logging.Error(ex, "There was a problem loading configuration.");
            }

            if (null == configuration_record)
            {
                Logging.Info("There is no existing configuration, so creating one from scratch.");
                configuration_record = new ConfigurationRecord();
            }

            // Attach a bindable to the configuration record so that GUIs can update it
            configuration_record_bindable = new AugmentedBindable<ConfigurationRecord>(configuration_record);
            configuration_record_bindable.PropertyChanged += configuration_record_bindable_PropertyChanged;

            // Make sure we have a GA tracking ID
            if (String.IsNullOrEmpty(configuration_record.Feedback_GATrackingCode))
            {
                configuration_record.Feedback_GATrackingCode = Guid.NewGuid().ToString();
            }
        }

        void configuration_record_bindable_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender == configuration_record_bindable)
            {
                SaveConfigurationRecord();
                
                GeckoManager.SetupProxyAndUserAgent(false);
            }
            else
            {
                Logging.Warn("Not saving configuration record from old bindable wrapper");
            }
        }

        public void SaveConfigurationRecord()
        {
            try
            {
                Logging.Info("Saving configuration");
                ObjectSerializer.SaveObject(ConfigFilenameForUser, configuration_record);
                Logging.Info("Saved configuration");
            }
            catch (Exception ex)
            {
                Logging.Error(ex, "There was a problem saving the configuration.");
            }
        }

        #region --- Search history ----------------------------------------------------------------------------------------

        HashSet<string> search_history = new HashSet<string>();
        HashSet<string> search_history_from_disk = null;
        public HashSet<string> SearchHistory
        {
            get
            {
                // Add any user specified searches
                var user_specified_searches = ConfigurationManager.Instance.ConfigurationRecord.GUI_UserDefinedSearchStrings;
                if (!String.IsNullOrEmpty(user_specified_searches))
                {
                    foreach (string search in StringTools.splitAtNewline(user_specified_searches))
                    {
                        search_history.Add(search);
                    }
                }

                if (null == search_history_from_disk)
                {
                    search_history_from_disk = new HashSet<string>();
                    // Try to load it from disk (if we are premium or premium+)
                    try
                    {
                        if (File.Exists(SearchHistoryFilename))
                        {
                            foreach (string search in File.ReadAllLines(SearchHistoryFilename))
                            {
                                search_history.Add(search);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Error(ex, "There was a problem loading the search history.");
                    }
                }
                foreach (string search in search_history_from_disk)
                {
                    search_history.Add(search);
                    if (search_history.Count > 100) break;
                }

                return search_history;
            }
        }

        void SaveSearchHistory()
        {
            try
            {
                File.WriteAllLines(SearchHistoryFilename, SearchHistory);
            }
            catch (Exception ex)
            {
                Logging.Error(ex, "There was a problem saving the search history.");
            }
        }

        #endregion

        #region --- Public initialisation ----------------------------------------------------------------------------------------

        public void ResetConfigurationRecordToGuest()
        {            
            ResetConfigurationRecord("Guest", true);
        }

        public void ResetConfigurationRecordToUser(string user_guid)
        {
            ResetConfigurationRecord(user_guid, false);
        }

        #endregion

        #region --- Public accessors ----------------------------------------------------------------------------------------

        public bool IsGuest
        {
            get
            {
                return is_guest;
            }
        }

        public Visibility NoviceVisibility
        {
            get
            {
                return this.ConfigurationRecord.GUI_IsNovice ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        
        public ConfigurationRecord ConfigurationRecord
        {
            get
            {
                return configuration_record;
            }
        }

        public AugmentedBindable<ConfigurationRecord> ConfigurationRecord_Bindable
        {
            get
            {
                return configuration_record_bindable;
            }
        }

        /// <summary>
        /// Uses the proxy settings specified by the user.  Otherwise defaults to the system proxy.
        /// </summary>
        public IWebProxy Proxy
        {
            get
            {
                string hostname = ConfigurationRecord.Proxy_Hostname;
                if (!String.IsNullOrEmpty(hostname))
                {
                    hostname = hostname.Trim();
                    hostname = hostname.ToLower();
                    hostname = hostname.Replace("http://", "");
                    hostname = hostname.Replace("https://", "");
                }

                string username = ConfigurationRecord.Proxy_Username;
                if (!String.IsNullOrEmpty(username)) username = username.Trim();

                string password = ConfigurationRecord.Proxy_Password;
                if (!String.IsNullOrEmpty(password)) password = password.Trim();

                return ProxyTools.CreateProxy(ConfigurationRecord.Proxy_UseProxy, hostname, ConfigurationRecord.Proxy_Port, username, password);
            }
        }
        
        #endregion
    }
}
