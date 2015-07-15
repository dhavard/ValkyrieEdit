using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication2
{
    public class ByteLine : ByteString
    {
        public ByteLine()
        {

        }

        public ByteLine(int position)
        {
            _position = position;
            _length = 0x10;
            _bytes = new byte[_length];
        }
    }
}
