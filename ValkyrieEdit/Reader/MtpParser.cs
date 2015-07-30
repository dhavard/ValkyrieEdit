using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleApplication2.Reader;
using ConsoleApplication2;
using ConsoleApplication2.Data.Mxe;
using System.IO;
using ValkyrieEdit.Data.Mtp;

namespace ValkyrieEdit.Reader
{
    public class MtpParser : Parser
    {
        private const int _eofcOnePos = 0x04;
        private const int _eofcTwoPos = 0x14;
        private const int _sentenceCountPos = 0x24;
        private const int _aCountPos = 0x28;
        private const int _bCountPos = 0x2C;
        private const int _aBlockPos = 0x30;
        private const int _sentenceIndexSize = 0x0F;

        private MxeWord _sentenceCount;
        private MxeWord _aCount;
        private MxeWord _bCount;

        private int _indexesStart;

        public int IndexesStart
        {
            get { return _indexesStart; }
            set { _indexesStart = value; }
        }
        private int _sentencesStart;

        public int SentencesStart
        {
            get { return _sentencesStart; }
            set { _sentencesStart = value; }
        }

        protected Dictionary<int, MtpIndexEntry> _indexes;

        public MtpParser(String filename)
        {
            _indexes = new Dictionary<int, MtpIndexEntry>();
            _filename = filename;
            _basedir = @".\" + Path.GetFileNameWithoutExtension(_filename);
            Directory.CreateDirectory(_basedir);
        }

        protected override void ReadTableMeta(FileStream stream)
        {
            _sentenceCount = new MxeWord(_sentenceCountPos, "i");
            _aCount = new MxeWord(_aCountPos, "i");
            _bCount = new MxeWord(_bCountPos, "i");

            _sentenceCount.ReadFromFile(stream);
            _aCount.ReadFromFile(stream);
            _bCount.ReadFromFile(stream);

            int sc = (int)_sentenceCount.GetValueAsRawInt();
            int ac = (int)_aCount.GetValueAsRawInt();
            int bc = (int)_bCount.GetValueAsRawInt();
            int pos = _aBlockPos + _aCount.Length * ac + _bCount.Length * bc;

            Console.Out.WriteLine("Sentence count: " + sc);

            _indexesStart = pos;

            List<MtpIndexEntry> entries = new List<MtpIndexEntry>();
            MtpIndexEntry prevE = null;
            for (int i = 0; i < sc; i++)
            {
                MtpIndexEntry e = new MtpIndexEntry(pos, this, prevE);
                entries.Add(e);

                prevE = e;
                pos += _sentenceCount.Length * 4;
            }

            _sentencesStart = pos;

            //can't read or setup all this stuff until after _sentencesStart is set.
            foreach (MtpIndexEntry e in entries)
            {
                e.ReadEntry(stream); 
                _indexes.Add(e.Id.GetValueAsRawInt(), e);
            }
        }

        public override bool ReadCsvs()
        {
            throw new NotImplementedException();
        }

        public override void Write()
        {
            try
            {
                using (var stream = new FileStream(_filename, FileMode.Open, FileAccess.Write, FileShare.Read))
                {
                    foreach (MtpIndexEntry mie in _indexes.Values)
                    {
                        mie.Write(stream);
                    }

                    stream.Close();
                }
            }
            catch (Exception exc)
            {
                Console.Out.WriteLine(exc.ToString());
            }

        }

        private void WriteCsvItems(StreamWriter stream)
        {
            MtpIndexEntry.WriteCsvHeaders(stream);
            stream.Write(stream.NewLine);

            foreach (MtpIndexEntry mie in _indexes.Values)
            {
                mie.WriteCsv(stream);
                stream.Write(stream.NewLine);
            }
        }

        public override void WriteIndexes()
        {
            throw new NotImplementedException();
        }

        public override void WriteCsv()
        {
            string fn = _basedir + ".csv";
            String msg = "Writing data file ";
            Console.Out.WriteLine(msg + fn);
            try
            {
                using (var stream = new StreamWriter(fn))
                {
                    WriteCsvItems(stream);
                    
                    stream.Close();
                }
            }
            catch (Exception exc)
            {
                Console.Out.WriteLine(exc.ToString());
            }
        }
    }
}
