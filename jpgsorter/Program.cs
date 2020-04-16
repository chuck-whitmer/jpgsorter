using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jpgsorter
{
    class Program
    {
        static string filename = null;

        static void Main(string[] args)
        {
            if (!ReadArgs(args)) return;
            Console.WriteLine("File is {0}", filename);
            try
            {
                JpegInfo ji = new JpegInfo(filename);
                Console.WriteLine("File size = {0}", ji.FileSize);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
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
                        default:
                            Console.WriteLine("Invalid command line switch");
                            return false;
                        case "-?":
                            PrintUsage();
                            return false;
                    }
                }
                else if (filename == null)
                {
                    filename = args[i];
                }
                else
                {
                    Console.WriteLine("Invalid command line switch");
                    return false;
                }
            }
            if (filename == null)
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
