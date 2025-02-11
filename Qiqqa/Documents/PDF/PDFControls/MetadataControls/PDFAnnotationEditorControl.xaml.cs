﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using icons;
using Qiqqa.Documents.Common;
using Qiqqa.UtilisationTracking;
using Utilities.GUI;

namespace Qiqqa.Documents.PDF.PDFControls.MetadataControls
{
    /// <summary>
    /// Interaction logic for PDFAnnotationEditorControl.xaml
    /// </summary>
    public partial class PDFAnnotationEditorControl : UserControl
    {
        PDFAnnotation pdf_annotation;

        public PDFAnnotationEditorControl()
        {
            InitializeComponent();

            this.Background = ThemeColours.Background_Brush_Blue_LightToDark;

            ButtonColor1.Background = Brushes.LightPink;
            ButtonColor2.Background = Brushes.LightSalmon;
            ButtonColor3.Background = Brushes.LightGreen;
            ButtonColor4.Background = Brushes.SkyBlue;
            ButtonColor5.Background = Brushes.Yellow;

            ButtonColor1.Click += ButtonColor_Click;
            ButtonColor2.Click += ButtonColor_Click;
            ButtonColor3.Click += ButtonColor_Click;
            ButtonColor4.Click += ButtonColor_Click;
            ButtonColor5.Click += ButtonColor_Click;

            ButtonDeleteAnnotation.Icon = Icons.GetAppIcon(Icons.Delete);
            ButtonDeleteAnnotation.CaptionDock = Dock.Right;
            ButtonDeleteAnnotation.Caption = "Delete annotation";
            ButtonDeleteAnnotation.Click += ButtonDeleteAnnotation_Click;

            ComboBoxRating.ItemsSource = Choices.Ratings;

            ObjColorPicker.SelectedColorChanged += ObjColorPicker_SelectedColorChanged;

            ObjTagEditorControl.TagFeature_Add = Features.Document_AddAnnotationTag;
            ObjTagEditorControl.TagFeature_Remove = Features.Document_RemoveAnnotationTag;
        }

        void ButtonDeleteAnnotation_Click(object sender, RoutedEventArgs e)
        {
            this.pdf_annotation.Deleted = true;
            this.pdf_annotation.Bindable.NotifyPropertyChanged(() => (pdf_annotation.Deleted));
        }

        void ButtonColor_Click(object sender, RoutedEventArgs e)
        {
            AugmentedButton button = (AugmentedButton) sender;
            SolidColorBrush brush = (SolidColorBrush) button.Background;
            ObjColorPicker.SelectedColor = brush.Color;            
        }

        static Color last_annotation_color = Colors.SkyBlue;
        public static Color LastAnnotationColor
        {
            get { return last_annotation_color; }
        }

        void ObjColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            last_annotation_color = ObjColorPicker.SelectedColor;

            if (null != pdf_annotation)
            {
                pdf_annotation.Color = ObjColorPicker.SelectedColor;
                pdf_annotation.Bindable.NotifyPropertyChanged(() => (pdf_annotation.Color));
            }            
        }

        public PDFAnnotation PDFAnnotation
        {
            set
            {
                this.pdf_annotation = value;
                this.DataContext = pdf_annotation.Bindable;

                if (null != pdf_annotation)
                {
                    ObjColorPicker.SelectedColor = pdf_annotation.Color;
                    this.Visibility = Visibility.Visible;
                }
                else
                {
                    this.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
