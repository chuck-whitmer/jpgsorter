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


        public JpegInfo(string filename)
        {
            FileName = filename;
            FileInfo fi = new FileInfo(filename);
            if (!fi.Exists) throw new Exception(string.Format("File {0} does not exist", filename));
            FileSize = fi.Length;
            FileTime = fi.CreationTime;
            FileStream fs = new FileStream(filename, FileMode.Open,FileAccess.Read);
            if (ReadBigEndianUshort(fs) != 0xFFD8u) throw new Exception(string.Format("File {0} is not a JPG image file", filename));
            byte[] tiffData = TryToLoadExifData(fs);
            // Give it only two shots. It should be at the start of the file.
            if (tiffData == null) tiffData = TryToLoadExifData(fs);
            if (tiffData == null) throw new Exception(string.Format("EXIF data not found in file {0}", filename));
            // Read the byte order.
            string byteOrder = Encoding.UTF8.GetString(tiffData, 0, 2);
            // Leave Motorola byte order unhandled until we see one in the wild.
            if (byteOrder != "II")
                throw new Exception(string.Format("Tell Baba: unhandled byte order {0} in file {1}", byteOrder, filename));
            // Check the TIFF Header identifier.
            if (BitConverter.ToUInt16(tiffData, 2) != 42)
                throw new Exception("TIFF Header lacks id tag");
            int ifdOffset = BitConverter.ToInt32(tiffData, 4) + 2;
            int ifdCount = BitConverter.ToInt16(tiffData, 8);
            for (int i = 0; i < ifdCount; i++)
            {
                IfdElement ifd = new IfdElement(tiffData, ifdOffset + 12 * i);
                Console.WriteLine(ifd.ToString());
            }





        }

        void HandleIFD(byte[] data, int offset)
        {
            UInt16 tag = BitConverter.ToUInt16(data, offset);
            int type = BitConverter.ToUInt16(data, offset + 2);
            int count = BitConverter.ToInt32(data, offset + 4);
            switch (type)
            {
                case 2: // String
                    int idx = (count < 5) ? offset + 8 : BitConverter.ToInt32(data, offset + 8);
                    string str = Encoding.UTF8.GetString(data, idx, count - 1);
                    Console.WriteLine(" ifd tag {0,4:X}  \"{1}\"", tag, str);
                    break;
                default:
                    Console.WriteLine(" ifd tag {0,4:X} type {1} count {2}  {3}", tag, type, count, BitConverter.ToString(data, offset + 8, 4));
                    break;
            }
        }

        static UInt16 ReadBigEndianUshort(FileStream fs)
        {
            byte[] bytePair = new byte[2];
            if (fs.Read(bytePair, 0, 2) < 2) throw new Exception("Unexpected end of file");
            byte[] flippedPair = new byte[] {bytePair[1], bytePair[0]};
            return BitConverter.ToUInt16(flippedPair,0);
        }

        static byte[] TryToLoadExifData(FileStream fs)
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
            fs.Read(data, 0, data.Length);
            // Return the data if the marker and EXIF header match.
            if (marker == 0xFFE1u && ByteArraysSame(header, new byte[] {(byte) 'E', (byte)'x', (byte)'i', (byte)'f', 0, 0 }))
                return data;
            else
                return null;
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
