﻿using System;
using System.Collections.Generic;
using System.IO;
using Qiqqa.Documents.PDF;
using Utilities;
using Utilities.DateTimeTools;

namespace Qiqqa.DocumentLibrary.RecentlyReadStuff
{
    public class RecentlyReadManager
    {
        Library library;

        public RecentlyReadManager(Library library)
        {
            this.library = library;
        }

        public string Filename_Store
        {
            get
            {
                return library.LIBRARY_BASE_PATH + "Qiqqa.recently_read";
            }
        }

        public void AddRecentlyRead(PDFDocument pdf_document)
        {
            using (StreamWriter sr = new StreamWriter(Filename_Store, true))
            {
                sr.WriteLine(
                    "{0},{1},{2}",
                    Guid.NewGuid().ToString(),
                    DateFormatter.asYYYYMMDDHHMMSS(DateTime.UtcNow),
                    pdf_document.Fingerprint
                    );
            }
        }

        public List<DateTime> GetRecentlyReadDates()
        {
            List<DateTime> results = new List<DateTime>();

            try
            {
                if (!File.Exists(Filename_Store)) return results;
                using (StreamReader sr = new StreamReader(Filename_Store))
                {
                    while (true)
                    {
                        string line = sr.ReadLine();
                        if (String.IsNullOrEmpty(line))
                        {
                            break;
                        }

                        string[] line_splits = line.Split(',');
                        DateTime read_date = DateFormatter.FromYYYYMMDDHHMMSS(line_splits[1]);
                        results.Add(read_date);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error(ex, "There was a problem with getting the recently read documents.");
            }

            return results;
        }
    }
}
