﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using icons;
using Qiqqa.Common.Configuration;
using Qiqqa.Documents.PDF.PDFControls.MetadataControls;
using Qiqqa.UtilisationTracking;
using Utilities.GUI;
using Utilities.GUI.Animation;

namespace Qiqqa.Documents.PDF.PDFControls.Page.Annotation
{
    /// <summary>
    /// Interaction logic for PDFAnnotationItem.xaml
    /// </summary>
    public partial class PDFAnnotationItem : UserControl
    {
        PDFAnnotationLayer pdf_annotation_layer;
        PDFAnnotation pdf_annotation;
        PDFRendererControlStats pdf_renderer_control_stats;

        AugmentedToolWindow pdf_annotation_editor_control_popup;

        double actual_page_width;
        double actual_page_height;

        bool moving_because_mouse_is_down = false;
        bool scaling_because_of_double_tap = false;        
        Point mouse_down_position;

        public PDFAnnotationItem(PDFAnnotationLayer pdf_annotation_layer, PDFAnnotation pdf_annotation, PDFRendererControlStats pdf_renderer_control_stats)
        {
            this.pdf_annotation_layer  = pdf_annotation_layer;
            this.pdf_annotation = pdf_annotation;
            this.pdf_renderer_control_stats = pdf_renderer_control_stats;

            this.DataContext = pdf_annotation.Bindable;

            InitializeComponent();

            this.MouseEnter += TextAnnotationText_MouseEnter;
            this.MouseLeave += TextAnnotationText_MouseLeave;

            ButtonAnnotationDetails.MouseEnter += ButtonAnnotationDetails_MouseEnter;
            ButtonAnnotationDetails.MouseLeave += ButtonAnnotationDetails_MouseLeave;
            ButtonAnnotationDetails.Cursor = Cursors.Hand;
            ButtonAnnotationDetails.MouseDown += ButtonAnnotationDetails_MouseDown;
            ButtonAnnotationDetails.Source = Icons.GetAppIcon(Icons.Metadata);
            ButtonAnnotationDetails.Width = 32;
            ButtonAnnotationDetails.ToolTip = "Edit this annotation.";
            RenderOptions.SetBitmapScalingMode(ButtonAnnotationDetails, BitmapScalingMode.HighQuality);

            TextAnnotationText.Background = Brushes.Transparent;
            TextAnnotationText.GotFocus += TextAnnotationText_GotFocus;            
            TextAnnotationText.LostFocus += TextAnnotationText_LostFocus;
            TextAnnotationText.PreviewMouseDown += TextAnnotationText_PreviewMouseDown;
            TextAnnotationText.PreviewMouseMove += TextAnnotationText_PreviewMouseMove;
            TextAnnotationText.PreviewMouseUp += TextAnnotationText_PreviewMouseUp;

            ObjTagEditorControl.GotFocus += ObjTagEditorControl_GotFocus;
            ObjTagEditorControl.LostFocus += ObjTagEditorControl_LostFocus;
            
            pdf_annotation.Bindable.PropertyChanged += pdf_annotation_PropertyChanged;

            ObjTagEditorControl.TagFeature_Add = Features.Document_AddAnnotationTag;
            ObjTagEditorControl.TagFeature_Remove = Features.Document_RemoveAnnotationTag;

            ReColor();
        }

        void ObjTagEditorControl_LostFocus(object sender, RoutedEventArgs e)
        {
            ReColor();
        }

        void ObjTagEditorControl_GotFocus(object sender, RoutedEventArgs e)
        {
            ReColor();
        }

        internal void Dispose()
        {
            pdf_annotation.Bindable.PropertyChanged -= pdf_annotation_PropertyChanged;
            this.DataContext = null;
        }


        void ButtonAnnotationDetails_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // If we have never had a popup, create it now
            if (null == pdf_annotation_editor_control_popup)
            {
                PDFAnnotationEditorControl pdf_annotation_editor_control = new PDFAnnotationEditorControl();
                pdf_annotation_editor_control.PDFAnnotation = pdf_annotation;
                pdf_annotation_editor_control_popup = new AugmentedToolWindow(pdf_annotation_editor_control, "Edit Annotation");
            }

            pdf_annotation_editor_control_popup.IsOpen = true;

            e.Handled = true;
        }


        DateTime last_mouse_down_timestamp = DateTime.MinValue;

