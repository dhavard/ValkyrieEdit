using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleApplication2.Data.Mxe;
using System.IO;
using System.Text.RegularExpressions;
using ValkyrieEdit.Reader;

namespace ValkyrieEdit.Data.Mtp
{
    public class MtpIndexEntry
    {
        private const string ASTERICK = "*";
        private const string MTP_FOLDER_NAME = @"mtpa";
        private const string ESR_FOLDER_NAME = @"event";
        private const string ESR_NAME_FORMAT = @"ev{0}{1}.esr";
        private const string MTP_ESR_NAME_FORMAT = @"mtpa_adv_{0}.mtp";
        private const string MTP_ESR_NAME_PATTERN = @"mtpa_adv_(\d+).mtp";
        private const string ADV_PART = @"mtpa_adv_";

        protected MtpParser _parser;
        protected MtpIndexEntry _previous;

        protected MtpTimingEntry _timing;

        public MtpTimingEntry Timing
        {
            get { return _timing; }
            set { _timing = value; }
        }

        protected MtpSentence _sentence;

        public MtpSentence Sentence
        {
            get { return _sentence; }
            set { _sentence = value; }
        }

        protected int _position;

        public int Position
        {
            get { return _position; }
            set { _position = value; }
        }

        protected MxeWord _id;

        public MxeWord Id
        {
            get { return _id; }
            set { _id = value; }
        }

        protected MxeWord _actor;

        public MxeWord Actor
        {
            get { return _actor; }
            set { _actor = value; }
        }

        protected MxeWord _start;

        public MxeWord Start
        {
            get { return _start; }
            set { _start = value; }
        }

        protected MxeWord _unknown;

        public MxeWord Unknown
        {
            get { return _unknown; }
            set { _unknown = value; }
        }

        protected MtpIndexEntry()
        {

        }

        public MtpIndexEntry(int position, MtpParser parser, MtpIndexEntry prev)
        {
            _position = position;
            _parser = parser;
            _previous = prev;

            _id = new MxeWord(position, "hId");
            _actor = new MxeWord(position + 0x4, "hActor");
            _start = new MxeWord(position + 0x8, "hStart");
            _unknown = new MxeWord(position + 0xC, "hUnknown");
        }
        
        public void ReadEntry(FileStream stream)
        {
            _id.ReadFromFile(stream);
            _actor.ReadFromFile(stream);
            _start.ReadFromFile(stream);
            _unknown.ReadFromFile(stream);

            ReadTiming(stream);

            _sentence = new MtpSentence(_parser.SentencesStart + _start.GetValueAsRawInt());
            _sentence.Read(stream);
        }

        public void Write(FileStream stream)
        {
            _id.WriteToFile(stream);
            _actor.WriteToFile(stream);
            _start.WriteToFile(stream);
            _unknown.WriteToFile(stream);

            if (_timing != null)
            {
                _timing.Write();
            }

            _sentence.Write(stream, _previous == null ? null : _previous.Sentence);
        }

        public void WriteCsv(StreamWriter stream)
        {
            _id.WriteCsv(stream);
            stream.Write(',');
            _actor.WriteCsv(stream);
            stream.Write(',');
            _start.WriteCsv(stream);
            stream.Write(',');
            _unknown.WriteCsv(stream);
            stream.Write(',');

            if (_timing != null)
            {
                _timing.WriteCsv(stream);
            }
            else
            {
                MtpTimingEntry.WriteCsvFiller(stream);
            }

            _sentence.WriteCsv(stream);
        }

        public static void WriteCsvHeaders(StreamWriter stream)
        {
            stream.Write("hId,hActor,hStart,hUnknown,");
            MtpTimingEntry.WriteCsvHeaders(stream);            
            MtpSentence.WriteCsvHeaders(stream);
        }

        protected virtual void ReadTiming(FileStream mtpStream)
        {
            if (_parser.EsrFiles == null)
            {
                List<FileInfo> esrCollection = new List<FileInfo>();
                FindEsrFile(mtpStream, esrCollection);
                _parser.EsrFiles = esrCollection;
            }

            int matchLoc = -1;
            matchLoc = FindTimingsIfExists(matchLoc);
        }

