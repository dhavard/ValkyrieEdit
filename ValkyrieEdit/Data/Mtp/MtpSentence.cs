using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleApplication2.Data.Mxe;
using ConsoleApplication2;
using System.IO;

namespace ValkyrieEdit.Data.Mtp
{
    public class MtpSentence
    {
        private MxeWord _size;

        public MxeWord Size
        {
            get { return _size; }
            set { _size = value; }
        }
        private ByteString _sentence;

        public ByteString Sentence
        {
            get { return _sentence; }
            set { _sentence = value; }
        }

        public MtpSentence(int pos)
        {
            _size = new MxeWord(pos, "iSize");
            //_sentence = new MxeWord(Int32.MaxValue, "pSenetence");
            //_sentence.SetBytes(BitConverter.GetBytes(pos + 4));
        }

        public void Read(FileStream stream)
        {
            _size.ReadFromFile(stream);

            _sentence = new ByteString(Size.Position + Size.Length, Size.GetValueAsRawInt());
            _sentence.ReadFromFile(stream);
        }

        public void Write(FileStream stream, MtpSentence previous)
        {
            //int withEndLen = Sentence.Length + 1;
            //int needsToAdd = 4 - (withEndLen % 4 );
            if (previous != null)
            {
                _size.Position = previous.Size.Position + _size.Length + previous.Sentence.Length;
                _sentence.Position = _size.Position + _size.Length;
            }

            List<byte> bytes = _sentence.GetRawBytes().ToList();
            for (int i = 0; i < 5 - (_sentence.Length + 1) % 4; i++)
            {
                bytes.Add((byte)0);
            }

            _size.SetValue("iSize", String.Empty + bytes.Count);
            _sentence.Length = bytes.Count;
            _sentence.SetBytes(bytes.ToArray());

            _size.WriteToFile(stream);
            _sentence.WriteToFile(stream);
        }

        public void WriteCsv(StreamWriter stream)
        {
            stream.Write(Encoding.UTF8.GetString(_sentence.GetRawBytes().ToArray(), 0, _sentence.Length));
        }

        public static void WriteCsvHeaders(StreamWriter stream)
        {
            stream.Write("sentence");
        }
    }
}
