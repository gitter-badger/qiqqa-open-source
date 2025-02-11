﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using Qiqqa.Common.Configuration;
using Qiqqa.DocumentLibrary.BundleLibrary;
using Qiqqa.DocumentLibrary.IntranetLibraryStuff;
using Utilities;
using Utilities.Files;
using Utilities.GUI;
using Utilities.Misc;

namespace Qiqqa.DocumentLibrary.WebLibraryStuff
{
    class WebLibraryManager
    {
        public static WebLibraryManager Instance = new WebLibraryManager();

        private Dictionary<string, WebLibraryDetail> web_library_details = new Dictionary<string, WebLibraryDetail>();
        private WebLibraryDetail guest_web_library_detail;

        public delegate void WebLibrariesChangedDelegate();
        public event WebLibrariesChangedDelegate WebLibrariesChanged;

        private WebLibraryManager()
        {   
            // Look for any web libraries that we know about
            LoadKnownWebLibraries();

            AddLocalGuestLibraryIfMissing();

            // *************************************************************************************************************
            // *** MIGRATION TO OPEN SOURCE CODE ***************************************************************************
            // *************************************************************************************************************
            AddLegacyWebLibrariesThatCanBeFoundOnDisk();
            // *************************************************************************************************************

            FireWebLibrariesChanged();
        }

        private void FireWebLibrariesChanged()
        {
            Logging.Info("+Notifying everyone that web libraries have changed");
            if (null != WebLibrariesChanged)
            {
                WebLibrariesChanged();
            }
            Logging.Info("-Notifying everyone that web libraries have changed");
        }


        // *************************************************************************************************************
        // *** MIGRATION TO OPEN SOURCE CODE ***************************************************************************
        // *************************************************************************************************************
        private void AddLegacyWebLibrariesThatCanBeFoundOnDisk()
        {
            /**
             * Plan:
             * Iterate through all the folders in the Qiqqa data directory
             * If a folder contains a valid Library record and it is a WEB library, then add it to our list with the word '[LEGACY]' in front of it
             */

            string base_directory_path = UpgradePaths.V037To038.SQLiteUpgrade.BaseDirectoryForQiqqa;
            Logging.Info("Going to scan for web libraries at: " + base_directory_path);
            if (Directory.Exists(base_directory_path))
            {
                string[] library_directories = Directory.GetDirectories(base_directory_path);
                foreach (string library_directory in library_directories)
                {
                    Logging.Info("Inspecting directory {0}", library_directory);

                    string database_file = library_directory + @"\Qiqqa.library";
                    if (File.Exists(database_file))
                    {
                        var library_id = Path.GetFileName(library_directory);
                        if (web_library_details.ContainsKey(library_id))
                        {
                            Logging.Info("We already know about this library, so skipping legacy locate: " + library_id);
                            continue;
                        }

                        WebLibraryDetail new_web_library_detail = new WebLibraryDetail();

                        new_web_library_detail.Id = library_id;
                        new_web_library_detail.Title = "Legacy Web Library - " + new_web_library_detail.Id.Substring(0, 8);
                        new_web_library_detail.IsReadOnly = true;
                        new_web_library_detail.library = new Library(new_web_library_detail);
                        web_library_details[new_web_library_detail.Id] = new_web_library_detail;
                    }
                }
            }
                


                



        }
        // *************************************************************************************************************

        private void AddLocalGuestLibraryIfMissing()
        {
            // Check if we have an existing Guest library
            foreach (var pair in web_library_details)
            {
                if (pair.Value.IsLocalGuestLibrary)
                {
                    guest_web_library_detail = pair.Value;
                    break;
                }
            }

            // If we did not have a guest library, create one...
            if (null == guest_web_library_detail)
            {
                WebLibraryDetail new_web_library_detail = new WebLibraryDetail();
                new_web_library_detail.Id = "Guest";
                new_web_library_detail.Title = "Local Guest Library";
                new_web_library_detail.Description = "This is the library that comes with your Qiqqa guest account.";
                new_web_library_detail.Deleted = false;
                new_web_library_detail.IsLocalGuestLibrary = true;
                new_web_library_detail.library = new Library(new_web_library_detail);
                web_library_details[new_web_library_detail.Id] = new_web_library_detail;

                // Store this reference to guest
                guest_web_library_detail = new_web_library_detail;
            }

            // Import the Qiqqa manuals...
            QiqqaManualTools.AddManualsToLibrary(guest_web_library_detail.library);
        }

        public WebLibraryDetail WebLibraryDetails_Guest
        {
            get
            {
                return guest_web_library_detail;
            }
        }

        public Library Library_Guest
        {
            get
            {
                return guest_web_library_detail.library;
            }
        }

