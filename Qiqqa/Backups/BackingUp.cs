﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Qiqqa.Common.Configuration;
using Utilities.DateTimeTools;
using Utilities.GUI;

namespace Qiqqa.Backups
{
    class BackingUp
    {
        public static void DoBackup()
        {
            string DEFAULT_FILENAME = String.Format("QiqqaBackup.{0}.qiqqa_backup", DateFormatter.asYYYYMMDDHHMMSS(DateTime.Now));

            SaveFileDialog save_file_dialog = new SaveFileDialog();
            save_file_dialog.AddExtension = true;
            save_file_dialog.CheckPathExists = true;
            save_file_dialog.DereferenceLinks = true;
            save_file_dialog.OverwritePrompt = true;
            save_file_dialog.ValidateNames = true;
            save_file_dialog.DefaultExt = "qiqqa_backup";
            save_file_dialog.Filter = "Qiqqa backup files (*.qiqqa_backup)|*.qiqqa_backup|All files (*.*)|*.*";
            save_file_dialog.FileName = DEFAULT_FILENAME;
            save_file_dialog.Title = "Please select the file to which you want to backup.";

            if (true != save_file_dialog.ShowDialog())
            {
                MessageBoxes.Warn("You have cancelled your backup of the Qiqqa database.");
                return;
            }

            string target_filename = save_file_dialog.FileName;
            string source_directory = ConfigurationManager.Instance.BaseDirectoryForQiqqa;
                
            string parameters = String.Format("a -tzip -mm=Deflate -mmt=on -mx9 \"{0}\" \"{1}\\*\"", target_filename, source_directory);
            Process.Start(ConfigurationManager.Instance.Program7ZIP, parameters);
        }

        public static void DoRestore()
        {
            string target_directory = ConfigurationManager.Instance.BaseDirectoryForQiqqa;

            // Warn if we are about to overwrite...
            if (Directory.Exists(target_directory))
            {
                var files_in_folder = Directory.EnumerateFiles(target_directory);

                if (0 < files_in_folder.Count())
                {
                    if (!MessageBoxes.AskQuestion("You are about to overwrite {0} with this restore.  Files already in that directory may be overwritten.  Are you sure you want to continue with your restore?", target_directory))
                    {
                        MessageBoxes.Warn("You have cancelled your restore of the Qiqqa database.");
                        return;
                    }
                }
            }            
            
            OpenFileDialog open_file_dialog = new OpenFileDialog();
            open_file_dialog.AddExtension = true;
            open_file_dialog.CheckPathExists = true;
            open_file_dialog.DereferenceLinks = true;
            open_file_dialog.ValidateNames = true;
            open_file_dialog.DefaultExt = "qiqqa_backup";
            open_file_dialog.Filter = "Qiqqa backup files (*.qiqqa_backup)|*.qiqqa_backup|All files (*.*)|*.*";
            open_file_dialog.Title = "Please select the file from a previous Qiqqa Backup that you wish to Restore.";

            if (true != open_file_dialog.ShowDialog())
            {
                MessageBoxes.Warn("You have cancelled your restore of the Qiqqa database.");
                return;
            }

            string source_filename = open_file_dialog.FileName;
            
            // Check that the target directory exists
            if (!Directory.Exists(target_directory))
            {
                Directory.CreateDirectory(target_directory);
            }

            string parameters = String.Format("x \"{0}\" -o\"{1}\"", source_filename, target_directory);
            Process.Start(ConfigurationManager.Instance.Program7ZIP, parameters);            
        }

        internal static void DoBackupRestoreInstructions()
        {
            if (ConfigurationManager.Instance.IsGuest)
            {
                MessageBoxes.Info("To backup or restore your Qiqqa Database, you need to please restart Qiqqa.  You will see the Backup/Restore option at the bottom of the login window.");
            }
            else
            {
                MessageBoxes.Info("To backup or restore your Qiqqa Database, you need to please log out of Qiqqa (by clicking on the logout link at the bottom of the window).  Then, restart Qiqqa and you will see the Backup/Restore option at the bottom of the login window.");
            }
        }
    }
}
