﻿using System.Reflection;
using System.Windows.Documents;
using Utilities.GUI;

namespace Qiqqa.Common.GUI
{
    [Obfuscation(Feature = "properties renaming")]
    public class StandardFlowDocument : FlowDocument
    {
        public StandardFlowDocument()
        {
            this.Background = ThemeColours.Background_Brush_Blue_Light;
            this.FontSize = 13;
            this.FontFamily = ThemeTextStyles.FontFamily_Standard;
        }
    }
}
