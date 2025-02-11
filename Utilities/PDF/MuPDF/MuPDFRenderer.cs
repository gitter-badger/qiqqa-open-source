﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Utilities.Files;
using Utilities.GUI;
using Utilities.Images;
using Utilities.ProcessTools;
using Image = System.Windows.Controls.Image;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace Utilities.PDF.MuPDF
{
    public class MuPDFRenderer
    {
        public static MemoryStream RenderPDFPage(string pdf_filename, int page_number, int dpi, string password, ProcessPriorityClass priority_class)
        {
            string process_parameters = String.Format(
                ""
                + " " + "-o stdout.png "
                + " " + "-r " + dpi
                + " " + (String.IsNullOrEmpty(password) ? "" : "-p " + password)
                + " " + '"' + pdf_filename + '"'
                + " " + page_number
                );

            MemoryStream ms = ReadEntireStandardOutput(process_parameters, priority_class);
            return ms;
        }

        public class TextChunk
        {
            public string text;
            public string font_name;
            public double font_size;
            public int page;
            public double x0, y0, x1, y1;

            public override string ToString()
            {
                return String.Format("{0} p{5} {1:.000},{2:.000} {3:.000},{4:.000} ", text, x0, y0, x1, y1, page);
            }
        }

        public static List<TextChunk> GetEmbeddedText(string pdf_filename, string page_numbers, string password, ProcessPriorityClass priority_class)
        {
            string process_parameters = String.Format(
                ""
                + " " + "-tt "
                + " " + (String.IsNullOrEmpty(password) ? "" : "-p " + password)
                + " " + '"' + pdf_filename + '"'
                + " " + page_numbers
                );

            MemoryStream ms = ReadEntireStandardOutput(process_parameters, priority_class);
            ms.Seek(0, SeekOrigin.Begin);
            StreamReader sr_lines = new StreamReader(ms);

            List<TextChunk> text_chunks = new List<TextChunk>();

            int page = 0;
            double page_x0 = 0;
            double page_y0 = 0;
            double page_x1 = 0;
            double page_y1 = 0;
            double page_rotation = 0;

            string current_font_name = "";
            double current_font_size = 0;
            
            string line;
            while (null != (line = sr_lines.ReadLine()))
            {
                // Look for a character element (note that even a " can be the character in the then malformed XML)
                {
                    Match match = Regex.Match(line, "char ucs=\"(.*)\" bbox=\"\\[(\\S*) (\\S*) (\\S*) (\\S*)\\]");
                    if (Match.Empty != match)
                    {
                        string text = match.Groups[1].Value;
                        double word_x0 = Convert.ToDouble(match.Groups[2].Value, Internationalization.DEFAULT_CULTURE);
                        double word_y0 = Convert.ToDouble(match.Groups[3].Value, Internationalization.DEFAULT_CULTURE);
                        double word_x1 = Convert.ToDouble(match.Groups[4].Value, Internationalization.DEFAULT_CULTURE);
                        double word_y1 = Convert.ToDouble(match.Groups[5].Value, Internationalization.DEFAULT_CULTURE);

                        ResolveRotation(page_rotation, ref word_x0, ref word_y0, ref word_x1, ref word_y1);

                        // Position this little grubber
                        TextChunk text_chunk = new TextChunk();
                        text_chunk.text = text;
                        text_chunk.font_name = current_font_name;
                        text_chunk.font_size = current_font_size;
                        text_chunk.page = page;
                        text_chunk.x0 = (word_x0 - page_x0) / (page_x1 - page_x0);
                        text_chunk.y0 = 1 - (word_y0 - page_y0) / (page_y1 - page_y0);
                        text_chunk.x1 = (word_x1 - page_x0) / (page_x1 - page_x0);
                        text_chunk.y1 = 1 - (word_y1 - page_y0) / (page_y1 - page_y0);

                        // Cater for the rotation
                        if (0 != page_rotation)
                        {
                            text_chunk.y0 = 1 - text_chunk.y0;
                            text_chunk.y1 = 1 - text_chunk.y1;
                        }

                        // Make sure the bounding box is TL-BR
                        if (text_chunk.x1 < text_chunk.x0)
                        {
                            Swap.swap(ref text_chunk.x0, ref text_chunk.x1);
                        }
                        if (text_chunk.y1 < text_chunk.y0)
                        {
                            Swap.swap(ref text_chunk.y0, ref text_chunk.y1);
                        }

                        if (text_chunk.x1 <= text_chunk.x0 || text_chunk.y1 <= text_chunk.y0)
                        {
                            Logging.Warn("Bad bounding box for text chunk");
                        }

                        // And add him to the result list6
                        text_chunks.Add(text_chunk);

                        continue;
                    }
                }

                // Look for a change in font name
                {
                    Match match = Regex.Match(line, " font=\"(\\S*)\" size=\"(\\S*)\" ");
                    if (Match.Empty != match)
                    {
                        current_font_name = match.Groups[1].Value;
                        current_font_size = Convert.ToDouble(match.Groups[2].Value, Internationalization.DEFAULT_CULTURE);

                        continue;
                    }
                }

                // Look for the page header with dimensions
                {
                    Match match = Regex.Match(line, @"\[Page (.+) X0 (\S+) Y0 (\S+) X1 (\S+) Y1 (\S+) R (\S+)\]");
                    if (Match.Empty != match)
                    {
                        page = Convert.ToInt32(match.Groups[1].Value, Internationalization.DEFAULT_CULTURE);
                        page_x0 = Convert.ToDouble(match.Groups[2].Value, Internationalization.DEFAULT_CULTURE);
                        page_y0 = Convert.ToDouble(match.Groups[3].Value, Internationalization.DEFAULT_CULTURE);
                        page_x1 = Convert.ToDouble(match.Groups[4].Value, Internationalization.DEFAULT_CULTURE);
                        page_y1 = Convert.ToDouble(match.Groups[5].Value, Internationalization.DEFAULT_CULTURE);
                        page_rotation = Convert.ToDouble(match.Groups[6].Value, Internationalization.DEFAULT_CULTURE);

                        ResolveRotation(page_rotation, ref page_x0, ref page_y0, ref page_x1, ref page_y1);

                        continue;
                    }
                }
            }

            text_chunks = AggregateOverlappingTextChunks(text_chunks);
            return text_chunks;
        }

        private static void ResolveRotation(double page_rotation, ref double x0, ref double y0, ref double x1, ref double y1)
        {
            // If this page is rotated -- i guess we should be looking for 90 or 270, etc, but lets assume non-zero will work
            if (0 != page_rotation)
            {
                Swap<double>.swap(ref x0, ref y0);
                Swap<double>.swap(ref x1, ref y1);
            }
        }

        private static List<TextChunk> AggregateOverlappingTextChunks(List<TextChunk> text_chunks_original)
        {
            List<TextChunk> text_chunks = new List<TextChunk>();

            TextChunk current_text_chunk = null;
            foreach (TextChunk text_chunk in text_chunks_original)
            {
                if (text_chunk.x1 <= text_chunk.x0 || text_chunk.y1 <= text_chunk.y0)
                {
                    Logging.Warn("Bad bounding box for raw text chunk");
                }
                
                // If we flushed the last word
                if (null == current_text_chunk)
                {
                    current_text_chunk = text_chunk;
                    text_chunks.Add(text_chunk);
                    continue;
                }

                // If it's a space
                if (0 == text_chunk.text.CompareTo(" "))
                {
                    current_text_chunk = null;
                    continue;
                }

                // If it's on a different page...
                if (text_chunk.page != current_text_chunk.page)
                {
                    current_text_chunk = text_chunk;
                    text_chunks.Add(text_chunk);
                    continue;
                }

                // If its substantially below the current chunk
                if (text_chunk.y0 > current_text_chunk.y1)
                {
                    current_text_chunk = text_chunk;
                    text_chunks.Add(text_chunk);
                    continue;
                }

                // If its substantially above the current chunk
                if (text_chunk.y1 < current_text_chunk.y0)
                {
                    current_text_chunk = text_chunk;
                    text_chunks.Add(text_chunk);
                    continue;
                }
                
                // If it is substantially to the left of the current chunk
                if (text_chunk.x1 < current_text_chunk.x0)
                {
                    current_text_chunk = text_chunk;
                    text_chunks.Add(text_chunk);
                    continue;
                }

                // If its more than a letters distance across from the current word
                double average_letter_width = (current_text_chunk.x1 - current_text_chunk.x0) / current_text_chunk.text.Length;
                double current_letter_gap = (text_chunk.x0 - current_text_chunk.x1);
                if (current_letter_gap > average_letter_width)
                {
                    current_text_chunk = text_chunk;
                    text_chunks.Add(text_chunk);
                    continue;
                }



                // If we get here we aggregate
                {
                    current_text_chunk.text = current_text_chunk.text + text_chunk.text;
                    current_text_chunk.x0 = Math.Min(current_text_chunk.x0, Math.Min(text_chunk.x0, text_chunk.x1));
                    current_text_chunk.y0 = Math.Min(current_text_chunk.y0, Math.Min(text_chunk.y0, text_chunk.y1));
                    current_text_chunk.x1 = Math.Max(current_text_chunk.x1, Math.Max(text_chunk.x0, text_chunk.x1));
                    current_text_chunk.y1 = Math.Max(current_text_chunk.y1, Math.Max(text_chunk.y0, text_chunk.y1));
                }

                if (current_text_chunk.x1 <= current_text_chunk.x0 || current_text_chunk.y1 <= current_text_chunk.y0)
                {
                    Logging.Warn("Bad bounding box for aggregated text chunk");
                }
            }

            return text_chunks;
        }


        private static MemoryStream ReadEntireStandardOutput(string process_parameters, ProcessPriorityClass priority_class)
        {
            Process process = ProcessSpawning.SpawnChildProcess("pdfdraw.exe", process_parameters, priority_class);
            process.ErrorDataReceived += (sender, e) => { };
            process.BeginErrorReadLine();

            // Read image from stdout
            StreamReader sr = process.StandardOutput;
            FileStream fs = (FileStream)sr.BaseStream;
            MemoryStream ms = new MemoryStream(128 * 1024);
            int total_size = StreamToFile.CopyStreamToStream(fs, ms);

            // Check that the process has exited properly
            process.WaitForExit(1000);
            if (!process.HasExited)
            {
                Logging.Error("PDFRenderer process did not terminate, so killing it");

                try
                {
                    Logging.Info("Killing PDFRenderer process");
                    process.Kill();
                    Logging.Info("Killed PDFRenderer process");
                }
                catch (Exception)
                {
                    Logging.Error("These was an exception while trying to kill the PDFRenderer process");
                }
            }

            return ms;
        }

        #region --- Tests ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------


        public static void TestHarness_TEXT_RENDER()
        {
            // SUCCESSES
            //TestHarness_TEXT_RENDER_ONE(@"C:\temp\2.pdf", 2);
            //TestHarness_TEXT_RENDER_ONE(@"C:\temp\3.pdf", 3);
            //TestHarness_TEXT_RENDER_ONE(@"C:\temp\6.pdf", 3);
            //TestHarness_TEXT_RENDER_ONE(@"C:\temp\japanese.pdf", 3);
            //TestHarness_TEXT_RENDER_ONE(@"C:\temp\p5.pdf", 3);
            //TestHarness_TEXT_RENDER_ONE(@"C:\temp\7.pdf", 3);
            //TestHarness_TEXT_RENDER_ONE(@"C:\temp\7a.pdf", 3);
         
            // FAILURES
            //TestHarness_TEXT_RENDER_ONE(@"C:\temp\kevin.pdf", 3);
            //TestHarness_TEXT_RENDER_ONE(@"C:\Users\Jimme\AppData\Roaming\Quantisle\Qiqqa\\7CDA3872-F99B-49B5-A0EB-E58C08719C1C\documents\1\1E18A4945DA8F9CDB6621F12FECE3CFFC3CB7CF.pdf", 2);
            //TestHarness_TEXT_RENDER_ONE(@"C:\Users\Jimme\AppData\Roaming\Quantisle\Qiqqa\\7CDA3872-F99B-49B5-A0EB-E58C08719C1C\documents\3\3760DF1C9E1D7944F15FAC5E077E9CE4520F33E.pdf", 8);

            for (int i = 0; i < 2; ++i)
            {
                TestHarness_TEXT_RENDER_ONE(@"C:\Users\Jimme\AppData\Roaming\Quantisle\Qiqqa\\7CDA3872-F99B-49B5-A0EB-E58C08719C1C\documents\1\1E18A4945DA8F9CDB6621F12FECE3CFFC3CB7CF.pdf", 8);
            }
            
        }

        public static void TestHarness_TEXT_RENDER_ONE(string PDF_FILENAME, int PAGE)
        {
            SolidColorBrush BRUSH_EDGE = new SolidColorBrush(Colors.Red);
            SolidColorBrush BRUSH_BACKGROUND = new SolidColorBrush(ColorTools.MakeTransparentColor(Colors.GreenYellow, 64));

            // Load the image graphic
            MemoryStream ms = RenderPDFPage(PDF_FILENAME, PAGE, 150, null, ProcessPriorityClass.Normal);
            BitmapSource bitmap_image = BitmapImageTools.LoadFromBytes(ms.ToArray());
            Image image = new Image();
            image.Source = bitmap_image;
            image.Stretch = Stretch.Fill;

            // Load the image text
            Canvas canvas = new Canvas();
            List<TextChunk> text_chunks = GetEmbeddedText(PDF_FILENAME, "" + PAGE, null, ProcessPriorityClass.Normal);
            List<Rectangle> rectangles = new List<Rectangle>();
            foreach (TextChunk text_chunk in text_chunks)
            {
                Rectangle rectangle = new Rectangle();
                rectangle.Stroke = BRUSH_EDGE;
                rectangle.Fill = BRUSH_BACKGROUND;
                rectangle.Tag = text_chunk;
                rectangles.Add(rectangle);
                canvas.Children.Add(rectangle);
            }

            RectanglesManager rm = new RectanglesManager(rectangles, canvas);

            Grid grid = new Grid();
            grid.Children.Add(image);
            grid.Children.Add(canvas);

            ControlHostingWindow window = new ControlHostingWindow("PDF", grid);
            window.Show();
        }

        class RectanglesManager
        {
            List<Rectangle> rectangles;
            Canvas canvas;
            public RectanglesManager(List<Rectangle> rectangles, Canvas canvas)
            {
                this.rectangles = rectangles;
                this.canvas = canvas;

                canvas.SizeChanged += canvas_SizeChanged;
            }

            void canvas_SizeChanged(object sender, SizeChangedEventArgs e)
            {
                if (Double.IsNaN(canvas.ActualWidth) || Double.IsNaN(canvas.ActualHeight))
                {
                    return;
                }

                foreach (Rectangle rectangle in rectangles)
                {
                    TextChunk text_chunk = (TextChunk)rectangle.Tag;

                    rectangle.Width = canvas.ActualWidth * (text_chunk.x1 - text_chunk.x0);
                    rectangle.Height = canvas.ActualHeight * (text_chunk.y1 - text_chunk.y0);

                    Canvas.SetLeft(rectangle, canvas.ActualWidth * text_chunk.x0);
                    Canvas.SetTop(rectangle, canvas.ActualHeight * text_chunk.y0);
                }
            }
        }

        public static void TestHarness_TEXT()
        {
            Logging.Info("Start!");

            List<TextChunk> chunks = GetEmbeddedText(@"C:\temp\poo.pdf", "1", null, ProcessPriorityClass.Normal);
            
            
            //for (int i = 1; i <= 32; ++i)
            //{
            //    Logging.Info("Page {0}", i);
            //    List<TextChunk> chunks = GetEmbeddedText(@"C:\temp\2.pdf", i, null, ProcessPriorityClass.Normal);
            //}
            //for (int i = 1; i <= 32; ++i)
            //{
            //    Logging.Info("Page {0}", i);
            //    List<TextChunk> chunks = GetEmbeddedText(@"C:\temp\3.pdf", i, null, ProcessPriorityClass.Normal);
            //}
            //for (int i = 1; i <= 32; ++i)
            //{
            //    Logging.Info("Page {0}", i);
            //    List<TextChunk> chunks = GetEmbeddedText(@"C:\temp\kevin.pdf", i, null, ProcessPriorityClass.Normal);
            //}
            //for (int i = 1; i <= 132; ++i)
            //{
            //    Logging.Info("Page {0}", i);
            //    List<TextChunk> chunks = GetEmbeddedText(@"C:\temp\6.pdf", i, null, ProcessPriorityClass.Normal);
            //}

            Logging.Info("Done!");
        }
        
        
        public static void TestHarness_IMAGE()
        {
            Logging.Info("Start!");
            for (int i = 0; i < 10; ++i)
            {
                MemoryStream ms = RenderPDFPage(@"C:\temp\3.pdf", 1, 200, null, ProcessPriorityClass.Normal);
                Bitmap bitmap = new Bitmap(ms);
            }
            Logging.Info("Done!");
        }

        #endregion ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
