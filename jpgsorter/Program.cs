using System;
using System.IO;
using System.Collections.Generic;

namespace jpgsorter
{
    class Program
    {
        static string files = null;
        static bool doSubFolders = false;
        static string directory = ".";

        static void Main(string[] args)
        {
            if (!ReadArgs(args)) return;
            Console.WriteLine("Directory: {0}  File: {1}", directory, files);
            if (!Directory.Exists(directory))
                Console.WriteLine("Given directory {0} is invalid", directory);
            string[] filenames = Directory.GetFiles(directory, files, doSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            List<string> badFiles = new List<string>();
            int count = 0;
            foreach (string filename in filenames)
            {
                bool haveTime = false;
                DateTime dt;
                try
                {
                    JpegInfo ji = new JpegInfo(filename);
                    if (ji.HaveOriginalTime)
                    {
                        dt = ji.OriginalTime;
                        haveTime = true;
                    }
                }
                catch (Exception e)
                {
                    //Console.WriteLine(" Exception: {0} in file {1}", e.Message, filename);
                }
                if (!haveTime)
                    badFiles.Add(filename);
                count++;
                if (count % 10 == 0)
                {
                    Console.Write("{0}\r", count);
                }
            }
            Console.WriteLine("{0} files processed", filenames.Length);
            Console.WriteLine("{0} files had no original date", badFiles.Count);
            if (badFiles.Count > 0)
            {
                Console.WriteLine("Unsorted files:");
                foreach (string str in badFiles)
                    Console.WriteLine(str);
            }
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
