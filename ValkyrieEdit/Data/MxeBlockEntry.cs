using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleApplication2.Reader;
using System.IO;

namespace ConsoleApplication2.Data
{
    public class MxeBlockEntry
    {
        private int _position;

        public int Position
        {
            get { return _position; }
            set { _position = value; }
        }

        private MxeEntryType _type;

        public MxeEntryType Type
        {
            get { return _type; }
            set { _type = value; }
        }
        private List<MxeWord> _entries;

        public MxeBlockEntry( MxeIndexEntry mie, int position )
        {
            _type = MxeEntryType.GetEntryType(mie);
            _position = position;
            _entries = new List<MxeWord>();

            for (int i = 0; i < _type.Length && _type.Headers != null && _type.Headers.Count > i; i++)
            {
                _entries.Add(new MxeWord(position + i * 4, _type.Headers[i]));
            }

            if(_type.Equals(MxeEntryType.Other) && _entries.Count < 1)
            {
                for (int i = 0; i < mie.GetExpectedByteWords(); i++)
                {
                    _entries.Add(new MxeWord(position + i * 4, String.Empty));
                }
            }
        }

        public void WriteToMxe(FileStream stream)
        {
            foreach (MxeWord word in _entries)
            {
                word.WriteToFile(stream);
            }
        }

        public void ReadBlock(FileStream stream)
        {
            foreach (MxeWord word in _entries)
            {
                word.ReadFromFile(stream);
            }
        }

        public void WriteCsv(StreamWriter stream)
        {
            foreach (MxeWord word in _entries)
            {
                if (!String.IsNullOrEmpty(word.Header) || MxeWord.Verbose)
                {
                    word.WriteCsv(stream);
                    stream.Write(',');
                }
            }
        }

        public bool ReadCsvLineData(List<string> headers, List<string> data)
        {
            bool ret = false;
            if (data.Count < _entries.Count)
            {
                Console.Out.WriteLine("Insufficient data count. Skipping record.");                
            }

            for (int i = 0; i < data.Count && i < _entries.Count; i++)
            {
                ret = _entries[i].SetValue(headers[i], data[i]) || ret;
            }

            return ret;
        }
    }
}
