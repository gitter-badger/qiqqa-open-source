﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Qiqqa.DocumentLibrary;
using Qiqqa.DocumentLibrary.LibraryCatalog;
using Qiqqa.Documents.PDF;
using Qiqqa.UtilisationTracking;
using Utilities;
using Utilities.GUI;

namespace Qiqqa.Common.GUI
{
    public class ListFormattingTools
    {

        public class DocumentTextBlockTag
        {
            public PDFDocument pdf_document;
            public Feature feature;
            public object additional_tag;
            public LibraryIndexHoverPopup library_index_hover_popup = null;
        }

        public static TextBlock GetDocumentTextBlock(PDFDocument pdf_document, ref bool alternator, Feature feature)
        {
            return GetDocumentTextBlock(pdf_document, ref alternator, feature, null, null, null);
        }

        public static TextBlock GetDocumentTextBlock(PDFDocument pdf_document, ref bool alternator, Feature feature, MouseButtonEventHandler mouse_down)
        {
            return GetDocumentTextBlock(pdf_document, ref alternator, feature, mouse_down, null, null);
        }

        public static TextBlock GetDocumentTextBlock(PDFDocument pdf_document, ref bool alternator, Feature feature, MouseButtonEventHandler mouse_down, string prefix)
        {
            return GetDocumentTextBlock(pdf_document, ref alternator, feature, mouse_down, prefix, null);
        }
        
        public static TextBlock GetDocumentTextBlock(PDFDocument pdf_document, ref bool alternator, Feature feature, MouseButtonEventHandler mouse_down, string prefix, object additional_tag)
        {
            string header = GetPDFDocumentDescription(pdf_document, null);

            // If they have not given us a mouse down event handler, then just open the PDF
            if (null == mouse_down)
            {
                mouse_down = text_doc_MouseDown;
            }

            TextBlock text_doc = new TextBlock();
            text_doc.Text = prefix + header;
            text_doc.Tag = new DocumentTextBlockTag { pdf_document = pdf_document, feature = feature, additional_tag = additional_tag };
            text_doc.Cursor = Cursors.Hand;
            text_doc.MouseLeftButtonUp += mouse_down;
            text_doc.MouseRightButtonUp += text_doc_MouseRightButtonUp;

            text_doc.ToolTip = "";
            text_doc.ToolTipClosing += PDFDocumentNodeContentControl_ToolTipClosing;
            text_doc.ToolTipOpening += PDFDocumentNodeContentControl_ToolTipOpening;

            alternator = !alternator;

            text_doc.Background = Brushes.Transparent;
            AddGlowingHoverEffect(text_doc);

            return text_doc;
        }

        public static string GetPDFDocumentDescription(PDFDocument pdf_document, string prefix)
        {
            StringBuilder sb = new StringBuilder();
            {
                if (!String.IsNullOrEmpty(prefix))
                {
                    sb.Append(prefix);
                    sb.Append(' ');
                }

                if (null != pdf_document)
                {
                    string year = pdf_document.YearCombined;
                    if (PDFDocument.UNKNOWN_YEAR != year)
                    {
                        sb.Append("(" + year + ") ");
                    }

                    sb.Append(pdf_document.TitleCombined);

                    string authors = pdf_document.AuthorsCombined;
                    if (PDFDocument.UNKNOWN_AUTHORS != authors)
                    {
                        sb.Append(" by " + authors);
                    }
                }

                else
                {
                    Logging.Warn("ListFormattingTools.GetDocumentTextBlock can not process a null PDFDocument.");
                    sb.Append("<null>");
                }
            }

            return sb.ToString();
        }

        static void text_doc_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock text_block = (TextBlock)sender;
            DocumentTextBlockTag tag = (DocumentTextBlockTag)text_block.Tag;

            LibraryCatalogPopup popup = new LibraryCatalogPopup(new List<PDFDocument> { tag.pdf_document });
            popup.Open();

            e.Handled = true;
        }

        
        static void text_doc_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock text_block = (TextBlock)sender;
            DocumentTextBlockTag tag = (DocumentTextBlockTag)text_block.Tag;

            if (null != tag.feature)
            {
                FeatureTrackingManager.Instance.UseFeature(tag.feature);
            }

            MainWindowServiceDispatcher.Instance.OpenDocument(tag.pdf_document);

            e.Handled = true;
        }

        static void PDFDocumentNodeContentControl_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            TextBlock text_block = (TextBlock)sender;
            DocumentTextBlockTag tag = (DocumentTextBlockTag)text_block.Tag;

            try
            {
                if (null == tag.library_index_hover_popup)
                {
                    tag.library_index_hover_popup = new LibraryIndexHoverPopup();
                    tag.library_index_hover_popup.SetPopupContent(tag.pdf_document, 1);
                    text_block.ToolTip = tag.library_index_hover_popup;
                }
            }

            catch (Exception ex)
            {
                Logging.Error(ex, "Exception while displaying document preview popup");
            }
        }

        static void PDFDocumentNodeContentControl_ToolTipClosing(object sender, ToolTipEventArgs e)
        {
            TextBlock text_block = (TextBlock)sender;
            DocumentTextBlockTag tag = (DocumentTextBlockTag)text_block.Tag;

            if (null != tag.library_index_hover_popup)
            {
                text_block.ToolTip = "";
                tag.library_index_hover_popup.Dispose();
                tag.library_index_hover_popup = null;
            }
        }

        public static void AddGlowingHoverEffect(FrameworkElement fe)
        {            
            fe.MouseEnter += AddGlowingHoverEffect_MouseEnter;
            fe.MouseLeave += AddGlowingHoverEffect_MouseLeave;
        }

        static void AddGlowingHoverEffect_MouseLeave(object sender, MouseEventArgs e)
        {
            {
                TextBlock o = sender as TextBlock;
                if (null != o) o.Background = Brushes.Transparent;
            }
            {
                Panel o = sender as Panel;
                if (null != o) o.Background = Brushes.Transparent;
            }
        }

        static void AddGlowingHoverEffect_MouseEnter(object sender, MouseEventArgs e)
        {
            {
                TextBlock o = sender as TextBlock;
                if (null != o) o.Background = ThemeColours.Background_Brush_Blue_LightToVeryLight;
            }
            {
                Panel o = sender as Panel;
                if (null != o) o.Background = ThemeColours.Background_Brush_Blue_LightToVeryLight;
            }
        }
    }
}
