using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleApplication2.Reader;
using ConsoleApplication2.Data;
using System.IO;
using ValkyrieEdit.Discover;

namespace ConsoleApplication2
{
    class Program
    {
        private const string TARGET_FILE = @".\target.txt";

        static void Main(string[] args)
        {
            string fn = @"C:\Program Files (x86)\Steam\steamapps\common\Valkyria Chronicles\data\mx\game_info_game_param.mxe";
            
            bool wasGivenFile = false;
            bool isSync = false;
            bool isTest = false;
            bool isHelp = false;
            bool writeHex = false;
            bool writeIndex = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-q":
                    case "-Q":
                        MxeWord.Verbose = false;
                        break;
                    case "-l":
                    case "-L":
                        MxeWord.Literal = true;
                        break;
                    case "-h":
                    case "-H":
                        writeHex = true;
                        break;
                    case "-i":
                    case "-I":
                        writeIndex = true;
                        break;
                    case "-s":
                    case "-S":
                        isSync = true;
                        isTest = true;
                        break;
                    case "-t":
                    case "-T":
                        isTest = true;
                        break;
                    case "-r":
                    case "-R":
                        isSync = false;
                        isTest = false;
                        break;
                    case "help":
                    case "Help":
                    case "-help":
                    case "-Help":
                    case "--help":
                    case "--Help":
                        isHelp = true;
                        break;
                    default:
                        if (args[i].StartsWith("-"))
                        {
                            WriteOutArgumentError(args[i]);
                            return;
                        }
                        else
                        {
                            fn = args[i];
                            wasGivenFile = true;
                            WriteTargetFile(fn);
                        }
                        break;
                }
            }

            if (!isHelp)
            {
                FigureOutWhatFileToUse(ref fn, ref wasGivenFile);

                HandleFileOrMethod(fn, isSync, isTest, writeHex, writeIndex);

                if (ConfigDiscovery.HasDiscoveries())
                {
                    ConfigDiscovery.PrintDiscoveries();
                }
                else
                {
                    ConfigDiscovery.DeleteDiscoveryFile();
                }
            }
            else
            {
                WriteOutHelp();
            }

