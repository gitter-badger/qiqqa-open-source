﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Utilities;
using Utilities.Files;
using Utilities.Misc;
using Utilities.OCR;
using Utilities.PDF.Sorax;

namespace Qiqqa.Documents.PDF.PDFRendering
{
    public class PDFRenderer
    {
        private static readonly int TEXT_PAGES_PER_GROUP = 20;

        string pdf_filename;
        string pdf_user_password;
        string pdf_owner_password;
        string document_fingerprint;

        PDFRendererFileLayer pdf_render_file_layer;        
        Dictionary<int, TypedWeakReference<WordList>> texts = new Dictionary<int, TypedWeakReference<WordList>>();

        object pages_to_render_lock = new object();
        List<int> pages_to_render = new List<int>();

        public delegate void OnPageTextAvailableDelegate(int page_from, int page_to);
        public event OnPageTextAvailableDelegate OnPageTextAvailable;

        SoraxPDFRenderer sorax_pdf_renderer;

        public PDFRenderer(string pdf_filename, string pdf_user_password, string pdf_owner_password) 
            : this(null, pdf_filename, pdf_user_password, pdf_owner_password)
        {
        }

        public PDFRenderer(string precomputed_document_fingerprint, string pdf_filename, string pdf_user_password, string pdf_owner_password)
        {
            this.pdf_filename = pdf_filename;
            this.pdf_user_password = pdf_user_password;
            this.pdf_owner_password = pdf_owner_password;
            this.document_fingerprint = precomputed_document_fingerprint ?? StreamFingerprint.FromFile(this.pdf_filename);

            pdf_render_file_layer = new PDFRendererFileLayer(this.document_fingerprint, pdf_filename);
            sorax_pdf_renderer = new SoraxPDFRenderer(pdf_filename, pdf_user_password, pdf_owner_password);
        }

        public string PDFFilename
        {
            get
            {
                return pdf_filename;
            }
        }

        public string PDFUserPassword
        {
            get
            {
                return pdf_user_password;
            }
        }

        public PDFRendererFileLayer PDFRendererFileLayer
        {
            get
            {
                return pdf_render_file_layer;
            }
        }

        /// <summary>
        /// NB: 1 based offset
        /// </summary>
        public int PageCount
        {
            get
            {
                return pdf_render_file_layer.PageCount;
            }
        }

        public string DocumentFingerprint
        {
            get
            {
                return document_fingerprint;
            }
        }

        public override string ToString()
        {
            return string.Format(
                "{0}T/{1} - {2}",                
                texts.Count,
                PageCount,
                document_fingerprint
            );
        }

        /// <summary>
        /// Page is 1 based
        /// </summary>
        /// <param name="page"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        internal byte[] GetPageByHeightAsImage(int page, double height)
        {
            return sorax_pdf_renderer.GetPageByHeightAsImage(page, height);
        }

        internal byte[] GetPageByDPIAsImage(int page, float dpi)
        {
            return sorax_pdf_renderer.GetPageByDPIAsImage(page, dpi);
        }
        
        public void CauseAllPDFPagesToBeOCRed()
        {
            for (int i = PageCount; i >= 1; --i)
            {
                GetOCRText(i);
            }
        }

        /// <summary>
        /// Returns the OCR words on the page.  Null if the words are not yet available.
        /// The page will be queued for OCRing if they are not available...
        /// Page is 1 based...
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public WordList GetOCRText(int page)
        {
            return GetOCRText(page, true);
        }

