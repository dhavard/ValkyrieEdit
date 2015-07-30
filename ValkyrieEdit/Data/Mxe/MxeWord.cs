using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ConsoleApplication2.Reader;
using System.Text.RegularExpressions;

namespace ConsoleApplication2.Data.Mxe
{
    public class MxeWord : ByteWord
    {
        private static bool _verbose = true;

        public static bool Verbose
        {
            get { return MxeWord._verbose; }
            set { MxeWord._verbose = value; }
        }

        private static bool _literal = false;

        public static bool Literal
        {
            get { return MxeWord._literal; }
            set { MxeWord._literal = value; }
        }

        private static bool _hex = false;

        public static bool Hex
        {
            get { return MxeWord._hex; }
            set { MxeWord._hex = value; }
        }

        enum ValueType
        {
            Int = 'i', 
            Pointer = 'p',
            Float = 'f',
            Hex = 'h',
            OneByOne = 'l',
            Binary = 'b'
        }

        private ValueType _valueType = ValueType.Hex;
        private string _header;

        public string Header
        {
            get { return _header; }
            set { _header = value; }
        }

        private bool _shouldWritePstring = false;
        private ByteString _pstringbytes;
        private string _pstring;

        public string Pstring
        {
            get { return _pstring; }
            set { _pstring = value; }
        }

        public MxeWord( int position, string header ) : base(position)
        {
            _header = header;
            if (header.Length > 0 && Enum.IsDefined(typeof(ValueType), (int)header[0]))
            {
                _valueType = (ValueType)header[0];
            }
        }

        public override void ReadFromFile(FileStream str)
        {
            base.ReadFromFile(str);
            if (ValueType.Pointer.Equals(_valueType))
            {
                ReadPString(str, false);
            }
        }

        private string ReadPString(FileStream str, bool isTest)
        {
            int p = MxeParser.GetRealAddress(GetValueAsInt());
            string ret = String.Empty;

            if (p >= str.Length)
            {
                if (!isTest)
                {
                    Console.Out.WriteLine(String.Format(@"Pointer header [{0}] contains overly large pointer [{1}] exceeding source file size. Are you sure this is a pointer? CHECK YOU config.txt FILE! Skipping value.", _header, GetValueAsHex()));
                }
            }
            else if (p > 0)
            {
                List<byte> pbytes = new List<byte>();
                str.Seek(p, SeekOrigin.Begin);
                byte b = (byte)str.ReadByte();
                if (b != 0x0)
                {
                    int pos = p;
                    while (b != 0x0 && pos < str.Length)
                    {
                        pbytes.Add(b);
                        b = (byte)str.ReadByte();
                        pos++;
                    }
                    ret = Encoding.UTF8.GetString(pbytes.ToArray(), 0, pbytes.Count);
                }
            }

            if (!isTest)
            {
                _pstring = ret;
                _pstringbytes = new ByteString(p, _pstring.Length);
            }
            return ret;
        }

        public bool SetValue(string header, string val)
        {
            if (!header.Equals(_header))
            {
                Console.Out.WriteLine(String.Format(@"Non-matching header found. Expected [{0}] found [{1}]. Skipping value.", _header, header));
            }
            else
            {
                string original = GetValueAsHex();

                if (String.IsNullOrEmpty(header) || header.Length < 1)
                {
                    SetValueAsHex(val);
                }
                else
                {
                    switch ((int)header[0])
                    {
                        case (int)ValueType.Float:
                            SetValueAsFloat(val);
                            break;
                        case (int)ValueType.Hex:
                        default:
                            SetValueAsHex(val);
                            break;
                        case (int)ValueType.Int:
                            SetValueAsInt(val);
                            break;
                        case (int)ValueType.OneByOne:
                            SetValueAsOnes(val);
                            break;
                        case (int)ValueType.Pointer:
                            SetValueAsPString(val);
                            break;
                        case (int)ValueType.Binary:
                            SetValueAsBinary(val);
                            break;
                    }
                }

                string newval = GetValueAsHex();

                if (!(String.IsNullOrEmpty(header) || header.Length < 1))
                {
                    if ((int)header[0] == (int)ValueType.Float)
                    {
                        string shortOrig = original.Substring(0, 5);
                        string shortNew = newval.Substring(0, 5);

                        if (shortOrig.Equals(shortNew))
                        {
                            SetValueAsHex(original);
                            newval = original;
                        }
                    }
                }
                
                if (!original.Equals(newval))
                {
                    Console.Out.WriteLine(String.Format(@"Changing [{0}] original value [{1}] to new value [{2}]", _header, original, newval));
                    return true;
                }
            }

            return false;
        }

        public object GetValue()
        {
            if (Hex)
            {
                return GetValueAsHex();
            }

