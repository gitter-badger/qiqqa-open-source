﻿using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using icons;
using Qiqqa.Documents.PDF;
using Qiqqa.UtilisationTracking;
using Utilities;
using Utilities.Collections;
using Utilities.GUI;
using Utilities.GUI.DualTabbedLayoutStuff;
using Utilities.Misc;
using Qiqqa.Common.Configuration;

namespace Qiqqa.DocumentLibrary.LibraryFilter
{
    /// <summary>
    /// Interaction logic for LibraryFilterControl.xaml
    /// </summary>
    public partial class LibraryFilterControl : UserControl
    {
        internal LibraryFilterControl_Search library_filter_control_search;

        internal string search_quick_query = null;
        internal Dictionary<string, double> search_quick_scores = null;
        internal HashSet<string> search_quick_fingerprints = null;
        internal Span search_quick_fingerprints_span = null;

        HashSet<string> search_tag_fingerprints = null;
        Span search_tag_fingerprints_span = null;
        HashSet<string> select_tag_fingerprints = null;
        Span select_tag_fingerprints_span = null;
        HashSet<string> select_ai_tag_fingerprints = null;
        Span select_ai_tag_fingerprints_span = null;
        HashSet<string> select_author_fingerprints = null;
        Span select_author_fingerprints_span = null;
        HashSet<string> select_publication_fingerprints = null;
        Span select_publication_fingerprints_span = null;
        HashSet<string> select_reading_stage_fingerprints = null;
        Span select_reading_stage_fingerprints_span = null;
        HashSet<string> select_year_fingerprints = null;
        Span select_year_fingerprints_span = null;
        HashSet<string> select_rating_fingerprints = null;
        Span select_rating_fingerprints_span = null;
        HashSet<string> select_theme_fingerprints = null;
        Span select_theme_fingerprints_span = null;

        public delegate void OnFilterChangedDelegate(LibraryFilterControl library_filter_control, List<PDFDocument> pdf_documents, Span descriptive_span, string filter_terms, Dictionary<string, double> search_scores, PDFDocument pdf_document_to_focus_on);
        public event OnFilterChangedDelegate OnFilterChanged;

        public LibraryFilterControl()
        {
            InitializeComponent();

            // Rotate the SORT icon 270 degrees
            TransformedBitmap sort_icon_rotated = new TransformedBitmap();
            {
                BitmapImage sort_icon = Icons.GetAppIcon(Icons.Sort);
                sort_icon_rotated.BeginInit();
                sort_icon_rotated.Transform = new RotateTransform(270);
                sort_icon_rotated.Source = sort_icon;
                sort_icon_rotated.EndInit();
            }

            // Move the tabs into their correct places...
            DualTabTags.Children.Clear();
            DualTabTags.AddContent("Sort", "Sort", sort_icon_rotated, false, false, TabSort);
            DualTabTags.AddContent("Tag", "Tag", null, false, false, TabTags);
            DualTabTags.AddContent("AutoTag", "AutoTag", null, false, false, TabAITags);
            DualTabTags.AddContent("Author", "Author", null, false, false, TabAuthors);
            DualTabTags.AddContent("Publ.", "Publ.", null, false, false, TabPublications);
            DualTabTags.AddContent("Year", "Year", null, false, false, TabYear);
            DualTabTags.AddContent("Stage", "Stage", null, false, false, TabReadingStages);
            DualTabTags.AddContent("Rating", "Rating", null, false, false, TabRatings);
            DualTabTags.AddContent("Theme", "Theme", null, false, false, TabThemes);
            DualTabTags.AddContent("Type", "Type", null, false, false, TabTypes);
            DualTabTags.MakeActive("Tag");
            DualTabTags.TabPosition = DualTabbedLayout.TabPositions.Sides;

            SearchTag.OnSoftSearch += SearchTag_OnSoftSearch;

            ObjTagExplorerControl.OnTagSelectionChanged += ObjTagExplorerControl_OnTagSelectionChanged;
            ObjAITagExplorerControl.OnTagSelectionChanged += ObjAITagExplorerControl_OnTagSelectionChanged;
            ObjAuthorExplorerControl.OnTagSelectionChanged += ObjAuthorExplorerControl_OnTagSelectionChanged;
            ObjPublicationExplorerControl.OnTagSelectionChanged += ObjPublicationExplorerControl_OnTagSelectionChanged;
            ObjReadingStageExplorerControl.OnTagSelectionChanged += ObjReadingStageExplorerControl_OnTagSelectionChanged;
            ObjYearExplorerControl.OnTagSelectionChanged += ObjYearExplorerControl_OnTagSelectionChanged;
            ObjRatingExplorerControl.OnTagSelectionChanged += ObjRatingExplorerControl_OnTagSelectionChanged;
            ObjThemeExplorerControl.OnTagSelectionChanged += ObjThemeExplorerControl_OnTagSelectionChanged;
            ObjTypeExplorerControl.OnTagSelectionChanged += ObjTypeExplorerControl_OnTagSelectionChanged;

            ObjLibraryFilterControl_Sort.SortChanged += ObjLibraryFilterControl_Sort_SortChanged;

            ObjPanelSearchByTag.Visibility = ConfigurationManager.Instance.NoviceVisibility;
        }

