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
    public class MtpIndexExtendedEntry : MtpIndexEntry
    {
        public MtpIndexExtendedEntry(int position, MtpParser parser, MtpIndexEntry prev)
        {
            _position = position;
            _parser = parser;
            _previous = prev;

            _id = new MxeWord(position, "hId");
            _start = new MxeWord(position + 0x4, "hStart");
            _actor = new MxeWord(position + 0x10, "hActor");
            _unknown = new MxeWord(position + 0x14, "hUnknown");
        }

        protected override void ReadTiming(FileStream mtpStream)
        {
            // These will never have an esr entry that I can tell
            return;
        }
    }
}
