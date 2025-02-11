﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using icons;
using Qiqqa.Common.Configuration;
using Qiqqa.Common.GUI;
using Qiqqa.Common.WebcastStuff;
using Qiqqa.DocumentLibrary;
using Qiqqa.DocumentLibrary.LibraryCatalog;
using Qiqqa.Documents.PDF.Search;
using Qiqqa.Localisation;
using Qiqqa.UtilisationTracking;
using Qiqqa.WebBrowsing;
using Utilities;
using Utilities.BibTex;
using Utilities.BibTex.Parsing;
using Utilities.GUI;
using Utilities.Internet.GoogleScholar;
using Utilities.Language;
using Utilities.Misc;
using Utilities.Random;
using Qiqqa.Documents.PDF.MetadataSuggestions;
using Utilities.Reflection;

namespace Qiqqa.Documents.PDF.PDFControls.MetadataControls
{
    /// <summary>
    /// Interaction logic for GoogleBibTexSnifferControl.xaml
    /// </summary>
    public partial class GoogleBibTexSnifferControl : StandardWindow
    {
        class SearchOptions
        {
#pragma warning disable 0649
            public bool Missing { get; set; }
            public bool Skipped { get; set; }
            public bool Auto { get; set; }
            public bool Manual { get; set; }
#pragma warning restore 0649

            public SearchOptions()
            {
                Missing = true;
            }
        }

        private SearchOptions search_options;
        private AugmentedBindable<SearchOptions> search_options_bindable;

        PDFDocument user_specified_pdf_document = null;
        List<PDFDocument> pdf_documents_total_pool = new List<PDFDocument>();
        List<PDFDocument> pdf_documents_search_pool = new List<PDFDocument>();
        int pdf_documents_search_index = 0;

        PDFDocument pdf_document;

        PDFDocument pdf_document_rendered = null;
        PDFRendererControl pdf_renderer_control;

        string last_autonavigated_url;

