using WoWNamingLib.Services;
using WoWNamingLib.Utils;

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

            var soundKitDB = Namer.LoadDBC("SoundKit");
            var sceneScriptTextDB = Namer.LoadDBC("SceneScriptText");
            SceneScriptParser.DebugOutput = false;
            foreach (var sceneScriptTextRow in sceneScriptTextDB.Values)
            {
                // Note: This will work for now because music scenescripts usually arent long enough to have multiple parts

                // Skip non-music scenescripts
                if (!sceneScriptTextRow["Name"].ToString()!.Contains("Music"))
                    continue;

                var text = sceneScriptTextRow["Script"].ToString()!;

                if (!text.Contains("SceneTimeline"))
                    continue;

                try
                {
                    var timeline = SceneScriptParser.ParseTimelineScript(text);

                    foreach (var script in timeline)
                    {
                        foreach (var actor in script.Value.actors)
                        {
                            if (actor.Value.properties.SoundKit == null)
                                continue;

                            foreach (var soundKitEvent in actor.Value.properties.SoundKit.Value.events)
                            {
                                if (doneSoundKits.Contains((uint)soundKitEvent.soundKitID))
                                    continue;

                                var soundKitEntry = soundKitDB[(int)soundKitEvent.soundKitID];
                                if ((int)soundKitEntry["SoundType"] != 28)
                                {
                                    Console.WriteLine("SoundKitID " + (int)soundKitEvent.soundKitID + " is used as music in scenescript " + sceneScriptTextRow["Name"].ToString() + " but isn't tagged as music in SoundKit.db2, skipping..");
                                    continue;
                                }

                                foreach (var soundFDID in SoundKitHelper.GetFDIDsByKitID((uint)soundKitEvent.soundKitID))
                                {
                                    if (Namer.IDToNameLookup.ContainsKey(soundFDID) && !Namer.placeholderNames.Contains(soundFDID))
                                        continue;

                                    NewFileManager.AddNewFile(soundFDID, "Sound/Music/" + GetFolderName(soundFDID) + "/SceneScript_unknown_" + sceneScriptTextRow.ID  + "_" + soundFDID + ".mp3", Namer.placeholderNames.Contains(soundFDID));
                                }

                                doneSoundKits.Add((uint)soundKitEvent.soundKitID);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error parsing SceneScript " + sceneScriptTextRow["Name"].ToString() + " (ID " + sceneScriptTextRow.ID + "): " + e.Message);
                }
            }

            SceneScriptParser.DebugOutput = true;
        }
    }
}
