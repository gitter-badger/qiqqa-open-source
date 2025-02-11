﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Gecko;
using Gecko.Net;
using Gecko.Observers;
using icons;
using Qiqqa.Common;
using Qiqqa.Common.Configuration;
using Qiqqa.DocumentLibrary;
using Qiqqa.Documents.PDF;
using Qiqqa.Documents.PDF.PDFControls;
using Qiqqa.UtilisationTracking;
using Utilities;
using Utilities.Files;
using Utilities.Misc;

namespace Qiqqa.WebBrowsing.GeckoStuff
{
    class PDFInterceptor : BaseHttpRequestResponseObserver
    {
        public static PDFInterceptor Instance = new PDFInterceptor();

        static bool have_notified_about_installing_acrobat = false;


        private PDFDocument potential_attachment_pdf_document = null;
        
        private PDFInterceptor() : base()
        {
        }

        public PDFDocument PotentialAttachmentPDFDocument
        {
            set
            {
                potential_attachment_pdf_document = value;
            }
        }

        protected override void Response(HttpChannel channel)
        {
            if (channel.ContentType.Contains("pdf"))
            {
                StreamListenerTee stream_listener_tee = new StreamListenerTee();
                stream_listener_tee.Completed += streamListener_Completed;

                TraceableChannel tc = channel.CastToTraceableChannel();
                tc.SetNewListener(stream_listener_tee);
            }
        }

        void streamListener_Completed(object sender, EventArgs e)
        {
            try
            {
                StreamListenerTee stream_listener_tee = (StreamListenerTee)sender;

                byte[] captured_data = stream_listener_tee.GetCapturedData();
                if (0 == captured_data.Length)
                {
                    if (!have_notified_about_installing_acrobat)
                    {
                        have_notified_about_installing_acrobat = true;

                        NotificationManager.Instance.AddPendingNotification(
                            new NotificationManager.Notification(
                                "We notice that your PDF files are not loading in your browser.  Please install Acrobat Reader for Qiqqa to be able to automatically add PDFs to your libraries.",
                                "We notice that your PDF files are not loading in your browser.  Please install Acrobat Reader for Qiqqa to be able to automatically add PDFs to your libraries.",
                                NotificationManager.NotificationType.Info,
                                Icons.DocumentTypePdf,
                                "Download",
                                DownloadAndInstallAcrobatReader
                            ));
                    }

                    Logging.Error("We seem to have been notified about a zero-length PDF");
                    return;
                }

                string temp_pdf_filename = TempFile.GenerateTempFilename("pdf");
                File.WriteAllBytes(temp_pdf_filename, captured_data);

                string pdf_source_url = null; // Can we find this?!!
                PDFDocument pdf_document = Library.GuestInstance.AddNewDocumentToLibrary_SYNCHRONOUS(temp_pdf_filename, pdf_source_url, null, null, null, true, true);
                File.Delete(temp_pdf_filename);

                Application.Current.Dispatcher.Invoke
                (
                    new Action(() =>
                    {
                        PDFReadingControl pdf_reading_control = MainWindowServiceDispatcher.Instance.OpenDocument(pdf_document);
                        pdf_reading_control.EnableGuestMoveNotification(potential_attachment_pdf_document);
                    }),
                    DispatcherPriority.Background
                );
            }

            catch (Exception ex)
            {
                Logging.Error(ex, "There was a problem while intercepting the download of a PDF.");
            }
        }

        private void DownloadAndInstallAcrobatReader(object obj)
        {
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => MainWindowServiceDispatcher.Instance.OpenUrlInBrowser("http://get.adobe.com/reader/", true))
            );
        }
    }
}
