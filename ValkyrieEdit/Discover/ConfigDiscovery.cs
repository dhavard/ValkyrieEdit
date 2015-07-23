using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleApplication2.Data;
using System.IO;

namespace ValkyrieEdit.Discover
{
    public class ConfigDiscovery
    {
        private const string ADDITIONAL_FORMAT = @"Found missing config entry for config type [{0}]. CHECK YOUR config.txt FILE. Sample config entry (saved to discovery.txt) as follows [{1}].";
        private const string DISCOVERY_FILE = @".\discovery.txt";

        private static List<MxeEntryType> _mxeTypes = new List<MxeEntryType>();

        protected static List<MxeEntryType> MxeTypes
        {
            get { return ConfigDiscovery._mxeTypes; }
            set { ConfigDiscovery._mxeTypes = value; }
        }

        public static void AddNewMxeType(MxeEntryType newType)
        {
            Console.Out.WriteLine(String.Format(ADDITIONAL_FORMAT, newType.Type1, newType.ToString()));
            _mxeTypes.Add(newType);
        }

        public static bool HasDiscoveries()
        {
            return _mxeTypes.Count > 0;
        }

        public static void PrintDiscoveries()
        {
            try
            {
                using (var stream = new StreamWriter(DISCOVERY_FILE))
                {
                    foreach (MxeEntryType newType in _mxeTypes)
                    {
                        stream.WriteLine(newType.ToString());
                    }

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
