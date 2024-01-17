using CASCLib;
using DBCD;
using DBCD.Providers;
using WoWNamingLib.Namers;
using WoWNamingLib.Services;

namespace WoWNamingLib
{
    public static class Namer
    {
        public static Dictionary<uint, string> IDToNameLookup = new();
        public static HashSet<uint> placeholderNames = new();
        public static Dictionary<string, uint> DB2ToIDLookup = new();

        public static string localProduct = "";
        public static string build = "";
        public static string wowDir = "";

        private static DBCManager? dbcManager;

        public static void SetCASC(ref CASCHandler handler, ref List<int> availableFDIDs)
        {
            CASCManager.InitializeCASC(ref handler);
            CASCManager.AvailableFDIDs = availableFDIDs;
        }

        public static void SetProviders(IDBCProvider dbcProvider, IDBDProvider dbdProvider)
        {
            dbcManager = new DBCManager(dbcProvider, dbdProvider);
        }

        public static IDBCDStorage LoadDBC(string name)
        {
            if (dbcManager == null)
                throw new Exception("DBCManager not initialized!");

            return dbcManager.Load(name);
        }

        public static void SetInitialListfile(ref Dictionary<uint, string> listfile)
        {
            IDToNameLookup = new(listfile);

            foreach (var entry in IDToNameLookup)
            {
                var fileDataID = entry.Key;
                var filename = entry.Value;

                if (filename.EndsWith(".db2"))
                    DB2ToIDLookup.Add(Path.GetFileNameWithoutExtension(filename).ToLower(), fileDataID);

                if (filename.StartsWith("models") ||
                    filename.StartsWith("unkmaps") ||
                    filename.Contains("autogen-names") ||
                    filename.Contains(fileDataID.ToString()) ||
                    filename.Contains("unk_exp") ||
                    filename.Contains("tileset/unused"))
                {
                    placeholderNames.Add(fileDataID);
                }
            }

            CASCManager.LoadOfficialListfile();
        }

        public static Dictionary<uint, string> GetNewFiles()
        {
            return NewFileManager.ReturnNewNames();
        }

        public static void NameVO(string creatureCacheLocation)
        {
            var creatureNames = new Dictionary<uint, string>();

            try
            {
                if (File.Exists(creatureCacheLocation))
                {
                    Console.WriteLine("Loading creature cache for model naming");
                    var creatureWDB = WDBManager.LoadWDB(creatureCacheLocation);
                    foreach (var entry in creatureWDB.entries)
                    {
                        creatureNames.Add(uint.Parse(entry.Key), entry.Value["Name[0]"]);
                    }
                }
                else
                {
                    Console.WriteLine("creaturecache.wdb not found, skipping VO naming");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error loading creaturecache.wdb: " + e.Message);
            }

            try
            {
                VO.Name(creatureNames);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during VO naming: " + e.Message);
            }
        }
    }
}