        public GoogleBibTexSnifferControl()
        {
            InitializeComponent();

            // Search options
            search_options = new SearchOptions();
            search_options_bindable = new AugmentedBindable<SearchOptions>(search_options);
            ObjSearchOptionsPanel.DataContext = search_options_bindable;
            search_options_bindable.PropertyChanged += search_options_bindable_PropertyChanged;

            // Fades of buttons
            Utilities.GUI.Animation.Animations.EnableHoverFade(PDFRendererControlAreaButtonPanel);
            Utilities.GUI.Animation.Animations.EnableHoverFade(ObjBibTeXEditButtonPanel);

            this.Title = "Qiqqa BibTeX Sniffer";

            this.Closing += GoogleBibTexSnifferControl_Closing;
            this.Closed += GoogleBibTexSnifferControl_Closed;

            this.KeyUp += GoogleBibTexSnifferControl_KeyUp;

            ButtonPrev.Icon = Icons.GetAppIcon(Icons.Back);
            ButtonPrev.ToolTip = "Move to previous PDF.";
            ButtonPrev.Click += ButtonPrev_Click;

            ButtonNext.Icon = Icons.GetAppIcon(Icons.Forward);
            ButtonNext.ToolTip = "Move to next PDF.  You can press the middle key (the 5 key) as a shortcut.";
            ButtonNext.Click += ButtonNext_Click;

            ButtonClear.Icon = Icons.GetAppIcon(Icons.GoogleBibTexSkipForever);
            ButtonClear.ToolTip = "Clear this BibTeX.";
            ButtonClear.Click += ButtonClear_Click;

            ButtonSkipForever.Icon = Icons.GetAppIcon(Icons.GoogleBibTexSkip);
            ButtonSkipForever.ToolTip = "This document has no BibTeX.  Skip it!";
            ButtonSkipForever.Click += ButtonSkipForever_Click;

            ButtonValidate.Icon = Icons.GetAppIcon(Icons.GoogleBibTexNext);
            ButtonValidate.ToolTip = "The automatic BibTeX for this document is great.  Mark it as valid!";
            ButtonValidate.Click += ButtonValidate_Click;

            ButtonConfig.Icon = Icons.GetAppIcon(Icons.DocumentMisc);
            ButtonConfig.ToolTip = LocalisationManager.Get("PDF/TIP/MORE_MENUS");
            ButtonConfig.Click += ButtonConfig_Click;

            ButtonRedo.Icon = Icons.GetAppIcon(Icons.DesktopRefresh);
            ButtonRedo.ToolTip = "Retry detection of this PDF.";
            ButtonRedo.Click += ButtonRedo_Click;

            ButtonWizard.Icon = Icons.GetAppIcon(Icons.BibTeXSnifferWizard);
            ButtonWizard.ToolTip = "Toggle the BibTeX Sniffer Wizard.\nWhen this is enabled, the sniffer will automatically browse to the first item it sees in Google Scholar.\nThis saves you time because you just have to scan that the BibTeX is correct before moving onto your next paper!";
            ButtonWizard.DataContext = ConfigurationManager.Instance.ConfigurationRecord_Bindable;

            ObjWebBrowser.GoBibTeXMode();
            ObjWebBrowser.PageLoaded += ObjWebBrowser_PageLoaded;
            ObjWebBrowser.TabChanged += ObjWebBrowser_TabChanged;

            PDFRendererControlArea.ToolTip = "This is the current PDF that has no BibTeX associated with it.  You can select text from the PDF to automatically search for that text.";
            ObjWebBrowser.ToolTip = "Use this browser to hunt for BibTeX of PubMed XML.  As soon as you find some, it will automatically be associated with your PDFF.";
            TxtBibTeX.ToolTip = "This is the BibTeX that is currently associated with the displayed PDF.\nFeel free to edit this or replace it with a # if there is no BibTeX for this record and you do not want the Sniffer to keep prompting you for some...";

            HyperlinkBibTeXLinksMissing.Click += HyperlinkBibTeXLinksMissing_Click;

            Webcasts.FormatWebcastButton(ButtonWebcast, Webcasts.BIBTEX_SNIFFER);

            // Set dimensions
            {
                this.Width = 800;
                this.Height = 600;

                // Be a little larger if possible
                if (SystemParameters.FullPrimaryScreenWidth > 1024)
                {
                    this.Height = 1024;
                }
                if (SystemParameters.FullPrimaryScreenHeight > 1024)
                {
                    this.Height = 1024;
                }
            }

            TxtBibTeX.TextChanged += TxtBibTeX_TextChanged;

            // Navigate to GS in a bid to not have the first .bib prompt for download
            ObjWebBrowser.ForceAdvancedMenus();
            ObjWebBrowser.ForceSnifferSearchers();
            ObjWebBrowser.DefaultWebSearcherKey = WebSearchers.SCHOLAR_KEY;
        }

        void GoogleBibTexSnifferControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (false) { }
            else if (Key.Clear == e.Key)
            {
                MoveDelta(+1);
                e.Handled = true;
            }

            else if (Key.Escape == e.Key)
            {
                this.Close();
                e.Handled = true;
            }
        }        

        void TxtBibTeX_TextChanged(object sender, TextChangedEventArgs e)
        {
            TxtBibTeX.Background = null;

            if (null == pdf_document) return;

            try
            {
                string bibtex = pdf_document.BibTex;

                PDFSearchResultSet search_result_set;
                if (BibTeXGoodnessOfFitEstimator.DoesBibTeXMatchDocument(bibtex, pdf_document, out search_result_set))
                {
                    TxtBibTeX.Background = Brushes.LightGreen;
                    pdf_renderer_control.SetSearchKeywords(search_result_set);

                    // If we are feeling really racy, let the wizard button also move onto the next guy cos we are cooking on GAS
                    if (ConfigurationManager.Instance.ConfigurationRecord.Metadata_UseBibTeXSnifferWizard)
                    {
                        if (!pdf_document.BibTex.Contains(BibTeXActionComments.AUTO_GS))
                        {
                            pdf_document.BibTex =
                                BibTeXActionComments.AUTO_GS
                                + "\r\n"
                                + pdf_document.BibTex;
                            pdf_document.Bindable.NotifyPropertyChanged(() => pdf_document.BibTex);
                        }

                        MoveDelta(+1);
                    }
                }
            }
            catch (Exception) {}
        }