        public bool HaveOnlyLocalGuestLibrary()
        {
            bool have_only_local_guest_library = true;
            foreach (WebLibraryDetail wld in WebLibraryDetails_All_IncludingDeleted)
            {
                if (!wld.IsLocalGuestLibrary) have_only_local_guest_library = false;
            }
            return have_only_local_guest_library;
        }

        public bool HaveOnlyOneWebLibrary()
        {
            return 1 == WebLibraryDetails_WorkingWebLibraries.Count;
        }


        /// <summary>
        /// Returns all working web libraries.  If the user has a web library, guest and deleted libraries are not in this list.
        /// If they have only a guest library, then this list is empty...
        /// </summary>
        public List<WebLibraryDetail> WebLibraryDetails_WorkingWebLibrariesWithoutGuest
        {
            get
            {
                List<WebLibraryDetail> details = new List<WebLibraryDetail>();
                foreach (WebLibraryDetail wld in WebLibraryDetails_All_IncludingDeleted)
                {
                    if (!wld.Deleted && !wld.IsLocalGuestLibrary)
                    {
                        details.Add(wld);
                    }
                }

                return details;
            }
        }
        
        /// <summary>
        /// Returns all working web libraries.  If the user has a web library, guest and deleted libraries are not in this list.
        /// If they have only a guest library, then it is in this list...
        /// </summary>
        public List<WebLibraryDetail> WebLibraryDetails_WorkingWebLibraries
        {
            get
            {
                List<WebLibraryDetail> details = WebLibraryDetails_WorkingWebLibrariesWithoutGuest;

                // If they don't have any real libraries, throw in the guest library
                if (0 == details.Count)
                {
                    details.Add(WebLibraryDetails_Guest);
                }

                return details;
            }
        }

        /// <summary>
        /// Returns all working web libraries, including the guest library.
        /// </summary>
        public List<WebLibraryDetail> WebLibraryDetails_WorkingWebLibraries_All
        {
            get
            {
                List<WebLibraryDetail> details = new List<WebLibraryDetail>();
                foreach (WebLibraryDetail wld in WebLibraryDetails_All_IncludingDeleted)
                {
                    if (!wld.Deleted && !wld.IsLocalGuestLibrary)
                    {
                        details.Add(wld);
                    }
                }

                // Always add the guest library
                details.Add(WebLibraryDetails_Guest);

                return details;
            }
        }

        public List<WebLibraryDetail> WebLibraryDetails_All_IncludingDeleted
        {
            get
            {
                List<WebLibraryDetail> details = new List<WebLibraryDetail>();
                details.AddRange(web_library_details.Values);
                return details;
            }
        }

        public Library GetLibrary(string library_id)
        {
            WebLibraryDetail web_library_detail;
            if (web_library_details.TryGetValue(library_id, out web_library_detail))
            {
                return GetLibrary(web_library_detail);
            }
            else
            {
                return null;
            }
        }

        public Library GetLibrary(WebLibraryDetail web_library_detail)
        {
            return web_library_detail.library;
        }

        public void NotifyOfChangeToWebLibraryDetail()
        {
            SaveKnownWebLibraries();
        }

        public void SortWebLibraryDetailsByLastAccessed(List<WebLibraryDetail> web_library_details)
        {
            string last_open_ordering = ConfigurationManager.Instance.ConfigurationRecord.GUI_LastSelectedLibraryId;

            // Is there nothing to do?
            if (String.IsNullOrEmpty(last_open_ordering))
            {
                return;
            }

            web_library_details.Sort(
                delegate(WebLibraryDetail a, WebLibraryDetail b)
                {
                    if (a == b) return 0;

                    if (b.Deleted) return -1;
                    if (a.Deleted) return +1;

                    if (b.IsLocalGuestLibrary) return -1;
                    if (a.IsLocalGuestLibrary) return +1;

                    int pos_b = last_open_ordering.IndexOf(b.Id);
                    if (-1 == pos_b) return -1;
                    int pos_a = last_open_ordering.IndexOf(a.Id);
                    if (-1 == pos_a) return +1;

                    return Sorting.Compare(pos_a, pos_b);
                }
            );
        }

        #region --- Known web library management -------------------------------------------------------------------------------------------------------------------------

        public static string KNOWN_WEB_LIBRARIES_FILENAME
        {
            get
            {
                return ConfigurationManager.Instance.BaseDirectoryForUser + "\\" + "Qiqqa.known_web_libraries";
            }
        }

