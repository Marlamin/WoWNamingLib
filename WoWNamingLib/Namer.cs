using CASCLib;
using DBCD;
using DBCD.Providers;
using TACTSharp;
using WoWNamingLib.Namers;
using WoWNamingLib.Services;
using WoWNamingLib.Utils;

namespace WoWNamingLib
{
    public static class Namer
    {
        public static Dictionary<int, string> IDToNameLookup = new();
        public readonly static HashSet<int> placeholderNames = new();
        public readonly static Dictionary<string, int> DB2ToIDLookup = new();
        public readonly static List<uint> ForceRename = new();

        public static string localProduct = "";
        public static string build = "";
        public static string wowDir = "";
        public static string cacheDir = "";

        private static DBCManager? dbcManager;

        public static bool isInitialized = false;
        public static bool IsCASCLibInit = false;
        public static bool IsTACTSharpInit = false;

        public static Func<int, uint> GetAddedInPatch = (int fileDataID) => { return 0; };
        public static Func<int, string, bool> SetCreatureNameForFDID = (int fileDataID, string name) => { return false; };
        public static Func<int, string> GetCreatureNameByDisplayID = (int displayID) => { return ""; };

        public static void SetTACT(ref BuildInstance instance, ref List<int> availableFDIDs)
        {
            CASCManager.InitializeTACT(ref instance);
            CASCManager.AvailableFDIDs = availableFDIDs;
        }

        public static void SetCASC(ref CASCHandler handler, ref List<int> availableFDIDs)
        {
            CASCManager.InitializeCASC(ref handler);
            CASCManager.AvailableFDIDs = availableFDIDs;
        }

        public static void SetProviders(IDBCProvider dbcProvider, IDBDProvider dbdProvider)
        {
            dbcManager = new DBCManager(dbcProvider, dbdProvider);
        }

        public static void SetProviders(IDBCProvider dbcProvider, Stream bdbdStream)
        {
            dbcManager = new DBCManager(dbcProvider, bdbdStream);
        }

        public static void SetGetExpansionFunction(Func<int, uint> function)
        {
            GetAddedInPatch = function;
        }
        public static void SetSetCreatureNameForFDIDFunction(Func<int, string, bool> function)
        {
            SetCreatureNameForFDID = function;
        }

        public static void SetGetCreatureNameByDisplayIDFunction(Func<int, string> function)
        {
            GetCreatureNameByDisplayID = function;
        }

        public static void AddNewFile(uint fileDataID, string filename, bool updateIfExists = false, bool forceUpdate = false)
        {
            NewFileManager.AddNewFile((int)fileDataID, filename, updateIfExists, forceUpdate);
        }

        public static IDBCDStorage LoadDBC(string name)
        {
            if (dbcManager == null)
                throw new Exception("DBCManager not initialized!");

            return dbcManager.Load(name);
        }

        public static void SetInitialListfile(ref Dictionary<int, string> listfile)
        {
            Console.WriteLine("Setting initial listfile..");
            IDToNameLookup = new(listfile);
            DB2ToIDLookup.Clear();

            foreach (var entry in IDToNameLookup)
            {
                var fileDataID = entry.Key;
                var filename = entry.Value;

                if (filename.ToLower().EndsWith(".db2"))
                    DB2ToIDLookup.Add(Path.GetFileNameWithoutExtension(filename).ToLower(), fileDataID);
            }

            ReloadPlaceholders();

            IDToNameLookup = new(listfile.Where(x => x.Value != ""));

            CASCManager.LoadOfficialListfile();

            isInitialized = true;
        }

        public static void MergeLookups(Dictionary<int, ulong> lookups)
        {
            CASCManager.MergeLookups(lookups);
        }

        static void ReloadPlaceholders()
        {
            foreach (var entry in IDToNameLookup)
            {
                var fileDataID = entry.Key;
                var filename = entry.Value;

                if (filename.StartsWith("models") ||
                    filename.StartsWith("unkmaps") ||
                    filename.Contains("autogen-names") ||
                    filename.Contains(fileDataID.ToString()) ||
                    filename.Contains("unk_exp") ||
                    filename.Contains("tileset/unused"))
                {
                    placeholderNames.Add(fileDataID);
                    continue;
                }

                if (filename.EndsWith(".ogg") && filename.Substring(filename.Length - 10, 6).All(char.IsDigit))
                {
                    placeholderNames.Add(fileDataID);
                    continue;
                }
            }
        }

        public static Dictionary<int, string> GetNewFiles()
        {
            return NewFileManager.ReturnNewNames();
        }

        public static void ClearNewFiles()
        {
            NewFileManager.ClearNewFiles();
        }