        internal Library library = null;
        public Library Library
        {
            set
            {
                if (null != library)
                {
                    throw new Exception("Library can only be set once");
                }

                this.library = value;

                // Set our child objects
                this.ObjTagExplorerControl.Library = library;
                this.ObjAITagExplorerControl.Library = library;
                this.ObjAuthorExplorerControl.Library = library;
                this.ObjPublicationExplorerControl.Library = library;
                this.ObjReadingStageExplorerControl.Library = library;
                this.ObjYearExplorerControl.Library = library;
                this.ObjRatingExplorerControl.Library = library;
                this.ObjThemeExplorerControl.Library = library;
                this.ObjTypeExplorerControl.Library = library;

                // WEAK EVENT HANDLER FOR: library.OnDocumentsChanged += Library_OnNewDocument;
                WeakEventHandler<Library.PDFDocumentEventArgs>.Register<Library, LibraryFilterControl>(
                    library,
                    (d, eh) => d.OnDocumentsChanged += eh,
                    (d, eh) => d.OnDocumentsChanged -= eh,
                    this,
                    (me, event_sender, args) => me.Library_OnNewDocument(event_sender, args.PDFDocument)
                );
            }
        }

        public void ResetFilters(bool reset_explorers = true)
        {
            library_filter_control_search.ResetFilters();

            SearchTag.Clear();

            if (reset_explorers)
            {
                ObjTagExplorerControl.Reset();
                ObjAITagExplorerControl.Reset();
                ObjAuthorExplorerControl.Reset();
                ObjPublicationExplorerControl.Reset();
                ObjReadingStageExplorerControl.Reset();
                ObjYearExplorerControl.Reset();
                ObjRatingExplorerControl.Reset();
                ObjThemeExplorerControl.Reset();
                ObjTypeExplorerControl.Reset();
            }

            ReviewParameters();
        }
        
        public void SearchLibrary(string query)
        {
            ResetFilters();

            library_filter_control_search.SearchQuick.Text = query;
            library_filter_control_search.ExecuteSearchQuick();
            ReviewParameters();
        }

        void ObjLibraryFilterControl_Sort_SortChanged()
        {
            this.ReviewParameters();
        }

        #region --- Automated update to match Library ------------------------------------------------------------------------------------------------------------------------

        void Library_OnNewDocument(object sender, PDFDocument pdf_document)
        {
            Dispatcher.BeginInvoke(new Action(() => ReExecuteAllSearches(pdf_document)));
        }

        void ReExecuteAllSearches(PDFDocument pdf_document_to_focus_on)
        {
            library_filter_control_search.ExecuteSearchQuick();
            ExecuteSearchTag();
            ReviewParameters(pdf_document_to_focus_on);
        }

        #endregion ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        void ObjTagExplorerControl_OnTagSelectionChanged(HashSet<string> fingerprints, Span descriptive_span)
        {
            if (null == fingerprints || 0 == fingerprints.Count)
            {
                select_tag_fingerprints = null;
                select_tag_fingerprints_span = null;
            }
            else
            {
                FeatureTrackingManager.Instance.UseFeature(Features.Library_TagExplorer);
                select_tag_fingerprints = fingerprints;
                select_tag_fingerprints_span = descriptive_span;
            }

            ReviewParameters();
        }

