using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ConsoleApplication2
{
    public class ByteString
    {
        protected int _position;
        protected int _length;
        protected byte[] _bytes;

        public ByteString()
        {

        }

        public ByteString(int position, int length)
        {
            _position = position;
            _length = length;
            _bytes = new byte[_length];
        }

        public int GetPosition()
        {
            return _position;
        }

        public int GetLength()
        {
            return _length;
        }

        public byte[] GetBytes()
        {
            return _bytes.Reverse().ToArray();
        }

        public byte[] GetRawBytes()
        {
            return _bytes;
        }

        public void SetBytes(byte[] bytes)
        {
            _bytes = bytes;
        }

        public virtual void ReadFromFile(FileStream str)
        {
            if (_bytes == null)
            {
                _bytes = new byte[_length];
            }

            str.Seek(_position, SeekOrigin.Begin);
            str.Read(_bytes, 0, _length);
        }

        public virtual void WriteToFile(FileStream str)
        {
            str.Seek(_position, SeekOrigin.Begin);
            str.Write(_bytes, 0, _length);
        }

        public static bool CompareByteArrays(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
            {
                return false;
            }

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static int TryParseInt(string txt)
        {
            int i = -1;
            if (!Int32.TryParse(txt, out i))
            {
                throw new ApplicationException("Value must be integer values");
            }
            return i;
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
