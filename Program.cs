using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace PicturesApp
{
    public static class Program
    {
        //public const string ConfigPath = @"..\..\Config\config.xml"; //unused, decided to use App.config instead
        private static string DefaultFolderName;
        private static int Counter;
        private static string NewPath;
        private static string NewImageName;
        private static bool VerboseMode = false; //used for debugging
        private static string NewFolderName; //Processed
        private static readonly int SureLimit = 6;
        private static readonly int UpperAvailableCharactersBound = 230;

        static void Main(string[] args)
        {
            if (args.Length == 0) StartManualMode();
            else StartDragDropMode(args);
        }


        private static void StartManualMode()
        {
            SetVariables();
            var dir = new DirectoryInfo(DefaultFolderName);
            CopyAndRenameFiles(dir);
            DisplayFinishCounter();
        }

        private static void StartDragDropMode(IEnumerable<string> arguments)
        {
            Console.WriteLine("Drag&Drop Mode. Press any key to continue");
            Console.ReadLine();
            foreach (var folderName in arguments)
            {
                if (!CheckDragDropFolder(folderName)) 
                    throw new Exception("Wrong Arguments! Please try manual mode");
                LoadVarsFromConfig(folderName);
                var dir = new DirectoryInfo(DefaultFolderName);
                CopyAndRenameFiles(dir);
            }
            DisplayFinishCounter();
        }


        private static void DisplayFinishCounter()
        {
            Console.WriteLine("All done! " + Counter + " files copied");
            Console.ReadLine();
        }

        private static bool CheckDragDropFolder(string folderPath)
        {
            return !string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath);
        }


        //untested, I mostly use LoadVarsFromConfig()
        private static void SetVariables()
        {
            Console.WriteLine("Press Y to read variables from config, any key otherwise.");
            var keyInfo = Console.ReadKey();
            if (keyInfo.Key == ConsoleKey.Y) LoadVarsFromConfig();
            else
            {
                Console.WriteLine("Enter folderName: ");
                DefaultFolderName = Console.ReadLine();
                if (string.IsNullOrEmpty(DefaultFolderName)) DefaultFolderName = "folder";
                Console.WriteLine("Enter imageName: ");
                NewImageName = Console.ReadLine();
                if (string.IsNullOrEmpty(NewImageName)) NewImageName = "image";
                NewPath = @"C:\TEMP\" + DefaultFolderName;
            }
            if (!Directory.Exists(NewPath)) Directory.CreateDirectory(NewPath);
        }

        //default argument in case of drag&drop
        private static void LoadVarsFromConfig(string defaultFolderName = null)
        {
            var _config = new Config();
            var newFolderName = _config.GetNewFolderName();
            var rootFolder = _config.GetRootFolder();
            DefaultFolderName = defaultFolderName ?? rootFolder;
            NewPath = DefaultFolderName + "\\" + newFolderName;
            NewFolderName = newFolderName;
        }


        private static bool CheckFilenames(IReadOnlyCollection<FileInfo> files)
        {
            //if picture names are too short, we'll work without confirmation
            var arewesure = 0;
            int surelimit;
            surelimit = files.Count < 5 ? files.Count : 5;
            foreach (var fileInfo in files)
            {
                var filename = fileInfo.Name;
                if (filename.IndexOf('.') != -1)
                {
                    filename = filename.Remove(filename.IndexOf('.'));
                }
                if (filename.Length <= SureLimit)
                    arewesure++;
                if (arewesure >= surelimit) return true;
            }
            return false;
        }


        private static void CopyAndRenameFiles(DirectoryInfo dir)
        {
            var skipFlag = false;
            foreach (var folder in dir.GetDirectories())
            {
                if (folder.Name == NewFolderName) continue;
                var filesCount = folder.GetFiles();
                var dirsCount = folder.GetDirectories();
                if (filesCount.Length == 0 && dirsCount.Length == 0) continue;
                CopyAndRenameFiles(folder);
            }

            Console.WriteLine("\nWorking in " + dir.Name);
            var images = dir.GetFiles();
            var firstImage = images.FirstOrDefault(s => s.Name.EndsWith("jpg") || s.Name.EndsWith("png"));
            if (firstImage == null) 
                Console.WriteLine("No image files");
            else
            {
                var shortFileNames = CheckFilenames(images);
                FolderPartOfName = dir.Name;
                var newName = GetNewImageName(dir.Name, firstImage.Name);
                if (!shortFileNames)
                {
                    if (VerboseMode)
                    {
                        Console.WriteLine("Image name would be " + newName + 
                                          "\nPress M to modify, S to skip, any other key to continue\n");
                        var keyInfo = Console.ReadKey();
                        if (keyInfo.Key == ConsoleKey.M)
                        {
                            Console.WriteLine("Please enter new name: ");
                            NewImageName = Console.ReadLine();
                        }
                        else if (keyInfo.Key == ConsoleKey.S)
                        {
                            skipFlag = true;
                        }
                        else NewImageName = newName;
                    }
                    else
                        NewImageName = newName;
                }
                else
                {
                    Console.WriteLine("Short names detected. Working without confirmation...\n");
                    NewImageName = newName;
                }
            }

            if (!skipFlag)
            {
                CopyRenameFilesInDirectory(dir);
            }
        }

        private static void CopyRenameFilesInDirectory(DirectoryInfo dir)
        {
            var messageShown = false;
            foreach (var file in dir.GetFiles())
            {
                var currentFolder = NewPath + "\\" + dir.Name;
                if (!Directory.Exists(currentFolder)) Directory.CreateDirectory(currentFolder);
                var path = GetNewImageName(dir.Name, file.Name);

                if (File.Exists(path))
                {
                    Console.WriteLine("File " + file.FullName + " already exists. Press any key to overwrite");
                    Console.ReadLine();
                }

                if (VerboseMode && messageShown == false)
                {
                    Console.WriteLine("Saving to new path " + path + "\n");
                    messageShown = true;
                }
                file.CopyTo(path);

                Counter++;
            }
        }

        private static string _folderPartOfName;
        static string FolderPartOfName
        {
            get => _folderPartOfName;
            set
            {
                var temp = value;
                _folderPartOfName = TrimFolderName(temp);
            }
        }


        private static string GetNewImageName(string folderName, string oldImageName)
        {
            var trimmedFolderName = FolderPartOfName;
            var availableBytes = UpperAvailableCharactersBound - (NewPath.Length + folderName.Length);
            if (availableBytes <= 0) throw new Exception("Folder Name too long");
            var newName = trimmedFolderName + " - " + oldImageName;
            if (newName.Length > availableBytes)
            {
                Console.WriteLine("Name " + newName + " is too long! Trimming...\n");
                var trimIndex = availableBytes - oldImageName.Length - 8;
                if (trimIndex <= 0) throw new Exception("Cannot trim properly, names too long");

                string newFolderName;
                if (trimmedFolderName.Length > oldImageName.Length)
                {
                    newFolderName = trimmedFolderName.Remove(trimIndex);
                    newFolderName += "... - ";
                    newFolderName += oldImageName;
                }
                else
                {
                    var newImageName = oldImageName.Remove(0, oldImageName.Length - 8);
                    newFolderName = trimmedFolderName + " -  ..." + newImageName;
                }
                newName = newFolderName;
            }
            var newNameFinal = NewPath + "\\" + folderName + "\\" + newName;
            return newNameFinal;
        }


        private static string TrimFolderName(string folderName)
        {
            string trimmed;
            try
            {
                var so = new StringOptimization();
                trimmed = so.OptimizeString(folderName);
            }
            catch
            {
                Console.WriteLine("Exception while trimming!");
                trimmed = folderName;
            }
            return trimmed;

        }        

    }


    public class StringOptimization
    {
        private string _unfiltered;
        public string OptimizeString(string input)
        {
            _unfiltered = input;
            RemoveDoubleSpaces();
            RemoveDates();
            FixApostrophe();
            return _unfiltered;
        }

        //Olympics  2014 -> Olympics 2014
        private void RemoveDoubleSpaces()
        {
            if (_unfiltered.IndexOf("  ") != -1)
            {
                _unfiltered = _unfiltered.Replace("  ", " ");
            }
        }

        //Family Trip [27052017] -> Family Trip
        private void RemoveDates()
        {
            var rgx = new Regex("\\[\\d+\\]");
            var result = rgx.Match(_unfiltered);
            if (!result.Success) return;
            
            var substring = result.Value;
            _unfiltered = _unfiltered.Replace(substring, string.Empty);
        }

        //Andrey&#039;s Wedding -> Andrey's Wedding
        private void FixApostrophe()
        {
            var apostropheCode = @"&#039;";
            if (_unfiltered.IndexOf(apostropheCode) != -1)
            {
                _unfiltered = _unfiltered.Replace(apostropheCode, "\'");
            }
        }
    }

}
