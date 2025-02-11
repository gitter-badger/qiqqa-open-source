﻿using System;
using System.Threading;
using icons;
using Qiqqa.ClientVersioning;
using Qiqqa.Common.Configuration;
using Qiqqa.DocumentLibrary;
using Qiqqa.DocumentLibrary.AutoSyncStuff;
using Qiqqa.DocumentLibrary.BundleLibrary.BundleLibraryDownloading;
using Qiqqa.DocumentLibrary.Import.Auto;
using Qiqqa.DocumentLibrary.MetadataExtractionDaemonStuff;
using Qiqqa.DocumentLibrary.WebLibraryStuff;
using Qiqqa.Main;
using Qiqqa.Marketing;
using Qiqqa.Synchronisation.PDFSync;
using Utilities;
using Utilities.ClientVersioning;
using Utilities.Maintainable;

namespace Qiqqa.Common.BackgroundWorkerDaemonStuff
{
    public class BackgroundWorkerDaemon
    {
        public static readonly BackgroundWorkerDaemon Instance = new BackgroundWorkerDaemon();

        MetadataExtractionDaemon metadata_extraction_daemon;

        BackgroundWorkerDaemon()
        {
            Logging.Info("Starting background worker daemon.");

            metadata_extraction_daemon = new MetadataExtractionDaemon();

            MaintainableManager.Instance.Register(DoMaintenance_OnceOff, 10 * 1000, ThreadPriority.BelowNormal);
            MaintainableManager.Instance.Register(DoMaintenance_Frequent, 10 * 1000, ThreadPriority.BelowNormal);
            MaintainableManager.Instance.Register(DoMaintenance_Infrequent, 10 * 1000, ThreadPriority.BelowNormal);
            MaintainableManager.Instance.Register(DoMaintenance_QuiteInfrequent, 10 * 1000, ThreadPriority.BelowNormal);
            MaintainableManager.Instance.Register(DoMaintenance_VeryInfrequent, 10 * 1000, ThreadPriority.BelowNormal);
        }

        void DoMaintenance_OnceOff(Daemon daemon)
        {
            if (daemon.StillRunning)
            {
                // KICK THEM OFF

                try
                {
                    StartupCommandLineParameterChecker.Check();
                }
                catch (Exception ex) { Logging.Error(ex, "Exception during StartupCommandLineParameterChecker.Check"); }

                try
                {
                    ClientUpdater.Init("Qiqqa",
                                       Icons.Upgrade,
                                       WebsiteAccess.GetOurFileUrl(WebsiteAccess.OurSiteFileKind.ClientVersion),
                                       WebsiteAccess.GetOurFileUrl(WebsiteAccess.OurSiteFileKind.ClientSetup),
                                       WebsiteAccess.IsTestEnvironment,
                                       ShowReleaseNotes);

                    ClientUpdater.Instance.CheckForNewClientVersion(ConfigurationManager.Instance.Proxy);
                }
                catch (Exception ex) { Logging.Error(ex, "Exception during Utilities.ClientVersioning.ClientUpdater.Instance.CheckForNewClientVersion"); }


                try
                {
                    AlternativeToReminderNotification.CheckIfWeWantToNotify();
                }
                catch (Exception ex) { Logging.Error(ex, "Exception during AlternativeToReminderNotification.CheckIfWeWantToNotify"); }

                try
                {
                    DropboxChecker.DoCheck();
                }
                catch (Exception ex) { Logging.Error(ex, "Exception during DropboxChecker.DoCheck"); }

                try
                {
                    AutoImportFromMendeleyChecker.DoCheck();
                }
                catch (Exception ex) { Logging.Error(ex, "Exception during AutoImportFromMendeleyChecker.DoCheck"); }

                try
                {
                    AutoImportFromEndnoteChecker.DoCheck();
                }
                catch (Exception ex) { Logging.Error(ex, "Exception during AutoImportFromEndnoteChecker.DoCheck"); }
            }

            // We only want this to run once
            daemon.Stop();
        }

        private void ShowReleaseNotes(string release_notes)
        {
            new ClientVersionReleaseNotes(release_notes).ShowDialog();
        }