            Console.Out.WriteLine("Closing program...");
        }

        private static void WriteOutArgumentError(string arg)
        {
            Console.Out.WriteLine(String.Format(@"ERROR - Unrecognized argument received: [{0}]. Please see the help message for usage instructions.", arg));
            Console.Out.WriteLine(String.Empty);
            Console.Out.WriteLine(String.Empty);
            WriteOutHelp();
        }

        private static void WriteOutHelp()
        {
            Console.Out.WriteLine(@"This program is designed to read and edit *.mxe files used by the PC version of Valkyrie Chronicles. Requires DotNetFramework 3.5 or higher.");
            Console.Out.WriteLine(@"Example call:");
            Console.Out.WriteLine(@"   > ValkyrieEdit.exe [Path\To\File\game_info.mxe] [-HILQRST]");
            Console.Out.WriteLine(String.Empty);
            Console.Out.WriteLine(String.Empty);
            Console.Out.WriteLine(@"All arguments are optional, required information such as file path will either be prompted or defaulted if not supplied.");
            Console.Out.WriteLine(@"Mode arguments:");
            Console.Out.WriteLine(@"-help : Output this help message.");
            Console.Out.WriteLine(@"-H : Hex mode. Output all data in hex format");
            Console.Out.WriteLine(@"-I : Output the index tables extracted from the *.mxe file in *.csv format.");
            Console.Out.WriteLine(@"-L : Output pointer data in hex, as opposed to resolving the pointer to the value to which it points");
            Console.Out.WriteLine(@"-Q : Do not output columns/data where the column is not defined by the config.txt file");
            Console.Out.WriteLine(@"-R : Read mode [default mode]. Read in the *.mxe file and generate a set of csv files from it. Uses the config.txt file to determine column names and types.");
            Console.Out.WriteLine(@"-S : Sync mode. Read in the *.csv files generated by a Read mode execution and write them to the *.mxe file. By default, *-Data.csv files will be read. In Hex mode, *-Hex.csv files will be read. *-OTHER.csv files which are generated for data types not defined by config.txt will never be read to avoid writing 'not-understood' data.");
            Console.Out.WriteLine(@"-T : Test mode. Read in the *.csv files generated by a Read mode execution and determine what would be written to the *.mxe file; HIGHLY RECOMMENDED to use this prior to a Sync mode run.");
            Console.Out.WriteLine(String.Empty);
            Console.Out.WriteLine(String.Empty);
            Console.Out.WriteLine(@"Recommended workflow:");
            Console.Out.WriteLine(@"0. !!!!!MAKE A BACKUP OF THE FILE YOU ARE INTERESTED IN!!!!!!");
            Console.Out.WriteLine(@"1. Do a read mode execution on the file you are interested in.");
            Console.Out.WriteLine(@"  > ValkyrieEdit.exe Path\To\File\game_info.mxe");
            Console.Out.WriteLine(@"2. Edit one of the generated *-Data.csv files (or *-Hex.csv for Hex mode) using an editor of your choice. Open Office spreadsheet set to use UTF-8 character set works perfectly.");
            Console.Out.WriteLine(@"3. Do a test mode execution on the file you wish to change.");
            Console.Out.WriteLine(@"  > ValkyrieEdit.exe Path\To\File\game_info.mxe -T");
            Console.Out.WriteLine(@"4. Confirm that console output reports only changing data you really wish to change.");
            Console.Out.WriteLine(@"5. Do a sync mode execution on the file you wish to change.");
            Console.Out.WriteLine(@"  > ValkyrieEdit.exe Path\To\File\game_info.mxe -s");
            Console.Out.WriteLine(@"6. Run the Valkyrie Chronicles game normally and confirm your changes are in place!");
            Console.Out.WriteLine(@"7. ^_^ Enjoy ^_^");
        }

        private static void HandleFileOrMethod(string fn, bool isSync, bool isTest, bool writeHex, bool writeIndex)
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
            FileInfo[] files;
            string search = "*.mxe";
            files = d.GetFiles(search);
            foreach (FileInfo fi in files)
            {
                HandleFile(fi.FullName, isSync, isTest, writeHex, writeIndex);
            }
        }

        private static void HandleFile(string fn, bool isSync, bool isTest, bool writeHex, bool writeIndex)
        {
            MxeParser parser = ReadMxe(fn);

            if (!isTest)
            {
                DoMxeToCsv(writeHex, writeIndex, parser);
            }
            else
            {
                DoCsvToMxeSync(fn, isSync, parser);
            }
        }

        private static void FigureOutWhatFileToUse(ref string fn, ref bool wasGivenFile)
        {
            string filename = fn;
            if (!wasGivenFile)
            {
                filename = ReadTargetFile();
                if (!String.IsNullOrEmpty(filename))
                {
                    fn = filename;
                    wasGivenFile = true;
                }
            }
            filename = PromptForFile(fn);
            if (!filename.Equals(fn))
            {
                fn = filename;
                WriteTargetFile(fn);
            }
        }

        private static MxeParser ReadMxe(string fn)
        {
            Console.Out.WriteLine("Using [" + fn + "] as source file.");
            MxeParser parser = new MxeParser(fn);

            Console.Out.WriteLine("Reading data...");
            parser.Read();
            return parser;
        }

        private static string PromptForFile(string fn)
        {
            Console.Out.WriteLine("Provide a path to the mxe file:");
            Console.Out.WriteLine("Default value is [" + fn + "]");
            string inFn = Console.ReadLine();

            if (!String.IsNullOrEmpty(inFn))
            {
                fn = inFn;
            }
            return fn;
        }

        private static void DoMxeToCsv(bool writeHex, bool writeIndex, MxeParser parser)
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

        private static void DoCsvToMxeSync(string fn, bool isSync, MxeParser parser)
        {
            //read in CSV and write to MXE if we found a change
            Console.Out.WriteLine("Reading in CSV data...");
            if (parser.ReadCsvs() && isSync)
            {
                Console.Out.WriteLine("Backup MXE file and then Writing out MXE data to [" + fn + "]...");
                
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

        private static string ReadTargetFile()
        {
            string filename = null;
            try
            {
                if (File.Exists(TARGET_FILE))
                {
                    Console.Out.WriteLine("Retrieving file cache from [" + TARGET_FILE + "]");
                    using (var stream = new StreamReader(TARGET_FILE))
                    {
                        if (stream.BaseStream.CanRead)
                        {
                            filename = stream.ReadLine();
                        }

                        stream.Close();
                    }
                }
            }
            catch (Exception exc)
            {
                Console.Out.WriteLine(exc.ToString());
            }

            return filename;
        }

        private static void WriteTargetFile(string filename)
        {
            Console.Out.WriteLine("Writing out file cache to [" + TARGET_FILE + "]");
            try
            {
                using (var stream = new StreamWriter(TARGET_FILE))
                {
                    stream.WriteLine(filename);
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
