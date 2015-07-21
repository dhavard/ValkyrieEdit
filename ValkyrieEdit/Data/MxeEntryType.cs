using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ConsoleApplication2.Data
{
    public class MxeEntryType
    {
        private const string VIM_PART = @"VlMx";
        private const string ADDITIONAL_PART = @"Additional";
        private const string A_120_PART = @"_120";
        private const string ERROR_FORMAT = @"Failed to determine MxeEntryType due to non-matching lengths for type [{0}]. CHECK YOUR config.txt FILE. Expected [{1}] but found [{2}] (both numbers rounded to the nearest count 16 bytes since that is how they are addressed).";
        private const string ADDITIONAL_FORMAT = @"Found missing config entry for config type [{0}]. CHECK YOUR config.txt FILE. Sample config entry as follows [{1}].";
        private const string CONFIG_FILE = @".\config.txt";
        
        private static Dictionary<string, MxeEntryType> _knownTypes;
        private static MxeEntryType _other;

        public static MxeEntryType Other
        {
            get { return MxeEntryType._other; }
        }

        public static Dictionary<string, MxeEntryType> KnownTypes
        {
            get { return MxeEntryType._knownTypes; }
            set { MxeEntryType._knownTypes = value; }
        }

        static MxeEntryType()
        {
            string otherName = "Other";
            _other = new MxeEntryType(otherName, 0);
            _knownTypes = new Dictionary<string, MxeEntryType>();

            try
            {
                using (var stream = new StreamReader(CONFIG_FILE))
                {
                    string line;
                    while ((line = stream.ReadLine()) != null)
                    {
                        string[] info = line.Split(',');
                        List<string> headers = info.ToList();
                        headers.RemoveAt(0);
                        _knownTypes.Add(info[0], new MxeEntryType(info[0], headers));
                    }
                    
                    stream.Close();
                }
            }
            catch (Exception exc)
            {
                Console.Out.WriteLine(exc.ToString());
            }

            // i, p, f, h, l, b
            // int, pointer, float, hex, l one-by-one, binary
            /*
            HyColor,iId,f,f,f
            MxParameterFog,,,,,,,,
            MxParameterLight,,,,,,,,,,,,,,,,,,,,,,,,
            VlMxAddEsdInfo,,,,
            VlMxCharacterAffinityInfo,iUnit1,iUnit2,iPercentage,
            VlMxCharacterCommonInfo,,,,,,,,,,,,,,,,,,,,,,,,
            VlMxCharacterEachInfo,iId1,iId2,iModel,,,,iGrowthRate,,,,fHp,fAccuracy,fEvasion,,,,,,,fOverHp,fOverAccuracy,fOverEvade,fOverAp,fOverDef,iOverLevel,iOverCp,,,,,,,,,,,,,,,,,,,,,,,,,,,,,iAccess1,iAccess2,,,iAccess3,iAccess4,,,,,,,,,,iPersonalPotential1,iPersonalPotential2,iPersonalPotential3,iPersonalPotential4,iPersonalPotDefault1,iPersonalPotDefault2,iPersonalPotDefault3,iPersonalPotDefault4,,,,,,,,,,,,,,,,,hVoice1,hVoice2,hVoice3,hVoice4,,,,,iClassPotential1,iClassPotential2,iClassPotential3,iClassPotential4,,,,,iClassPotLevel1,iClassPotLevel2,iClassPotLevel3,iClassPotLevel4,,,,,,,,,,,,,hVoice5,hVoice6,hVoice7,hVoice8,,iAnimation1,iAnimation2,,,,
            VlMxCharacterInfo,iModel,pFirstName1,pLastName,pFirstName2,pFirstName3,,pFirstName4,,,,,,,,,,,,,
            VlMxClothesInfo,pDescription,pName,,iBlastProtection,iRating,iChapter,iEpisode,iCost,fArmor,,,
            VlMxExplosiveInfo,,,,,,,,
            VlMxForceInfo,pDescription,pName,,iBlastProtection,iRating,iChapter,iEpisode,iCost,fArmor,,,
            VlMxGalliaRareWeaponCandidateInfo,,,,,,hWeaponId8Pair,hWeaponId8Pair2,
            VlMxGeneralCharInfo,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,
            VlMxGeneralCharParamSetInfo,,,,,,,,,,,,,,,,,,,,,,,,
            VlMxJobInfo,,iClassType,pName1,pEliteName,,,fGrassDetect,,iLevel1,iLevel2,iLevel3,iLevel4,iLevel5,iLevel6,iLevel7,iLevel8,iLevel9,iLevel10,iLevel11,iLevel12,iLevel13,iLevel14,iLevel15,iLevel16,iLevel17,iLevel18,iLevel19,iLevel20,,,fInterceptAngle,f,fAssistAngle,f,fBaseHp,fBaseAccuracy,fBaseEvade,fBaseAp,fBaseDefense,fEliteHp,fEliteAccuracy,fEliteEvade,fEliteAp,fEliteDefense,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,
            VlMxMapObjectCommonInfo,,,,
            VlMxMapObjectInfo,,,,,,,,,,,,
            VlMxNewsPaperInfo,,,,,,,,
            VlMxOrderDirectionInfo,iId,f,f,f
            VlMxOrderInfo,iId,pName1,pDescription,,,,lPowerminPowermax,fRange,iEffect,lCp,,,,,,,,,,,,,,
            VlMxParameterConvertTable,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,fRadiatorCrit,f,f,f,f,f,fHeadshotCrit,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f
            VlMxPotentialInfo,iId,pName1,pDescription,lAccuracyEvasionVspersonVsarmor,lDefenseShotsHpGrassdetect,lRagnaid,hSpecial1,hAnimation,hSpecial2,iActivationCondition,fActivationRate,
            VlMxSlgInfo,,,,,,,,,,,,
            VlMxSlgLandformInfo,,,,,,,,
            VlMxSlgStrongholdCommonInfo,,,,,,,,,,,,
            VlMxVehicleCommonInfo,pDescription,pName,,iBlastProtection,iRating,iChapter,iEpisode,iCost,fArmor,,,
            VlMxVehicleDevChangeParamInfo,,,,
            VlMxVehicleDevInfo,pName1,pDescription1,pDescription2,,iId1,iId2,hId3,iChapter?,iEpisode?,iPreviousUpgrade?,,hUnlock,iPrice,lVsizeHsizeNothingNothing,iStats1,iStats2,iStats3,iStats4,iStats5,,hWeaponry,hBodyHp,hTreadHp,fAp,iAmmo,fBodyDef,fTreadDef,fCritDef,fAccuracy,hGridSize,,
            VlMxVehicleEachInfo,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,
            VlMxVehicleInfo,iId,,pName1,pName2,pWeaponName1,pWeaponName2,pWeaponName3,,,,pWeaponName4,,,,fInterceptAngle,,fAssistAngle,,,,fBodyHp,fTreadHp,,,fAp,fBodyDef,fTreadDef,,,,,,,fCrit,fFlamethrower,,fWeaponAccuracy1?,fWeaponAccuracy2?,iWeaponId1,iWeaponId2,iWeaponId3,iWeaponId4,,,,,,,,,,
            VlMxUnitCommonInfo,,,,,,,,,,,,,,,,,,,,,,,,,,,,
            VlMxUnitGrowthTypeInfo,iId,f,f,f
            VlMxWeaponBringOnUnwholesomeInfo,,,,,,,,
            VlMxWeaponCommonInfo,,,,
            VlMxWeaponInfo,pDescription,pName,,iModel,hId1,hId2,hId3,hId4,hId5,hId6,hId7,hId8,iChapter,iEpisode,iElite,iPrice,,,,,,,,,,iStatusDown,bWhoCanEquip,iInterception,,,,iAmmo,iShots,fZoom,fAccuracy,iExplosion1,iExplosion2,iExplosion3,fExplosion4,,,,,,f,fMaxEffectiveRange,f,fRange,f,fRangeAttackReduction,i,fVsPerson,fVsArmor,,,,,,,,,,,
             * */
        }

        private int _length;

        public int Length
        {
            get { return _length; }
            set { _length = value; }
        }

        private string _type;

        public string Type1
        {
            get { return _type; }
            set { _type = value; }
        }

        private List<string> _headers;

        public List<string> Headers
        {
            get { return _headers; }
            set { _headers = value; }
        }

        public MxeEntryType(string typ, int length)
        {
            _type = typ;
            _length = length;
            _headers = new List<string>();

            for (int i = 0; i < length; i++)
            {
                _headers.Add(String.Empty);
            }
        }

        public MxeEntryType(string typ, List<string> headers)
        {
            _type = typ;
            _length = headers.Count;
            _headers = headers;
        }

        public bool Equals(MxeEntryType met)
        {
            return _type.Equals(met.Type1);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(Type1);

            foreach (string h in Headers)
            {
                sb.Append(',');
                sb.Append(h);
            }

            return sb.ToString();
        }

        public static MxeEntryType GetEntryType( MxeIndexEntry mie )
        {
            int length = BitConverter.ToInt32(mie.TypeCode.GetBytes(),0);
            string name = mie.GetVmTitle();
            MxeEntryType ret = _other;

            if (_knownTypes.ContainsKey(name))
            {
                ret = CheckTypeLength(mie, name, ret);
            }
            else if(name.StartsWith(VIM_PART))
            {
                string oldName = StripAdditionalName(name);
                if (_knownTypes.ContainsKey(oldName))
                {
                    MxeEntryType newType = new MxeEntryType(name, mie.GetExpectedByteWords());
                    newType.Headers = _knownTypes[oldName].Headers.ToArray().ToList(); //cheater clone?
                    while (newType.Headers.Count < newType.Length)
                    {
                        newType.Headers.Add(String.Empty);
                    }

                    Console.Out.WriteLine(String.Format(ADDITIONAL_FORMAT, newType.Type1, newType.ToString()));
                    _knownTypes.Add(newType.Type1, newType);
                    ret = newType;
                }
            }

            return ret;
        }

        private static string StripAdditionalName(string name)
        {
            return name.Replace(ADDITIONAL_PART, String.Empty).Replace(A_120_PART, String.Empty);
        }

        private static MxeEntryType CheckTypeLength(MxeIndexEntry mie, string name, MxeEntryType ret)
        {
            MxeEntryType met = _knownTypes[name];
            int indexLength = mie.GetExpectedByteWords();
            if (met.Length == indexLength)
            {
                ret = met;
            }
            else
            {
                Console.Out.WriteLine(String.Format(ERROR_FORMAT, name, met.Length, indexLength));
            }
            return ret;
        }
    }
}
