﻿using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using icons;
using Qiqqa.UtilisationTracking;
using Utilities;
using Utilities.Files;
using Utilities.GUI.Wizard;

namespace Qiqqa.AnnotationsReportBuilding
{
    /// <summary>
    /// Interaction logic for ReportViewerControl.xaml
    /// </summary>
    public partial class ReportViewerControl : UserControl, IDisposable
    {
        private AsyncAnnotationReportBuilder.AnnotationReport annotation_report;

        public ReportViewerControl(AsyncAnnotationReportBuilder.AnnotationReport annotation_report)
        {
            InitializeComponent();

            WizardDPs.SetPointOfInterest(this, "LibraryAnnotationReportViewer");

            ButtonPrint.Icon = Icons.GetAppIcon(Icons.Printer);
            ButtonPrint.ToolTip = "Print this report";
            ButtonPrint.Click += ButtonPrint_Click;

            ButtonToWord.Icon = Icons.GetAppIcon(Icons.AnnotationReportExportToWord);
            ButtonToWord.ToolTip = "Export to Word";
            ButtonToWord.Click += ButtonToWord_Click;

            ButtonToPDF.Icon = Icons.GetAppIcon(Icons.AnnotationReportExportToPDF);
            ButtonToPDF.ToolTip = "Export to PDF";
            ButtonToPDF.Click += ButtonToPDF_Click;

            //ButtonCollapseClickOptions.Caption = "Collapse";
            //ButtonCollapseClickOptions.Click += ButtonCollapseClickOptions_Click;
            //ButtonExpandClickOptions.Caption = "Expand";
            //ButtonExpandClickOptions.Click += ButtonExpandClickOptions_Click;
            
            this.annotation_report = annotation_report;
            this.ObjDocumentViewer.Document = annotation_report.flow_document;
        }

        void ButtonExpandClickOptions_Click(object sender, RoutedEventArgs e)
        {
            annotation_report.ExpandClickOptions();
        }

        void ButtonCollapseClickOptions_Click(object sender, RoutedEventArgs e)
        {
            annotation_report.CollapseClickOptions();
        }

        ~ReportViewerControl()
        {
            Logging.Info("~ReportViewerControl()");
            Dispose(false);            
        }

        public void Dispose()
        {
            Logging.Info("Disposing ReportViewerControl");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Get rid of managed resources
                this.ObjDocumentViewer.Document.Blocks.Clear();
                this.ObjDocumentViewer.Document = null;
            }

            // Get rid of unmanaged resources 
        }


        string SaveToRTF()
        {
            FlowDocument flow_document = ObjDocumentViewer.Document;
            TextRange text_range = new TextRange(flow_document.ContentStart, flow_document.ContentEnd);

            string filename = TempFile.GenerateTempFilename("rtf");
            using (FileStream fs = File.OpenWrite(filename))
            {
                text_range.Save(fs, DataFormats.Rtf);
            }

            return filename;
        }
        
        void ButtonToWord_Click(object sender, RoutedEventArgs e)
        {
            FeatureTrackingManager.Instance.UseFeature(Features.AnnotationReport_ToWord);

            annotation_report.CollapseClickOptions();
            string filename = SaveToRTF();
            annotation_report.ExpandClickOptions();
            Process.Start(filename);
        }

        void ButtonToPDF_Click(object sender, RoutedEventArgs e)
        {
            /*
            PdfDocument doc = new PdfDocument();

            PdfPage page = doc.Pages.Add();            
            SizeF bounds = page.GetClientSize();

            string filename_rtf = SaveToRTF();
            string text = File.ReadAllText(filename_rtf);

            PdfMetafile metafile = (PdfMetafile)PdfImage.FromRtf(text, bounds.Width, PdfImageType.Metafile);
            PdfMetafileLayoutFormat format = new PdfMetafileLayoutFormat();

            //Allow the text to flow multiple pages without any breaks.
            format.SplitTextLines = true;
            format.SplitImages = true;

            //Draw the image.
            metafile.Draw(page, 0, 0, format);

            string filename_pdf = TempFile.GenerateTempFilename("pdf");

            doc.Save(filename_pdf);
            Process.Start(filename_pdf);
            */ 
        }

        void ButtonPrint_Click(object sender, RoutedEventArgs e)
        {
            FeatureTrackingManager.Instance.UseFeature(Features.AnnotationReport_Print);
            annotation_report.CollapseClickOptions();
            ObjDocumentViewer.Print();
            annotation_report.ExpandClickOptions();
        }
    }
}
