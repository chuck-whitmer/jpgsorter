using System;
using System.IO;
using System.Collections.Generic;

namespace jpgsorter
{
    class Program
    {
        static string files = null;
        static bool doSubFolders = false;
        static bool deleteDuplicates = false;
        static string directory = ".";

        static void Main(string[] args)
        {
            if (!ReadArgs(args)) return;
            Console.WriteLine("Directory: {0}  Files: {1}", directory, files);
            if (!Directory.Exists(directory))
                Console.WriteLine("Given directory {0} is invalid", directory);
            string[] filenames = Directory.GetFiles(directory, files, doSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            List<string> badFiles = new List<string>();
            List<string> collisions = new List<string>();
            StreamWriter log = File.AppendText("jpegsort.log");
            log.WriteLine(":: {0}", DateTime.Now);
            log.WriteLine(":: {0}", Environment.CommandLine);
            foreach (string filename in filenames)
            {
                bool haveTime = false;
                DateTime dt = new DateTime();
                try
                {
                    JpegInfo ji = new JpegInfo(filename);
                    {
                        if (ji.HaveOriginalTime)
                        {
                            dt = ji.OriginalTime;
                            haveTime = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    //Console.WriteLine(" Exception: {0} in file {1}", e.Message, filename);
                }
                if (haveTime)
                {
                    string yearDirectory = string.Format("{0,4}", dt.Year);
                    string monthDirectory = string.Format("{0}\\{1,2:00}", yearDirectory, dt.Month);
                    string dayDirectory = string.Format("{0}\\{1,2:00}", monthDirectory, dt.Day);
                    if (!Directory.Exists(yearDirectory))
                        Directory.CreateDirectory(yearDirectory);
                    if (!Directory.Exists(monthDirectory))
                        Directory.CreateDirectory(monthDirectory);
                    if (!Directory.Exists(dayDirectory))
                        Directory.CreateDirectory(dayDirectory);

                    string newFileLocation = string.Format("{0}\\{1}", dayDirectory, new FileInfo(filename).Name);
                    if (!File.Exists(newFileLocation))
                    {
                        log.WriteLine("move {0} {1}", filename, newFileLocation);
                        Console.WriteLine("{0}", newFileLocation);
                        File.Move(filename, newFileLocation);
                    }
                    else
                    {
                        JpegInfo jiExisting = new JpegInfo(newFileLocation);
                        bool resolvedCollision = false;
                        if (jiExisting.HaveOriginalTime && DateTime.Compare(dt, jiExisting.OriginalTime) == 0)
                        {
                            if (deleteDuplicates)
                            {
                                Console.WriteLine("{0} already exists, times match - Deleting {1}", newFileLocation, filename);
                                log.WriteLine(":: {0} already exists, times match - Deleting {1}", newFileLocation, filename);
                                File.Delete(filename);
                                resolvedCollision = true;
                            }
                            else
                            {
                                Console.WriteLine("{0} already exists, times match", newFileLocation);
                            }
                        }
                        else
                        {
                            Console.WriteLine("{0} already exists, times do not match", newFileLocation);
                        }
                        if (!resolvedCollision) collisions.Add(filename);
                    }
                }
                else
                {
                    badFiles.Add(filename);
                }
            }
            Console.WriteLine("{0} files processed", filenames.Length);
            if (badFiles.Count > 0)
            {
                Console.WriteLine("{0} file{1} had no original date", badFiles.Count, badFiles.Count>1 ? "s":"");
                Console.WriteLine("Unsorted files:");
                foreach (string str in badFiles)
                    Console.WriteLine(str);
            }
            if (collisions.Count > 0)
            {
                Console.WriteLine("{0} file{1} already in destination folder", collisions.Count, collisions.Count > 1 ? "s" : "");
                Console.WriteLine("Unresolved Collisions:");
                foreach (string str in collisions)
                    Console.WriteLine(str);
            }
            log.WriteLine();
            log.Close();
        }

        static bool ReadArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i][0] == '-')
                {
                    switch (args[i])
                    {
                        case "-d":
                            i++;
                            if (i >= args.Length)
                            {
                                Console.WriteLine("Missing directory name");
                                return false;
                            }
                            directory = args[i];
                            break;
                        case "-s":
                            doSubFolders = true;
                            break;
                        case "-x":
                            deleteDuplicates = true;
                            break;
                        default:
                            Console.WriteLine("Invalid command line switch");
                            return false;
                        case "-?":
                            PrintUsage();
                            return false;
                    }
                }
                else if (files == null)
                {
                    files = args[i];
                }
                else
                {
                    Console.WriteLine("Invalid command line switch");
                    return false;
                }
            }
            if (files == null)
            {
                Console.WriteLine("A filename is required");
                return false;
            }
            return true;
        }

        static void PrintUsage()
        {
            string[] usage = {
@"jpgsort [switches] - Sort picture files into folders by date.",
@" -?  - Prints this usage message."
                };

            foreach (string s in usage)
                Console.WriteLine(s);
        }
    }

}