        void HyperlinkBibTeXLinksMissing_Click(object sender, RoutedEventArgs e)
        {
            string message = 
                ""
                + "If you are not seeing an \"Import into BibTeX\" link below each search result, it means that you have not yet enabled BibTeX support in Google Scholar.\n\n" 
                + "If you want Qiqqa to attempt to take you to the Google Scholar settings screen, press YES and then press 'Save Preferences' when you have reviewed all the available preferences.\n\n"
                + "If you would prefer to do it yourself, press NO.  Go to the main Google Scholar page (http://scholar.google.com) USING THE QIQQA BUILT-IN BROWSER, and open the Google Scholar Settings.  Make sure you are on the .com version of Scholar, by looking at the web address.  If the address does not end in \".com\", then look for a link in the bottom right called \"Go to Google Scholar\" which should take you there.   Once on the .com Scholar settings page, in the 'Bibliography Manager' section, select 'Show links to import citations into BibTeX' and then press 'Save'.";

            if (MessageBoxes.AskQuestion(message))
            {
                string preferences_url = "http://scholar.google.com/scholar_setprefs?scis=yes&scisf=4";
                ObjWebBrowser.OpenUrl(preferences_url);
            }

            e.Handled = true;
        }

        void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            MoveDelta(+1);
        }
        void ButtonPrev_Click(object sender, RoutedEventArgs e)
        {
            MoveDelta(-1);
        }

        private void MoveFirst()
        {
            if (0 < pdf_documents_search_pool.Count)
            {
                pdf_documents_search_index = 0;
                pdf_document = pdf_documents_search_pool[pdf_documents_search_index];
            }
            else
            {
                pdf_documents_search_index = 0;
                pdf_document = null;
            }

            ReflectPDFDocument(null);
        }

        private void MoveDelta(int direction)
        {
            if (0 < pdf_documents_search_pool.Count)
            {
                pdf_documents_search_index += direction;
                if (pdf_documents_search_index >= pdf_documents_search_pool.Count) pdf_documents_search_index = 0;
                if (pdf_documents_search_index < 0) pdf_documents_search_index = pdf_documents_search_pool.Count-1;
                pdf_document = pdf_documents_search_pool[pdf_documents_search_index];
            }
            else
            {
                pdf_documents_search_index = 0;
                pdf_document = null;
            }

            ReflectPDFDocument(null);
        }        

        private void ButtonConfig_Click(object sender, RoutedEventArgs e)
        {
            if (null != pdf_document)
            {
                LibraryCatalogPopup popup = new LibraryCatalogPopup(new List<PDFDocument> {pdf_document});
                popup.Open();
                e.Handled = true;
            }
        }

        void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            if (null != pdf_document)
            {
                pdf_document.BibTex = "";
                pdf_document.Bindable.NotifyPropertyChanged(() => pdf_document.BibTex);
            }
        }


        void ButtonSkipForever_Click(object sender, RoutedEventArgs e)
        {
            if (null != pdf_document)
            {
                pdf_document.BibTex = BibTeXActionComments.SKIP;
                pdf_document.Bindable.NotifyPropertyChanged(() => pdf_document.BibTex);
            }

            MoveDelta(+1);
        }

        void ButtonValidate_Click(object sender, RoutedEventArgs e)
        {
            if (null != pdf_document && null != pdf_document.BibTex)
            {
                pdf_document.BibTex = pdf_document.BibTex.Replace(BibTeXActionComments.AUTO_GS, "");
                pdf_document.BibTex = pdf_document.BibTex.Replace(BibTeXActionComments.AUTO_BIBTEXSEARCH, "");
                pdf_document.BibTex = pdf_document.BibTex.Trim();
                pdf_document.Bindable.NotifyPropertyChanged(() => pdf_document.BibTex);
            }

            MoveDelta(+1);
        }

        private new void Show()
        {
            // We don't want outsiders to be able to use this without supplying a PDFDocument or a Library...
        }


        public void Show(PDFDocument pdf_document)
        {            
            Show(pdf_document, null);
        }

        public void Show(PDFDocument pdf_document, string search_terms)
        {
            Show(new List<PDFDocument> { pdf_document }, pdf_document, search_terms);
        }

        public void Show(List<PDFDocument> pdf_documents)
        {
            Show(pdf_documents, null, null);
        }

        public void Show(List<PDFDocument> pdf_documents, PDFDocument user_specified_pdf_document, string search_terms)
        {
            this.user_specified_pdf_document = user_specified_pdf_document;
            this.pdf_documents_total_pool.Clear();
            this.pdf_documents_total_pool.AddRange(pdf_documents);
            RecalculateSearchPool();

            base.Show();
        }

        void search_options_bindable_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RecalculateSearchPool();
        }

        private void RecalculateSearchPool()
        {
            pdf_documents_search_pool.Clear();
            foreach (PDFDocument pdf_document in pdf_documents_total_pool)
            {
                bool include_in_search_pool = false;

                if (pdf_document.Deleted) continue;

                if (search_options.Missing && String.IsNullOrEmpty(pdf_document.BibTex)) include_in_search_pool = true;

                if (!String.IsNullOrEmpty(pdf_document.BibTex))
                {
                    if (search_options.Auto && pdf_document.BibTex.Contains(BibTeXActionComments.AUTO_BIBTEXSEARCH)) include_in_search_pool = true;
                    if (search_options.Auto && pdf_document.BibTex.Contains(BibTeXActionComments.AUTO_GS)) include_in_search_pool = true;
                    if (search_options.Skipped && pdf_document.BibTex.Contains(BibTeXActionComments.SKIP)) include_in_search_pool = true;
                    if (search_options.Manual && !pdf_document.BibTex.Contains(BibTeXActionComments.AUTO_BIBTEXSEARCH) && !pdf_document.BibTex.Contains(BibTeXActionComments.AUTO_GS)) include_in_search_pool = true;
                }

                if (pdf_document == user_specified_pdf_document || include_in_search_pool && pdf_document.DocumentExists)
                {
                    pdf_documents_search_pool.Add(pdf_document);
                }
            }

            MoveFirst();            
        }

        void ButtonRedo_Click(object sender, RoutedEventArgs e)
        {
            ReflectPDFDocument(null);
        }

        private void ReflectPDFDocument(string search_terms)
        {
            if (0 < pdf_documents_search_pool.Count)
            {
                TxtProgress.Text = String.Format("Document {0} of {1}.", pdf_documents_search_index + 1, pdf_documents_search_pool.Count);
                ObjProgress.Value = pdf_documents_search_index + 1;
                ObjProgress.Maximum = pdf_documents_search_pool.Count;
            }
            else
            {
                TxtProgress.Text = "No documents";
                ObjProgress.Value = 1;
                ObjProgress.Maximum = 1;
            }

            if (null != this.pdf_document_rendered)
            {
                // Clear down the previous renderer control
                PDFRendererControlArea.Children.Clear();

                if (null != this.pdf_renderer_control)
                {
                    this.pdf_renderer_control.Dispose();
                    this.pdf_renderer_control = null;
                }

                this.pdf_document_rendered = null;
                this.DataContext = null;
            }

            if (null != pdf_document)
            {
                // Force inference of the title in case it has not been populated...
                PDFMetadataInferenceFromOCR.InferTitleFromOCR(pdf_document, true);

                this.pdf_document_rendered = pdf_document;
                this.DataContext = pdf_document.Bindable;                

                if (pdf_document.DocumentExists)
                {
                    ObjNoPDFAvailableMessage.Visibility = Visibility.Collapsed;
                    PDFRendererControlArea.Visibility = Visibility.Visible;

                    // Make sure the first page is OCRed...
                    pdf_document.PDFRenderer.GetOCRText(1);

                    // Set up the new renderer control                
                    this.pdf_renderer_control = new PDFRendererControl(this.pdf_document, false, PDFRendererControl.ZoomType.Zoom1Up);
                    this.pdf_renderer_control.ReconsiderOperationMode(PDFRendererControl.OperationMode.TextSentenceSelect);
                    this.pdf_renderer_control.TextSelected += pdf_renderer_control_TextSelected;
                    PDFRendererControlArea.Children.Add(pdf_renderer_control);
                }
                else
                {
                    ObjNoPDFAvailableMessage.Visibility = Visibility.Visible;
                    PDFRendererControlArea.Visibility = Visibility.Collapsed;
                }

                // Make sure we have something to search for
                if (String.IsNullOrEmpty(search_terms))                
                {
                    string title_combined = pdf_document.TitleCombined;
                    if (PDFDocument.TITLE_UNKNOWN != title_combined && pdf_document.DownloadLocation != title_combined)
                    {
                        search_terms = pdf_document.TitleCombined;
                    }
                }

                // Kick off the search
                if (!String.IsNullOrEmpty(search_terms))
                {   
                    ObjWebBrowser.DoWebSearch(search_terms);
                }
            }
        }

        void pdf_renderer_control_TextSelected(string selected_text)
        {
            if (null != selected_text)
            {
                ObjWebBrowser.DoWebSearch(selected_text);
            }
        }

        void GoogleBibTexSnifferControl_Closing(object sender, CancelEventArgs e)
        {
            ObjWebBrowser.PageLoaded -= ObjWebBrowser_PageLoaded;
            ObjWebBrowser.TabChanged -= ObjWebBrowser_TabChanged; 
        }
        
        void GoogleBibTexSnifferControl_Closed(object sender, EventArgs e)
        {
            ObjWebBrowser.Dispose();
        }
        
        void ObjWebBrowser_PageLoaded()
        {
            ReflectLatestBrowserContent();
        }

        void ObjWebBrowser_TabChanged()
        {
            ReflectLatestBrowserContent();
        }

        private void ReflectLatestBrowserContent()
        {
            try
            {
                // Neaten the text in the browser
                string text = ObjWebBrowser.CurrentPageText;

                if (null == text)
                {
                    return;
                }

                // Process
                text = text.Trim();

                // If this is valid BibTeX, offer it
                {
                    if (IsValidBibTex(text))
                    {
                        FeatureTrackingManager.Instance.UseFeature(Features.MetadataSniffer_ValidBibTeX);
                        UseAsBibTeX(text);
                        
                        return;
                    }
                }

                // If this is valid PubMed XML, offer it
                {
                    string converted_bibtex;
                    List<string> messages;
                    bool success = PubMedXMLToBibTex.TryConvert(text, out converted_bibtex, out messages);
                    if (success)
                    {
                        FeatureTrackingManager.Instance.UseFeature(Features.MetadataSniffer_ValidPubMed);
                        UseAsBibTeX(converted_bibtex);
                        return;
                    }
                    else
                    {
                        if (0 < messages.Count)
                        {
                            foreach (string message in messages)
                            {
                                Logging.Info(message);
                            }
                        }
                    }
                }

                // Otherwise lets try parse the page cos it might be a google scholar page and if so we are going to want to try to get the first link to BibTeX
                if (ConfigurationManager.Instance.ConfigurationRecord.Metadata_UseBibTeXSnifferWizard)
                {
                    // Only do this automatically if there is not already bibtex in the record
                    if (null != pdf_document && String.IsNullOrEmpty(pdf_document.BibTex))
                    { 
                        string url = ObjWebBrowser.CurrentUri.ToString();
                        string html = ObjWebBrowser.CurrentPageHTML;
                        List<GoogleScholarScrapePaper> gssps = GoogleScholarScraper.ScrapeHtml(html, url);                    
                    
                        try
                        {
                            // Try to process the first bibtex record
                            if (0 < gssps.Count)
                            {
                                GoogleScholarScrapePaper gssp = gssps[0];
                                if (!String.IsNullOrEmpty(gssp.bibtex_url))
                                {
                                    if (last_autonavigated_url != gssp.bibtex_url)
                                    {
                                        last_autonavigated_url = gssp.bibtex_url;

                                        gssp.bibtex_url = gssp.bibtex_url.Replace("&amp;", "&");
                                        ObjWebBrowser.OpenUrl(gssp.bibtex_url);
                                    }
                                }
                            }

                        }

                        catch (Exception ex)
                        {
                            Logging.Warn(ex, "Sniffer was not able to parse the results that came back from GS.");
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                Logging.Error(ex, "There was an exception while trying to parse the html back from Google Scholar");
            }
        }

        DateTime bibtexsearch_backoff_timestamp = DateTime.MinValue;
        string last_posted_bibtex = null;

        private void PostBibTeXToAggregator(string bibtex)
        {
            if (last_posted_bibtex == bibtex) return;
            if (bibtexsearch_backoff_timestamp > DateTime.UtcNow) return;

            // Post the bibtex to bibtexsearch.com
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(bibtex);

                HttpWebRequest web_request = (HttpWebRequest)HttpWebRequest.Create("http://submit.bibtexsearch.com:80/submit");
                web_request.Proxy = ConfigurationManager.Instance.Proxy;
                web_request.Method = "POST";
                web_request.ContentLength = buffer.Length;
                web_request.ContentType = "text/plain; charset=utf-8";
                using (Stream stream = web_request.GetRequestStream())
                {
                    stream.Write(buffer, 0, buffer.Length);
                }

                using (web_request.GetResponse())
                {
                }

                last_posted_bibtex = bibtex;

                //using (Stream stream = web_response.GetResponseStream())
                //{
                //    using (StreamReader stream_reader = new StreamReader(stream, Encoding.UTF8))
                //    {
                //        Logging.Info("bibtexsearch.com says:\n{0}", stream_reader.ReadToEnd());
                //    }
                //}
            }
            catch (Exception ex)
            {
                Logging.Warn(ex, "Problem with bibtexsearch.com");
                bibtexsearch_backoff_timestamp = DateTime.UtcNow.AddHours(1);
            }
        }
        
        private void UseAsBibTeX(string text)
        {
            SafeThreadPool.QueueUserWorkItem(o => PostBibTeXToAggregator(text));

            if (null != pdf_document)
            {
                pdf_document.BibTex = text;
                pdf_document.Bindable.NotifyPropertyChanged(() => pdf_document.BibTex);
            }
            else
            {
                MessageBoxes.Error("Please first select a PDF in your library before trying to search for BibTeX.");
            }
        }

        private static bool IsValidBibTex(string text)
        {
            return (text.StartsWith("@") && text.EndsWith("}"));
        }

        public static void TestHarness()
        {
            Library library = Library.GuestInstance;
            Thread.Sleep(1000);

            GoogleBibTexSnifferControl c = new GoogleBibTexSnifferControl();
            PDFDocument pdf_document = library.PDFDocuments[0];
            //c.Show(pdf_document.Library, pdf_document, "test search term");
            //c.Show(pdf_document.Library, pdf_document);
            c.Show(pdf_document.Library.PDFDocuments);
        }
    }
}
