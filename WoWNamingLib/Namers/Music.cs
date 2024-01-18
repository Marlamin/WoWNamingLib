using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    public class Music
    {
        public static void Name()
        {
            var soundKitDB = Namer.LoadDBC("SoundKitEntry");
            var soundKitFDIDMap = new Dictionary<uint, List<int>>();
            foreach (var soundKitEntry in soundKitDB.Values)
            {
                var soundKitID = uint.Parse(soundKitEntry["SoundKitID"].ToString());
                var soundKitFileDataID = int.Parse(soundKitEntry["FileDataID"].ToString());
                if (!soundKitFDIDMap.ContainsKey(soundKitID))
                {
                    soundKitFDIDMap.Add(soundKitID, new List<int>() { soundKitFileDataID });
                }
                else
                {
                    soundKitFDIDMap[soundKitID].Add(soundKitFileDataID);
                }
            }

            var zoneMusicDB = Namer.LoadDBC("ZoneMusic");
            foreach (var zoneMusicRow in zoneMusicDB.Values)
            {
                var setName = zoneMusicRow["SetName"].ToString();
                for (int i = 0; i < 2; i++)
                {
                    var soundID = ((uint[])zoneMusicRow["Sounds"])[i];
                    if (soundKitFDIDMap.TryGetValue(soundID, out var soundFDIDs))
                    {
                        foreach (var soundFDID in soundFDIDs)
                        {
                            if (Namer.IDToNameLookup.ContainsKey(soundFDID))
                                continue;

                            NewFileManager.AddNewFile(soundFDID, "sound/music/unknown/" + setName + "_" + soundFDID + ".mp3");
                        }
                    }
                }
            }

            var zoneIntroMusicDB = Namer.LoadDBC("ZoneIntroMusicTable");
            foreach (var zoneIntroMusicRow in zoneIntroMusicDB.Values)
            {
                var setName = zoneIntroMusicRow["Name"].ToString();
                var soundID = (uint)zoneIntroMusicRow["SoundID"];
                if (soundKitFDIDMap.TryGetValue(soundID, out var soundFDIDs))
                {
                    foreach (var soundFDID in soundFDIDs)
                    {
                        if (Namer.IDToNameLookup.ContainsKey(soundFDID))
                            continue;

                        NewFileManager.AddNewFile(soundFDID, "sound/music/unknown/" + setName + "_" + soundFDID + ".mp3");
                    }
                }
            }
        }
    }
}
