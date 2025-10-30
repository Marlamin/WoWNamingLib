using MoonSharp.Interpreter;
using WoWNamingLib.Services;
using WoWNamingLib.Utils;

namespace WoWNamingLib.Namers
{
    class VO
    {
        private static Dictionary<(uint ID, string name), List<string>> creatureVO = new();
        private static Dictionary<uint, string> creatureNames = new();
        private static Dictionary<uint, string> creaturesToFDID = new();

        public static void Name(Dictionary<uint, string> creatureNames, Dictionary<string, List<uint>> textToSoundKitID, Dictionary<uint, string> creaturesToFDID, Dictionary<uint, (uint, uint)> BCTextToSKitIDs, bool overrideVO = true)
        {
            creatureVO.Clear();
            VO.creatureNames = creatureNames;
            VO.creaturesToFDID = creaturesToFDID;

            var sceneScriptDB = Namer.LoadDBC("SceneScript");
            var sceneScriptTextDB = Namer.LoadDBC("SceneScriptText");
            SceneScriptParser.DebugOutput = false;
            foreach (var sceneScriptTextRow in sceneScriptTextDB.Values)
            {
                // Skip non-VO scenescripts
                if (!sceneScriptTextRow["Name"].ToString()!.Contains("_VO") && !sceneScriptTextRow["Name"].ToString()!.Contains("_BroadcastText"))
                    continue;

                try
                {
                    var sceneScriptRow = sceneScriptDB[sceneScriptTextRow.ID];

                    // Skip partial scenescripts (these are compiled in with the main one)
                    if (int.Parse(sceneScriptRow["FirstSceneScriptID"].ToString()) != 0)
                        continue;

                    var parsedScript = SceneScriptParser.CompileScript(sceneScriptTextRow.ID);
                    var timeline = SceneScriptParser.ParseTimelineScript(parsedScript);

                    foreach (var script in timeline)
                    {
                        foreach (var actor in script.Value.actors)
                        {
                            if (actor.Value.properties.Appearance == null || actor.Value.properties.Appearance.Value.events == null)
                            {
                                Console.WriteLine("TODO: BroadcastText scene has no appearance info, likely targets actors, skipping..");
                                continue;
                            }

                            var creatureID = actor.Value.properties.Appearance.Value.events.First().Value.creatureID.ID;
                            if (creatureID == 0)
                            {
                                Console.WriteLine("TODO: Got creature ID 0, maybe check model ID instead, skipping for now");
                                continue;
                            }

                            if (actor.Value.properties.BroadcastText != null)
                            {
                                if (!creatureNames.TryGetValue((uint)creatureID, out var creatureName))
                                {
                                    Console.WriteLine("Unknown creature name for SceneScript creature ID " + creatureID);
                                    continue;
                                }

                                foreach (var broadcastTextEvent in actor.Value.properties.BroadcastText.Value.events)
                                {
                                    Console.WriteLine("bctext ID " + broadcastTextEvent.Value.broadcastTextID.ID + " for creature " + creatureID + " (" + creatureName + ")");

                                    if (!BCTextToSKitIDs.TryGetValue((uint)broadcastTextEvent.Value.broadcastTextID.ID, out var soundKitIDs))
                                    {
                                        Console.WriteLine("Could not find SoundKitIDs for BroadcastTextID " + broadcastTextEvent.Value.broadcastTextID.ID + ", skipping..");
                                        continue;
                                    }

                                    if (soundKitIDs.Item1 != 0)
                                    {
                                        foreach (var fileDataID in SoundKitHelper.GetFDIDsByKitID(soundKitIDs.Item1))
                                        {
                                            NameVO(creatureName, fileDataID, true, overrideVO);
                                        }
                                    }

                                    if (soundKitIDs.Item2 != 0)
                                    {
                                        foreach (var fileDataID in SoundKitHelper.GetFDIDsByKitID(soundKitIDs.Item2))
                                        {
                                            NameVO(creatureName, fileDataID, true, overrideVO);
                                        }
                                    }
                                }
                            }

                            //    if (actor.Value.properties.SoundKit == null)
                            //        continue;

                            //    foreach (var soundKitEvent in actor.Value.properties.SoundKit.Value.events)
                            //    {
                            //        if (doneSoundKits.Contains((uint)soundKitEvent.soundKitID))
                            //            continue;

                            //        var soundKitEntry = soundKitDB[(int)soundKitEvent.soundKitID];
                            //        if ((int)soundKitEntry["SoundType"] != 28)
                            //        {
                            //            Console.WriteLine("SoundKitID " + (int)soundKitEvent.soundKitID + " is used as music in scenescript " + sceneScriptTextRow["Name"].ToString() + " but isn't tagged as music in SoundKit.db2, skipping..");
                            //            continue;
                            //        }

                            //        foreach (var soundFDID in SoundKitHelper.GetFDIDsByKitID((uint)soundKitEvent.soundKitID))
                            //        {
                            //            if (Namer.IDToNameLookup.ContainsKey(soundFDID) && !Namer.placeholderNames.Contains(soundFDID))
                            //                continue;

                            //            NewFileManager.AddNewFile(soundFDID, "Sound/Music/" + GetFolderName(soundFDID) + "/SceneScript_unknown_" + sceneScriptTextRow.ID + "_" + soundFDID + ".mp3", Namer.placeholderNames.Contains(soundFDID));
                            //        }

                            //        doneSoundKits.Add((uint)soundKitEvent.soundKitID);
                            //    }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error parsing SceneScript " + sceneScriptTextRow["Name"].ToString() + " (ID " + sceneScriptTextRow.ID + "): " + e.Message);
                }
            }

            SceneScriptParser.DebugOutput = true;

            try
            {
                foreach (var datamineLua in Directory.GetFiles(Path.Combine(Namer.wowDir, "_retail_", "WTF\\Account"), "Datamine_Data.lua", SearchOption.AllDirectories))
                {
                    ParseDatamineLua(datamineLua);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error parsing VO Datamine LUA: " + e.Message);
            }

            try
            {
                foreach (var datamineLua in Directory.GetFiles(Namer.cacheDir, "Datamine_Data*", SearchOption.AllDirectories))
                {
                    ParseDatamineLua(datamineLua);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error parsing VO Datamine LUA: " + e.Message);
            }

            try
            {
                foreach (var wowdbLua in Directory.GetFiles(Path.Combine(Namer.wowDir, "_retail_", "WTF\\Account"), "WoWDBProfiler.lua", SearchOption.AllDirectories))
                {
                    ParseWoWDBLua(wowdbLua);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error parsing VO WoWDB Lua: " + e.Message);
            }

            try
            {
                foreach (var wowdbLua in Directory.GetFiles(Path.Combine(Namer.wowDir, "_retail_", "WTF\\Account"), "+Wowhead_Looter.lua", SearchOption.AllDirectories))
                {
                    ParseWowheadLua(wowdbLua);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error parsing VO Wowhead LUA: " + e.Message);
            }

            if (textToSoundKitID.Count == 0)
            {
                try
                {
                    var broadcastTextDB = Namer.LoadDBC("BroadcastText");
                    foreach (var broadcastText in broadcastTextDB.Values)
                    {
                        var soundKits = (uint[])broadcastText["SoundKitID"];

                        if (!string.IsNullOrEmpty(broadcastText["Text_lang"].ToString()))
                        {
                            if (soundKits[0] != 0)
                            {
                                if (!textToSoundKitID.ContainsKey(broadcastText["Text_lang"].ToString()))
                                    textToSoundKitID.Add(broadcastText["Text_lang"].ToString(), new List<uint>());

                                textToSoundKitID[broadcastText["Text_lang"].ToString()].Add(soundKits[0]);
                            }
                        }

                        if (!string.IsNullOrEmpty(broadcastText["Text1_lang"].ToString()))
                        {
                            if (soundKits[1] != 0)
                            {
                                if (!textToSoundKitID.ContainsKey(broadcastText["Text1_lang"].ToString()))
                                    textToSoundKitID.Add(broadcastText["Text1_lang"].ToString(), new List<uint>());

                                textToSoundKitID[broadcastText["Text1_lang"].ToString()].Add(soundKits[1]);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error parsing BroadcastText: " + e.Message);
                }
            }

            foreach (var creatureVOEntry in creatureVO)
            {
                var creatureID = creatureVOEntry.Key.ID;
                var creatureName = creatureVOEntry.Key.name;
                var creatureVOList = creatureVOEntry.Value;

                //Console.WriteLine(creatureID + " " + creatureName);
                foreach (var vo in creatureVOList)
                {
                    //Console.WriteLine("\t" + vo);
                    if (textToSoundKitID.TryGetValue(vo, out var soundKits))
                    {
                        foreach (var soundKit in soundKits)
                        {
                            // Console.WriteLine("\t\t" + soundKit);
                            foreach (var fileDataID in SoundKitHelper.GetFDIDsByKitID(soundKit))
                            {
                                NameVO(creatureName, fileDataID, true, overrideVO);
                            }
                        }
                    }
                }
            }

            foreach (var creatureToFDID in creaturesToFDID)
            {
                NameVO(creatureToFDID.Value, (int)creatureToFDID.Key, false, overrideVO);
            }
        }

        public static string NameSingle(int fileDataID, string creatureName)
        {
            return NameVO(creatureName, fileDataID, false);
        }

        private static void ParseAdventureArchivesLua(string filename)
        {
            // AdventureArchivesTalkingHeadDB_9 -- has "VO" for soundkit, "nm" for creature name
            // AdventureArchivesMessageDB_9 -- NPC text in chat, manual like before :(
        }

        private static void ParseDatamineLua(string filename)
        {
            var script = new Script();

            var prepend = File.ReadAllText(filename);
            var val = script.DoString(prepend + "\n return DatamineData");
            var npcTable = val.Table.Get("Creature").ToObject<Table>();

            if (npcTable == null)
                return;

            foreach (var key in npcTable.Keys)
            {
                var creatureID = (uint)key.Number;

                string creatureName = "";

                var creatureNameTable = npcTable.Get(key).ToObject<Table>().Get("Name").ToObject<Table>();
                if (creatureNameTable != null)
                    creatureName = creatureNameTable.Get("enUS").String;

                if (string.IsNullOrEmpty(creatureName) && !creatureNames.TryGetValue(creatureID, out creatureName))
                    continue;

                var broadcastText = npcTable.Get(key).ToObject<Table>().Get("BroadcastText").ToObject<Table>();
                if (broadcastText == null)
                    continue;

                var quotes = broadcastText.Get("enUS").ToObject<Table>();

                if (quotes == null)
                    continue;

                if (!creatureVO.ContainsKey((creatureID, creatureName)))
                    creatureVO.Add((creatureID, creatureName), new List<string>());

                foreach (var quoteType in quotes.Keys)
                {
                    creatureVO[(creatureID, creatureName)].Add(quoteType.String.Replace("$PLAYER_NAME", "$n"));
                }
            }

            var bcTable = val.Table.Get("BroadcastTextCache").ToObject<Table>();
            if (bcTable == null)
                return;

            foreach (var key in bcTable.Keys)
            {
                var creatureName = key.String;
                var bcEntries = bcTable.Get(key).ToObject<Table>();
                if (bcEntries == null) continue;
                foreach (var bcEntry in bcEntries.Keys)
                {
                    var text = bcEntry.String;

                    // We don't have a creature ID here
                    if (!creatureVO.ContainsKey((0, creatureName)))
                        creatureVO.Add((0, creatureName), new List<string>());

                    creatureVO[(0, creatureName)].Add(text.Replace("$PLAYER_NAME", "$n"));
                }
            }

        }
        private static void ParseWoWDBLua(string filename)
        {
            var script = new Script();

            var prepend = File.ReadAllText(filename);
            var val = script.DoString(prepend + "\n return WoWDBProfilerData");
            var globalTable = val.Table.Get("global").ToObject<Table>();
            var npcTable = globalTable.Get("npcs").ToObject<Table>();

            if (npcTable == null)
                return;

            foreach (var key in npcTable.Keys)
            {
                var creatureID = (uint)key.Number;

                if (!creatureNames.TryGetValue(creatureID, out string creatureName))
                    continue;

                var quotes = npcTable.Get(key).ToObject<Table>().Get("quotes").ToObject<Table>();

                if (quotes == null)
                    continue;

                if (!creatureVO.ContainsKey((creatureID, creatureName)))
                    creatureVO.Add((creatureID, creatureName), new List<string>());

                foreach (var quoteType in quotes.Keys)
                {
                    var messages = quotes.Get(quoteType).ToObject<Table>();
                    foreach (var message in messages.Keys)
                    {
                        creatureVO[(creatureID, creatureName)].Add(message.String.Replace("$PLAYER_NAME", "$n"));
                    }
                }
            }
        }

        private static void ParseWowheadLua(string filename)
        {
            var script = new Script();

            var prepend = File.ReadAllText(filename);
            var val = script.DoString(prepend + "\n return wlUnit");

            if (val.Table == null)
                return;

            foreach (var key in val.Table.Keys)
            {
                var creatureID = uint.Parse(key.String);

                if (!creatureNames.TryGetValue(creatureID, out string creatureName))
                {
                    Console.WriteLine("No NPC name found for creature ID " + creatureID + ", skipping!");
                    continue;
                }

                var quotes = val.Table.Get(key).ToObject<Table>().Get("quote").ToObject<Table>();

                if (quotes == null)
                    continue;

                if (!creatureVO.ContainsKey((creatureID, creatureName)))
                    creatureVO.Add((creatureID, creatureName), new List<string>());

                foreach (var quoteType in quotes.Keys)
                {
                    var messages = quotes.Get(quoteType).ToObject<Table>();
                    foreach (var message in messages.Keys)
                    {
                        creatureVO[(creatureID, creatureName)].Add(message.String.Replace("$PLAYER_NAME", "$n"));
                    }
                }
            }
        }

        private static string NameVO(string creatureName, int fileDataID, bool addonName = true, bool overrideVO = true)
        {
            //if (!Namer.placeholderNames.Contains(fileDataID))
            //    return;

            // var splitBuild = Program.build.Split('.');
            // voVersion = uint.Parse(splitBuild[0]) * 100 + uint.Parse(splitBuild[1]) * 10 + uint.Parse(splitBuild[2]);
            var forceUpdate = false;

            if (addonName && creaturesToFDID.TryGetValue((uint)fileDataID, out var creatureFDIDName))
            {
                if (creatureFDIDName != creatureName)
                {
                    Console.WriteLine("Skipping " + fileDataID + ", not naming it " + creatureName + " as it's already named with " + creatureFDIDName);
                }
                return "";
            }

            var cleanName = cleanCreatureName(creatureName);

            uint voVersion = makeVOVersion(fileDataID);

            var newFilename = "Sound/Creature/" + cleanName + "/VO_" + voVersion + "_" + cleanName + "_" + fileDataID + ".ogg";

            if (Namer.IDToNameLookup.TryGetValue(fileDataID, out var existingName))
            {
                if (!existingName.ToLower().Contains("/vo_"))
                {
                    //Console.WriteLine("File " + fileDataID + " is not currently tagged as VO, leaving it alone");
                    //Console.WriteLine("\t existing name: " + existingName);
                    //Console.WriteLine("\t incoming creature name: " + creatureName);
                    return "";
                }

                // skip files that end in _xx.ogg or _xxx.ogg where x is a digit, thes were already named
                var splitExistingName = existingName.Split('_');
                if (splitExistingName.Length > 2)
                {
                    var lastPart = splitExistingName[^1].Replace(".ogg", "");
                    if (lastPart == "f" || lastPart == "m" || lastPart == "rtc" || lastPart == "darkshore" || lastPart == "corruptedgamon") // don't ask
                        return "";

                    if ((lastPart.Length == 2 || lastPart.Length == 3) && lastPart.All(char.IsDigit))
                        return "";
                }

                if (existingName.Equals(newFilename, StringComparison.CurrentCultureIgnoreCase) && existingName != newFilename)
                {
                    // Prefer new name with capitals
                    forceUpdate = true;
                }
            }

            Namer.SetCreatureNameForFDID(fileDataID, creatureName);

            NewFileManager.AddNewFile(fileDataID, newFilename, overrideVO, forceUpdate);

            return newFilename;
        }

        private static string cleanCreatureName(string creatureName)
        {
            switch (creatureName)
            {
                case "Alexstrasza the Life-Binder":
                    creatureName = "Alexstrasza";
                    break;
                case "Taelia Fordragon":
                    creatureName = "Taelia";
                    break;
                case "Rupert, the Gentleman Elemental":
                    creatureName = "Rupert";
                    break;
            }

            return creatureName.Replace(",", "").Replace(" ", "_").Replace("'", "").Replace("\"", "");
        }
        private static uint makeVOVersion(int fileDataID)
        {
            var upstreamVersion = Namer.GetAddedInPatch(fileDataID);
            if (upstreamVersion != 0)
            {
                var major = upstreamVersion / 10000;
                var minor = ((upstreamVersion - (major * 10000)) / 100);
                var patch = upstreamVersion - (major * 10000) - (minor * 100);

                var calculatedVOVersion = major * 100 + minor * 10 + patch;

                // 8.2 is a very early case where we need to not override literally all the filenames
                if (calculatedVOVersion == 820)
                    return 82;

                return calculatedVOVersion;
            }

            uint voVersion = 9999;

            if (fileDataID > 5524626)
            {
                voVersion = 1100;
            }
            else if (fileDataID > 5313885)
            {
                voVersion = 1025;
            }
            else if (fileDataID > 5279429)
            {
                voVersion = 1020;
            }
            else if (fileDataID > 5222554)
            {
                voVersion = 1017;
            }
            else if (fileDataID > 5013808)
            {
                voVersion = 1015;
            }
            else if (fileDataID > 4901580)
            {
                voVersion = 1010;
            }
            else if (fileDataID > 4561244)
            {
                voVersion = 1000;
            }
            else if (fileDataID > 4423411)
            {
                voVersion = 925;
            }
            else if (fileDataID > 4208596)
            {
                voVersion = 920;
            }
            else if (fileDataID > 4035327)
            {
                voVersion = 910;
            }
            else if (fileDataID > 3380485)
            {
                voVersion = 901;
            }

            return voVersion;
        }
    }
}