        public WordList GetOCRText(int page, bool queue_for_ocr)
        {
            lock (texts)
            {
                // First check our cache                
                {
                    TypedWeakReference<WordList> word_list_weak;
                    texts.TryGetValue(page, out word_list_weak);
                    if (null != word_list_weak)
                    {
                        WordList word_list = word_list_weak.TypedTarget;
                        if (null != word_list)
                        {
                            return word_list;
                        }
                    }
                }

                // Then check for an existing SINGLE file
                {
                    string filename = pdf_render_file_layer.MakeFilename_TextSingle(page);
                    try
                    {
                        if (File.Exists(filename))
                        {
                            // Get this ONE page
                            Dictionary<int, WordList> word_lists = WordList.ReadFromFile(filename, page);
                            WordList word_list = word_lists[page];                            
                            texts[page] = new TypedWeakReference<WordList>(word_list);
                            return word_list;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Warn(ex, "There was an error loading the OCR text for {0} page {1}.", document_fingerprint, page);
                        FileTools.Delete(filename);
                    }
                }

                // Then check for an existing GROUP file
                {
                    string filename = pdf_render_file_layer.MakeFilename_TextGroup(page, TEXT_PAGES_PER_GROUP);
                    try
                    {
                        if (File.Exists(filename))
                        {
                            Dictionary<int, WordList> word_lists = WordList.ReadFromFile(filename);
                            foreach (var pair in word_lists)
                            {
                                texts[pair.Key] = new TypedWeakReference<WordList>(pair.Value);
                            }

                            TypedWeakReference<WordList> word_list_weak;
                            texts.TryGetValue(page, out word_list_weak);
                            if (null != word_list_weak)
                            {
                                WordList word_list = word_list_weak.TypedTarget;
                                if (null != word_list)
                                {
                                    return word_list;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Warn(ex, "There was an error loading the OCR text group for {0} page {1}.", document_fingerprint, page);
                        FileTools.Delete(filename);
                    }
                }
            }

            // If we get this far then the text was not available so queue extraction
            if (queue_for_ocr)
            {
                // If we have never tried the GROUP version before, queue for it
                string filename = pdf_render_file_layer.MakeFilename_TextGroup(page, TEXT_PAGES_PER_GROUP);
                if (!File.Exists(filename))
                {
                    PDFTextExtractor.Instance.QueueJobGroup(new PDFTextExtractor.Job(this, page, TEXT_PAGES_PER_GROUP));
                }
                else
                {
                    PDFTextExtractor.Instance.QueueJobSingle(new PDFTextExtractor.Job(this, page, TEXT_PAGES_PER_GROUP));
                }
            }

            return null;
        }

        internal string GetFullOCRText(int page)
        {
            StringBuilder sb = new StringBuilder();

            WordList word_list = GetOCRText(page);
            if (null != word_list)
            {
                foreach (Word word in word_list)
                {
                    sb.Append(word.Text);
                    sb.Append(' ');
                }
            }

            return sb.ToString();
        }
        
        // Gets the full concatednated text for this document.
        // This is slow as it concatenates all the words from the OCR result...
        internal string GetFullOCRText()
        {
            StringBuilder sb = new StringBuilder();

            for (int page = 1; page <= this.PageCount; ++page)
            {
                WordList word_list = GetOCRText(page);
                if (null != word_list)
                {
                    foreach (Word word in word_list)
                    {
                        sb.Append(word.Text);
                        sb.Append(' ');
                    }
                }
            }

            return sb.ToString();
        }

        public void ClearOCRText()
        {
            Logging.Info("Clearing OCR for document " + document_fingerprint);

            // Delete the OCR files
            for (int page = 1; page <= PageCount; ++page)
            {
                // First the SINGLE file
                {
                    string filename = pdf_render_file_layer.MakeFilename_TextSingle(page);

                    try
                    {
                        File.Delete(filename);
                    }
                    catch (Exception ex)
                    {
                        Logging.Error(ex, "There was a problem while trying to delete the OCR file " + filename);
                    }
                }

                // Then the MULTI file
                {
                    string filename = pdf_render_file_layer.MakeFilename_TextGroup(page, TEXT_PAGES_PER_GROUP);

                    try
                    {
                        File.Delete(filename);
                    }
                    catch (Exception ex)
                    {
                        Logging.Error(ex, "There was a problem while trying to delete the OCR file " + filename);
                    }
                }
            }

            // Clear out the old texts
            lock (texts)
            {
                texts.Clear();
            }
        }
        
        public void ForceOCRText()
        {
            ForceOCRText("eng");
        }

        public void ForceOCRText(string language)
        {
            Logging.Info("Forcing OCR for document {0} in language {1}", document_fingerprint, language);

            // Clear out the old texts
            lock (texts)
            {
                texts.Clear();
            }

            // Queue all the pages for OCR
            for (int page = 1; page <= PageCount; ++page)
            {
                PDFTextExtractor.Job job = new PDFTextExtractor.Job(this, page, TEXT_PAGES_PER_GROUP);
                job.force_job = true;
                job.language = language;
                PDFTextExtractor.Instance.QueueJobSingle(job);
            }
        }

        internal void StorePageTextSingle(int page, string source_filename)
        {
            string filename = pdf_render_file_layer.StorePageTextSingle(page, source_filename);

            if (null != OnPageTextAvailable)
            {
                OnPageTextAvailable(page, page);
            }
        }

        internal void StorePageTextGroup(int page, int TEXT_PAGES_PER_GROUP, string source_filename)
        {
            string filename = pdf_render_file_layer.StorePageTextGroup(page, TEXT_PAGES_PER_GROUP, source_filename);

            if (null != OnPageTextAvailable)
            {
                int page_range_start = ((page-1) / TEXT_PAGES_PER_GROUP) * TEXT_PAGES_PER_GROUP + 1;
                int page_range_end = page_range_start + TEXT_PAGES_PER_GROUP - 1;
                page_range_end = Math.Min(page_range_end, PageCount);

                OnPageTextAvailable(page_range_start, page_range_end);
            }
        }

        internal void DumpImageCacheStats(out int pages_count, out int pages_bytes)
        {
            pages_count = 0;
            pages_bytes = 0;
        }

        public void FlushCachedPageRenderings()
        {
            Logging.Info("Flushing the cached page renderings for {0}", document_fingerprint);

            this.sorax_pdf_renderer.Flush();
        }

        public void FlushCachedTexts()
        {
            Logging.Info("Flushing the cached texts for {0}", document_fingerprint);

            lock (texts)
            {
                this.texts.Clear();
            }
        }
    }
}
