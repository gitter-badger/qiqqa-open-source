﻿using System.Windows.Controls;
using System.Windows.Input;
using Qiqqa.DocumentLibrary.WebLibraryStuff;

namespace Qiqqa.Common.LibraryPickerStuff
{
    /// <summary>
    /// Interaction logic for LibraryPickerTextBoxControl.xaml
    /// </summary>
    public partial class LibraryPickerTextBoxControl : UserControl
    {
        public delegate void WebLibraryPicked(WebLibraryDetail web_library_detail);
        public event WebLibraryPicked OnWebLibraryPicked;

        public LibraryPickerTextBoxControl()
        {
            InitializeComponent();

            ObjLibraryTextBox.IsReadOnly = true;
            ObjLibraryTextBox.Cursor = Cursors.Hand;
            ObjLibraryTextBox.ToolTip = "Click here to choose a different library.";
            ObjLibraryTextBox.PreviewMouseDown += ObjLibraryTextBox_PreviewMouseDown;

            ChooseNewLibrary(null);
        }

        void ObjLibraryTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Pick a new library...
            WebLibraryDetail web_library_detail = WebLibraryPicker.PickWebLibrary();
            if (null != web_library_detail)
            {
                ChooseNewLibrary(web_library_detail);
            }

            e.Handled = true;
        }

        public WebLibraryDetail WebLibraryDetail { get; private set; }
        
        public void ChooseNewLibrary(WebLibraryDetail web_library_detail)
        {
            this.WebLibraryDetail = web_library_detail;

            if (null != web_library_detail)
            {
                ObjLibraryTextBox.Text = web_library_detail.Title;
            }
            else
            {
                ObjLibraryTextBox.Text = "Click to choose a library.";
            }

            // Callback
            if (null != OnWebLibraryPicked)
            {
                OnWebLibraryPicked(web_library_detail);
            }
        }
    }
}
