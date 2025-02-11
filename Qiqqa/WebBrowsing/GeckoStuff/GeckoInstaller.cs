﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Qiqqa.Common.Configuration;
using Utilities;
using Utilities.ProcessTools;
using Utilities.Strings;

namespace Qiqqa.WebBrowsing.GeckoStuff
{
    public class GeckoInstaller
    {
        private static readonly string XULPackageFilename = ConfigurationManager.Instance.StartupDirectoryForQiqqa + @"xulrunner-33.1.1.en-US.win32.zip";
        private static readonly string UnpackDirectoryDirectory = ConfigurationManager.Instance.BaseDirectoryForQiqqa + @"xulrunner-33\";

        internal static void CheckForInstall()
        {
            bool should_install = false;

            if (!Directory.Exists(InstallationDirectory))
            {
                Logging.Info("XULRunner directory {0} does not exist, so installing it.", InstallationDirectory);
                should_install = true;
            }
            else
            {
                IEnumerable<string> directory_contents = Directory.EnumerateFiles(InstallationDirectory, "*.*", SearchOption.AllDirectories);
                int directory_contents_count = directory_contents.Count();
                if (46 != directory_contents_count)
                {
                    string directory_contents_string = StringTools.ConcatenateStrings(directory_contents, "\n\t");
                    Logging.Warn("XULRunner directory {0} does not contain all necessary files (only {2} files), so reinstalling it.  The contents were:\n\t{1}", InstallationDirectory, directory_contents_string, directory_contents_count);
                    should_install = true;
                }
            }
            
            if (should_install)
            {
                Logging.Info("Installing XULRunner into {0}.", InstallationDirectory);
                Directory.CreateDirectory(InstallationDirectory);

                string process_parameters = String.Format("x -y \"{0}\" -o\"{1}\"", XULPackageFilename, UnpackDirectoryDirectory);
                using (Process process = ProcessSpawning.SpawnChildProcess(ConfigurationManager.Instance.Program7ZIP, process_parameters, ProcessPriorityClass.Normal))
                {
                    using (ProcessOutputReader process_output_reader = new ProcessOutputReader(process))
                    {
                        process.WaitForExit();
                    }
                }

                Logging.Info("XULRunner installed.");
            }
        }

        
        public static readonly string InstallationDirectory = UnpackDirectoryDirectory + @"xulrunner\";
    }
}