        private static void FindEsrFile(FileStream mtpStream, List<FileInfo> esrCollection)
        {
            Regex r = new Regex(MTP_ESR_NAME_PATTERN);
            MatchCollection mc = r.Matches(mtpStream.Name);
            if (mc.Count > 0 && mc[0].Groups.Count > 1)
            {
                FileInfo fi = new FileInfo(Path.GetFileName(mtpStream.Name));
                DirectoryInfo mtpDir = new DirectoryInfo(Path.GetDirectoryName(mtpStream.Name));
                DirectoryInfo esrDir = new DirectoryInfo(Path.Combine(mtpDir.Parent.FullName, ESR_FOLDER_NAME)); 

                string chapter = mc[0].Groups[1].Value;
                string search = String.Format(ESR_NAME_FORMAT, chapter, ASTERICK);

                Console.Out.WriteLine(String.Format(@"Searching for timing values in esr files matching [{0}]", Path.Combine(esrDir.FullName, search)));

                foreach (FileInfo fil in esrDir.GetFiles(search))
                {
                    esrCollection.Add(fil);
                }
            }
        }

        private int FindTimingsIfExists(int matchLoc)
        {
            if (_parser.PreferredEsrFile != null)
            {
                matchLoc = SearchEsrForTiming(matchLoc, _parser.PreferredEsrFile);
                if (matchLoc > -1)
                {
                    return matchLoc;
                }
                else
                {
                    //don't look in this file again. Timing are grouped by ESR file, so if we found it here
                    // before and not now then we are done with this one
                    _parser.EsrFiles.Remove(_parser.PreferredEsrFile);
                    _parser.PreferredEsrFile = null;
                }
            }

            foreach (FileInfo fi in _parser.EsrFiles)
            {
                matchLoc = SearchEsrForTiming(matchLoc, fi);
                if (matchLoc > -1)
                {
                    //Note that this should only log the FIRST timing in that ESR file, all others should be found by "Preferred" block.
                    Console.Out.WriteLine(String.Format(@"Found ESR timing at [{0}] in file [{1}]", matchLoc, fi.Name));
                    _parser.PreferredEsrFile = fi;
                    break;
                }
            }
            return matchLoc;
        }

        private int SearchEsrForTiming(int matchLoc, FileInfo fi)
        {
            try
            {
                using (var esrStream = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    matchLoc = ReadForTiming(matchLoc, esrStream);
                }
            }
            catch (Exception exc)
            {
                Console.Out.WriteLine(exc.ToString());
            }
            return matchLoc;
        }

        private int ReadForTiming(int matchLoc, FileStream esrStream)
        {
            int size = 1024 * 1024;
            for (int pl = 0; pl < esrStream.Length; pl += size)
            {
                var buff = new byte[size];
                int bytesRead = esrStream.Read(buff, 0, buff.Length);
                matchLoc = indexOfMatch(buff, bytesRead, _id.GetRawBytes());
            }

            if (matchLoc > -1)
            {
                //extract info see: http://pastebin.com/sPTiT3b4
                //  [id] [startframe / framecount] [position?] [position?]
                _timing = new MtpTimingEntry(esrStream, matchLoc);
            }

            esrStream.Close();
            return matchLoc;
        }

        // Lazy for this so from http://forums.codeguru.com/showthread.php?511356-Binary-Pattern-Search
        //Returns the last index of a match if any; otherwise -1
        //If I needed this to actually be fast... write a C function and invoke that instead.
        public static int indexOfMatch(byte[] data, int dataEnd, byte[] pattern)
        {
            for (int i = 0; i < data.Length - pattern.Length + 1; i++)
            {
                bool isMatch = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        isMatch = false;
                        break;
                    }
                }
                if (isMatch)
                {
                    return i;  //Return the index of the matched pattern
                }
            }

            return -1;
        }
    }
}
