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
            Console.WriteLine("File: {0}", filename);
            try
            {
                JpegInfo ji = new JpegInfo(filename);
                Console.WriteLine("File size: {0}", ji.FileSize);
                Console.WriteLine("Camera make: {0}", ji.CameraMake);
                Console.WriteLine("Camera model: {0}", ji.CameraModel);
                Console.WriteLine("Camera orientation: {0}", ji.CameraOrientation);
                if (ji.HaveFileChangeTime)
                    Console.WriteLine("File change time: {0}", ji.FileChangeTime.ToString("MM/dd/yyyy HH:mm:ss.fff"));
                Console.WriteLine("Exposure: {0:0.0000}", ji.Exposure);
                Console.WriteLine("f-stop: {0:0.00}", ji.Fstop);
                Console.WriteLine("Focal length: {0:0.00}", ji.FocalLength);
                Console.WriteLine("ISO: {0}", ji.IsoSpeed);
                if (ji.HaveOriginalTime)
                    Console.WriteLine("Image taken: {0}", ji.OriginalTime.ToString("MM/dd/yyyy HH:mm:ss.fff"));
                Console.WriteLine("Image width: {0}", ji.ImageWidth);
                Console.WriteLine("Image height: {0}", ji.ImageHeight);
                Console.WriteLine("Focal length (on 35mm): {0}", ji.FocalLengthOn35mm);
                if (ji.HaveGps)
                {
                    Console.WriteLine("GPS coords: {0}", ji.GpsToString);
                    Console.WriteLine("GPS altitude: {0:0.00}", ji.GpsAltitude);
                    if (ji.HaveGpsTimeStamp)
                        Console.WriteLine("GPS Timestamp: {0} UTC", ji.GpsTimeStamp.ToString("MM/dd/yyyy HH:mm:ss.fff"));
                }


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
