using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jpgsorter
{
    class JpegInfo
    {
        public string FileName { get; private set; }
        public long FileSize { get; private set; }
        public DateTime FileTime { get; private set; }

        public Dictionary<UInt16, IfdElement> TiffAttributes = new Dictionary<ushort, IfdElement>();
        public Dictionary<UInt16, IfdElement> ExifAttributes = new Dictionary<ushort, IfdElement>();
        public Dictionary<UInt16, IfdElement> GpsAttributes = new Dictionary<ushort, IfdElement>();

        public JpegInfo(string filename)
        {
            FileName = filename;
            FileInfo fi = new FileInfo(filename);
            if (!fi.Exists) throw new Exception(string.Format("File {0} does not exist", filename));
            FileSize = fi.Length;
            FileTime = fi.CreationTime;
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                if (ReadBigEndianUshort(fs) != 0xFFD8) throw new Exception(string.Format("File {0} is not a JPG image file", filename));
                byte[] tiffData = TryToLoadExifData(fs);
                // Give it only two shots. It should be at the start of the file.
                if (tiffData == null) tiffData = TryToLoadExifData(fs);
                if (tiffData == null) throw new Exception(string.Format("EXIF data not found in file {0}", filename));
                // Read the byte order.
                string byteOrder = Encoding.UTF8.GetString(tiffData, 0, 2);
                bool isIntelByteOrder = byteOrder == "II"; // Intel order is Little Endian
                if (!isIntelByteOrder)  // Otherwise better be Motorola byte ordering
                {
                    if (byteOrder != "MM") throw new Exception("Invalid byte ordering");
                }
                // Check the TIFF Header identifier.
                if (GetUint16(tiffData, 2, isIntelByteOrder) != 42)
                    throw new Exception("TIFF Header lacks id tag");
                int ifdOffset = GetInt32(tiffData, 4, isIntelByteOrder) + 2;
                int ifdCount = GetUint16(tiffData, 8, isIntelByteOrder);
                for (int i = 0; i < ifdCount; i++)
                {
                    IfdElement ifd = new IfdElement(tiffData, ifdOffset + 12 * i, isIntelByteOrder);
                    TiffAttributes.Add(ifd.Tag, ifd);
                }
                //Console.WriteLine("TIFF Attributes:");
                //foreach (IfdElement ifd in TiffAttributes.Values)
                //{
                //    Console.WriteLine(ifd.ToString());
                //}

                // Look for the Exif IFD.
                if (TiffAttributes.ContainsKey(0x8769))
                {
                    int newIdx = TiffAttributes[0x8769].Ints[0];
                    ifdOffset = newIdx + 2;
                    ifdCount = GetUint16(tiffData, newIdx, isIntelByteOrder);
                    for (int i = 0; i < ifdCount; i++)
                    {
                        IfdElement ifd = new IfdElement(tiffData, ifdOffset + 12 * i, isIntelByteOrder);
                        ExifAttributes.Add(ifd.Tag, ifd);
                    }
                    //Console.WriteLine("Exif Attributes:");
                    //foreach (IfdElement ifd in ExifAttributes.Values)
                    //{
                    //    Console.WriteLine(ifd.ToString());
                    //}
                }

                // Look for GPS data
                if (TiffAttributes.ContainsKey(0x8825))
                {
                    int newIdx = TiffAttributes[0x8825].Ints[0];
                    ifdOffset = newIdx + 2;
                    ifdCount = GetUint16(tiffData, newIdx, isIntelByteOrder);
                    for (int i = 0; i < ifdCount; i++)
                    {
                        IfdElement ifd = new IfdElement(tiffData, ifdOffset + 12 * i, isIntelByteOrder);
                        GpsAttributes.Add(ifd.Tag, ifd);
                    }
                    //Console.WriteLine("GPS Attributes:");
                    //foreach (IfdElement ifd in GpsAttributes.Values)
                    //{
                    //    Console.WriteLine(ifd.ToString());
                    //}
                }
            }
        }

        static UInt16 GetUint16(byte[] jj, int offset, bool isIntel)
        {
            if (isIntel)
                return (UInt16)((jj[offset + 1] << 8) + jj[offset]);
            else
                return (UInt16)((jj[offset] << 8) + jj[offset + 1]);
        }

        static UInt32 GetUint32(byte[] jj, int offset, bool isIntel)
        {
            if (isIntel)
                return (UInt32)((jj[offset + 3] << 24) + (jj[offset + 2] << 16) + (jj[offset + 1] << 8) + jj[offset]);
            else
                return (UInt32)((jj[offset] << 24) + (jj[offset + 1] << 16) + (jj[offset + 2] << 8) + jj[offset + 3]);
        }

        static Int32 GetInt32(byte[] jj, int offset, bool isIntel)
        {
            if (isIntel)
                return ((jj[offset + 3] << 24) + (jj[offset + 2] << 16) + (jj[offset + 1] << 8) + jj[offset]);
            else
                return ((jj[offset] << 24) + (jj[offset + 1] << 16) + (jj[offset + 2] << 8) + jj[offset + 3]);
        }

        public bool HaveGps
        {
            get
            {
                return GpsAttributes.ContainsKey(0x0002) && GpsAttributes.ContainsKey(0x0004);
            }
        }

        double GenericDouble(Dictionary<UInt16, IfdElement> dict, UInt16 tag)
        {
            if (!dict.ContainsKey(tag)) return 0.0;
            return dict[tag].Doubles[0];
        }

        int GenericInt(Dictionary<UInt16, IfdElement> dict, UInt16 tag)
        {
            if (!dict.ContainsKey(tag)) return 0;
            return dict[tag].Ints[0];
        }

        string GenericString(Dictionary<UInt16, IfdElement> dict, UInt16 tag)
        {
            if (!dict.ContainsKey(tag)) return null;
            return dict[tag].Text;
        }

        public double Zoom
        {
            get
            {
                return GenericDouble(ExifAttributes, 0xA404);
            }
        }

        public double GpsLatitude
        {
            get
            {
                if (!GpsAttributes.ContainsKey(0x01) || !GpsAttributes.ContainsKey(0x02))
                    return 0.0;
                bool isNorth = GpsAttributes[0x01].Text == "N";
                double[] ee = GpsAttributes[0x02].Doubles;
                double degrees = ee[0] + ee[1] / 60.0 + ee[2] / 3600.0;
                if (!isNorth) degrees = -degrees;
                return degrees;
            }
        }

        public double GpsLongitude
        {
            get
            {
                if (!GpsAttributes.ContainsKey(0x03) || !GpsAttributes.ContainsKey(0x04))
                    return 0.0;
                bool isEast = GpsAttributes[0x03].Text == "E";
                double[] ee = GpsAttributes[0x04].Doubles;
                double degrees = ee[0] + ee[1] / 60.0 + ee[2] / 3600.0;
                if (!isEast) degrees = -degrees;
                return degrees;
            }
        }

        public double GpsAltitude
        {
            get
            {
                if (!GpsAttributes.ContainsKey(0x05) || !GpsAttributes.ContainsKey(0x06))
                    return 0.0;
                bool isUp = GpsAttributes[0x05].Bytes[0] == 0;
                double alt = GpsAttributes[0x06].Doubles[0];
                if (!isUp) alt = -alt;
                return alt;
            }
        }

        public string GpsToString
        {
            get
            {
                double lat = GpsLatitude;
                double lon = GpsLongitude;
                return string.Format("{0:0.00000}{1} {2:0.00000}{3}",
                    Math.Abs(lat), lat > 0.0 ? "N" : "S",
                    Math.Abs(lon), lon > 0.0 ? "E" : "W");
            }
        }

        public bool HaveGpsTimeStamp
        {
            get
            {
                return GpsAttributes.ContainsKey(0x1D) && GpsAttributes.ContainsKey(0x07);
            }
        }

        static char[] dtSeparators = new char[] { ' ', ':' };

        public DateTime GpsTimeStamp
        {
            get
            {
                string[] words = GpsAttributes[0x1D].Text.Split(dtSeparators);
                int year = int.Parse(words[0]);
                int month = int.Parse(words[1]);
                int day = int.Parse(words[2]);
                double[] ee = GpsAttributes[0x07].Doubles;
                return new DateTime(year, month, day).AddHours(ee[0]).AddMinutes(ee[1]).AddSeconds(ee[2]);
            }
        }

        public double Exposure
        {
            get
            {
                return GenericDouble(ExifAttributes, 0x829A);
            }
        }

        public double Fstop
        {
            get
            {
                return GenericDouble(ExifAttributes, 0x829D);
            }
        }

        public double FocalLength
        {
            get
            {
                return GenericDouble(ExifAttributes, 0x920A);
            }
        }

        public int FocalLengthOn35mm
        {
            get
            {
                return GenericInt(ExifAttributes, 0xA405);
            }
        }

        public int IsoSpeed
        {
            get
            {
                return GenericInt(ExifAttributes, 0x8827);
            }
        }

        // A DateTime is not nullable, so we need another way to check.
        public bool HaveOriginalTime
        {
            get
            {
                return ExifAttributes.ContainsKey(0x9003);
            }
        }

        public DateTime OriginalTime
        {
            get
            {
                DateTime dt = dtFromString(ExifAttributes[0x9003].Text);
                if (ExifAttributes.ContainsKey(0x9291))
                {
                    string str = ExifAttributes[0x9291].Text.TrimEnd();
                    if (str.Length > 0)
                    {
                        int n = int.Parse(str);
                        double deltaMilliseconds = n * 1000.0 / Math.Pow(10.0, (double)str.Length);
                        dt = dt.AddMilliseconds(deltaMilliseconds);
                    }
                }
                return dt;
            }
        }

        public int ImageWidth
        {
            get
            {
                return GenericInt(ExifAttributes, 0xA002);
            }
        }

        public int ImageHeight
        {
            get
            {
                return GenericInt(ExifAttributes, 0xA003);
            }
        }

        public string CameraMake
        {
            get
            {
                return GenericString(TiffAttributes, 0x10F);
            }
        }

        public string CameraModel
        {
            get
            {
                return GenericString(TiffAttributes, 0x110);
            }
        }

        public int CameraOrientation
        {
            get
            {
                return GenericInt(TiffAttributes, 0x112);
            }
        }

        // A DateTime is not nullable, so we need another way to check.
        public bool HaveFileChangeTime 
        {
            get
            {
                return TiffAttributes.ContainsKey(0x132);
            }
        }

        public DateTime FileChangeTime
        {
            get
            {
                DateTime dt = dtFromString(TiffAttributes[0x132].Text);
                if (ExifAttributes.ContainsKey(0x9290))
                {
                    string str = ExifAttributes[0x9290].Text.TrimEnd();
                    if (str.Length > 0)
                    {
                        int n = int.Parse(str);
                        double deltaMilliseconds = n * 1000.0 / Math.Pow(10.0, (double)str.Length);
                        dt = dt.AddMilliseconds(deltaMilliseconds);
                    }
                }
                return dt;
            }
        }


        public DateTime dtFromString(string str)
        {
            string[] words = str.Split(dtSeparators);
            if (words.Length != 6) throw new Exception(string.Format("Invalid date/time in file {0}",FileName));
            return new DateTime(int.Parse(words[0]), int.Parse(words[1]), int.Parse(words[2]), int.Parse(words[3]), int.Parse(words[4]), int.Parse(words[5]));
        }

        UInt16 ReadBigEndianUshort(FileStream fs)
        {
            byte[] bytePair = new byte[2];
            if (fs.Read(bytePair, 0, 2) < 2) throw new Exception(string.Format("Unexpected end of file {0}",FileName));
            return GetUint16(bytePair, 0, false);
        }

        byte[] TryToLoadExifData(FileStream fs)
        {
            // Read marker and length.
            UInt16 marker = ReadBigEndianUshort(fs);
            int length = ReadBigEndianUshort(fs);
            // Read EXIF header
            byte[] header = new Byte[6];
            fs.Read(header, 0, 6);
            // We read the data even if not returning the record, so as to
            // position the next read correctly.
            byte[] data = new Byte[length - 8];
            if (fs.Read(data, 0, data.Length)<data.Length) throw new Exception(string.Format("Unexpected end of file {0}", FileName));
            // Return the data if the marker and EXIF header match.
            if (marker == 0xFFE1u && ByteArraysSame(header, new byte[] { (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0 }))
                return data;
            else
            {
                //Console.WriteLine("failed: marker = {0,4:X}  header = {1}", marker, Encoding.UTF8.GetString(header));
                return null;
            }
        }

        static bool ByteArraysSame(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i=0; i<a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}
