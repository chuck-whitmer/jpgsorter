using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public IfdElement(byte[] data, int offset)
        {
            Tag = BitConverter.ToUInt16(data, offset);
            Type = BitConverter.ToUInt16(data, offset + 2);
            int count = BitConverter.ToInt32(data, offset + 4);
            int[] typeSizes = new int[] { 0, 1, 1, 2, 4, 8, 0, 1, 0, 4, 8 };
            if (Type < 0 || Type >= typeSizes.Length)
                throw new Exception("Bad type in IFD element");
            int typeSize = typeSizes[Type];
            int idx = (count*typeSize < 5) ? offset + 8 : BitConverter.ToInt32(data, offset + 8);
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
                        Ints[i] = (int)BitConverter.ToUInt16(data, idx + 2 * i);
                    break;
                case 4: // Long (UInt32)
                    Ints = new int[count];
                    for (int i = 0; i < count; i++)
                        Ints[i] = (int)BitConverter.ToUInt32(data, idx + 4 * i);
                    break;
                case 5: // Rational (2 x UInt32)
                case 10: // Srational (2 x Int32)
                    Numerators = new int[count];
                    Denominators = new int[count];
                    Doubles = new double[count];
                    for (int i = 0; i < count; i++)
                    {
                        Numerators[i] = BitConverter.ToInt32(data, idx + 8 * i);
                        Denominators[i] = BitConverter.ToInt32(data, idx + 8 * i + 4);
                        Doubles[i] = ((double)Numerators[i]) / Denominators[i];
                    }
                    break;
                case 9: // Slong (Int32)
                    Ints = new int[count];
                    for (int i = 0; i < count; i++)
                        Ints[i] = BitConverter.ToInt32(data, idx + 4 * i);
                    break;
                default:
                    throw new Exception("Unknown type in IFD element");
            }

        }

        public override string ToString()
        {
            StringBuilder sb;
            switch (Type)
            {
                case 1: // Byte
                case 7: // Undefined
                    return string.Format("tag {0,4:X} type {1}  {2}", Tag, Type, BitConverter.ToString(Bytes, 0, Bytes.Length));
                case 2: // Ascii
                    return string.Format("tag {0,4:X} type 2  \"{1}\"", Tag, Text);
                case 3: // Short (UInt16)
                case 4: // Long (UInt32)
                case 9: // Slong (Int32)
                    sb = new StringBuilder();
                    sb.AppendFormat("tag {0,4:X} type {1}", Tag, Type);
                    for (int i = 0; i < Ints.Length; i++)
                        sb.AppendFormat(" {0}", Ints[i]);
                    return sb.ToString();
                case 5: // Rational (2 x UInt32)
                case 10: // Srational (2 x Int32)
                    sb = new StringBuilder();
                    sb.AppendFormat("tag {0,4:X} type {1}", Tag, Type);
                    for (int i = 0; i < Numerators.Length; i++)
                        sb.AppendFormat(" {0}/{1}", Numerators[i], Denominators[i]);
                    return sb.ToString();
                default:
                    return null;
            }

        }
    }
}