        void ObjAITagExplorerControl_OnTagSelectionChanged(HashSet<string> fingerprints, Span descriptive_span)
        {
            if (null == fingerprints || 0 == fingerprints.Count)
            {
                select_ai_tag_fingerprints = null;
                select_ai_tag_fingerprints_span = null;
            }
            else
            {
                FeatureTrackingManager.Instance.UseFeature(Features.Library_AITagExplorer);
                select_ai_tag_fingerprints = fingerprints;
                select_ai_tag_fingerprints_span = descriptive_span;
            }

            ReviewParameters();
        }

        void ObjAuthorExplorerControl_OnTagSelectionChanged(HashSet<string> fingerprints, Span descriptive_span)
        {
            if (null == fingerprints || 0 == fingerprints.Count)
            {
                select_author_fingerprints = null;
                select_author_fingerprints_span = null;
            }
            else
            {
                FeatureTrackingManager.Instance.UseFeature(Features.Library_AuthorExplorer);
                select_author_fingerprints = fingerprints;
                select_author_fingerprints_span = descriptive_span;
            }

            ReviewParameters();
        }

        void ObjPublicationExplorerControl_OnTagSelectionChanged(HashSet<string> fingerprints, Span descriptive_span)
        {
            if (null == fingerprints || 0 == fingerprints.Count)
            {
                select_publication_fingerprints = null;
                select_publication_fingerprints_span = null;
            }
            else
            {
                FeatureTrackingManager.Instance.UseFeature(Features.Library_PublicationExplorer);
                select_publication_fingerprints = fingerprints;
                select_publication_fingerprints_span = descriptive_span;
            }

            ReviewParameters();
        }

        void ObjReadingStageExplorerControl_OnTagSelectionChanged(HashSet<string> fingerprints, Span descriptive_span)
        {
            if (null == fingerprints || 0 == fingerprints.Count)
            {
                select_reading_stage_fingerprints = null;
                select_reading_stage_fingerprints_span = null;
            }
            else
            {
                FeatureTrackingManager.Instance.UseFeature(Features.Library_ReadingStageExplorer);
                select_reading_stage_fingerprints = fingerprints;
                select_reading_stage_fingerprints_span = descriptive_span;
            }

            ReviewParameters();
        }

        void ObjYearExplorerControl_OnTagSelectionChanged(HashSet<string> fingerprints, Span descriptive_span)
        {
            if (null == fingerprints || 0 == fingerprints.Count)
            {
                select_year_fingerprints = null;
                select_year_fingerprints_span = null;
            }
            else
            {
                FeatureTrackingManager.Instance.UseFeature(Features.Library_YearExplorer);
                select_year_fingerprints = fingerprints;
                select_year_fingerprints_span = descriptive_span;
            }

            ReviewParameters();
        }

        void ObjRatingExplorerControl_OnTagSelectionChanged(HashSet<string> fingerprints, Span descriptive_span)
        {
            if (null == fingerprints || 0 == fingerprints.Count)
            {
                select_rating_fingerprints = null;
                select_rating_fingerprints_span = null;
            }
            else
            {
                FeatureTrackingManager.Instance.UseFeature(Features.Library_RatingExplorer);
                select_rating_fingerprints = fingerprints;
                select_rating_fingerprints_span = descriptive_span;
            }

            ReviewParameters();
        }

        void ObjThemeExplorerControl_OnTagSelectionChanged(HashSet<string> fingerprints, Span descriptive_span)
        {
            if (null == fingerprints || 0 == fingerprints.Count)
            {
                select_theme_fingerprints = null;
                select_theme_fingerprints_span = null;
            }
            else
            {
                FeatureTrackingManager.Instance.UseFeature(Features.Library_ThemeExplorer);
                select_theme_fingerprints = fingerprints;
                select_theme_fingerprints_span = descriptive_span;
            }

            ReviewParameters();
        }

        void ObjTypeExplorerControl_OnTagSelectionChanged(HashSet<string> fingerprints, Span descriptive_span)
        {
            if (null == fingerprints || 0 == fingerprints.Count)
            {
                select_theme_fingerprints = null;
                select_theme_fingerprints_span = null;
            }
            else
            {
                FeatureTrackingManager.Instance.UseFeature(Features.Library_TypeExplorer);
                select_theme_fingerprints = fingerprints;
                select_theme_fingerprints_span = descriptive_span;
            }

            ReviewParameters();
        }

        

