using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ConsoleApplication2.Reader;

namespace ConsoleApplication2.Data
{
    public class MxeIndexEntry
    {
        private int _position;

        public int Position
        {
            get { return _position; }
            set { _position = value; }
        }

        private MxeWord _index;

        public MxeWord Index
        {
            get { return _index; }
            set { _index = value; }
        }
        private MxeWord _vm;

        public MxeWord Vm
        {
            get { return _vm; }
            set { _vm = value; }
        }
        private MxeWord _typeCode;

        public MxeWord TypeCode
        {
            get { return _typeCode; }
            set { _typeCode = value; }
        }
        private MxeWord _address;

        public MxeWord Address
        {
            get { return _address; }
            set { _address = value; }
        }

        private MxeBlockEntry _block;

        public MxeBlockEntry Block
        {
            get { return _block; }
            set { _block = value; }
        }

        public MxeIndexEntry(int position)
        {
            _position = position;

            _index = new MxeWord(position, "iIndex");
            _vm = new MxeWord(position + 0x4, "pVm");
            _typeCode = new MxeWord(position + 0x8, "iType");
            _address = new MxeWord(position + 0xC, "hAddress");
        }

        public void WriteToMxe(FileStream stream)
        {
            _index.WriteToFile(stream);
            _vm.WriteToFile(stream);
            _typeCode.WriteToFile(stream);
            _address.WriteToFile(stream);

            WriteBlockToMxe(stream);
        }

        private void WriteBlockToMxe(FileStream stream)
        {
            _block.WriteToMxe(stream);
        }

        public void ReadEntry( FileStream stream )
        {
            _index.ReadFromFile(stream);
            _vm.ReadFromFile(stream);
            _typeCode.ReadFromFile(stream);
            _address.ReadFromFile(stream);

            ReadBlock(stream);
        }

        private void ReadBlock(FileStream stream)
        {
            _block = new MxeBlockEntry(this, MxeParser.GetRealAddress(_address));
            _block.ReadBlock(stream);
        }

        public void WriteCsv(StreamWriter stream)
        {
            _index.WriteCsv(stream);
            stream.Write(',');
            _vm.WriteCsv(stream);
            stream.Write(',');
            _block.WriteCsv(stream);
        }

        public void WriteIndex(StreamWriter stream)
        {
            _index.WriteCsv(stream);
            stream.Write(',');
            _vm.WriteCsv(stream);
            stream.Write(',');
            _typeCode.WriteCsv(stream);
            stream.Write(',');
            _address.WriteCsv(stream);
        }

        public string GetVmTitle()
        {
            int i = _vm.Pstring.IndexOf(':');
            return _vm.Pstring.Remove(i);
        }

        public int GetIndex()
        {
            return BitConverter.ToInt32(_index.GetBytes(),0);
        }

        public int GetTypeCode()
        {
            return BitConverter.ToInt32(_typeCode.GetBytes(), 0);
        }

        public int GetExpectedByteWords()
        {
            return (int)Math.Ceiling(GetTypeCode() / 16.0) * 4;
        }

        public bool ReadCsvLineData(List<string> headers, List<string> data)
        {
            return _block.ReadCsvLineData(headers, data);
        }
    }
}
