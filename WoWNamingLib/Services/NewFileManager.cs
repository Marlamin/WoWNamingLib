using CASCLib;
using System.Data;
using System.Diagnostics;

namespace WoWNamingLib.Services
{
    public static class NewFileManager
    {
        private static Dictionary<int, string> newFiles;
        private static Jenkins96 Hasher = new Jenkins96();

        static NewFileManager()
        {
            newFiles = new Dictionary<int, string>();
        }

        public static string GetExpansionForFileDataID(uint fileDataID)
        {
            var upstreamVersion = Namer.GetAddedInPatch((int)fileDataID);
            if (upstreamVersion == 0)
            {
                if (fileDataID > 4559188)
                    return "exp10";
                else
                    return "exp09";
            }
            else
            {
                var expansionNum = (upstreamVersion / 10000) - 1;
                return "exp" + expansionNum.ToString("D2");
            }
        }

        public static Dictionary<int, string> ReturnNewNames()
        {
            return newFiles;
        }

        public static void ClearNewFiles()
        {
            newFiles.Clear();
        }

        public static void AddNewFileByname(string filename)
        {
            var fdid = CASCManager.GetFileDataIDByName(filename).Result;
            if (fdid == 0)
                return;

            AddNewFile(fdid, filename, true, true);
        }

        // Overload for old namer code compatibility
        public static void AddNewFile(uint fileDataID, string filename, bool updateIfExists = false, bool forceUpdate = false)
        {
            AddNewFile((int)fileDataID, filename, updateIfExists, forceUpdate);
        }

        public static void AddNewFile(int fileDataID, string filename, bool updateIfExists = false, bool forceUpdate = false)
        {


            // Please don't overwrite these files.
            if (fileDataID == 0 || fileDataID == 4279042 || fileDataID == 5044357 || fileDataID == 2887301 || fileDataID == 3557051)
                return;

            var newLookup = Hasher.ComputeHash(filename);

            // Retrieve lookup from lookup.csv.
            if (CASCManager.LookupMap.TryGetValue(fileDataID, out var cachedLookup))
            {
                if(newLookup != cachedLookup)
                {
                    Console.WriteLine("Incoming filename " + filename + " for FDID " + fileDataID + " does not match known lookup " + cachedLookup.ToString("X16") + ", skipping.");
                    return;
                }
            }

            // Retrieve lookup from CASC.
            var hashByFDID = CASCManager.GetHashByFileDataID((int)fileDataID).Result;
            if (hashByFDID != 0)
            {
                if (hashByFDID != newLookup)
                {
                    Console.WriteLine("Hash mismatch for " + fileDataID + ": " + filename);
                    return;
                }

                if(cachedLookup != hashByFDID)
                {
                    Console.WriteLine("!!! CASC-sourced lookup for file " + fileDataID + " does not match known lookup from listfile repo (" + hashByFDID.ToString("X16") + " != " + cachedLookup.ToString("X16") + " )!");
                }
            }

            if (Namer.IDToNameLookup.TryGetValue(fileDataID, out var currentFileName))
            {
                var oldHash = Hasher.ComputeHash(currentFileName);

                var caseOnlyFix = currentFileName.Equals(filename, StringComparison.OrdinalIgnoreCase) && !currentFileName.Equals(filename, StringComparison.Ordinal);
                if (!Namer.AllowCaseRenames && caseOnlyFix)
                    return;

                if (cachedLookup != 0 && cachedLookup != newLookup)
                    return;

                if (
                    (
                    (currentFileName.Contains("exp09") && !filename.Contains("exp09")) ||
                    (currentFileName.Contains("exp10") && !filename.Contains("exp10"))
                    )
                     || currentFileName.All(char.IsDigit))
                    updateIfExists = true;

                if (updateIfExists)
                {
                    if (filename == currentFileName)
                        return;

                    if (!forceUpdate)
                    {
                        if(!Namer.placeholderNames.Contains(fileDataID))
                        {
                            Console.WriteLine("Skipping " + fileDataID + ", attempted to overwrite " + currentFileName + " with " + filename);
                            return;
                        }

                        if (!currentFileName.StartsWith("models") && !currentFileName.Contains("exp09") && !currentFileName.Contains("exp10") && !Path.GetFileNameWithoutExtension(currentFileName).All(char.IsDigit) && currentFileName.EndsWith(".blp"))
                            return;
                    }

                    if (filename.Contains("world/wmo", StringComparison.CurrentCultureIgnoreCase) && currentFileName.Contains("tileset", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.WriteLine("Skipping " + fileDataID + ", attempted to overwrite tileset with WMO texture: " + currentFileName + " => " + filename);
                        return;
                    }

                    Console.WriteLine("Overriding " + fileDataID + ": " + currentFileName + " with " + filename);
                    newFiles[fileDataID] = filename;
                    Namer.IDToNameLookup[fileDataID] = filename;
                }

                return;
            }

            Console.WriteLine("Adding new file: " + fileDataID + ";" + filename);
            newFiles.Add(fileDataID, filename);
            Namer.IDToNameLookup.TryAdd(fileDataID, filename);
        }

        public static void ScanForLongBasenames()
        {
            var longNames = Namer.IDToNameLookup.Select(x => x.Value).Where(x => Path.GetFileName(x).Length > 64).ToList();
            if (longNames.Count > 0)
            {
                Console.WriteLine("Found " + longNames.Count + " files with long basenames.");
                File.WriteAllLines("longnames.txt", longNames);
            }
        }
        public static void DumpNewFiles()
        {
            ScanForLongBasenames();
            if (newFiles.Count == 0)
                Console.WriteLine("No new files found.");

            if (File.Exists("newfiles.txt"))
                File.Delete("newfiles.txt");

            List<string> lines = new List<string>();
            foreach (var file in newFiles)
            {
                Console.WriteLine(file.Key + ";" + file.Value);
                lines.Add(file.Key + ";" + file.Value);
            }

            File.WriteAllLines("newfiles.txt", lines);
        }
    }
}