        void SearchTag_OnSoftSearch()
        {
            FeatureTrackingManager.Instance.UseFeature(Features.Library_TagFilter);

            ExecuteSearchTag();
            ReviewParameters();            
        }

        private void ExecuteSearchTag()
        {
            string terms = SearchTag.Text;

            if (String.IsNullOrEmpty(terms))
            {
                search_tag_fingerprints = null;
            }
            else
            {
                search_tag_fingerprints = new HashSet<string>();

                string[] search_tags = terms.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);

                foreach (var pdf_document in library.PDFDocuments)
                {
                    bool has_all_tags = true;
                    foreach (string search_tag in search_tags)
                    {
                        if (!pdf_document.Tags.Contains(search_tag))
                        {
                            has_all_tags = false;
                            break;
                        }
                    }

                    if (has_all_tags)
                    {
                        search_tag_fingerprints.Add(pdf_document.Fingerprint);
                    }
                }

                // Create the feedback
                search_tag_fingerprints_span = new Span();
                Bold bold = new Bold();
                bold.Inlines.Add("TagSearch");
                search_tag_fingerprints_span.Inlines.Add(bold);
                search_tag_fingerprints_span.Inlines.Add(": ");
                search_tag_fingerprints_span.Inlines.Add("'");
                search_tag_fingerprints_span.Inlines.Add(terms);
                search_tag_fingerprints_span.Inlines.Add("'");
                search_tag_fingerprints_span.Inlines.Add(LibraryFilterHelpers.GetClearImageInline("Clear this filter.", hyperlink_search_tag_fingerprints_span_OnClick));
            }
        }

        void hyperlink_search_tag_fingerprints_span_OnClick(object sender, MouseButtonEventArgs e)
        {
            SearchTag.Clear();
        }

        internal void ReviewParameters()
        {
            ReviewParameters(null);
        }
        
