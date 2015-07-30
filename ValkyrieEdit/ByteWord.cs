using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication2
{
    public class ByteWord : ByteString
    {
        public ByteWord()
        {

        }

        public ByteWord(int position)
        {
            Position = position;
            Length = 4;
            _bytes = new byte[Length];
        }
        
        protected virtual byte[] ResizeArrayIfNeeded(byte[] bytes)
        {
            if (bytes.Length != 4)
            {
                Array.Resize(ref bytes, 4);
            }
            return bytes;
        }
    }
}
