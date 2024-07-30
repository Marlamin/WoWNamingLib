using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    public class Music
    {
        private static string GetFolderName(int fdid)
        {
            var addedIn = Namer.GetAddedInPatch(fdid);
            var folderName = "Unknown";
            if (addedIn > 0)
            {
                if (addedIn >= 130000)
                {
                    folderName = "LastTitan";
                }
                else if (addedIn >= 120000)
                {
                    folderName = "Midnight";
                }
                else if (addedIn >= 110000)
                {
                    folderName = "WarWithin";
                }
                else if (addedIn >= 100000)
                {
                    folderName = "Dragonflight";
                }
                else if (addedIn >= 90000)
                {
                    folderName = "Shadowlands";
                }
            }

            return folderName;
        }

        public static void Name()
        {
            var soundKitEntryDB = Namer.LoadDBC("SoundKitEntry");
            var soundKitFDIDMap = new Dictionary<uint, List<int>>();
            foreach (var soundKitEntry in soundKitEntryDB.Values)
            {
                var soundKitID = uint.Parse(soundKitEntry["SoundKitID"].ToString()!);
                var soundKitFileDataID = int.Parse(soundKitEntry["FileDataID"].ToString()!);
                if (!soundKitFDIDMap.ContainsKey(soundKitID))
                {
                    soundKitFDIDMap.Add(soundKitID, new List<int>() { soundKitFileDataID });
                }
                else
                {
                    soundKitFDIDMap[soundKitID].Add(soundKitFileDataID);
                }
            }

            var doneSoundKits = new List<uint>();

            var zoneMusicDB = Namer.LoadDBC("ZoneMusic");
            foreach (var zoneMusicRow in zoneMusicDB.Values)
            {
                var setName = zoneMusicRow["SetName"].ToString()!.Trim();
                for (int i = 0; i < 2; i++)
                {
                    var soundID = ((uint[])zoneMusicRow["Sounds"])[i];
                    if (soundKitFDIDMap.TryGetValue(soundID, out var soundFDIDs))
                    {
                        foreach (var soundFDID in soundFDIDs)
                        {
                            if (Namer.IDToNameLookup.ContainsKey(soundFDID) && !Namer.placeholderNames.Contains(soundFDID))
                                continue;

                            NewFileManager.AddNewFile(soundFDID, "Sound/Music/" + GetFolderName(soundFDID) + "/" + setName + "_" + soundFDID + ".mp3", Namer.placeholderNames.Contains(soundFDID));
                        }

                        doneSoundKits.Add(soundID);
                    }
                }
            }

            var zoneIntroMusicDB = Namer.LoadDBC("ZoneIntroMusicTable");
            foreach (var zoneIntroMusicRow in zoneIntroMusicDB.Values)
            {
                var setName = zoneIntroMusicRow["Name"].ToString()!.Trim();
                var soundID = (uint)zoneIntroMusicRow["SoundID"];
                if (soundKitFDIDMap.TryGetValue(soundID, out var soundFDIDs))
                {
                    foreach (var soundFDID in soundFDIDs)
                    {
                        if (Namer.IDToNameLookup.ContainsKey(soundFDID) && !Namer.placeholderNames.Contains(soundFDID))
                            continue;

                        NewFileManager.AddNewFile(soundFDID, "Sound/Music/" + GetFolderName(soundFDID) + "/" + setName + "_" + soundFDID + ".mp3", Namer.placeholderNames.Contains(soundFDID));
                    }

                    doneSoundKits.Add(soundID);
                }
            }

            //var soundKitDB = Namer.LoadDBC("SoundKit");
            //foreach (var soundKitEntry in soundKitDB.Values)
            //{
            //    var soundKitID = (int)soundKitEntry["ID"];
            //    if (doneSoundKits.Contains((uint)soundKitID))
            //        continue;

            //    if (soundKitEntry["SoundType"].ToString() == "28")
            //    {
            //        if (soundKitFDIDMap.TryGetValue((uint)soundKitID, out var soundFDIDs))
            //        {
            //            foreach (var soundFDID in soundFDIDs)
            //            {
            //                if (Namer.IDToNameLookup.ContainsKey(soundFDID) && !Namer.placeholderNames.Contains(soundFDID))
            //                    continue;

            //                NewFileManager.AddNewFile(soundFDID, "sound/music/" + GetFolderName(soundFDID) + "/" + "unk_" + soundKitID + "_" + soundFDID + ".mp3", Namer.placeholderNames.Contains(soundFDID));
            //            }
            //        }
            //    }
            //}
        }
    }
}
