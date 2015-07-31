using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleApplication2;
using System.IO;
using ConsoleApplication2.Reader;
using ConsoleApplication2.Data.Mxe;

namespace ValkyrieEdit.Reader
{
    public abstract class Parser
    {
        protected const string CSV_MATCH_ERROR = @"Mxe Index referenced in file [{0}] of value [{1}] could not be found in source. Skipping record.";
        protected const string CSV_PARSE_ERROR = @"Error parsing index of file [{0}] line [{1}]. Skipping record.";
        private const string MXE_END = ".mxe";
        private const string MTP_END = ".mtp";

        protected const int _headerSize = 0x20;

        protected string _filename;
        protected string _basedir;

        public static Parser GetParser(string fn)
        {
            if (fn.EndsWith(MXE_END))
            {
                return new MxeParser(fn);
            }
            else if (fn.EndsWith(MTP_END))
            {
                return new MtpParser(fn);
            }

            return null;
        }

        public static int GetRealAddress(ByteWord addr)
        {
            return GetRealAddress(BitConverter.ToInt32(addr.GetBytes(), 0));
        }

        public static int GetRealAddress(int addr)
        {
            return addr + _headerSize;
        }

        public virtual void Read()
        {
            try
            {
                using (var stream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    ReadTableMeta(stream);

                    stream.Close();
                }
            }
            catch (Exception exc)
            {
                Console.Out.WriteLine(exc.ToString());
            }
        }

        protected abstract void ReadTableMeta(FileStream stream);
        public abstract bool ReadCsvs();

        public abstract void Write();
        public abstract void WriteIndexes();
        public abstract void WriteCsv();

        public static void HandleFileOrMethod(string fn, bool isSync, bool isTest, bool writeHex, bool writeIndex)
        {
            FileAttributes attr = File.GetAttributes(fn);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                HandleDirectory(fn, isSync, isTest, writeHex, writeIndex);
            }
            else
            {
                HandleFile(fn, isSync, isTest, writeHex, writeIndex);
            }
        }

        private static void HandleDirectory(string fn, bool isSync, bool isTest, bool writeHex, bool writeIndex)
        {
            DirectoryInfo d = new DirectoryInfo(fn);

            SearchAndHandleFileOfType(isSync, isTest, writeHex, writeIndex, d, "*.mxe");
            SearchAndHandleFileOfType(isSync, isTest, writeHex, writeIndex, d, "*.mtp");
        }

        private static void SearchAndHandleFileOfType(bool isSync, bool isTest, bool writeHex, bool writeIndex, DirectoryInfo d, string search)
        {
            foreach (FileInfo fi in d.GetFiles(search))
            {
                HandleFile(fi.FullName, isSync, isTest, writeHex, writeIndex);
            }
        }

        private static void HandleFile(string fn, bool isSync, bool isTest, bool writeHex, bool writeIndex)
        {
            Parser parser = ReadFile(fn);

            if (!isTest)
            {
                DoSourceToCsv(writeHex, writeIndex, parser);
            }
            else
            {
                DoCsvToSourceSync(fn, isSync, parser);
            }
        }

        private static Parser ReadFile(string fn)
        {
            Console.Out.WriteLine("Using [" + fn + "] as source file.");
            Parser parser = GetParser(fn);

            Console.Out.WriteLine("Reading data...");
            parser.Read();
            return parser;
        }
        
        private static void DoSourceToCsv(bool writeHex, bool writeIndex, Parser parser)
        {
            if (writeIndex)
            {
                Console.Out.WriteLine("Writing out Index data...");
                parser.WriteIndexes();
            }
            Console.Out.WriteLine("Writing out CSV data...");
            parser.WriteCsv();
            if (writeHex)
            {
                Console.Out.WriteLine("Writing out Hex data...");
                MxeWord.Hex = true;
                parser.WriteCsv();
                MxeWord.Hex = false;
            }
        }

        private static void DoCsvToSourceSync(string fn, bool isSync, Parser parser)
        {
            //read in CSV and write to MXE if we found a change
            Console.Out.WriteLine("Reading in CSV data...");
            if (parser.ReadCsvs() && isSync)
            {
                Console.Out.WriteLine("Backup Source file and then Writing out Source data to [" + fn + "]...");

                // back up the mxe file with a .bak file
                int fileCount = -1;
                string backup = fn + ".bak";
                do
                {
                    fileCount++;
                }
                while (File.Exists(backup + (fileCount > 0 ? fileCount.ToString() : String.Empty)));

                File.Copy(fn, backup + (fileCount > 0 ? fileCount.ToString() : String.Empty));
                // write out the changed data
                parser.Write();
            }
        }
    }
}
