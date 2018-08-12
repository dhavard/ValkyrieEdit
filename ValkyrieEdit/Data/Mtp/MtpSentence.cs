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
        // The game appears to use Shift-JIS encoding internally and this allows Japanese text data to be read/written correctly
        private static readonly Encoding SourceEncoding = Encoding.GetEncoding("shift_jis");

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

            if (_size.GetValueAsRawInt() < 50000)
            {
                _sentence = new ByteString(Size.Position + Size.Length, Size.GetValueAsRawInt());
                _sentence.ReadFromFile(stream);
            }
            else
            {
                Console.Out.WriteLine("Maximum sentence length [50000] exceeded.");
            }
        }

        public void Write(FileStream stream, MtpSentence previous)
        {
            _size.WriteToFile(stream);
            if (_sentence != null)
            {
                PadSentence();
                _sentence.WriteToFile(stream);
            }
        }

        public void WriteCsv(StreamWriter stream)
        {
            if (_sentence != null)
            {
                string sent = SourceEncoding.GetString(_sentence.GetRawBytes().ToArray(), 0, _sentence.Length);
                stream.Write(sent.Replace("\n", "\\n").Replace("\r", "\\r").Replace(",", "~"));
            }
        }

        public static void WriteCsvHeaders(StreamWriter stream)
        {
            stream.Write("sentence");
        }

        public bool ReadCsvData(List<string> data)
        {
            bool ret = false;
            if (data.Count < 1)
            {
                Console.Out.WriteLine("Insufficient sentence data count. Skipping record.");
                return ret;
            }

            string newSentence = data[0].Replace("\\n", "\n").Replace("\\r", "\r").Replace("~", ",");
            string sent = SourceEncoding.GetString(_sentence.GetRawBytes().ToArray(), 0, _sentence.Length);
            if (!sent.Equals(newSentence))
            {
                Console.Out.WriteLine(String.Format(@"Changing [{0}] original value [{1}] to new value [{2}]", "sentence", sent, newSentence));
                byte[] bytes = SourceEncoding.GetBytes(newSentence);
                _size.SetValue(_size.Header, String.Empty + bytes.Length, true);
                _sentence.SetBytes(bytes);
                
                ret = true;
            }
            TrimData(data);

            return ret;
        }

        public void PadSentence()
        {
            PadSentence(_sentence.GetRawBytes());
        }

        private void PadSentence(byte[] bytes)
        {
            List<byte> bytesl = bytes.ToList();
            for (int i = 0; i < 4 - (bytes.Length % 4); i++)
            {
                bytesl.Add((byte)0);
            }
            _sentence.SetBytes(bytesl.ToArray());
            _sentence.Length = bytesl.Count;
        }

        public static void TrimData(List<string> data)
        {
            data.RemoveRange(0, 1);
        }
    }
}