        void TextAnnotationText_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && (Keyboard.Modifiers & ModifierKeys.Control) > 0)
            {
                DragDropEffects dde = DragDrop.DoDragDrop(this, pdf_annotation, DragDropEffects.Copy);
                e.Handled = true;
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (DateTime.UtcNow.Subtract(last_mouse_down_timestamp).TotalMilliseconds < 500)
                {
                    scaling_because_of_double_tap = true;
                }
                else
                {
                    scaling_because_of_double_tap = false;
                }

                moving_because_mouse_is_down = true;
                mouse_down_position = e.GetPosition(null);
                TextAnnotationText.Focus();
                TextAnnotationText.CaptureMouse();
            }

            last_mouse_down_timestamp = DateTime.UtcNow;
        }

        void TextAnnotationText_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            moving_because_mouse_is_down = false;
            TextAnnotationText.ReleaseMouseCapture();
        }

        void TextAnnotationText_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (moving_because_mouse_is_down)
            {
                Point new_mouse_position = e.GetPosition(null);

                if (scaling_because_of_double_tap || (Keyboard.Modifiers & ModifierKeys.Shift) > 0)
                {
                    pdf_annotation.Left = (pdf_annotation.Left * actual_page_width - (new_mouse_position.X - mouse_down_position.X) / 2.0) / actual_page_width;
                    pdf_annotation.Top = (pdf_annotation.Top * actual_page_height - (new_mouse_position.Y - mouse_down_position.Y) / 2.0) / actual_page_height;

                    pdf_annotation.Width = (pdf_annotation.Width * actual_page_width + new_mouse_position.X - mouse_down_position.X) / actual_page_width;
                    pdf_annotation.Height = (pdf_annotation.Height * actual_page_height + new_mouse_position.Y - mouse_down_position.Y) / actual_page_height;
                }
                else
                {
                    pdf_annotation.Left = (pdf_annotation.Left * actual_page_width + new_mouse_position.X - mouse_down_position.X) / actual_page_width;
                    pdf_annotation.Top = (pdf_annotation.Top * actual_page_height + new_mouse_position.Y - mouse_down_position.Y) / actual_page_height;
                }

                ResizeToPage();

                pdf_annotation.Bindable.NotifyPropertyChanged();

                mouse_down_position = new_mouse_position;
                e.Handled = true;
            }
        }

        void TextAnnotationText_MouseEnter(object sender, MouseEventArgs e)
        {
            ReColor();
        }

        void ButtonAnnotationDetails_MouseEnter(object sender, MouseEventArgs e)
        {
            ReColor();            
        }

        void ButtonAnnotationDetails_MouseLeave(object sender, MouseEventArgs e)
        {
            ReColor();
        }

        void TextAnnotationText_MouseLeave(object sender, MouseEventArgs e)
        {
            ReColor();
        }

        void TextAnnotationText_GotFocus(object sender, RoutedEventArgs e)
        {
            ReColor(); 
        }

        void TextAnnotationText_LostFocus(object sender, RoutedEventArgs e)
        {
            ReColor();
        }

        void pdf_annotation_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReColor();

            // If we are suddenly deleted, we need to cloe our popup and notify our parent so they can remove us from their viewing list
            if (pdf_annotation.Deleted)
            {
                pdf_annotation_layer.DeletePDFAnnotationItem(this);
                if (null != pdf_annotation_editor_control_popup)
                {
                    pdf_annotation_editor_control_popup.Close();
                }                
            }
        }

        void ReColor()
        {
            double LIGHT_TRANSPARENCY = ConfigurationManager.Instance.ConfigurationRecord.GUI_AnnotationScreenTransparency;
            double LIGHT_TRANSPARENCY_TEXT_VISIBLE = Math.Max(0.6, LIGHT_TRANSPARENCY);

            Color lighter_color = pdf_annotation.Color;
            Color darker_color = ColorTools.MakeDarkerColor(lighter_color);
            this.Background = new LinearGradientBrush(darker_color, lighter_color, 45);

            if (GUITools.IsDescendentOf(Keyboard.FocusedElement, TextAnnotationText) || GUITools.IsDescendentOf(Keyboard.FocusedElement, ObjTagEditorControl) || this.IsMouseOver)
            {
                TextAnnotationText.Foreground = Brushes.Black;

                PanelAdditionalControls.Visibility = Visibility.Visible;
                Animations.Fade(this, LIGHT_TRANSPARENCY, 0.95);
            }
            else
            {
                PanelAdditionalControls.Visibility = Visibility.Collapsed;

                if (!pdf_annotation.AnnotationTextAlwaysVisible)
                {
                    TextAnnotationText.Foreground = Brushes.Transparent;
                    Animations.Fade(this, 0.95, LIGHT_TRANSPARENCY);
                }
                else
                {
                    Animations.Fade(this, 0.95, LIGHT_TRANSPARENCY_TEXT_VISIBLE);
                }
            }
        }

        internal void ResizeToPage()
        {
            ResizeToPage(actual_page_width, actual_page_height);
        }

        internal void ResizeToPage(double actual_page_width, double actual_page_height)
        {
            pdf_annotation.Width = Math.Min(pdf_annotation.Width, 1.00);
            pdf_annotation.Height = Math.Min(pdf_annotation.Height, 1.00);
            pdf_annotation.Width = Math.Max(pdf_annotation.Width, 0.01);
            pdf_annotation.Height = Math.Max(pdf_annotation.Height, 0.01);

            pdf_annotation.Left = Math.Max(pdf_annotation.Left, 0);
            pdf_annotation.Top = Math.Max(pdf_annotation.Top, 0);
            pdf_annotation.Left = Math.Min(pdf_annotation.Left, 1.0 - pdf_annotation.Width);
            pdf_annotation.Top = Math.Min(pdf_annotation.Top, 1.0 - pdf_annotation.Height);

            this.actual_page_width = actual_page_width;
            this.actual_page_height = actual_page_height;

            Canvas.SetLeft(this, pdf_annotation.Left * actual_page_width);
            Canvas.SetTop(this, pdf_annotation.Top * actual_page_height);
            this.Width = pdf_annotation.Width * actual_page_width;
            this.Height = pdf_annotation.Height * actual_page_height;
        }
    }
}
