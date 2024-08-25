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
            var doneSoundKits = new List<uint>();

            var zoneMusicDB = Namer.LoadDBC("ZoneMusic");
            foreach (var zoneMusicRow in zoneMusicDB.Values)
            {
                var setName = zoneMusicRow["SetName"].ToString()!.Trim();
                for (int i = 0; i < 2; i++)
                {
                    var soundID = ((uint[])zoneMusicRow["Sounds"])[i];
                    foreach (var soundFDID in SoundKitHelper.GetFDIDsByKitID(soundID))
                    {
                        if (Namer.IDToNameLookup.ContainsKey(soundFDID) && !Namer.placeholderNames.Contains(soundFDID))
                            continue;

                        NewFileManager.AddNewFile(soundFDID, "Sound/Music/" + GetFolderName(soundFDID) + "/" + setName + "_" + soundFDID + ".mp3", Namer.placeholderNames.Contains(soundFDID));
                    }

                    doneSoundKits.Add(soundID);
                }
            }

            var zoneIntroMusicDB = Namer.LoadDBC("ZoneIntroMusicTable");
            foreach (var zoneIntroMusicRow in zoneIntroMusicDB.Values)
            {
                var setName = zoneIntroMusicRow["Name"].ToString()!.Trim();
                var soundID = (uint)zoneIntroMusicRow["SoundID"];
                foreach (var soundFDID in SoundKitHelper.GetFDIDsByKitID(soundID))
                {
                    if (Namer.IDToNameLookup.ContainsKey(soundFDID) && !Namer.placeholderNames.Contains(soundFDID))
                        continue;

                    NewFileManager.AddNewFile(soundFDID, "Sound/Music/" + GetFolderName(soundFDID) + "/" + setName + "_" + soundFDID + ".mp3", Namer.placeholderNames.Contains(soundFDID));
                }

                doneSoundKits.Add(soundID);
            }
        }
    }
}