        private void ReviewParameters(PDFDocument pdf_document_to_focus_on)
        {
            HashSet<string> intersection = null;
            intersection = SetTools.FoldInSet_Intersection(intersection, search_quick_fingerprints);
            intersection = SetTools.FoldInSet_Intersection(intersection, search_tag_fingerprints);
            intersection = SetTools.FoldInSet_Intersection(intersection, select_tag_fingerprints);
            intersection = SetTools.FoldInSet_Intersection(intersection, select_ai_tag_fingerprints);
            intersection = SetTools.FoldInSet_Intersection(intersection, select_author_fingerprints);
            intersection = SetTools.FoldInSet_Intersection(intersection, select_publication_fingerprints);
            intersection = SetTools.FoldInSet_Intersection(intersection, select_reading_stage_fingerprints);
            intersection = SetTools.FoldInSet_Intersection(intersection, select_year_fingerprints);
            intersection = SetTools.FoldInSet_Intersection(intersection, select_rating_fingerprints);
            intersection = SetTools.FoldInSet_Intersection(intersection, select_theme_fingerprints);

            Span descriptive_span = new Span();
            int colour_pick = 0;
            if (null != search_quick_fingerprints)
            {
                Logging.Info("search_quick_fingerprints={0}", search_quick_fingerprints.Count);
                descriptive_span.Inlines.Add(search_quick_fingerprints_span);
                descriptive_span.Inlines.Add("   ");
                search_quick_fingerprints_span.Background = new SolidColorBrush(ColorTools.GetTransparentRainbowColour(colour_pick++, 128));
            }
            if (null != search_tag_fingerprints)
            {
                Logging.Info("search_tag_fingerprints={0}", search_tag_fingerprints.Count);
                descriptive_span.Inlines.Add(search_tag_fingerprints_span);
                descriptive_span.Inlines.Add("   ");
                search_tag_fingerprints_span.Background = new SolidColorBrush(ColorTools.GetTransparentRainbowColour(colour_pick++, 128));
            }
            if (null != select_tag_fingerprints)
            {
                Logging.Info("select_tag_fingerprints={0}", select_tag_fingerprints.Count);
                descriptive_span.Inlines.Add(select_tag_fingerprints_span);
                descriptive_span.Inlines.Add("   ");
                select_tag_fingerprints_span.Background = new SolidColorBrush(ColorTools.GetTransparentRainbowColour(colour_pick++, 128));
            }
            if (null != select_ai_tag_fingerprints)
            {
                Logging.Info("select_ai_tag_fingerprints={0}", select_ai_tag_fingerprints.Count);
                descriptive_span.Inlines.Add(select_ai_tag_fingerprints_span);
                descriptive_span.Inlines.Add("   ");
                select_ai_tag_fingerprints_span.Background = new SolidColorBrush(ColorTools.GetTransparentRainbowColour(colour_pick++, 128));
            }
            if (null != select_author_fingerprints)
            {
                Logging.Info("select_author_fingerprints={0}", select_author_fingerprints.Count);
                descriptive_span.Inlines.Add(select_author_fingerprints_span);
                descriptive_span.Inlines.Add("   ");
                select_author_fingerprints_span.Background = new SolidColorBrush(ColorTools.GetTransparentRainbowColour(colour_pick++, 128));
            }
            if (null != select_publication_fingerprints)
            {
                Logging.Info("select_publication_fingerprints={0}", select_publication_fingerprints.Count);
                descriptive_span.Inlines.Add(select_publication_fingerprints_span);
                descriptive_span.Inlines.Add("   ");
                select_publication_fingerprints_span.Background = new SolidColorBrush(ColorTools.GetTransparentRainbowColour(colour_pick++, 128));
            }
            if (null != select_reading_stage_fingerprints)
            {
                Logging.Info("select_reading_stage_fingerprints={0}", select_reading_stage_fingerprints.Count);
                descriptive_span.Inlines.Add(select_reading_stage_fingerprints_span);
                descriptive_span.Inlines.Add("   ");
                select_reading_stage_fingerprints_span.Background = new SolidColorBrush(ColorTools.GetTransparentRainbowColour(colour_pick++, 128));
            }
            if (null != select_year_fingerprints)
            {
                Logging.Info("select_year_fingerprints={0}", select_year_fingerprints.Count);
                descriptive_span.Inlines.Add(select_year_fingerprints_span);
                descriptive_span.Inlines.Add("   ");
                select_year_fingerprints_span.Background = new SolidColorBrush(ColorTools.GetTransparentRainbowColour(colour_pick++, 128));
            }
            if (null != select_rating_fingerprints)
            {
                Logging.Info("select_rating_fingerprints={0}", select_rating_fingerprints.Count);
                descriptive_span.Inlines.Add(select_rating_fingerprints_span);
                descriptive_span.Inlines.Add("   ");
                select_rating_fingerprints_span.Background = new SolidColorBrush(ColorTools.GetTransparentRainbowColour(colour_pick++, 128));
            }
            if (null != select_theme_fingerprints)
            {
                Logging.Info("select_theme_fingerprints={0}", select_theme_fingerprints.Count);
                descriptive_span.Inlines.Add(select_theme_fingerprints_span);
                descriptive_span.Inlines.Add("   ");
                select_theme_fingerprints_span.Background = new SolidColorBrush(ColorTools.GetTransparentRainbowColour(colour_pick++, 128));
            }
            if (null != intersection)
            {
                Logging.Info("intersection={0}", intersection.Count);
            }

            // If we have nothing good to say, say nothing at all
            if (0 == descriptive_span.Inlines.Count)
            {
                descriptive_span = null;
            }

            List<PDFDocument> pdf_documents = null;
            // If there are no filters, use all document            
            if (null == intersection)
            {
                pdf_documents = library.PDFDocuments;
            }
            
            else // Otherwise get the subset of documents
            {
                pdf_documents = library.GetDocumentByFingerprints(intersection);
            }

            ObjLibraryFilterControl_Sort.ApplySort(pdf_documents, search_quick_scores);

            // Call the event
            if (null != OnFilterChanged)
            {
                OnFilterChanged(this, pdf_documents, descriptive_span, search_quick_query, search_quick_scores, pdf_document_to_focus_on);
            }
        }

        public static void Test()
        {
            LibraryFilterControl l = new LibraryFilterControl();
            ControlHostingWindow window = new ControlHostingWindow("Library index", l);
            window.Show();
        }
    }
}
