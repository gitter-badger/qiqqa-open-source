﻿using System;
using System.Windows;
using icons;
using Qiqqa.Common.Configuration;
using Qiqqa.DocumentLibrary.WebLibraryStuff;
using Utilities;
using Utilities.Misc;
using Qiqqa.UtilisationTracking;

namespace Qiqqa.DocumentLibrary.Import.Auto
{
    class AutoImportFromEndnoteChecker
    {
        private static EndnoteImporter.EndnoteDatabaseDetails mdd = null;

        internal static void DoCheck()
        {
            // If the user has indicated that they don't want this, obey them...
            if (ConfigurationManager.Instance.ConfigurationRecord.ImportFromEndnoteAutoDisabled)
            {
                return;
            }

            // Count the number of files in all our libraries
            int total_pdfs = 0;
            foreach (WebLibraryDetail web_library_detail in WebLibraryManager.Instance.WebLibraryDetails_WorkingWebLibraries_All)
            {
                total_pdfs += web_library_detail.library.PDFDocuments_IncludingDeleted_Count;
            }

            // Count the number of found Endnote papers
            mdd = EndnoteImporter.DetectEndnoteDatabaseDetails();

            // If there are way more Endnote papers than our own, scream
            if (mdd.documents_found > 2 * total_pdfs)
            {
                string notification =
                    mdd.PotentialImportMessage
                    + "  Do you want to import them now?"
                    ;

                // Report it to user
                NotificationManager.Instance.AddPendingNotification(
                    new NotificationManager.Notification(
                        notification
                        ,notification
                        ,NotificationManager.NotificationType.Info
                        , Icons.Import_EndNote
                        ,"Yes, Import!"
                        ,DoImportMyDocuments
                        ,"Don't Ask Again"
                        ,DoNoThanks
                    )
                );
            }
        }

        static void DoImportMyDocuments(object obj)
        {
            if (null == mdd)
            {
                Logging.Warn("Not sure how EndnoteImporter.EndnoteDatabaseDetails is null if we got a command to import...");
                return;
            }

            Qiqqa.UtilisationTracking.FeatureTrackingManager.Instance.UseFeature(Features.Library_ImportAutoFromEndNote);

            WebLibraryDetail web_library_detail = null;
            Application.Current.Dispatcher.Invoke(((Action)(() =>
                            web_library_detail = WebLibraryPicker.PickWebLibrary()
                        )));

            if (null != web_library_detail)
            {
                ImportingIntoLibrary.AddNewPDFDocumentsToLibraryWithMetadata_ASYNCHRONOUS(web_library_detail.library, false, false, mdd.metadata_imports.ToArray());
            }
        }

        static void DoNoThanks(object obj)
        {
            ConfigurationManager.Instance.ConfigurationRecord.ImportFromEndnoteAutoDisabled = true;
            ConfigurationManager.Instance.ConfigurationRecord_Bindable.NotifyPropertyChanged(() => ConfigurationManager.Instance.ConfigurationRecord.ImportFromEndnoteAutoDisabled);
        }
    }
}
