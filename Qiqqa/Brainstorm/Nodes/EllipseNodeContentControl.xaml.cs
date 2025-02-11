﻿using System.Windows.Controls;
using System.Windows.Media;

namespace Qiqqa.Brainstorm.Nodes
{    
    /// <summary>
    /// Interaction logic for StringNodeContentControl.xaml
    /// </summary>
    public partial class EllipseNodeContentControl : UserControl
    {

        static readonly Brush FILL_BRUSH = Brushes.LightBlue;
        static readonly Brush STROKE_BRUSH = Brushes.DarkBlue;
        static readonly double STROKE_THICKNESS = 1;

        EllipseNodeContent circle_node_content;

        public EllipseNodeContentControl(NodeControl node_control, EllipseNodeContent circle_node_content)
        {
            InitializeComponent();

            Focusable = true;

            this.circle_node_content = circle_node_content;
            this.ToolTip = circle_node_content.text;

            ObjEllipse.Fill = FILL_BRUSH;
            ObjEllipse.Stroke = STROKE_BRUSH;
            ObjEllipse.StrokeThickness = STROKE_THICKNESS;

        }
    }
}
