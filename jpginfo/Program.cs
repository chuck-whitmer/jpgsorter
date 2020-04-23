using System;
using System.IO;

namespace jpgsorter
{
    class Program
    {
        static string filespec = null;
        static bool verbose = false;
        static bool doSubFolders = false;

        static void Main(string[] args)
        {
            if (!ReadArgs(args)) return;

            string[] filenames = Directory.GetFiles(".", filespec, doSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            foreach (string filename in filenames)
            {


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
                    double zoom = ji.Zoom;
                    if (zoom != 0.0 && zoom != 1.0)
                        Console.WriteLine("Digital zoom: {0:0.0}:1", ji.Zoom);
                    if (ji.HaveGps)
                    {
                        Console.WriteLine("GPS coords: {0}", ji.GpsToString);
                        Console.WriteLine("GPS altitude: {0:0.00}", ji.GpsAltitude);
                        if (ji.HaveGpsTimeStamp)
                            Console.WriteLine("GPS Timestamp: {0} UTC", ji.GpsTimeStamp.ToString("MM/dd/yyyy HH:mm:ss.fff"));
                    }
                    if (verbose)
                    {
                        Console.WriteLine("Exif Attributes:");
                        foreach (IfdElement ifd in ji.ExifAttributes.Values)
                        {
                            Console.WriteLine(ifd.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: {0}", e.Message);
                }
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
                        case "-s":
                            doSubFolders = true;
                            break;
                        case "-v":
                            verbose = true;
                            break;
                        case "-?":
                            PrintUsage();
                            return false;
                        default:
                            Console.WriteLine("Invalid command line switch");
                            return false;
                    }
                }
                else if (filespec == null)
                {
                    filespec = args[i];
                }
                else
                {
                    Console.WriteLine("Invalid command line switch");
                    return false;
                }
            }
            if (filespec == null)
            {
                Console.WriteLine("A filename is required");
                return false;
            }
            return true;
        }

        static void PrintUsage()
        {
            string[] usage = {
@"jpginfo [switches] - Sort picture files into folders by date.",
@" -?  - Prints this usage message."
                };

            foreach (string s in usage)
                Console.WriteLine(s);
        }
    }

}
