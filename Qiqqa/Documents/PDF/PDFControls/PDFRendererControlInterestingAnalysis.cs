﻿using System;
using System.Collections.Generic;
using System.Threading;
using Qiqqa.Common.Configuration;
using Qiqqa.DocumentLibrary.SimilarAuthorsStuff;
using Qiqqa.Documents.PDF.InfoBarStuff.PDFDocumentTagCloudStuff;
using Utilities;
using Utilities.Collections;
using Utilities.Internet.GoogleScholar;
using Utilities.Language;
using Utilities.Misc;

namespace Qiqqa.Documents.PDF.PDFControls
{
    class PDFRendererControlInterestingAnalysis
    {
        public static void DoInterestingAnalysis(PDFReadingControl pdf_reading_control, PDFRendererControl pdf_renderer_control, PDFRendererControlStats pdf_renderer_control_stats)
        {
            pdf_reading_control.OnlineDatabaseLookupControl.PDFDocument = pdf_renderer_control_stats.pdf_document;

            Thread.Sleep(1000);

            // Uncomment once ready
            SafeThreadPool.QueueUserWorkItem(o => DoInterestingAnalysis_DuplicatesAndCitations(pdf_reading_control, pdf_renderer_control, pdf_renderer_control_stats));
            SafeThreadPool.QueueUserWorkItem(o => DoInterestingAnalysis_GoogleScholar(pdf_reading_control, pdf_renderer_control, pdf_renderer_control_stats));
            SafeThreadPool.QueueUserWorkItem(o => DoInterestingAnalysis_TagCloud(pdf_reading_control, pdf_renderer_control, pdf_renderer_control_stats));
            SafeThreadPool.QueueUserWorkItem(o => DoInterestingAnalysis_SimilarAuthors(pdf_reading_control, pdf_renderer_control, pdf_renderer_control_stats));
        }

        static void DoInterestingAnalysis_DuplicatesAndCitations(PDFReadingControl pdf_reading_control, PDFRendererControl pdf_renderer_control, PDFRendererControlStats pdf_renderer_control_stats)
        {            
            try
            {
                pdf_renderer_control.Dispatcher.Invoke(new Action(() =>
                {
                    pdf_reading_control.DuplicateDetectionControl.PDFDocument = pdf_renderer_control_stats.pdf_document;
                }
                ));
                pdf_renderer_control.Dispatcher.Invoke(new Action(() =>
                {
                    pdf_reading_control.CitationsControl.PDFDocument = pdf_renderer_control_stats.pdf_document;
                }
                ));
                pdf_renderer_control.Dispatcher.Invoke(new Action(() =>
                {
                    pdf_reading_control.LinkedDocumentsControl.PDFDocument = pdf_renderer_control_stats.pdf_document;
                }
                ));
            }
            catch (Exception ex)
            {
                Logging.Error(ex, "There was a problem with the citations analysis for document {0}", pdf_renderer_control_stats.pdf_document.Fingerprint);
            }
        }


        static void DoInterestingAnalysis_GoogleScholar(PDFReadingControl pdf_reading_control, PDFRendererControl pdf_renderer_control, PDFRendererControlStats pdf_renderer_control_stats)
        {
            /*
            // Get the GoogleScholar similar documents
            try
            {
                string title = pdf_renderer_control_stats.pdf_document.TitleCombined;
                if (PDFDocument.TITLE_UNKNOWN != title)
                {
                    GoogleScholarScrapePaperSet gssp_set = GoogleScholarScrapePaperSet.GenerateFromQuery(ConfigurationManager.Instance.Proxy, title, 10);

                    pdf_renderer_control.Dispatcher.Invoke(new Action(() =>
                    {
                        pdf_reading_control.SimilarDocsControl.PaperSet = gssp_set;
                    }
                    ));
                }
                else
                {
                    Logging.Info("We don't have a title, so skipping GoogleScholar similar documents");
                }
            }
            catch (Exception ex)
            {
                Logging.Error(ex, "There was a problem getting the GoogleScholar similar documents for document {0}", pdf_renderer_control_stats.pdf_document.Fingerprint);
            }
            */
        }

        static void DoInterestingAnalysis_TagCloud(PDFReadingControl pdf_reading_control, PDFRendererControl pdf_renderer_control, PDFRendererControlStats pdf_renderer_control_stats)
        {
            // Populate the tag cloud
            try
            {
                List<TagCloudEntry> tag_cloud_entries = PDFDocumentTagCloudBuilder.BuildTagCloud(pdf_renderer_control_stats.pdf_document.Library, pdf_renderer_control_stats.pdf_document);

                pdf_renderer_control.Dispatcher.Invoke(new Action(() =>
                {
                    pdf_reading_control.TagCloud.Entries = tag_cloud_entries;
                }
                ));
            }
            catch (Exception ex)
            {
                Logging.Error(ex, "There was a problem creating the tag cloud for document {0}", pdf_renderer_control_stats.pdf_document.Fingerprint);
            }
        }

        static void DoInterestingAnalysis_SimilarAuthors(PDFReadingControl pdf_reading_control, PDFRendererControl pdf_renderer_control, PDFRendererControlStats pdf_renderer_control_stats)
        {
            // Populate the similar authors
            try
            {
                List<NameTools.Name> authors = SimilarAuthors.GetAuthorsForPDFDocument(pdf_renderer_control_stats.pdf_document);
                MultiMap<string, PDFDocument> authors_documents = SimilarAuthors.GetDocumentsBySameAuthors(pdf_renderer_control_stats.pdf_document.Library, pdf_renderer_control_stats.pdf_document, authors);

                pdf_renderer_control.Dispatcher.Invoke(new Action(() =>
                {
                    pdf_reading_control.SimilarAuthorsControl.Items = authors_documents;
                }
                ));
            }
            catch (Exception ex)
            {
                Logging.Error(ex, "There was a problem creating the tag cloud for document {0}", pdf_renderer_control_stats.pdf_document.Fingerprint);
            }
        }
    }
}
