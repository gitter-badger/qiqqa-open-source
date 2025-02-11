﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Qiqqa.Common.Configuration;
using Qiqqa.Documents.PDF.PDFControls.MetadataControls;
using Qiqqa.Documents.PDF.PDFControls.Page.Tools;
using Qiqqa.Main.LoginStuff;
using Qiqqa.UtilisationTracking;
using Utilities;
using Utilities.GUI.Wizard;

namespace Qiqqa.Documents.PDF.PDFControls.Page.Annotation
{
    /// <summary>
    /// Interaction logic for PDFAnnotationLayer.xaml
    /// </summary>
    public partial class PDFAnnotationLayer : PageLayer
    {        
        PDFRendererControlStats pdf_renderer_control_stats;
        int page;

        DragAreaTracker drag_area_tracker;

        public PDFAnnotationLayer(PDFRendererControlStats pdf_renderer_control_stats, int page)
        {
            this.pdf_renderer_control_stats = pdf_renderer_control_stats;
            this.page = page;
            
            InitializeComponent();

            // Wizard
            if (1 == page)
            {
                WizardDPs.SetPointOfInterest(this, "PDFReadingAnnotationLayer");
            }

            this.Background = Brushes.Transparent;
            this.Cursor = Cursors.Cross;

            this.SizeChanged += PDFAnnotationLayer_SizeChanged;

            drag_area_tracker = new DragAreaTracker(this);
            drag_area_tracker.OnDragComplete += drag_area_tracker_OnDragComplete;

            // Add all the already existing annotations
            foreach (PDFAnnotation pdf_annotation in pdf_renderer_control_stats.pdf_document.Annotations)
            {
                if (pdf_annotation.Page == this.page)
                {
                    if (!pdf_annotation.Deleted)
                    {
                        Logging.Info("Loading annotation on page {0}", page);
                        PDFAnnotationItem pdf_annotation_item = new PDFAnnotationItem(this, pdf_annotation, pdf_renderer_control_stats);
                        pdf_annotation_item.ResizeToPage(ActualWidth, ActualHeight);
                        Children.Add(pdf_annotation_item);
                    }
                    else
                    {
                        Logging.Info("Not loading deleted annotation on page {0}", page);
                    }
                }
            }
        }

        public static bool IsLayerNeeded(PDFRendererControlStats pdf_renderer_control_stats, int page)
        {
            foreach (PDFAnnotation pdf_annotation in pdf_renderer_control_stats.pdf_document.Annotations)
            {
                if (pdf_annotation.Page == page)
                {
                    return true;
                }
            }

            return false;
        }

        internal override void Dispose()
        {
            foreach (PDFAnnotationItem pdf_annotation_item in Children.OfType<PDFAnnotationItem>())
            {
                pdf_annotation_item.Dispose();
            }
        }

        void drag_area_tracker_OnDragComplete(bool button_left_pressed, bool button_right_pressed, Point mouse_down_point, Point mouse_up_point)
        {
            FeatureTrackingManager.Instance.UseFeature(Features.Document_AddAnnotation);

            int MINIMUM_DRAG_SIZE_TO_CREATE_ANNOTATION = 20;
            if (Math.Abs(mouse_up_point.X - mouse_down_point.X) < MINIMUM_DRAG_SIZE_TO_CREATE_ANNOTATION ||
                Math.Abs(mouse_up_point.Y - mouse_down_point.Y) < MINIMUM_DRAG_SIZE_TO_CREATE_ANNOTATION)
            {
                Logging.Info("Drag area too small to create annotation");
                return;
            }

            PDFAnnotation pdf_annotation = new PDFAnnotation(pdf_renderer_control_stats.pdf_document.PDFRenderer.DocumentFingerprint, page, PDFAnnotationEditorControl.LastAnnotationColor, ConfigurationManager.Instance.ConfigurationRecord.Account_Nickname);
            pdf_annotation.Left = Math.Min(mouse_up_point.X, mouse_down_point.X) / this.ActualWidth;
            pdf_annotation.Top = Math.Min(mouse_up_point.Y, mouse_down_point.Y) / this.ActualHeight;
            pdf_annotation.Width = Math.Abs(mouse_up_point.X - mouse_down_point.X) / this.ActualWidth;
            pdf_annotation.Height = Math.Abs(mouse_up_point.Y - mouse_down_point.Y) / this.ActualHeight;

            pdf_renderer_control_stats.pdf_document.Annotations.AddUpdatedAnnotation(pdf_annotation);

            PDFAnnotationItem pdf_annotation_item = new PDFAnnotationItem(this, pdf_annotation, pdf_renderer_control_stats);
            pdf_annotation_item.ResizeToPage(ActualWidth, ActualHeight);
            Children.Add(pdf_annotation_item);
        }

        void PDFAnnotationLayer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (PDFAnnotationItem pdf_annotation_item in Children.OfType<PDFAnnotationItem>())
            {
                pdf_annotation_item.ResizeToPage(ActualWidth, ActualHeight);
            }
        }

        internal override void SelectPage()
        {
        }

        internal override void DeselectPage()
        {
        }

        internal override void PageTextAvailable()
        {
        }

        internal void DeletePDFAnnotationItem(PDFAnnotationItem pdf_annotation_item)
        {            
            Children.Remove(pdf_annotation_item);
        }
    }
}
