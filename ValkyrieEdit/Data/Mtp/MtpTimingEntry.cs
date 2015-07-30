using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleApplication2.Data.Mxe;
using System.IO;

namespace ValkyrieEdit.Data.Mtp
{
    //  [id] [startframe / framecount] [position?] [position?]
    public class MtpTimingEntry
    {
        private string _filename;

        public string Filename
        {
            get { return _filename; }
            set { _filename = value; }
        }

        private MxeWord _id;

        public MxeWord Id
        {
            get { return _id; }
            set { _id = value; }
        }
        private MxeWord _frames;

        public MxeWord Frames
        {
            get { return _frames; }
            set { _frames = value; }
        }
        private MxeWord _unknown1;

        public MxeWord Unknown1
        {
            get { return _unknown1; }
            set { _unknown1 = value; }
        }
        private MxeWord _unknown2;

        public MxeWord Unknown2
        {
            get { return _unknown2; }
            set { _unknown2 = value; }
        }

        public MtpTimingEntry( FileStream fs, int pos )
        {
            _filename = fs.Name;

            _id = new MxeWord(pos, "hId");
            _frames = new MxeWord(pos, "hStartframeCountframes");
            _unknown1 = new MxeWord(pos, "hUnknown1");
            _unknown2 = new MxeWord(pos, "hUnknown2");

            _id.ReadFromFile(fs);
            _frames.ReadFromFile(fs);
            _unknown1.ReadFromFile(fs);
            _unknown2.ReadFromFile(fs);
        }

        public void Write()
        {
            try
            {
                using (var stream = new FileStream(_filename, FileMode.Open, FileAccess.Write, FileShare.Read))
                {
                    WriteInnerParts(stream);
                    stream.Close();
                }
            }
            catch (Exception exc)
            {
                Console.Out.WriteLine(exc.ToString());
            }

        }

        protected void WriteInnerParts( FileStream fs )
        {
            //_id.WriteToFile(fs);
            _frames.WriteToFile(fs);
            _unknown1.WriteToFile(fs);
            _unknown2.WriteToFile(fs);
        }

        public void WriteCsv(StreamWriter stream)
        {
            stream.Write(_filename);
            stream.Write(',');
            _id.WriteCsv(stream);
            stream.Write(',');
            _frames.WriteCsv(stream);
            stream.Write(',');
            _unknown1.WriteCsv(stream);
            stream.Write(',');
            _unknown2.WriteCsv(stream);
            stream.Write(',');
        }

        public static void WriteCsvFiller(StreamWriter stream)
        {
            stream.Write(",,,,,");
        }

        public static void WriteCsvHeaders(StreamWriter stream)
        {
            stream.Write("filename,hId,hStartframeCountframes,hUnknown1,hUnknown2,");
        }
    }
}
