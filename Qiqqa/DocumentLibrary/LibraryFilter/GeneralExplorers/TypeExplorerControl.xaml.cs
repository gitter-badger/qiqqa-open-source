﻿using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Documents;
using Qiqqa.DocumentLibrary.TagExplorerStuff;
using Qiqqa.Documents.PDF;
using Utilities.Collections;

namespace Qiqqa.DocumentLibrary.LibraryFilter.GeneralExplorers
{
    /// <summary>
    /// Interaction logic for TypeExplorerControl.xaml
    /// </summary>
    public partial class TypeExplorerControl : UserControl
    {
        private Library library;

        public delegate void OnTagSelectionChangedDelegate(HashSet<string> fingerprints, Span descriptive_span);
        public event OnTagSelectionChangedDelegate OnTagSelectionChanged;

        public TypeExplorerControl()
        {
            InitializeComponent();

            this.ToolTip = "Here are the Types of your documents.  " + GenericLibraryExplorerControl.YOU_CAN_FILTER_TOOLTIP;

            TagExplorerTree.DescriptionTitle = "Type";

            TagExplorerTree.GetNodeItems = GetNodeItems;

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
                string type = null;
                if (null != pdf_document.BibTexItem)
                {
                    type = pdf_document.BibTexItem.Type;
                }

                tags_with_fingerprints.Add(type ?? "(none)", pdf_document.Fingerprint);
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
    }
}
