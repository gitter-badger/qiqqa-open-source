﻿using System;
using System.Collections.Generic;
using System.IO;
using Qiqqa.DocumentLibrary.DocumentLibraryIndex;
using Qiqqa.Documents.PDF;
using Utilities;
using Utilities.Collections;
using Utilities.Files;
using Utilities.GUI;
using Utilities.Language.Buzzwords;
using Utilities.Language.TextIndexing;
using Utilities.Misc;
using Utilities.OCR;

namespace Qiqqa.DocumentLibrary.AITagsStuff
{
    public class AITagManager
    {
        Library library;
        AITags current_ai_tags_record;

        bool regenerating_in_progress = false;

        public AITagManager(Library library)
        {
            this.library = library;

            current_ai_tags_record = new AITags();


            // Attempt to load the existing tags
            try
            {
                if (File.Exists(Filename_Store))
                {
                    Logging.Info("+Loading AutoTags");
                    current_ai_tags_record = SerializeFile.ProtoLoad<AITags>(Filename_Store);
                    Logging.Info("-Loading AutoTags");
                }
            }
            catch (Exception ex)
            {
                Logging.Warn(ex, "There was a problem loading existing AutoTags");
            }
        }

        private string Filename_Store
        {
            get
            {
                return library.LIBRARY_BASE_PATH + "Qiqqa.autotags";
            }
        }

        public void Regenerate()
        {
            Regenerate(null);
        }

