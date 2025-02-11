﻿using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Qiqqa.AnnotationsReportBuilding;
using Qiqqa.Common;
using Utilities.GUI;
using Utilities.Reflection;
using Utilities.Random;

namespace Qiqqa.Documents.PDF.PDFControls.MetadataControls
{
    /// <summary>
    /// Interaction logic for AnnotationsReviewControl.xaml
    /// </summary>
    public partial class AnnotationsReviewControl : UserControl
    {
        NonScrollingFlowDocumentScrollViewer ObjDocumentViewer;

        public AnnotationsReviewControl()
        {
            InitializeComponent();

            ObjDocumentViewer = new NonScrollingFlowDocumentScrollViewer();
            ObjDocumentViewer.ToolTip = "Any annotations or highlights that you have made in your document are shown here.\nYou can also copy these and paste them into your word processor or email editor.";
            ObjFlowDocumentHolder.Children.Add(ObjDocumentViewer);

            ObjTooManyAnnotationsButton.CaptionDock = Dock.Bottom;
            ObjTooManyAnnotationsButton.Caption = "There are too many annotations to show automatically.  Click here to show them.";
            ObjTooManyAnnotationsButton.Click += ObjTooManyAnnotationsButton_Click;

            ObjRefreshButton.CaptionDock = Dock.Bottom;
            ObjRefreshButton.Caption = "Refresh";
            ObjRefreshButton.Click += ObjRefreshButton_Click;

            ObjPopupButton.CaptionDock = Dock.Bottom;
            ObjPopupButton.Caption = "Popup";
            ObjPopupButton.Click += ObjPopupButton_Click;

            this.DataContextChanged += AnnotationsReviewControl_DataContextChanged;
        }

        void ObjTooManyAnnotationsButton_Click(object sender, RoutedEventArgs e)
        {
            ObjTooManyAnnotationsButton.Visibility = Visibility.Collapsed;

            PDFDocument pdf_document = (PDFDocument)ObjTooManyAnnotationsButton.Tag;
            PopulateWithAnnotationReport(pdf_document);
        }


        void ObjPopupButton_Click(object sender, RoutedEventArgs e)
        {
            AugmentedBindable<PDFDocument> pdf_document_bindable = DataContext as AugmentedBindable<PDFDocument>;
            if (null != pdf_document_bindable)
            {
                MainWindowServiceDispatcher.Instance.GenerateAnnotationReport(pdf_document_bindable.Underlying.Library, new List<PDFDocument>() { pdf_document_bindable.Underlying });
            }            
        }

        void ObjRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Rebuild();
        }
        
        void AnnotationsReviewControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Rebuild();
        }

        void Rebuild()
        {
            this.ObjDocumentViewer.Document = null;
            ObjTooManyAnnotationsButton.Visibility = Visibility.Collapsed;

            AugmentedBindable<PDFDocument> pdf_document_bindable = DataContext as AugmentedBindable<PDFDocument>;
            if (null != pdf_document_bindable)
            {
                PDFDocument pdf_document = pdf_document_bindable.Underlying;

                if (pdf_document.Annotations.Count > 50 || pdf_document.Highlights.Count > 1000)
                {
                    ObjTooManyAnnotationsButton.Visibility = Visibility.Visible;
                    ObjTooManyAnnotationsButton.Tag = pdf_document;
                }
                else
                {
                    PopulateWithAnnotationReport(pdf_document);
                }
            }
        }

        private void PopulateWithAnnotationReport(PDFDocument pdf_document)
        {
            List<PDFDocument> pdf_documents = new List<PDFDocument>();
            pdf_documents.Add(pdf_document);

            AnnotationReportOptionsWindow.AnnotationReportOptions annotation_report_options = new AnnotationReportOptionsWindow.AnnotationReportOptions();
            annotation_report_options.ObeySuppressedImages = true;
            annotation_report_options.ObeySuppressedText = true;
            annotation_report_options.SuppressAllImages = false;
            annotation_report_options.SuppressAllText = true;
            annotation_report_options.SuppressPDFDocumentHeader = true;
            annotation_report_options.SuppressPDFAnnotationTags = true;
            annotation_report_options.InitialRenderDelayMilliseconds = 1000;

            var annotation_report = AsyncAnnotationReportBuilder.BuildReport(pdf_document.Library, pdf_documents, annotation_report_options);
            this.ObjDocumentViewer.Document = annotation_report.flow_document;
        }
    }
}
