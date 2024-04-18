using CASCLib;
using System.Data;

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
            if(fileDataID > 4559188)
                return "exp10";
            else
                return "exp09";
        }

        public static Dictionary<int, string> ReturnNewNames()
        {
            return newFiles;
        }

        public static void ClearNewFiles()
        {
            newFiles.Clear();
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

            if (CASCManager.BuildName.Contains("Classic"))
            {
                // Check filehashes on classic
                var hashByFDID = CASCManager.GetHashByFileDataID((int)fileDataID).Result;
                if (hashByFDID != 0)
                {
                    var hash = Hasher.ComputeHash(filename);
                    if (hashByFDID != hash)
                    {
                        Console.WriteLine("Hash mismatch for " + fileDataID + ": " + filename);
                        return;
                    }
                }

                // Don't accept capital-only differences from Classic.
                if (Namer.IDToNameLookup.ContainsKey(fileDataID) && Namer.IDToNameLookup[fileDataID].ToLower() == filename.ToLower())
                    return;
            }

            if (Namer.IDToNameLookup.TryGetValue(fileDataID, out string currentFileName))
            {
                if(currentFileName.ToLower().StartsWith("interface"))
                {
                    Console.WriteLine("Skipping " + fileDataID + ", attempted to overwrite interface file: " + currentFileName + " => " + filename);
                    return;
                }

                var caseOnlyFix = currentFileName.ToLower() == filename.ToLower() && currentFileName != filename;

                var oldHash = Hasher.ComputeHash(currentFileName);
                if (CASCManager.OfficialLookups.Contains(oldHash))
                {
                    //Console.WriteLine("[ERROR] Official name being overriden for " + fileDataID + ", old " + currentFileName + ", new: " + filename);
                    return;
                }

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
                        //if (!currentFileName.Contains("exp09") && !Path.GetFileNameWithoutExtension(currentFileName).All(char.IsDigit) && currentFileName.EndsWith(".m2"))
                        //    return;

                        if (!currentFileName.Contains("exp09") && !currentFileName.Contains("exp10") && !Path.GetFileNameWithoutExtension(currentFileName).All(char.IsDigit) && currentFileName.EndsWith(".blp"))
                            return;

                        //if (Program.IDToNameLookup[fileDataID].Contains("exp09") && filename.Contains("exp09"))
                        //    return;
                    }

                    if (filename.ToLower().Contains("world/wmo") && currentFileName.ToLower().Contains("tileset"))
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