        public void Regenerate(AsyncCallback callback)
        {
            lock (this)
            {
                if (regenerating_in_progress)
                {
                    Logging.Info("Not regenerating AutoTags because a regeneration is already in progress.");
                    return;
                }

                regenerating_in_progress = true;
            }

            try
            {
                Logging.Info("+AITagManager is starting regenerating");

                StatusManager.Instance.UpdateStatusBusy("AITags", "Loading documents");
                List<PDFDocument> pdf_documents = library.PDFDocuments;

                int count_title_by_user = 0;
                int could_title_by_suggest = 0;
                StatusManager.Instance.UpdateStatusBusy("AITags", "Deciding whether to use suggested titles");
                foreach (PDFDocument pdf_document in pdf_documents)
                {
                    if (pdf_document.IsTitleGeneratedByUser)
                    {
                        ++count_title_by_user;                        
                    }
                    else
                    {
                        ++could_title_by_suggest;
                    }
                }

                bool use_suggested_titles = could_title_by_suggest > count_title_by_user;
                
                StatusManager.Instance.UpdateStatusBusy("AITags", "Scanning titles");
                List<string> titles = new List<string>();
                foreach (PDFDocument pdf_document in pdf_documents)
                {
                    if (use_suggested_titles || pdf_document.IsTitleGeneratedByUser)
                    {
                        titles.Add(pdf_document.TitleCombined);
                    }
                }

                StatusManager.Instance.UpdateStatusBusy("AITags", "Generating AutoTags");

                // Get the black/whitelists
                List<string> words_blacklist = new List<string>();
                List<string> words_whitelist = new List<string>();
                {
                    List<BlackWhiteListEntry> entries = library.BlackWhiteListManager.ReadList();
                    foreach (var entry in entries)
                    {
                        if (entry.is_deleted)
                        {
                            continue;
                        }

                        switch (entry.list_type)
                        {
                            case BlackWhiteListEntry.ListType.White:
                                words_whitelist.Add(entry.word);
                                break;
                            case BlackWhiteListEntry.ListType.Black:
                                words_blacklist.Add(entry.word);
                                break;
                            default:
                                Logging.Warn("Unknown black/whitelist type " + entry.list_type);
                                break;
                        }
                    }
                }

                // Generate them
                CountingDictionary<NGram> ai_tags = BuzzwordGenerator.GenerateBuzzwords(titles, words_blacklist, words_whitelist, true);
                Logging.Info("Generated {0} autotags", ai_tags.Count);
                if (ai_tags.Count < 20)
                {
                    Logging.Warn("There are too few autotags (only {0}), so not supressing Scrabble words...", ai_tags.Count);
                    ai_tags = BuzzwordGenerator.GenerateBuzzwords(titles, words_blacklist, words_whitelist, false);
                    Logging.Info("Generated {0} autotags without Scrabble suppression", ai_tags.Count);
                }

                StatusManager.Instance.UpdateStatusBusy("AITags", "AutoTagging documents");
                AITags ai_tags_record = new AITags();

                // Go through each ngram and see what documents contain it
                StatusManager.Instance.ClearCancelled("AITags");
                List<NGram> ai_tags_list = new List<NGram>(ai_tags.Keys);
                for (int i = 0; i < ai_tags_list.Count; ++i)
                {
                    try
                    {
                        NGram ai_tag = ai_tags_list[i];
                        string tag = ai_tag.text;

                        if (StatusManager.Instance.IsCancelled("AITags"))
                        {
                            break;
                        }

                        StatusManager.Instance.UpdateStatusBusy("AITags", String.Format("AutoTagging papers with '{0}'", tag), i, ai_tags_list.Count, true);

                        // Surround the tag with quotes and search the index
                        string search_tag = "\"" + tag + "\"";                        
                        List<IndexPageResult> fingerprints_potential = LibrarySearcher.FindAllPagesMatchingQuery(library, search_tag);

                        if (null != fingerprints_potential)
                        {
                            // Skip this tag if too many documents have it...
                            if (ai_tag.is_acronym && fingerprints_potential.Count > 0.05 * pdf_documents.Count)
                            {
                                Logging.Info("Skipping AutoTag {0} because too many documents have it...", tag);
                                continue;
                            }

                            foreach (var fingerprint_potential in fingerprints_potential)
                            {
                                // Non-acronyms are definitely tagged
                                if (!ai_tag.is_acronym)
                                {
                                    ai_tags_record.Associate(tag, fingerprint_potential.fingerprint);
                                }
                                else
                                {
                                    // Acronyms need to be done manually because we only want the upper case ones...

                                    PDFDocument pdf_document = library.GetDocumentByFingerprint(fingerprint_potential.fingerprint);
                                    if (null != pdf_document && !pdf_document.Deleted)
                                    {
                                        bool have_tag = false;

                                        if (!have_tag)
                                        {
                                            string doc_title = pdf_document.TitleCombined;
                                            if (!String.IsNullOrEmpty(doc_title))
                                            {
                                                if (!ai_tag.is_acronym) doc_title = doc_title.ToLower();
                                                if (doc_title.Contains(tag))
                                                {
                                                    have_tag = true;
                                                }
                                            }
                                        }

                                        if (!have_tag)
                                        {
                                            string doc_comment = pdf_document.Comments;
                                            if (!String.IsNullOrEmpty(doc_comment))
                                            {
                                                if (!ai_tag.is_acronym) doc_comment = doc_comment.ToLower();
                                                if (doc_comment.Contains(tag))
                                                {
                                                    have_tag = true;
                                                }
                                            }
                                        }

                                        if (!have_tag && pdf_document.DocumentExists)
                                        {
                                            foreach (var page_result in fingerprint_potential.page_results)                                            
                                            {
                                                if (have_tag)
                                                {
                                                    break;
                                                }
                                                
                                                int page = page_result.page;
                                                WordList page_word_list = pdf_document.PDFRenderer.GetOCRText(page);
                                                if (null != page_word_list && 0 < page_word_list.Count)
                                                {
                                                    foreach (Word word in page_word_list)
                                                    {
                                                        if (tag == word.Text)
                                                        {
                                                            have_tag = true;
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        // If we have this tag, record it
                                        if (have_tag)
                                        {
                                            ai_tags_record.Associate(tag, fingerprint_potential.fingerprint);
                                        }
                                    }
                                    else
                                    {
                                        Logging.Warn("Could not find a document matching fingerprint {0}", fingerprint_potential);
                                    }
                                }
                            }
                        }
                    } //try
                    catch (Exception ex)
                    {
                        Logging.Error(ex, "There was an exception while processing one of the autotags");
                    }
                } //for


                bool use_new_autotags = true;

                if (StatusManager.Instance.IsCancelled("AITags"))
                {
                    if (!MessageBoxes.AskQuestion("You cancelled the generation of your AutoTags.  Do you want to use the partially generated AutoTags (YES) or keep your old AutoTags (NO)?"))
                    {
                        use_new_autotags = false;
                    }
                }

                if (use_new_autotags)
                {
                    StatusManager.Instance.UpdateStatusBusy("AITags", "Saving AutoTags");
                    SerializeFile.ProtoSave<AITags>(Filename_Store, ai_tags_record);
                    current_ai_tags_record = ai_tags_record;
                }

                StatusManager.Instance.UpdateStatus("AITags", "AutoTags generated!");

                Logging.Info("-AITagManager is finished regenerating");
            }

            finally
            {
                lock (this)
                {
                    regenerating_in_progress = false;
                }
            }

            // Call any callback that might be interested
            if (null != callback)
            {
                callback(null);
            }
        }

        public AITags AITags        
        {
            get
            {
                return this.current_ai_tags_record;
            }
        }

        public static void TestHarness()
        {
            Daemon daemon = new Daemon("Test");
            Library library = Library.GuestInstance;

            AITagManager ai_tag_manager = new AITagManager(library);
            ai_tag_manager.Regenerate();                        
        }
    }
}
