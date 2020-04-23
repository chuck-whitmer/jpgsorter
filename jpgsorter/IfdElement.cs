using System;
using System.Text;

namespace jpgsorter
{
    class IfdElement
    {
        public UInt16 Tag { get; private set; }
        public string Text { get; private set; }
        public byte[] Bytes { get; private set; }
        public int[] Ints { get; private set; }
        public int[] Numerators { get; private set; }
        public int[] Denominators { get; private set; }
        public double[] Doubles { get; private set; }
        public int Type { get; private set; }

        static int[] typeSizes = new int[] { 0, 1, 1, 2, 4, 8, 0, 1, 0, 4, 8 };

        public IfdElement(byte[] data, int offset, bool isIntel)
        {
            Tag = GetUint16(data, offset, isIntel);
            Type = GetUint16(data, offset + 2, isIntel);
            int count = GetInt32(data, offset + 4, isIntel);
            if (Type < 0 || Type >= typeSizes.Length)
                throw new Exception(string.Format("Bad type in IFD element. type = {0} offset = {1}",Type,offset));
            int typeSize = typeSizes[Type];
            int idx = (count*typeSize < 5) ? offset + 8 : GetInt32(data, offset + 8, isIntel);
            switch (Type)
            {
                case 1: // Byte
                case 7: // Undefined
                    Bytes = new byte[count];
                    Array.Copy(data, idx, Bytes, 0, count);
                    break;
                case 2: // Ascii
                    Text = Encoding.UTF8.GetString(data, idx, count - 1);
                    break;
                case 3: // Short (UInt16)
                    Ints = new int[count];
                    for (int i = 0; i < count; i++)
                        Ints[i] = (int)GetUint16(data, idx + 2 * i, isIntel);
                    break;
                case 4: // Long (UInt32)
                    Ints = new int[count];
                    for (int i = 0; i < count; i++)
                        Ints[i] = GetInt32(data, idx + 4 * i, isIntel);
                    break;
                case 5: // Rational (2 x UInt32)
                case 10: // Srational (2 x Int32)
                    Numerators = new int[count];
                    Denominators = new int[count];
                    Doubles = new double[count];
                    for (int i = 0; i < count; i++)
                    {
                        Numerators[i] = GetInt32(data, idx + 8 * i, isIntel);
                        Denominators[i] = GetInt32(data, idx + 8 * i + 4, isIntel);
                        Doubles[i] = ((double)Numerators[i]) / Denominators[i];
                    }
                    break;
                case 9: // Slong (Int32)
                    Ints = new int[count];
                    for (int i = 0; i < count; i++)
                        Ints[i] = GetInt32(data, idx + 4 * i, isIntel);
                    break;
                case 8:
                    // Seen in Samsung 8822 tag.
                    //Console.WriteLine("Unknown type in IFD element. tag = {0:X} type = {1} offset = {2}", Tag, Type, offset);
                    break;
                default:
                    throw new Exception(string.Format("Unknown type in IFD element. tag = {0:X} type = {1} offset = {2}", Tag, Type, offset));
            }

        }

        public override string ToString()
        {
            StringBuilder sb;
            switch (Type)
            {
                case 1: // Byte
                case 7: // Undefined
                    int len = (Bytes.Length <= 10) ? Bytes.Length : 10;
                    bool truncated = len < Bytes.Length;
                    return string.Format("tag {0,4:X} type {1}  {2}{3}", Tag, Type, 
                        BitConverter.ToString(Bytes, 0, len),truncated?"...":"");
                case 2: // Ascii
                    return string.Format("tag {0,4:X} type 2  \"{1}\"", Tag, Text);
                case 3: // Short (UInt16)
                case 4: // Long (UInt32)
                case 9: // Slong (Int32)
                    sb = new StringBuilder();
                    sb.AppendFormat("tag {0,4:X} type {1} ", Tag, Type);
                    for (int i = 0; i < Ints.Length; i++)
                        sb.AppendFormat(" {0}", Ints[i]);
                    return sb.ToString();
                case 5: // Rational (2 x UInt32)
                case 10: // Srational (2 x Int32)
                    sb = new StringBuilder();
                    sb.AppendFormat("tag {0,4:X} type {1} ", Tag, Type);
                    for (int i = 0; i < Numerators.Length; i++)
                        sb.AppendFormat(" {0}/{1}", Numerators[i], Denominators[i]);
                    return sb.ToString();
                default:
                    return null;
            }
        }

        static UInt16 GetUint16(byte[] jj, int offset, bool isIntel)
        {
            if (isIntel)
                return (UInt16)((jj[offset + 1] << 8) + jj[offset]);
            else
                return (UInt16)((jj[offset] << 8) + jj[offset + 1]);
        }

        static Int32 GetInt32(byte[] jj, int offset, bool isIntel)
        {
            if (isIntel)
                return ((jj[offset + 3] << 24) + (jj[offset + 2] << 16) + (jj[offset + 1] << 8) + jj[offset]);
            else
                return ((jj[offset] << 24) + (jj[offset + 1] << 16) + (jj[offset + 2] << 8) + jj[offset + 3]);
        }
    }
}
