using CASCLib;
using DBDefsLib;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using TACTSharp;

namespace WoWNamingLib.Services
{
    public static class CASCManager
    {
        private static HttpClient client = new HttpClient();
        public static List<int> AvailableFDIDs = new();
        public static string BuildName;
        public static CASCLib.Jenkins96 Hasher = new CASCLib.Jenkins96();
        public static HashSet<ulong> OfficialLookups = new();
        public static Dictionary<int, ulong> LookupMap = new();

        private static CASCHandler cascHandler;
        private static BuildInstance buildInstance;

        private static Lock verifiedListfileLock = new();

        public static async Task<Stream> GetFileByID(uint filedataid)
        {
            Stream file = null;
            if(cascHandler == null)
            {
                return new MemoryStream(buildInstance.OpenFileByFDID(filedataid));
            }
            else
            {
                file = cascHandler.OpenFile((int)filedataid);
                if (file == null)
                    throw new Exception("Unable to open file " + filedataid);
            }

            return file;
        }

        public static bool FileExists(int fileDataID)
        {
            return AvailableFDIDs.Contains(fileDataID);
        }

        public static async Task<Stream> GetFileByName(string name)
        {
            Stream file = null;
            if(cascHandler == null)
            {
                using (var jenkins = new TACTSharp.Jenkins96())
                {
                    file = new MemoryStream(buildInstance.OpenFileByFDID(buildInstance.Root.GetEntriesByLookup(jenkins.ComputeHash(name))[0].fileDataID));
                }
            }
            else
            {
                file = cascHandler.OpenFile(name);
                if (file == null)
                    throw new Exception("Unable to open file " + name);
            }

            return file;
        }

        public static async Task<int> GetFileDataIDByName(string name)
        {
            if(cascHandler == null)
            {
                using (var jenkins = new TACTSharp.Jenkins96())
                {
                    var entries = buildInstance.Root.GetEntriesByLookup(jenkins.ComputeHash(name));
                    if (entries.Count == 0)
                        return 0;
                    return (int)entries[0].fileDataID;
                }
            }
            else
            {
                var wrh = cascHandler.Root as WowRootHandler;
                var hash = Hasher.ComputeHash(name);
                return wrh.GetFileDataIdByHash(hash);
            }
        }

        public static async Task<ulong> GetHashByFileDataID(int filedataid)
        {
            if(cascHandler == null)
            {
                using (var jenkins = new TACTSharp.Jenkins96())
                {
                    var entries = buildInstance.Root.GetEntriesByFDID((uint)filedataid);
                    if (entries.Count == 0)
                        return 0;
                    return entries[0].lookup;
                }
            }
            else
            {
                var wrh = cascHandler.Root as WowRootHandler;
                var cascLibLookup = wrh.GetHashByFileDataId(filedataid);

                // Check if this is a fake CASCLib lookup
                if (cascLibLookup == ComputeFakeCASCLibHash(filedataid))
                    return 0;
                else
                    return cascLibLookup;
            }
        }

        public static ulong ComputeFakeCASCLibHash(int fileDataId)
        {
            ulong baseOffset = 0xCBF29CE484222325UL;

            for (int i = 0; i < 4; i++)
            {
                baseOffset = 0x100000001B3L * ((((uint)fileDataId >> (8 * i)) & 0xFF) ^ baseOffset);
            }

            return baseOffset;
        }

        public static void InitializeTACT(ref BuildInstance build)
        {
            CASCManager.buildInstance = build;
            BuildName = build.BuildConfig.Values["build-name"][0];
        }

        public static void InitializeCASC(ref CASCHandler cascHandler)
        {
            CASCManager.cascHandler = cascHandler;
            BuildName = cascHandler.Config.BuildName;
        }

        public static void MergeLookups(Dictionary<int, ulong> lookups)
        {
            foreach (var lookup in lookups)
            {
                if (!LookupMap.ContainsKey(lookup.Key))
                    LookupMap.Add(lookup.Key, lookup.Value);

                if(!OfficialLookups.Contains(lookup.Value))
                    OfficialLookups.Add(lookup.Value);
            }
        }

        public static void LoadOfficialLookups()
        {
            Console.WriteLine("Loading official lookups..");

            var download = false;
            var filename = "lookup.csv";

            lock (verifiedListfileLock)
            {
                if (File.Exists(filename))
                {
                    var info = new FileInfo(filename);
                    if (DateTime.Now.Subtract(TimeSpan.FromDays(1)) > info.LastWriteTime)
                    {
                        Console.WriteLine("Official lookups outdated, redownloading..");
                        download = true;
                    }
                }
                else
                {
                    download = true;
                }

                if (download)
                {
                    var listfile = client.GetStringAsync("https://raw.githubusercontent.com/wowdev/wow-listfile/refs/heads/master/meta/lookup.csv").Result;
                    File.WriteAllText(filename, listfile);
                }

                foreach (var line in File.ReadAllLines(filename))
                {
                    var splitLine = line.Split(';');
                    var fdid = int.Parse(splitLine[0]);
                    var jenkinsHash = Convert.ToUInt64(splitLine[1], 16);

                    OfficialLookups.Add(jenkinsHash);

                    LookupMap.TryAdd(fdid, jenkinsHash);
                }
            }

            Console.WriteLine("Loaded " + OfficialLookups.Count + " official lookups!");
        }
    }
}
