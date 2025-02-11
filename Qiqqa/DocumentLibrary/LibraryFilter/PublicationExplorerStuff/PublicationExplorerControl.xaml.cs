﻿using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Documents;
using Qiqqa.DocumentLibrary.TagExplorerStuff;
using Qiqqa.Documents.PDF;
using Utilities.Collections;

namespace Qiqqa.DocumentLibrary.LibraryFilter.PublicationExplorerStuff
{
    /// <summary>
    /// Interaction logic for TagExplorerControl.xaml
    /// </summary>
    public partial class PublicationExplorerControl : UserControl
    {
        private Library library;

        public delegate void OnTagSelectionChangedDelegate(HashSet<string> fingerprints, Span descriptive_span);
        public event OnTagSelectionChangedDelegate OnTagSelectionChanged;

        public PublicationExplorerControl()
        {
            InitializeComponent();

            this.ToolTip = "Here are the Publications of your documents.  " + GenericLibraryExplorerControl.YOU_CAN_FILTER_TOOLTIP;

            TagExplorerTree.DescriptionTitle = "Publication";

            TagExplorerTree.GetNodeItems = GetNodeItems;

            TagExplorerTree.OnItemPopup = OnItemPopup;

            TagExplorerTree.OnTagSelectionChanged += TagExplorerTree_OnTagSelectionChanged;
        }

        // -----------------------------

        public Library Library
        {
            set
            {
                this.library = value;
                TagExplorerTree.Library = value;
            }
        }

        public void Reset()
        {
            TagExplorerTree.Reset();
        }

        // -----------------------------

        internal static MultiMapSet<string, string> GetNodeItems(Library library, HashSet<string> parent_fingerprints)
        {
            List<PDFDocument> pdf_documents = null;
            if (null == parent_fingerprints)
            {
                pdf_documents = library.PDFDocuments;
            }
            else
            {
                pdf_documents = library.GetDocumentByFingerprints(parent_fingerprints);
            }

            MultiMapSet<string, string> tags_with_fingerprints = new MultiMapSet<string, string>();
            foreach (PDFDocument pdf_document in pdf_documents)
            {
                tags_with_fingerprints.Add(pdf_document.Publication ?? "(none)", pdf_document.Fingerprint);
            }

            return tags_with_fingerprints;
        }

        void TagExplorerTree_OnTagSelectionChanged(HashSet<string> fingerprints, Span descriptive_span)
        {
            if (null != OnTagSelectionChanged)
            {
                OnTagSelectionChanged(fingerprints, descriptive_span);
            }
        }

        void OnItemPopup(Library library, string item_tag)
        {
            PublicationExplorerItemPopup popup = new PublicationExplorerItemPopup(library, item_tag);
            popup.Open();
        }

    }
}
