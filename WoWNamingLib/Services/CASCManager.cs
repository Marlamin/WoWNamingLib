using CASCLib;

namespace WoWNamingLib.Services
{
    public static class CASCManager
    {
        private static HttpClient client = new HttpClient();
        private static CASCHandler cascHandler;
        public static List<int> AvailableFDIDs = new();
        public static string BuildName;
        public static Jenkins96 Hasher = new Jenkins96();
        public static HashSet<ulong> OfficialLookups = new();

        public static async Task<Stream> GetFileByID(uint filedataid)
        {
            var file = cascHandler.OpenFile((int)filedataid);
            if (file == null)
                throw new Exception("Unable to open file " + filedataid);

            return file;
        }

        public static bool FileExists(int fileDataID)
        {
            return AvailableFDIDs.Contains(fileDataID);
        }

        public static async Task<Stream> GetFileByName(string name)
        {
            var file = cascHandler.OpenFile(name);
            if (file == null)
                throw new Exception("Unable to open file " + name);

            return file;
        }

        public static async Task<int> GetFileDataIDByName(string name)
        {
            var wrh = cascHandler.Root as WowRootHandler;
            var hash = Hasher.ComputeHash(name);
            return wrh.GetFileDataIdByHash(hash);
        }

        public static async Task<ulong> GetHashByFileDataID(int filedataid)
        {
            var wrh = cascHandler.Root as WowRootHandler;
            return wrh.GetHashByFileDataId(filedataid);
        }

        public static void InitializeCASC(ref CASCHandler cascHandler)
        {
            CASCManager.cascHandler = cascHandler;
            BuildName = cascHandler.Config.BuildName;
        }

        public static void LoadOfficialListfile()
        {
            Console.WriteLine("Loading official listfile..");

            var download = false;

            if (File.Exists("listfile.txt"))
            {
                var info = new FileInfo("listfile.txt");
                if (DateTime.Now.Subtract(TimeSpan.FromDays(1)) > info.LastWriteTime)
                {
                    Console.WriteLine("Official listfile outdated, redownloading..");
                    download = true;
                }
            }
            else
            {
                download = true;
            }

            if (download)
            {
                var listfile = client.GetStringAsync("https://github.com/wowdev/wow-listfile/raw/master/listfile.txt").Result;
                File.WriteAllText("listfile.txt", listfile);
            }

            foreach (var line in File.ReadAllLines("listfile.txt"))
            {
                var jenkinsHash = Hasher.ComputeHash(line);
                OfficialLookups.Add(jenkinsHash);
            }

            Console.WriteLine("Loaded " + OfficialLookups.Count + " files from official listfile!");
        }
    }
}
