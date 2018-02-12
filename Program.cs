using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

namespace PicturesApp
{
    public static class Program
    {
        //public const string ConfigPath = @"..\..\Config\config.xml"; //unused, decided to use App.config instead
        public static string DefaultFolderName;
        public static int Counter;
        public static string Path;
        public static string NewPath;
        public static string NewImageName;
        public static string ImageType;
        public static bool VerboseMode = false; //used for debugging
        public static string NewFolderName; //Processed
        public static readonly int SureLimit = 6;

        static void Main(string[] args)
        {
            if (args.Length == 0) StartManualMode();
            else StartDragDropMode(args);
        }


        public static void StartManualMode()
        {
            SetVariables();
            var dir = new DirectoryInfo(DefaultFolderName);
            CopyAndRenameFiles(dir);
            DisplayFinishCounter();
        }

        public static void StartDragDropMode(string[] arguments)
        {
            Console.WriteLine("Drag&Drop Mode. Press any key to continue");
            Console.ReadLine();
            foreach (var folderName in arguments)
            {
                if (!CheckDragDropFolder(folderName)) throw new Exception("Wrong Arguments! Please try manual mode");
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
            if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath)) return true;
            return false;
        }


        //untested, I mostly use LoadVarsFromConfig()
        static void SetVariables()
        {
            Console.WriteLine("Press Y to read variables from config, any key otherwise.");
            ConsoleKeyInfo keyInfo = Console.ReadKey();
            if (keyInfo.Key == ConsoleKey.Y) LoadVarsFromConfig();
            else
            {
                Console.WriteLine("Enter folderName: ");
                DefaultFolderName = Console.ReadLine();
                if (string.IsNullOrEmpty(DefaultFolderName)) DefaultFolderName = "folder";
                Console.WriteLine("Enter imageName: ");
                NewImageName = Console.ReadLine();
                if (string.IsNullOrEmpty(NewImageName)) NewImageName = "image";
                Path = @"C:\TEMP\";
                NewPath = @"C:\TEMP\" + DefaultFolderName;
            }
            if (!Directory.Exists(NewPath)) Directory.CreateDirectory(NewPath);
        }

        //default argument in case of drag&drop
        static void LoadVarsFromConfig(string defaultFolderName = null)
        {
            var _config = new Config();
            string newFolderName = _config.GetNewFolderName();
            string rootFolder = _config.GetRootFolder();
            DefaultFolderName = (defaultFolderName == null) ?
                rootFolder : defaultFolderName;
            NewPath = DefaultFolderName + "\\" + newFolderName;
            NewFolderName = newFolderName;
        }


        static private bool CheckFilenames(FileInfo[] files)
        {
            //if picture names are too short, we'll work without confirmation
            int arewesure = 0;
            int _surelimit;
            if (files.Length < 5) _surelimit = files.Length;
            else _surelimit = 5;
            foreach (var fileInfo in files)
            {
                var filename = fileInfo.Name;
                if (filename.IndexOf('.') != -1)
                {
                    filename = filename.Remove(filename.IndexOf('.'));
                }
                if (filename.Length <= SureLimit)
                    arewesure++;
                if (arewesure >= _surelimit) return true;
            }
            return false;
        }


        static void CopyAndRenameFiles(DirectoryInfo dir)
        {
            bool shortFileNames;
            bool skipFlag = false;
            foreach (var folder in dir.GetDirectories())
            {
                if (folder.Name == NewFolderName) continue;
                var filesCount = folder.GetFiles();
                var dirsCount = folder.GetDirectories();
                if (filesCount.Length == 0 && dirsCount.Length == 0) continue;
                //Thread t = new Thread(() => CopyAndRenameFiles(folder));
                CopyAndRenameFiles(folder);
            }

            Console.WriteLine("\nWorking in " + dir.Name);
            var images = dir.GetFiles();
            var firstImage = images.Where(s => s.Name.EndsWith("jpg") || s.Name.EndsWith("png")).FirstOrDefault();
            if (firstImage == null) Console.WriteLine("No image files");
            else
            {
                shortFileNames = CheckFilenames(images);
                FolderPartOfName = dir.Name;
                string newName = GetNewImageName(dir.Name, firstImage.Name);
                if (!shortFileNames)
                {
                    if (VerboseMode)
                    {
                        Console.WriteLine("Image name would be " + newName + "\nPress M to modify, S to skip, any other key to continue\n");
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

        static void CopyRenameFilesInDirectory(DirectoryInfo dir)
        {
            bool messageShown = false;
            foreach (FileInfo file in dir.GetFiles())
            {
                if (file.FullName.EndsWith("jpg") || file.FullName.EndsWith("JPG"))
                    ImageType = ".jpg";
                else if (file.FullName.EndsWith("png") || file.FullName.EndsWith("PNG"))
                    ImageType = ".png";
                else continue;

                string currentFolder = NewPath + "\\" + dir.Name;
                if (!Directory.Exists(currentFolder)) Directory.CreateDirectory(currentFolder);
                string path = GetNewImageName(dir.Name, file.Name);

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

        static string GetCurrentCounter()
        {
            string count = "_";
            count += Counter.ToString("D4");
            return count;
        }

        private static string _folderPartOfName;
        static string FolderPartOfName
        {
            get
            {
                return _folderPartOfName;
            }
            set
            {
                string temp = value;
                _folderPartOfName = TrimFolderName(temp);
            }
        }


        static string GetNewImageName(string folderName, string oldImageName)
        {
            string trimmedFolderName = FolderPartOfName;//TrimFolderName(folderName);
            int avaliableBytes = 230 - (NewPath.Length + folderName.Length);
            //по-хорошему 248, но не стоит рисковать
            if (avaliableBytes <= 0) throw new Exception("Folder Name too long");
            string newName = trimmedFolderName + " - " + oldImageName;
            if (newName.Length > avaliableBytes)
            {
                Console.WriteLine("Name " + newName + " is too long! Trimming...\n");
                int trimIndex = avaliableBytes - oldImageName.Length - 8;
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
                    string newImageName = oldImageName.Remove(0, oldImageName.Length - 8);
                    newFolderName = trimmedFolderName + " -  ..." + NewImageName;
                }
                newName = newFolderName;
            }
            string newNameFinal = NewPath + "\\" + folderName + "\\" + newName;
            //string newNameFinal = NewPath + "\\" + newName;
            return newNameFinal;
        }


        static string TrimFolderName(string folderName)
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
        string Unfiltered;
        public string OptimizeString(string input)
        {
            Unfiltered = input;
            RemoveDoubleSpaces();
            RemoveDates();
            FixApostrophe();
            return Unfiltered;
        }

        //Olympics  2014 -> Olympics 2014
        private void RemoveDoubleSpaces()
        {
            if (Unfiltered.IndexOf("  ") != -1)
            {
                Unfiltered = Unfiltered.Replace("  ", " ");
            }
        }

        //Family Trip [27052017] -> Family Trip
        private void RemoveDates()
        {
            var rgx = new Regex("\\[\\d+\\]");
            var result = rgx.Match(Unfiltered);
            if (result.Success)
            {
                var substring = result.Value;
                Unfiltered = Unfiltered.Replace(substring, string.Empty);
            }
        }

        //Andrey&#039;s Wedding -> Andrey's Wedding
        private void FixApostrophe()
        {
            string apostropheCode = @"&#039;";
            if (Unfiltered.IndexOf(apostropheCode) != -1)
            {
                Unfiltered = Unfiltered.Replace(apostropheCode, "\'");
            }
        }
    }

}