        void LoadKnownWebLibraries()
        {
            Logging.Info("+Loading known Web Libraries");
            try
            {
                if (File.Exists(KNOWN_WEB_LIBRARIES_FILENAME))
                {
                    KnownWebLibrariesFile known_web_libraries_file = SerializeFile.ProtoLoad<KnownWebLibrariesFile>(KNOWN_WEB_LIBRARIES_FILENAME);
                    if (null != known_web_libraries_file.web_library_details)
                    {
                        foreach (WebLibraryDetail new_web_library_detail in known_web_libraries_file.web_library_details)
                        {
                            Logging.Info("We have known details for library '{0}' ({1})", new_web_library_detail.Title, new_web_library_detail.Id);

                            if (!new_web_library_detail.IsPurged)
                            {
                                // Intranet libraries have their readonly flag set on the user's current premium status...
                                if (new_web_library_detail.IsIntranetLibrary)
                                {
                                    new_web_library_detail.IsReadOnly = false;
                                }

                                new_web_library_detail.library = new Library(new_web_library_detail);
                                web_library_details[new_web_library_detail.Id] = new_web_library_detail;
                            }
                            else
                            {
                                Logging.Info("Not loading purged library {0} with id {1}", new_web_library_detail.Title, new_web_library_detail.Id);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error(ex, "There was a problem loading the known Web Libraries");
            }
            Logging.Info("-Loading known Web Libraries");
        }

        void SaveKnownWebLibraries()
        {
            Logging.Info("+Saving known Web Libraries");

            try
            {
                KnownWebLibrariesFile known_web_libraries_file = new KnownWebLibrariesFile();
                known_web_libraries_file.web_library_details = new List<WebLibraryDetail>();
                foreach (WebLibraryDetail web_library_detail in web_library_details.Values)
                {
                    // *************************************************************************************************************
                    // *** MIGRATION TO OPEN SOURCE CODE ***************************************************************************
                    // *************************************************************************************************************
                    // Don't remember the web libraries - let them be discovered by this
                    if ("UNKNOWN" == web_library_detail.LibraryType())
                    {
                        continue;
                    }
                    // *************************************************************************************************************

                    known_web_libraries_file.web_library_details.Add(web_library_detail);
                }
                SerializeFile.ProtoSave<KnownWebLibrariesFile>(KNOWN_WEB_LIBRARIES_FILENAME, known_web_libraries_file);
            }
            catch (Exception ex)
            {
                Logging.Error(ex, "There was a problem saving the known web libraries");
            }

            Logging.Info("-Saving known Web Libraries");
        }

        private void UpdateKnownWebLibrary(WebLibraryDetail new_web_library_detail)
        {
            new_web_library_detail.library = new Library(new_web_library_detail);
            web_library_details[new_web_library_detail.Id] = new_web_library_detail;

            SaveKnownWebLibraries();
            FireWebLibrariesChanged();
        }

        public void UpdateKnownWebLibraryFromIntranet(string intranet_path)
        {
            Logging.Info("+Updating known Intranet Library from " + intranet_path);

            IntranetLibraryDetail intranet_library_detail = IntranetLibraryDetail.Read(IntranetLibraryTools.GetLibraryDetailPath(intranet_path));

            WebLibraryDetail new_web_library_detail = new WebLibraryDetail();
            new_web_library_detail.IntranetPath = intranet_path;
            new_web_library_detail.Id = intranet_library_detail.Id;
            new_web_library_detail.Title = intranet_library_detail.Title;
            new_web_library_detail.Description = intranet_library_detail.Description;
            new_web_library_detail.IsReadOnly = false;
            new_web_library_detail.Deleted = false;

            UpdateKnownWebLibrary(new_web_library_detail);

            Logging.Info("-Updating known Intranet Library from " + intranet_path);
        }

        public WebLibraryDetail UpdateKnownWebLibraryFromBundleLibraryManifest(BundleLibraryManifest manifest)
        {
            Logging.Info("+Updating known Bundle Library {0} ({1})", manifest.Title, manifest.Id);

            WebLibraryDetail new_web_library_detail = new WebLibraryDetail();
            new_web_library_detail.BundleManifestJSON = manifest.ToJSON();
            new_web_library_detail.Id = manifest.Id;
            new_web_library_detail.Title = manifest.Title;
            new_web_library_detail.Description = manifest.Description;
            new_web_library_detail.IsReadOnly = true;
            new_web_library_detail.Deleted = false;

            UpdateKnownWebLibrary(new_web_library_detail);

            Logging.Info("-Updating known Bundle Library {0} ({1})", manifest.Title, manifest.Id);

            return new_web_library_detail;
        }

        internal void ForgetKnownWebLibraryFromIntranet(WebLibraryDetail web_library_detail)
        {
            Logging.Info("+Forgetting known Intranet Library from " + web_library_detail.Title);

            if (MessageBoxes.AskQuestion("Are you sure you want to forget the Intranet Library '{0}'?", web_library_detail.Title))
            {
                web_library_details.Remove(web_library_detail.Id);
                SaveKnownWebLibraries();
                FireWebLibrariesChanged();
            }

            Logging.Info("-Forgetting known Intranet Library from " + web_library_detail.Title);
        }

        #endregion
    }
}