        public static void NameMusic()
        {
            try
            {
                Music.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during music naming: " + e.Message);
            }
        }
        public static void NameItemTexture()
        {
            try
            {
                ItemTexture.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during item texture naming: " + e.Message);
            }
        }
        public static void NameInterface()
        {
            try
            {
                Interface.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during interface naming: " + e.Message);
            }
        }

        public static void NameCreatureDisplayInfo(uint filterByFDID = 0)
        {
            try
            {
                CreatureDisplayInfo.Name(filterByFDID);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during CreatureDisplayInfo naming: " + e.Message);
            }
        }

        public static void NameCharCust()
        {
            try
            {
                CharCustomization.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during character customization naming: " + e.Message);
            }
        }
        public static void NameBakedNPC()
        {
            try
            {
                BakedNPC.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during Baked NPC texture naming: " + e.Message);
            }
        }

        public static void NameDBFilesClient(string definitionDir)
        {
            try
            {
                DBFilesClient.Name(definitionDir);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during DBFilesClient naming: " + e.Message);
            }
        }

        public static void NameVO(Dictionary<uint, string> creatureNames, Dictionary<string, List<uint>> textToSoundKitID, Dictionary<uint, string> creaturesToFDID, Dictionary<uint, (uint, uint)> BCTextToSKitIDs, bool overrideVO)
        {
            try
            {
                VO.Name(creatureNames, textToSoundKitID, creaturesToFDID, BCTextToSKitIDs, overrideVO);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during VO naming: " + e.Message);
            }
        }

        public static void NameWWF()
        {
            try
            {
                WWF.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during WWF naming: " + e.Message);
            }
        }

        public static void NameColorGrading()
        {
            try
            {
                ColorGrading.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during ColorGrading naming: " + e.Message);
            }
        }

        public static void NameEmotes()
        {
            try
            {
                Emotes.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during Emotes naming: " + e.Message);
            }
        }

        public static void NameTerrainMaterial()
        {
            try
            {
                TerrainMaterial.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during TerrainMaterial naming: " + e.Message);
            }
        }

        public static void NameAnima()
        {
            try
            {
                Anima.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during Anima naming: " + e.Message);
            }
        }

        public static void NameCollectable()
        {
            try
            {
                Collectable.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during Collectable naming: " + e.Message);
            }
        }

        public static void NameFullScreenEffect()
        {
            try
            {
                FullScreenEffect.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during FullScreenEffect naming: " + e.Message);
            }
        }

        public static void NameGODisplayInfo()
        {
            try
            {
                GODisplayInfo.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during GODisplayInfo naming: " + e.Message);
            }
        }

        public static void NameSound()
        {
            try
            {
                Sound.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during SoundKit naming: " + e.Message);
            }
        }

        public static void NameSpellTextures()
        {
            try
            {
                SpellTextures.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during SpellTextures naming: " + e.Message);
            }
        }

        public static void NameM2(uint fileDataID, bool forceFullRun = false, Dictionary<uint, string> objectModelNames = null)
        {
            try
            {
                Model.Name([fileDataID], forceFullRun, objectModelNames);
                ReloadPlaceholders();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during M2 naming (fullrun: " + false + ", fdid: " + fileDataID + "): " + e.Message);
            }
        }

        public static void NameM2s(List<uint> fileDataIDs, bool forceFullRun = false, Dictionary<uint, string> objectModelNames = null)
        {
            try
            {
                Model.Name(fileDataIDs, forceFullRun, objectModelNames);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during M2 naming: " + e.Message);
            }
        }

        public static void NameMap()
        {
            try
            {
                Map.Name();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during map naming: " + e.Message);
            }
        }

        public static void NameWMO(uint fileDataID = 0)
        {
            try
            {
                if (fileDataID != 0)
                {
                    WMO.Name(true, fileDataID.ToString());
                }
                else
                {
                    WMO.Name();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during WMO naming: " + e.Message);
            }
        }

        public static void NameByContentHashes(Dictionary<int, string> idToHashes)
        {
            try
            {
                ContentHashNamer.Name(idToHashes);
                ReloadPlaceholders();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during content hash naming: " + e.Message);
            }
        }

        public static string NameSingleVO(int fileDataID, string creatureName)
        {
            return VO.NameSingle(fileDataID, creatureName);
        }

        public static string GetSceneScriptDebug(uint packageID)
        {
            return SceneScriptParser.CompilePackage(packageID);
        }

        public static Dictionary<string, TimelineScene> GetSceneScriptCompiledDebug(uint packageID)
        {
            var script = SceneScriptParser.CompilePackage(packageID, "test", false);
            return SceneScriptParser.ParseTimelineScript(script);
        }
    }
}
