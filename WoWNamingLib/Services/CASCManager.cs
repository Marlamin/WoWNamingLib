﻿using CASCLib;
using DBDefsLib;
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

        public static void InitializeTACT(ref BuildInstance build)
        {
            CASCManager.buildInstance = build;

            var splitName = build.BuildConfig.Values["build-name"][0].Replace("WOW-", "").Split("patch");
            BuildName = splitName[1].Split("_")[0] + "." + splitName[0];
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

        public static void LoadOfficialListfile()
        {
            Console.WriteLine("Loading official listfile..");

            var download = false;

            lock (verifiedListfileLock)
            {
                if (File.Exists("verified-listfile.csv"))
                {
                    var info = new FileInfo("verified-listfile.csv");
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
                    var listfile = client.GetStringAsync("https://github.com/wowdev/wow-listfile/releases/latest/download/verified-listfile.csv").Result;
                    File.WriteAllText("verified-listfile.csv", listfile);
                }

                foreach (var line in File.ReadAllLines("verified-listfile.csv"))
                {
                    var splitLine = line.Split(';');
                    var jenkinsHash = Hasher.ComputeHash(splitLine[1]);
                    var fdid = int.Parse(splitLine[0]);

                    OfficialLookups.Add(jenkinsHash);

                    LookupMap.TryAdd(fdid, jenkinsHash);
                }
            }

            Console.WriteLine("Loaded " + OfficialLookups.Count + " files from official listfile!");
        }
    }
}