        void DoMaintenance_VeryInfrequent(Daemon daemon)
        {
            daemon.Sleep(15 * 60 * 1000);
            //daemon.Sleep(5 * 1000);

            if (RegistrySettings.Instance.IsSet(RegistrySettings.SuppressDaemon))
            {
                Logging.Info("Daemon is forced to sleep via registry SuppressDaemon");
                daemon.Sleep(10 * 1000);
                return;
            }

            try
            {
                AutoSyncManager.Instance.DoMaintenance();
            }
            catch (Exception ex)
            {
                Logging.Error(ex, "Exception in autosync_manager_daemon");
            }

            foreach (var x in WebLibraryManager.Instance.WebLibraryDetails_WorkingWebLibraries_All)
            {
                Library library = x.library;

                // If this library is busy, skip it for now
                if (Library.IsBusyAddingPDFs)
                {
                    Logging.Info("Not daemon processing a library that is busy with adds...");
                    continue;
                }

                try
                {
                    if (library.WebLibraryDetail.IsBundleLibrary)
                    {
                        BundleLibraryUpdatedManifestChecker.Check(library);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Warn(ex, "Exception in BundleLibraryUpdatedManifestChecker.Check()");
                }
            }
        }


        void DoMaintenance_QuiteInfrequent(Daemon daemon)
        {
            daemon.Sleep(1 * 60 * 1000);

            if (RegistrySettings.Instance.IsSet(RegistrySettings.SuppressDaemon))
            {
                Logging.Info("Daemon is forced to sleep via registry SuppressDaemon");
                daemon.Sleep(10 * 1000);
                return;
            }
        }

        void DoMaintenance_Infrequent(Daemon daemon)
        {
            daemon.Sleep(10 * 1000);

            if (RegistrySettings.Instance.IsSet(RegistrySettings.SuppressDaemon))
            {
                Logging.Info("Daemon is forced to sleep via registry SuppressDaemon");
                daemon.Sleep(10 * 1000);
                return;
            }

            foreach (var x in WebLibraryManager.Instance.WebLibraryDetails_WorkingWebLibraries_All)
            {
                Library library = x.library;

                // If this library is busy, skip it for now
                if (Library.IsBusyAddingPDFs)
                {
                    Logging.Info("Not daemon processing a library that is busy with adds...");
                    continue;
                }

                try
                {
                    metadata_extraction_daemon.DoMaintenance(library, daemon);
                }
                catch (Exception ex)
                {
                    Logging.Error(ex, "Exception in metadata_extraction_daemon");
                }

                try
                {
                    library.LibraryIndex.IncrementalBuildIndex();
                }
                catch (Exception ex)
                {
                    Logging.Error(ex, "Exception in LibraryIndex.IncrementalBuildIndex()");
                }
            }
        }

        void DoMaintenance_Frequent(Daemon daemon)
        {
            daemon.Sleep(1 * 1000);

            if (RegistrySettings.Instance.IsSet(RegistrySettings.SuppressDaemon))
            {
                Logging.Info("Daemon is forced to sleep via registry SuppressDaemon");
                daemon.Sleep(10 * 1000);
                return;
            }

            // Check for new syncing
            foreach (var x in WebLibraryManager.Instance.WebLibraryDetails_All_IncludingDeleted)
            {
                Library library = x.library;

                // If this library is busy, skip it for now
                if (Library.IsBusyAddingPDFs)
                {
                    Logging.Info("Not daemon processing a library that is busy with adds...");
                    continue;
                }

                try
                {
                    SyncQueues.Instance.DoMaintenance(daemon);
                }
                catch (Exception ex)
                {
                    Logging.Error(ex, "Exception in SyncQueues.Instance.DoMaintenance");
                }
            }

            // Check if documents have changed
            foreach (var x in WebLibraryManager.Instance.WebLibraryDetails_All_IncludingDeleted)
            {
                Library library = x.library;
                try
                {
                    library.CheckForSignalThatDocumentsHaveChanged();
                }
                catch (Exception ex)
                {
                    Logging.Error(ex, "Exception in Library.CheckForSignalThatDocumentsHaveChanged");
                }
            }
        }
    }
}
