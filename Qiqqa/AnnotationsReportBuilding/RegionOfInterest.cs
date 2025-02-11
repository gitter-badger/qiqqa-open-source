﻿using System;
using System.Collections.Generic;
using System.Windows.Media;
using Qiqqa.Documents.PDF;

namespace Qiqqa.AnnotationsReportBuilding
{
    class RegionOfInterest
    {
        public static readonly double PROXIMITY_MARGIN = 1.0 / 30.0;

        public double left;
        public double top;
        public double width;
        public double height;

        public RegionOfInterest(double left, double top, double width, double height)
        {
            this.left = left;
            this.top = top;
            this.width = width;
            this.height = height;
        }

        public bool IsCloseTo(RegionOfInterest other)
        {
            if (this.Contains(other)) return true;
            if (other.Contains(this)) return true;

            double other_distance_to_right = other.left - this.Right;
            double other_distance_to_left = this.left - other.Right;
            double horizontal_distance = Math.Max(other_distance_to_right, other_distance_to_left);

            double other_distance_to_top = this.top - other.Bottom;
            double other_distance_to_bottom = other.top - this.Bottom;
            double vertical_distance = Math.Max(other_distance_to_top, other_distance_to_bottom);

            return
                true
                //&& horizontal_distance <= PROXIMITY_MARGIN
                && vertical_distance <= PROXIMITY_MARGIN
                ;
        }

        public void Incorporate(RegionOfInterest other)
        {
            double right = Math.Max(this.Right, other.Right);
            double bottom = Math.Max(this.Bottom, other.Bottom);

            this.left = Math.Min(this.left, other.left);
            this.top = Math.Min(this.top, other.top);

            this.width = right - this.left;
            this.height = bottom - this.top;
        }

        public bool Contains(RegionOfInterest other)
        {
            return
                true
                && this.left <= other.left
                && this.Right >= other.Right
                && this.top <= other.top
                && this.Bottom >= other.Bottom
                ;
        }

        public double Right
        {
            get
            {
                return left + width;
            }
        }

        public double Bottom
        {
            get
            {
                return top + height;
            }
        }

        public override string ToString()
        {
            return String.Format("{0:0.000},{1:0.000}x{2:0.000},{3:0.000}", left, top, Right, Bottom);
        }

        public static void AggregateRegions(List<RegionOfInterest> regions)
        {
            // Aggregate them
            bool did_more_incorporations = true;
            while (did_more_incorporations)
            {
                did_more_incorporations = false;

                for (int i = 0; i < regions.Count - 1; ++i)
                {
                    for (int j = i + 1; j < regions.Count; ++j)
                    {
                        if (regions[i].IsCloseTo(regions[j]))
                        {
                            regions[i].Incorporate(regions[j]);
                            regions.RemoveAt(j);
                            
                            --j;
                            did_more_incorporations = true;
                        }
                    }
                }
            }
        }

        public static List<PDFAnnotation> ConvertRegionsToPDFAnnotations(List<RegionOfInterest> regions, string tag, PDFDocument pdf_document, int page)
        {
            List<PDFAnnotation> annotations = new List<PDFAnnotation>();
        
            foreach (var region in regions)
            {
                PDFAnnotation pdf_annotation = new PDFAnnotation(pdf_document.Fingerprint, page, Colors.Black, null);
                pdf_annotation.Left = 0;
                pdf_annotation.Top = Math.Max(0, region.top - PROXIMITY_MARGIN);
                pdf_annotation.Width = 1;
                pdf_annotation.Height = Math.Min(1, region.height + 2 * PROXIMITY_MARGIN);
                pdf_annotation.Tags = tag;

                annotations.Add(pdf_annotation);
            }

            return annotations;
        }
    }
}
