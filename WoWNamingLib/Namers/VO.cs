﻿using MoonSharp.Interpreter;
using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class VO
    {
        private static Dictionary<(uint ID, string name), List<string>> creatureVO = new();
        private static Dictionary<uint, string> creatureNames = new();

        public static void Name(Dictionary<uint, string> creatureNames)
        {
            creatureVO.Clear();
            VO.creatureNames = creatureNames;

            // TODO: Parse scenescripts

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

            var textToSoundKitID = new Dictionary<string, List<uint>>();

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

            var soundKitFDIDMap = new Dictionary<uint, List<int>>();

            try
            {
                var soundKitDB = Namer.LoadDBC("SoundKitEntry");
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
            }
            catch (Exception e)
            {
                Console.WriteLine("Error parsing SoundKit: " + e.Message);
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
                            if (soundKitFDIDMap.TryGetValue(soundKit, out var fileDataIDs))
                            {
                                foreach (var fileDataID in fileDataIDs)
                                {
                                    NameVO(creatureName, fileDataID);
                                }
                            }
                            else
                            {
                                //  Console.WriteLine("\t\t\tNo file data ID found for sound kit " + soundKit);
                            }
                        }
                    }
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
                var creatureID = uint.Parse(key.String);

                if (!creatureNames.TryGetValue(creatureID, out string creatureName))
                {
                    Console.WriteLine("No NPC name found for creature ID " + creatureID + ", skipping!");
                    continue;
                }

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
                        creatureVO[(creatureID, creatureName)].Add(message.String);
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
                        creatureVO[(creatureID, creatureName)].Add(message.String);
                    }
                }
            }
        }

        private static void NameVO(string creatureName, int fileDataID)
        {
            if (Namer.IDToNameLookup.ContainsKey(fileDataID))
                return;

            // var splitBuild = Program.build.Split('.');
            // voVersion = uint.Parse(splitBuild[0]) * 100 + uint.Parse(splitBuild[1]) * 10 + uint.Parse(splitBuild[2]);
            var cleanName = cleanCreatureName(creatureName);

            uint voVersion = makeVOVersion(fileDataID);

            var newFilename = "Sound/Creature/" + cleanName + "/VO_" + voVersion + "_" + cleanName + "_" + fileDataID + ".ogg";

            NewFileManager.AddNewFile(fileDataID, newFilename);
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

            return creatureName.Replace(" ", "_");
        }
        private static uint makeVOVersion(int fileDataID)
        {
            uint voVersion = 9999;

            if (fileDataID > 5313885)
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
