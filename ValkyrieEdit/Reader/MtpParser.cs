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
        private const int _aCountPos = 0x28; //this is also the size of the index entries, need to account for that.
        private const int _bCountPos = 0x2C;
        private const int _aBlockPos = 0x30;
        private const int _sentenceIndexSize = 0x0F;

        private MxeWord _eofcOne;
        private MxeWord _eofcTwo;
        private MxeWord _sentenceCount;
        private MxeWord _aCount;
        private MxeWord _bCount;

        private ByteString _restOfIt;

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

        private FileInfo _preferredEsrFile;

        public FileInfo PreferredEsrFile
        {
            get { return _preferredEsrFile; }
            set { _preferredEsrFile = value; }
        }

        private List<FileInfo> _esrFiles;

        public List<FileInfo> EsrFiles
        {
            get { return _esrFiles; }
            set { _esrFiles = value; }
        }

        public MtpParser(String filename)
        {
            _indexes = new Dictionary<int, MtpIndexEntry>();
            _filename = filename;
            _basedir = @"./mtpa";
            if (!Directory.Exists(_basedir))
            {
                Directory.CreateDirectory(_basedir);
            }
        }

        protected override void ReadTableMeta(FileStream stream)
        {
            _sentenceCount = new MxeWord(_sentenceCountPos, "i");
            _aCount = new MxeWord(_aCountPos, "i");
            _bCount = new MxeWord(_bCountPos, "i");
            _eofcOne = new MxeWord(_eofcOnePos, "i");
            _eofcTwo = new MxeWord(_eofcTwoPos, "i");

            _sentenceCount.ReadFromFile(stream);
            _aCount.ReadFromFile(stream);
            _bCount.ReadFromFile(stream);
            _eofcOne.ReadFromFile(stream);
            _eofcTwo.ReadFromFile(stream);

            int sc = (int)_sentenceCount.GetValueAsRawInt();
            int ac = (int)_aCount.GetValueAsRawInt(); // this is also the size of the index entries
            int bc = (int)_bCount.GetValueAsRawInt();
            int pos = _aBlockPos + _aCount.Length * ac + _bCount.Length * bc;

            Console.Out.WriteLine("Sentence count: " + sc);

            _indexesStart = pos;

            List<MtpIndexEntry> entries = new List<MtpIndexEntry>();
            MtpIndexEntry prevE = null;
            for (int i = 0; i < sc; i++)
            {
                MtpIndexEntry e = new MtpIndexEntry(pos, this, prevE);
                if (ac == 0x14)
                {
                    e = new MtpIndexExtendedEntry(pos, this, prevE);
                }
                entries.Add(e);

                prevE = e;
                pos += _sentenceCount.Length * ac;
            }

            _sentencesStart = pos;

            //can't read or setup all this stuff until after _sentencesStart is set.
            foreach (MtpIndexEntry e in entries)
            {
                e.ReadEntry(stream); 
                _indexes.Add(e.Id.GetValueAsRawInt(), e);
            }

            int endingLoc = prevE.Sentence.Sentence.Position + prevE.Sentence.Sentence.Length;
            _restOfIt = new ByteString(endingLoc, (int)stream.Length - endingLoc);
            _restOfIt.ReadFromFile(stream);
        }

        public override void Write()
        {
            try
            {
                using (var stream = new FileStream(_filename, FileMode.Open, FileAccess.Write, FileShare.Read))
                {
                    _eofcOne.WriteToFile(stream);
                    _eofcTwo.WriteToFile(stream);
                    foreach (MtpIndexEntry mie in _indexes.Values)
                    {
                        mie.Write(stream);
                    }
                    _restOfIt.WriteToFile(stream);

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

        private string GetCsvFullName()
        {
            return Path.Combine(_basedir, Path.GetFileNameWithoutExtension(_filename) + ".csv");
        }

        public override bool ReadCsvs()
        {
            bool foundAChange = false;
            string fn = GetCsvFullName();
            Console.Out.WriteLine(String.Format(@"Reading csv file [{0}]", fn));
            try
            {
                using (var stream = new StreamReader(fn))
                {
                    foundAChange = ReadCsvLines(stream, fn);
                    stream.Close();
                }
            }
            catch (Exception exc)
            {
                Console.Out.WriteLine(exc.ToString());
            }
            return foundAChange;
        }

        private bool ReadCsvLines(StreamReader stream, string filename)
        {
            bool foundAChange = false;
            int lineNum = 0;
            string line = stream.ReadLine();
            if (line != null)
            {
                lineNum++;
                //List<string> headers = GetCsvHeaders(line);

                while ((line = stream.ReadLine()) != null)
                {
                    lineNum++;
                    string[] info = line.Split(',');
                    MxeWord tempWord = new MxeWord(Int16.MinValue, "hId");
                    tempWord.CheckOriginal = false;
                    tempWord.SetValue(tempWord.Header, info[0]);
                    int indexId = tempWord.GetValueAsRawInt();
                    if (!_indexes.ContainsKey(indexId))
                    {
                        Console.Out.WriteLine(String.Format(CSV_MATCH_ERROR, filename, indexId));
                        continue;
                    }
                    List<string> data = info.ToList();

                    MtpIndexEntry index = _indexes[indexId];
                    bool thisOneChanged = HandleSentenceLengthChanges(data, index);
                    foundAChange = thisOneChanged || foundAChange;
                }
            }

            return foundAChange;
        }

        private bool HandleSentenceLengthChanges(List<string> data, MtpIndexEntry index)
        {
            string d0 = data[0];
            int previousSentenceLength = index.Sentence.Size.GetValueAsRawInt();
            bool thisOneChanged = index.ReadCsvLineData(data);
            if (thisOneChanged)
            {
                int diff = index.Sentence.Size.GetValueAsRawInt() - previousSentenceLength;
                if (diff != 0)
                {
                    if (diff % 4 != 0)
                    {
                        diff += 4 - diff % 4; // addresses are word aligned, so difference changes need to be word aligned as well.
                    }
                    Console.Out.WriteLine(String.Format(@"Adjusting positions of other objects due to length change of [{0}]", d0));
                    _eofcOne.SetValue(_eofcOne.Header, String.Empty + (_eofcOne.GetValueAsRawInt() + diff), true);
                    _eofcTwo.SetValue(_eofcTwo.Header, String.Empty + (_eofcTwo.GetValueAsRawInt() + diff), true);
                    _restOfIt.Position += diff;

                    int changePosition = index.Sentence.Size.Position;
                    foreach (MtpIndexEntry mie in _indexes.Values)
                    {
                        if (mie.Sentence.Size.Position > changePosition)
                        {
                            mie.Sentence.Size.Position += diff;
                            mie.Sentence.Sentence.Position += diff;
                            int thisStart = mie.Start.GetValueAsRawInt();
                            mie.Start.SetValue("ziStart", String.Empty + (thisStart + diff), true);
                        }
                    }
                    Console.Out.WriteLine(String.Format(@"Done with positions of other objects due to length change of [{0}]", d0));
                }
            }
            return thisOneChanged;
        }

        public override void WriteCsv()
        {
            string fn = GetCsvFullName();
            Console.Out.WriteLine(String.Format(@"Writing data file [{0}]", fn));
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
