using DBCD;

namespace WoWNamingLib.Services
{
    public static class SoundKitHelper
    {
        private static Dictionary<uint, List<int>> SoundKitMap = new Dictionary<uint, List<int>>();

        // Temp
        private static Dictionary<uint, List<int>> SoundKitEntryMap = new Dictionary<uint, List<int>>();
        private static Dictionary<uint, List<uint>> SoundKitChildMap = new Dictionary<uint, List<uint>>();

        private static void Initialize()
        {
            var soundKitChildDB = Namer.LoadDBC("SoundKitChild");
            if (!soundKitChildDB.AvailableColumns.Contains("ParentSoundKitID") || !soundKitChildDB.AvailableColumns.Contains("SoundKitID"))
                throw new Exception("SoundKitChild DB2 does not contain ParentSoundKitID or SoundKitID columns");

            foreach(var soundKitChild in soundKitChildDB.Values)
            {
                var parentSoundKitID = (uint)soundKitChild["ParentSoundKitID"];
                var soundKitID = (uint)soundKitChild["SoundKitID"];

                if (!SoundKitChildMap.TryGetValue(parentSoundKitID, out List<uint>? soundKits))
                    SoundKitChildMap[parentSoundKitID] = new List<uint> { soundKitID };
                else
                    soundKits.Add(soundKitID);
            }

            Console.WriteLine("Building SoundKit map");
            var soundKitEntryDB = Namer.LoadDBC("SoundKitEntry");
            if (!soundKitEntryDB.AvailableColumns.Contains("SoundKitID") || !soundKitEntryDB.AvailableColumns.Contains("FileDataID"))
                throw new Exception("SoundKitEntry DB2 does not contain SoundKitID or FileDataID columns");

            foreach (var soundKitEntry in soundKitEntryDB.Values)
            {
                var soundKitIDFromDB = (uint)soundKitEntry["SoundKitID"];
                var soundKitFileDataID = (int)soundKitEntry["FileDataID"];

                if (!SoundKitEntryMap.TryGetValue(soundKitIDFromDB, out List<int>? fdids))
                    SoundKitEntryMap.Add(soundKitIDFromDB, new List<int> { soundKitFileDataID });
                else
                    fdids.Add(soundKitFileDataID);
            }

            Console.WriteLine("Adding SoundKits from SoundKit.db2 that don't have filedataids..");
            // Make sure all soundkits are in the map
            var soundKitDB = Namer.LoadDBC("SoundKit");
            foreach (var soundKit in soundKitDB.Values)
            {
                var soundKitID = (uint)((int)soundKit["ID"]);
                if (!SoundKitEntryMap.ContainsKey(soundKitID))
                    SoundKitEntryMap.Add(soundKitID, new List<int>());
            }

            Console.WriteLine("Building recursive SoundKit map");
            // Loop over each SoundKitID and add all child SoundKitIDs
            foreach(var soundKitID in SoundKitEntryMap.Keys.ToList())
            {
                if(SoundKitEntryMap.TryGetValue(soundKitID, out var FDIDs))
                    SoundKitMap[soundKitID] = new List<int>(FDIDs);
                else
                    SoundKitMap[soundKitID] = new List<int>();

                SoundKitMap[soundKitID].AddRange(GetRecursiveFileDataIDs(soundKitID));
            }

            Console.WriteLine("Removing duplicate FileDataIDs");
            // Make sure SoundKitMap has no duplicates
            foreach (var soundKitID in SoundKitMap.Keys.ToList())
            {
                SoundKitMap[soundKitID] = SoundKitMap[soundKitID].Distinct().ToList();
            }

            Console.WriteLine("SoundKit map built");

            // Clear temp map to save memory
            SoundKitEntryMap.Clear();
            SoundKitChildMap.Clear();
        }

        public static List<int> GetRecursiveFileDataIDs(uint soundKitID)
        {
            var fileDataIDs = new List<int>();

            foreach(var childSoundKitID in GetChildSoundKits(soundKitID))
            {
                if (SoundKitEntryMap.TryGetValue(childSoundKitID, out var FDIDs))
                    fileDataIDs.AddRange(FDIDs);

                fileDataIDs.AddRange(GetRecursiveFileDataIDs(childSoundKitID));
            }

            return fileDataIDs;
        }

        public static List<int> GetFDIDsByKitID(int soundKitID)
        {
            return GetFDIDsByKitID((uint)soundKitID);
        }

        public static List<int> GetFDIDsByKitID(uint soundKitID)
        {
            if (SoundKitMap.Count == 0)
                Initialize();

            if(SoundKitMap.TryGetValue(soundKitID, out List<int>? fdids))
                return fdids;
            else
                return new List<int>();
        }

        private static List<uint> GetChildSoundKits(uint parentSoundKitID)
        {
            if (SoundKitChildMap.TryGetValue(parentSoundKitID, out List<uint>? childSoundKitIDs))
                return childSoundKitIDs;
            else
                return [];
        }
    }
}