            switch (_valueType)
            {
                case ValueType.Float:
                    return GetValueAsFloat();
                case ValueType.Hex:
                default:
                    return GetValueAsHex();
                case ValueType.Int:
                    return GetValueAsInt();
                case ValueType.OneByOne:
                    return GetValueAsOnes();
                case ValueType.Pointer:
                    return GetValueAsPString();
                case ValueType.Binary:
                    return GetValueAsBinary();
            }
        }

        // i, p, f, h, l, b
        // int, pointer, float, hex, l one-by-one, binary

        private string GetValueAsPString()
        {
            if (MxeWord.Literal || _pstring == null)
            {
                if (_pstring == null)
                {
                    Console.Out.WriteLine(String.Format(@"Pointer header [{0}] contains overly large pointer [{1}] exceeding source file size. Are you sure this is a pointer? CHECK YOU config.txt FILE! Skipping value.", _header, GetValueAsHex()));
                }
                return GetValueAsHex();
            }
            return _pstring.Replace(",","~");
        }

        private void SetValueAsPString(string val)
        {
            val = String.IsNullOrEmpty(val) ? String.Empty : val;
            string pstring = String.IsNullOrEmpty(_pstring) ? String.Empty : _pstring;
            val = val.Replace("~", ",");

            if (!val.Equals(_pstring))
            {
                byte[] newstr = System.Text.Encoding.UTF8.GetBytes(val);
                byte[] oldstr = System.Text.Encoding.UTF8.GetBytes(pstring);

                if (newstr.Length == oldstr.Length)
                {
                    Console.Out.WriteLine(String.Format(@"Changing [{0}] original value [{1}] to new value [{2}]", _header, pstring, val));
                    _pstring = val;
                    _pstringbytes.SetBytes(newstr);
                    _shouldWritePstring = true;
                }
                else
                {
                    Console.Out.WriteLine(String.Format(@"Could not change [{0}] original value [{1}] to new value [{2}] due to non-matching lengths.", _header, pstring, val));
                }
            }
        }

        public int GetValueAsInt()
        {
            return BitConverter.ToInt32(GetBytes(), 0);
        }

        public int GetValueAsRawInt()
        {
            return BitConverter.ToInt32(GetRawBytes(), 0);
        }

        private void SetValueAsInt(string val)
        {
            int v;
            if (val.StartsWith("0x"))
            {
                v = Int32.Parse(val.Substring(2), System.Globalization.NumberStyles.HexNumber);
            }
            else
            {
                v = Int32.Parse(val);
            }
            SetBytes(ResizeArrayIfNeeded(BitConverter.GetBytes(v)));
        }

        private float GetValueAsFloat()
        {
            return BitConverter.ToSingle(GetBytes(), 0);
        }

        private void SetValueAsFloat(string val)
        {
            Single f = Single.Parse(val);
            SetBytes(ResizeArrayIfNeeded(BitConverter.GetBytes(f)));
        }

        private string GetValueAsHex()
        {
            return GetValueAsOnes().Replace("-", "");
        }

        private void SetValueAsHex(string val)
        {
            SetValueAsInt(val);
        }

        private string GetValueAsOnes()
        {
            return "0x" + BitConverter.ToString(GetRawBytes());
        }

        private void SetValueAsOnes(string val)
        {
            SetValueAsInt(val.Replace("-", ""));
        }

        private string GetValueAsBinary()
        {
            IEnumerable<string> strings = GetRawBytes().Select(b => Convert.ToString(b, 2).PadLeft(8, '0'));
            StringBuilder sb = new StringBuilder();

            foreach (string s in strings)
            {
                sb.Append(s);
            }

            return sb.ToString();
        }

        private void SetValueAsBinary(string val)
        {
            SetBytes(ResizeArrayIfNeeded(BitConverter.GetBytes(Convert.ToInt32(val, 2)))); 
        }

        public void WriteCsv(StreamWriter stream)
        {
            stream.Write(GetValue());
        }

        protected override byte[] ResizeArrayIfNeeded(byte[] bytes)
        {
            return base.ResizeArrayIfNeeded(bytes).Reverse().ToArray();
        }

        public override void WriteToFile(FileStream str)
        {
            base.WriteToFile(str);
            if (_shouldWritePstring)
            {
                _pstringbytes.WriteToFile(str);
            }
        }

        public string SuggestType(FileStream stream)
        {
            int i = GetValueAsInt();
            if (i < 0xFFFFF && i > -0xFFFF)
            {
                if (stream != null && i < stream.Length && i > 0x1FF)
                {
                    string str = ReadPString(stream, true);
                    Regex r = new Regex(@"[^\w\s]");
                    if (!r.IsMatch(str)) 
                    {
                        return "p";
                    }
                }

                return "i";
            }

            string hex = GetValueAsHex();
            if (hex.EndsWith("000"))
            {
                return "f";
            }

            return "h";
        }
    }
}
