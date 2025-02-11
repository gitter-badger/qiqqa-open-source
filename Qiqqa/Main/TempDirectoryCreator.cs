﻿using System;
using System.IO;
using Utilities.Files;

namespace Qiqqa.Main
{
    // Creates a temp directory before anything else runs
    public class TempDirectoryCreator
    {        
        static TempDirectoryCreator()
        {
            try
            {
                if (!Directory.Exists(TempFile.TempDirectory))
                {
                    Directory.CreateDirectory(TempFile.TempDirectory);
                }
            }

            catch (Exception) {}
        }

        public static bool CheckTempExists()
        {
            return Directory.Exists(TempFile.TempDirectory);
        }
    }
}
