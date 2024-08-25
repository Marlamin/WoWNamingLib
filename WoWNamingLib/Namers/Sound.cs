﻿using DBCD;
using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class Sound
    {
        private static Dictionary<uint, DBCD.DBCDRow> CSDMap = new();
        private static IDBCDStorage CSDDB;
        private static IDBCDStorage CSFDB;

        private static void NameCSDSounds(uint csdID, uint modelFDID, uint displayID = 0)
        {
            if (csdID == 0)
                return;

            if (!Namer.IDToNameLookup.TryGetValue((int)modelFDID, out var modelFilename))
                return;

            if (modelFilename.ToLower().StartsWith("character"))
            {
                if (displayID != 0)
                {
                    var cleanedCreatureName = Namer.GetCreatureNameByDisplayID((int)displayID).Replace(" ", "").Replace("'", "").Replace("-", "").Replace("\"", "");
                    if (cleanedCreatureName == "")
                    {
                        return;
                    }
                    else
                    {
                        modelFilename = cleanedCreatureName;
                    }
                }
                else
                {
                    Console.WriteLine("Model " + modelFilename + " is a character model and CDI ID was not given, skipping..");
                    return;
                }
            }

            if (!CSDMap.TryGetValue(csdID, out var csdRow))
            {
                Console.WriteLine("Referenced CSD " + csdID + " does not exist");
                return;
            }

            foreach (var col in CSDDB.AvailableColumns)
            {
                // Skip non-SoundKitIDs
                if (col == "CreatureSoundDataIDPet" || col == "FidgetDelaySecondsMin" || col == "FidgetDelaySecondsMax" || col == "CreatureImpactType")
                    continue;

                var colType = csdRow[col].GetType();
                var targetSDIDs = new List<uint>();

                if (colType.IsArray && colType == typeof(uint[]))
                {
                    var csdArr = (uint[])csdRow[col];
                    for (var i = 0; i < csdArr.Length; i++)
                    {
                        if (csdArr[i] == 0)
                            continue;

                        targetSDIDs.Add(csdArr[i]);
                    }
                }
                else if (colType == typeof(int) || colType == typeof(uint))
                {
                    var soundKitID = uint.Parse(csdRow[col].ToString());
                    if (soundKitID == 0)
                        continue;

                    targetSDIDs.Add(soundKitID);
                }
                else
                {
                    Console.WriteLine("Unhandled CSD col type: " + colType.ToString());
                }

                foreach (var targetSDID in targetSDIDs)
                {
                    foreach (var soundFileDataID in SoundKitHelper.GetFDIDsByKitID(targetSDID))
                    {
                        if (!Namer.IDToNameLookup.ContainsKey((int)soundFileDataID))
                        {
                            var soundType = CSDColToSoundType(col);
                            NewFileManager.AddNewFile(soundFileDataID, "Sound/Creature/" + Path.GetFileNameWithoutExtension(modelFilename) + "/" + Path.GetFileNameWithoutExtension(modelFilename) + "_" + soundType + "_" + soundFileDataID + ".ogg");
                        }
                    }
                }
            }

            foreach (var creatureSoundFidget in CSFDB.Values)
            {
                if (csdID != uint.Parse(creatureSoundFidget["CreatureSoundDataID"].ToString()))
                    continue;

                foreach (var soundFileDataID in SoundKitHelper.GetFDIDsByKitID(uint.Parse(creatureSoundFidget["Fidget"].ToString())))
                {
                    if (!Namer.IDToNameLookup.ContainsKey((int)soundFileDataID))
                    {
                        NewFileManager.AddNewFile(soundFileDataID, "Sound/Creature/" + Path.GetFileNameWithoutExtension(modelFilename) + "/" + Path.GetFileNameWithoutExtension(modelFilename) + "_fidget" + creatureSoundFidget["Index"].ToString() + "_" + soundFileDataID + ".ogg");
                    }
                }
            }
        }
        public static void Name(bool partialSuggestions = false)
        {
            #region UI sound naming
            try
            {
                var luaFile = CASCManager.GetFileByID(5613600).Result; // Interface/AddOns/Blizzard_SharedXML/Mainline/SoundKitConstants.lua
                var luaText = new StreamReader(luaFile).ReadToEnd();
                foreach (var line in luaText.Split('\n'))
                {
                    var cleaned = line.Trim();
                    if (!cleaned.EndsWith(','))
                        continue;

                    var splitLine = cleaned.Split('=');

                    var uiSoundName = splitLine[0].Trim();
                    var uiSoundKitID = splitLine[1].Trim().TrimEnd(',').Trim();

                    var uiCounter = 0;
                    foreach (var soundFileDataID in SoundKitHelper.GetFDIDsByKitID(uint.Parse(uiSoundKitID)))
                    {
                        if (!Namer.IDToNameLookup.ContainsKey((int)soundFileDataID))
                        {
                            NewFileManager.AddNewFile(soundFileDataID, "Sound/UI/" + uiSoundName + "_" + uiCounter++ + "_" + soundFileDataID + ".ogg");
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            #endregion

            #region CreatureSounds
            if (CSDMap.Count == 0)
            {
                CSDDB = Namer.LoadDBC("CreatureSoundData");
                CSFDB = Namer.LoadDBC("CreatureSoundFidget");

                foreach (var csdEntry in CSDDB.Values)
                {
                    CSDMap.Add(uint.Parse(csdEntry["ID"].ToString()), csdEntry);
                }
            }

            var cmdDB = Namer.LoadDBC("CreatureModelData");
            var cmdIDToFDIDMap = new Dictionary<uint, uint>();

            foreach (var cmdEntry in cmdDB.Values)
            {
                var mFDID = uint.Parse(cmdEntry["FileDataID"].ToString());
                var csdID = uint.Parse(cmdEntry["SoundID"].ToString());

                cmdIDToFDIDMap.Add(uint.Parse(cmdEntry["ID"].ToString()), mFDID);
            }

            var creatureDisplayInfoDB = Namer.LoadDBC("CreatureDisplayInfo");
            var cdiMap = new Dictionary<uint, DBCD.DBCDRow>();
            var cdiToFDIDMap = new Dictionary<uint, uint>();
            var cmdToCDIMap = new Dictionary<uint, uint>(); // only care about first occurence here really
            foreach (var cdiRow in creatureDisplayInfoDB.Values)
            {
                var displayID = uint.Parse(cdiRow["ID"].ToString());
                var modelID = uint.Parse(cdiRow["ModelID"].ToString());

                cdiMap.Add(displayID, cdiRow);
                if (cmdIDToFDIDMap.TryGetValue(modelID, out var fdid))
                {
                    cdiToFDIDMap.Add(displayID, fdid);
                    NameCSDSounds(uint.Parse(cdiRow["SoundID"].ToString()), fdid, displayID);
                }

                cmdToCDIMap.TryAdd(modelID, displayID);
            }

            foreach (var cmdEntry in cmdDB.Values)
            {
                var cmdID = uint.Parse(cmdEntry["ID"].ToString());
                var mFDID = uint.Parse(cmdEntry["FileDataID"].ToString());
                var csdID = uint.Parse(cmdEntry["SoundID"].ToString());

                if (mFDID == 0 || csdID == 0)
                    continue;

                if (!Namer.IDToNameLookup.TryGetValue((int)mFDID, out var modelFilename))
                    continue;

                NameCSDSounds(csdID, mFDID, cmdToCDIMap.TryGetValue(cmdID, out var displayID) ? displayID : 0);
            }

            #endregion

            var mountDB = Namer.LoadDBC("Mount");
            var spellXSpellVisualDB = Namer.LoadDBC("SpellXSpellVisual");
            var spellXSpellVisualMap = new Dictionary<uint, List<uint>>();
            foreach (var svsRow in spellXSpellVisualDB.Values)
            {
                var spellID = uint.Parse(svsRow["SpellID"].ToString());
                var spellVisualID = uint.Parse(svsRow["SpellVisualID"].ToString());

                if (!spellXSpellVisualMap.ContainsKey(spellID))
                {
                    spellXSpellVisualMap.Add(spellID, new List<uint>() { spellVisualID });
                }
                else
                {
                    spellXSpellVisualMap[spellID].Add(spellVisualID);
                }
            }

            var spellVisualEventDB = Namer.LoadDBC("SpellVisualEvent");
            var spellVisualEventsByVisualIDMap = new Dictionary<uint, List<DBCD.DBCDRow>>();

            foreach (var sveRow in spellVisualEventDB.Values)
            {
                var spellVisualID = uint.Parse(sveRow["SpellVisualID"].ToString());

                if (!spellVisualEventsByVisualIDMap.ContainsKey(spellVisualID))
                {
                    spellVisualEventsByVisualIDMap.Add(spellVisualID, new List<DBCD.DBCDRow>() { sveRow });
                }
                else
                {
                    spellVisualEventsByVisualIDMap[spellVisualID].Add(sveRow);
                }
            }

            var spellVisualKitEffectDB = Namer.LoadDBC("SpellVisualKitEffect");
            var spellVisualKitSoundEffectMap = new Dictionary<uint, List<uint>>();

            foreach (var svkeRow in spellVisualKitEffectDB.Values)
            {
                var parentSpellVisualKitID = uint.Parse(svkeRow["ParentSpellVisualKitID"].ToString());
                var effectType = uint.Parse(svkeRow["EffectType"].ToString());
                var effectValue = uint.Parse(svkeRow["Effect"].ToString());

                if (effectType != 5)
                    continue;

                if (!spellVisualKitSoundEffectMap.ContainsKey(parentSpellVisualKitID))
                {
                    spellVisualKitSoundEffectMap.Add(parentSpellVisualKitID, new List<uint>() { effectValue });
                }
                else
                {
                    spellVisualKitSoundEffectMap[parentSpellVisualKitID].Add(effectValue);
                }
            }

            // Mounts
            var objectEffectPackageElemDB = Namer.LoadDBC("ObjectEffectPackageElem");
            var objectEffectPackageMap = new Dictionary<uint, List<(uint GroupID, uint StateType)>>();
            foreach (var oepeRow in objectEffectPackageElemDB.Values)
            {
                var objectEffectPackageID = uint.Parse(oepeRow["ObjectEffectPackageID"].ToString());
                var objectEffectGroupID = uint.Parse(oepeRow["ObjectEffectGroupID"].ToString());
                var stateType = uint.Parse(oepeRow["StateType"].ToString());

                if (!objectEffectPackageMap.ContainsKey(objectEffectPackageID))
                {
                    objectEffectPackageMap.Add(objectEffectPackageID, new List<(uint GroupID, uint StateType)>() { (objectEffectGroupID, stateType) });
                }
                else
                {
                    objectEffectPackageMap[objectEffectPackageID].Add((objectEffectGroupID, stateType));
                }
            }

            var objectEffectGroupMap = new Dictionary<uint, List<DBCD.DBCDRow>>();
            var objectEffectDB = Namer.LoadDBC("ObjectEffect");

            foreach (var oeRow in objectEffectDB.Values)
            {
                var objectEffectGroupID = uint.Parse(oeRow["ObjectEffectGroupID"].ToString());
                if (!objectEffectGroupMap.ContainsKey(objectEffectGroupID))
                {
                    objectEffectGroupMap.Add(objectEffectGroupID, new List<DBCD.DBCDRow>() { oeRow });
                }
                else
                {
                    objectEffectGroupMap[objectEffectGroupID].Add(oeRow);
                }
            }

            var mountXDisplay = Namer.LoadDBC("MountXDisplay");
            var mountToFDID = new Dictionary<uint, uint>();
            var mountXCDIMap = new Dictionary<uint, uint>();

            foreach (var mxdRow in mountXDisplay.Values)
            {
                mountXCDIMap.TryAdd(uint.Parse(mxdRow["MountID"].ToString()), uint.Parse(mxdRow["CreatureDisplayInfoID"].ToString()));

                var cdiID = uint.Parse(mxdRow["CreatureDisplayInfoID"].ToString());
                if (cdiToFDIDMap.TryGetValue(cdiID, out var mFDID))
                {
                    mountToFDID.TryAdd(uint.Parse(mxdRow["MountID"].ToString()), mFDID);
                }
            }

            foreach (var mountRow in mountDB.Values)
            {
                var mountID = uint.Parse(mountRow["ID"].ToString());

                // ObjectPackage logic
                if (mountXCDIMap.TryGetValue(mountID, out var cdiID))
                {
                    if (!cdiMap.TryGetValue(cdiID, out var cdiRow))
                        continue;

                    var objectEffectPackageID = uint.Parse(cdiRow["ObjectEffectPackageID"].ToString());
                    if (objectEffectPackageMap.TryGetValue(objectEffectPackageID, out var objectEffectPackages))
                    {
                        foreach (var objectEffectPackageRef in objectEffectPackages)
                        {
                            if (objectEffectGroupMap.TryGetValue(objectEffectPackageRef.GroupID, out var objectEffects))
                            {
                                foreach (var objectEffect in objectEffects)
                                {
                                    var effectType = uint.Parse(objectEffect["EffectRecType"].ToString());
                                    var effectRecID = uint.Parse(objectEffect["EffectRecID"].ToString());

                                    if (effectType != 1 || effectRecID == 0)
                                        continue;

                                    foreach (var soundFDID in SoundKitHelper.GetFDIDsByKitID(effectRecID))
                                    {
                                        if (Namer.IDToNameLookup.ContainsKey((int)soundFDID) && !Namer.placeholderNames.Contains((int)soundFDID))
                                            continue;

                                        if (mountToFDID.TryGetValue(mountID, out var mountFDID))
                                        {
                                            if (StateType.TryGetValue(objectEffectPackageRef.StateType, out var stateName))
                                            {
                                                if (Namer.IDToNameLookup.TryGetValue((int)mountFDID, out var mountFilename))
                                                {
                                                    var animName = stateName.Replace("Anim", "").Replace("Movement", "").Replace("Transport", "").Replace(" ", "").Replace("-", "");
                                                    NewFileManager.AddNewFile(soundFDID, "sound/creature/" + Path.GetFileNameWithoutExtension(mountFilename) + "/" + Path.GetFileNameWithoutExtension(mountFilename) + "_" + animName.ToLower() + "_" + soundFDID + ".ogg");
                                                }
                                                else
                                                {
                                                    Console.WriteLine(mountRow["Name_lang"].ToString() + " (" + mountFDID + ") is still unnamed");
                                                }
                                            }
                                            else
                                            {
                                                if (Namer.IDToNameLookup.TryGetValue((int)mountFDID, out var mountFilename))
                                                {
                                                    NewFileManager.AddNewFile(soundFDID, "sound/creature/" + Path.GetFileNameWithoutExtension(mountFilename) + "/" + Path.GetFileNameWithoutExtension(mountFilename) + "_unknown_" + soundFDID + ".ogg");
                                                }
                                                else
                                                {
                                                    Console.WriteLine(mountRow["Name_lang"].ToString() + " (" + mountFDID + ") is still unnamed");
                                                }

                                                if (!Namer.IDToNameLookup.ContainsKey((int)mountFDID))
                                                {
                                                    Console.WriteLine("!!!! " + mountRow["Name_lang"].ToString() + " has unnamed sound " + soundFDID + " for unknown state " + objectEffectPackageRef.StateType);

                                                }
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("!!!! " + mountRow["Name_lang"].ToString() + " has no attached FDID");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                var sourceSpellID = uint.Parse(mountRow["SourceSpellID"].ToString());
                if (!spellXSpellVisualMap.TryGetValue(sourceSpellID, out var spellVisuals))
                {
                    continue;
                }

                foreach (var spellVisualID in spellVisuals)
                {
                    if (!spellVisualEventsByVisualIDMap.TryGetValue(spellVisualID, out var spellVisualEvents))
                    {
                        continue;
                    }

                    foreach (var spellVisualEvent in spellVisualEvents)
                    {
                        if (!spellVisualKitSoundEffectMap.TryGetValue(uint.Parse(spellVisualEvent["SpellVisualKitID"].ToString()), out var spellVisualKitSoundEffects))
                        {
                            continue;
                        }

                        foreach (var spellVisualKitSoundEffect in spellVisualKitSoundEffects)
                        {
                            foreach (var soundKitFDID in SoundKitHelper.GetFDIDsByKitID(spellVisualKitSoundEffect))
                            {
                                if (!Namer.IDToNameLookup.ContainsKey((int)soundKitFDID) || Namer.placeholderNames.Contains((int)soundKitFDID))
                                {
                                    if (mountToFDID.TryGetValue(mountID, out var mountFDID))
                                    {

                                        var castTime = "precast";
                                        var soundLoop = "loop";
                                        var startEvent = uint.Parse(spellVisualEvent["StartEvent"].ToString());

                                        switch (startEvent)
                                        {
                                            case 1:
                                                castTime = "precaststart";
                                                break;
                                            case 2:
                                                castTime = "precastend";
                                                break;
                                            case 3:
                                                castTime = "cast";
                                                break;
                                            case 4:
                                                castTime = "travelstart";
                                                break;
                                            case 5:
                                                castTime = "travelend";
                                                break;
                                            case 6:
                                                castTime = "impact";
                                                break;
                                            case 7:
                                                castTime = "aurastart";
                                                break;
                                            case 8:
                                                castTime = "auraend";
                                                break;
                                            case 9:
                                                castTime = "areatriggerstart";
                                                break;
                                            case 10:
                                                castTime = "areatriggerend";
                                                break;
                                            case 11:
                                                castTime = "channelstart";
                                                break;
                                            case 12:
                                                castTime = "channelend";
                                                break;
                                            case 13:
                                                castTime = "oneshot";
                                                break;
                                            default:
                                                Console.WriteLine(" Start event " + startEvent + " needs supporting");
                                                break;
                                        }

                                        if (spellVisualEvent["EndEvent"].ToString() == "13")
                                            soundLoop = "oneshot";

                                        if (Namer.IDToNameLookup.TryGetValue((int)mountFDID, out var mountFilename))
                                        {
                                            NewFileManager.AddNewFile(soundKitFDID, "sound/creature/" + Path.GetFileNameWithoutExtension(mountFilename) + "/" + Path.GetFileNameWithoutExtension(mountFilename) + "_" + castTime + "_" + soundLoop + "_" + soundKitFDID + ".ogg");
                                        }
                                        else
                                        {
                                            Console.WriteLine(mountRow["Name_lang"].ToString() + " (" + mountFDID + " is still unnamed");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine(mountRow["Name_lang"].ToString() + " " + soundKitFDID);
                                    }
                                }
                            }
                        }
                    }
                }
            }


            var soundAmbienceFlavorDB = Namer.LoadDBC("SoundAmbienceFlavor");
            var soundAmbienceFlavorMap = new Dictionary<uint, List<(uint SoundKitIDDay, uint SoundKitIDNight, uint AmbienceID0, uint AmbienceID1)>>();
            foreach (var soundAmbienceFlavorRow in soundAmbienceFlavorDB.Values)
            {
                var soundAmbienceID = uint.Parse(soundAmbienceFlavorRow["SoundAmbienceID"].ToString());
                var soundKitIDDay = uint.Parse(soundAmbienceFlavorRow["SoundEntriesIDDay"].ToString());
                var soundKitIDNight = uint.Parse(soundAmbienceFlavorRow["SoundEntriesIDNight"].ToString());

                if (soundAmbienceFlavorMap.TryGetValue(soundAmbienceID, out List<(uint SoundKitIDDay, uint SoundKitIDNight, uint AmbienceID0, uint AmbienceID1)>? current))
                {
                    current.Add((soundKitIDDay, soundKitIDNight, 0, 0));
                }
                else
                {
                    soundAmbienceFlavorMap.Add(soundAmbienceID, new List<(uint, uint, uint, uint)> { (soundKitIDDay, soundKitIDNight, 0, 0) });
                }
            }

            var soundAmbienceDB = Namer.LoadDBC("SoundAmbience");
            foreach (var soundAmbienceRow in soundAmbienceDB.Values)
            {
                var soundAmbienceID = uint.Parse(soundAmbienceRow["ID"].ToString());
                var ambienceIDs = (uint[])soundAmbienceRow["AmbienceID"];

                if (soundAmbienceFlavorMap.TryGetValue(soundAmbienceID, out List<(uint SoundKitIDDay, uint SoundKitIDNight, uint AmbienceID0, uint AmbienceID1)>? current))
                {
                    current.Add((0, 0, ambienceIDs[0], ambienceIDs[1]));
                }
                else
                {
                    soundAmbienceFlavorMap.Add(soundAmbienceID, new List<(uint, uint, uint, uint)> { (0, 0, ambienceIDs[0], ambienceIDs[1]) });
                }
            }

            var areaTableDB = Namer.LoadDBC("AreaTable");
            foreach (var areaTableRow in areaTableDB.Values)
            {
                var ambienceID = uint.Parse(areaTableRow["AmbienceID"].ToString());
                var zoneName = areaTableRow["ZoneName"].ToString().ToLower();

                if (soundAmbienceFlavorMap.TryGetValue(ambienceID, out var soundAmbienceIDs))
                {
                    foreach (var soundAmbience in soundAmbienceIDs)
                    {
                        if (soundAmbience.SoundKitIDDay != 0)
                        {
                            foreach (var soundFile in SoundKitHelper.GetFDIDsByKitID(soundAmbience.SoundKitIDDay))
                            {
                                if (!Namer.IDToNameLookup.ContainsKey((int)soundFile))
                                {
                                    NewFileManager.AddNewFile(soundFile, "sound/ambience/zoneambience/amb_" + zoneName + "_day_" + soundFile + ".ogg");
                                }
                            }
                        }

                        if (soundAmbience.SoundKitIDNight != 0)
                        {
                            foreach (var soundFile in SoundKitHelper.GetFDIDsByKitID(soundAmbience.SoundKitIDNight))
                            {
                                if (!Namer.IDToNameLookup.ContainsKey((int)soundFile))
                                {
                                    NewFileManager.AddNewFile(soundFile, "sound/ambience/zoneambience/amb_" + zoneName + "_night_" + soundFile + ".ogg");
                                }
                            }
                        }

                        if (soundAmbience.AmbienceID0 != 0)
                        {
                            foreach (var soundFile in SoundKitHelper.GetFDIDsByKitID(soundAmbience.AmbienceID0))
                            {
                                if (!Namer.IDToNameLookup.ContainsKey((int)soundFile))
                                {
                                    NewFileManager.AddNewFile(soundFile, "sound/ambience/zoneambience/amb_" + zoneName + "_" + soundFile + ".ogg");
                                }
                            }
                        }

                        if (soundAmbience.AmbienceID1 != 0)
                        {
                            foreach (var soundFile in SoundKitHelper.GetFDIDsByKitID(soundAmbience.AmbienceID1))
                            {
                                if (!Namer.IDToNameLookup.ContainsKey((int)soundFile))
                                {
                                    NewFileManager.AddNewFile(soundFile, "sound/ambience/zoneambience/amb_" + zoneName + "_" + soundFile + ".ogg");
                                }
                            }
                        }
                    }
                }
            }

            var npcSoundsDB = Namer.LoadDBC("NPCSounds");
            foreach (var npcSound in npcSoundsDB.Values)
            {
                var npcsArr = (uint[])npcSound["SoundID"];

                for (var i = 0; i < npcsArr.Length; i++)
                {
                    foreach (var soundFileDataID in SoundKitHelper.GetFDIDsByKitID(npcsArr[i]))
                    {
                        if (!Namer.IDToNameLookup.ContainsKey((int)soundFileDataID))
                        {
                            foreach (var cdiRow in cdiMap.Values)
                            {
                                if ((ushort)cdiRow["NPCSoundID"] != (int)npcSound["ID"])
                                {
                                    continue;
                                }

                                if (!cdiToFDIDMap.TryGetValue(uint.Parse(cdiRow["ID"].ToString()), out var mFDID))
                                {
                                    continue;
                                }

                                if (!Namer.IDToNameLookup.TryGetValue((int)mFDID, out var modelFilename))
                                {
                                    //Console.WriteLine("Referenced model FDID " + mFDID + " does not exist in listfile");
                                    continue;
                                }

                                if (modelFilename.Contains("male_hd"))
                                    continue;

                                var soundType = "UNK";
                                switch (i)
                                {
                                    case 0:
                                        soundType = "greetings";
                                        break;
                                    case 1:
                                        soundType = "farewell";
                                        break;
                                    case 2:
                                        soundType = "pissed";
                                        break;
                                }

                                NewFileManager.AddNewFile(soundFileDataID, "sound/creature/" + Path.GetFileNameWithoutExtension(modelFilename) + "/" + Path.GetFileNameWithoutExtension(modelFilename) + "_" + soundType + "_" + soundFileDataID + ".ogg");
                            }
                        }
                    }
                }
            }

            var soundEmitterDB = Namer.LoadDBC("SoundEmitters");
            foreach (var soundEmitter in soundEmitterDB.Values)
            {
                var soundKitID = uint.Parse(soundEmitter["SoundEntriesID"].ToString());
                var emitterName = soundEmitter["Name"].ToString().Replace("\\", "").Replace("/", "").Replace(" ", "").Replace(",", "_").Replace("'", "");

                var soundCounter = 0;
                foreach (var soundFileDataID in SoundKitHelper.GetFDIDsByKitID(soundKitID))
                {
                    if (!Namer.IDToNameLookup.ContainsKey((int)soundFileDataID))
                    {
                        NewFileManager.AddNewFile(soundFileDataID, "Sound/Emitters/" + emitterName + "_" + soundCounter++ + "_" + soundFileDataID + ".ogg");
                    }
                }
            }

            if (true)
            {
                // RTPC
                var rtpcDB = Namer.LoadDBC("RTPC");
                var rtpcToSoundKitMap = new Dictionary<uint, uint>();

                foreach (var rtpcRow in rtpcDB.Values)
                {
                    var rtpcID = uint.Parse(rtpcRow["ID"].ToString());
                    var soundKitID = uint.Parse(rtpcRow["SoundKitID"].ToString());
                    rtpcToSoundKitMap.Add(rtpcID, soundKitID);
                }

                var rtpcDataDB = Namer.LoadDBC("RTPCData");
                foreach (var rtpcDataRow in rtpcDataDB.Values)
                {
                    if (rtpcToSoundKitMap.TryGetValue(uint.Parse(rtpcDataRow["ID"].ToString()), out uint soundKitID))
                    {
                        foreach (var soundFile in SoundKitHelper.GetFDIDsByKitID(soundKitID))
                        {
                            if (!Namer.IDToNameLookup.ContainsKey((int)soundFile))
                            {
                                Console.WriteLine("RTPC " + rtpcDataRow["ID"].ToString() + " (SoundKitID " + soundKitID + ", Creature " + rtpcDataRow["CreatureID"].ToString() + ", Spell " + rtpcDataRow["SpellID"].ToString() + ") " + soundFile);
                            }
                        }
                    }
                }
                // End of RTPC

                // Spells
                var spellDB = Namer.LoadDBC("Spell");
                var spellNameDB = Namer.LoadDBC("SpellName");

                foreach (var spellRow in spellDB.Values)
                {
                    var spellID = uint.Parse(spellRow["ID"].ToString());
                    var spellName = "";

                    if (!spellNameDB.TryGetValue((int)spellID, out var spellNameRow))
                    {
                        continue;
                    }
                    else
                    {
                        spellName = spellNameRow["Name_lang"].ToString();
                    }

                    if (!spellXSpellVisualMap.TryGetValue(spellID, out var spellVisuals))
                    {
                        continue;
                    }

                    foreach (var spellVisualID in spellVisuals)
                    {
                        if (!spellVisualEventsByVisualIDMap.TryGetValue(spellVisualID, out var spellVisualEvents))
                        {
                            continue;
                        }

                        foreach (var spellVisualEvent in spellVisualEvents)
                        {
                            if (!spellVisualKitSoundEffectMap.TryGetValue(uint.Parse(spellVisualEvent["SpellVisualKitID"].ToString()), out var spellVisualKitSoundEffects))
                            {
                                continue;
                            }

                            foreach (var spellVisualKitSoundEffect in spellVisualKitSoundEffects)
                            {
                                foreach (var soundKitFDID in SoundKitHelper.GetFDIDsByKitID(spellVisualKitSoundEffect))
                                {
                                    if (Namer.IDToNameLookup.ContainsKey((int)soundKitFDID))
                                        continue;

                                    var castTime = "precast";
                                    var soundLoop = "loop";
                                    var startEvent = uint.Parse(spellVisualEvent["StartEvent"].ToString());

                                    switch (startEvent)
                                    {
                                        case 1:
                                            castTime = "precaststart";
                                            break;
                                        case 2:
                                            castTime = "precastend";
                                            break;
                                        case 3:
                                            castTime = "cast";
                                            break;
                                        case 4:
                                            castTime = "travelstart";
                                            break;
                                        case 5:
                                            castTime = "travelend";
                                            break;
                                        case 6:
                                            castTime = "impact";
                                            break;
                                        case 7:
                                            castTime = "aurastart";
                                            break;
                                        case 8:
                                            castTime = "auraend";
                                            break;
                                        case 9:
                                            castTime = "areatriggerstart";
                                            break;
                                        case 10:
                                            castTime = "areatriggerend";
                                            break;
                                        case 11:
                                            castTime = "channelstart";
                                            break;
                                        case 12:
                                            castTime = "channelend";
                                            break;
                                        default:
                                            Console.WriteLine(" Start event " + startEvent + " needs supporting");
                                            break;
                                    }

                                    if (spellVisualEvent["EndEvent"].ToString() == "13")
                                        soundLoop = "oneshot";

                                    var cleanSpellname = spellName.Replace(" ", "").Replace("'", "").Replace("-", "").Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "").Replace(":", "").Replace(";", "").Replace("DNT", "").Replace("&", "").Replace("+", "").Replace("<", "").Replace(">", "").Replace("!", "");

                                    var filename = "Sound/Spell/" + cleanSpellname + "_" + castTime + "_" + soundLoop + "_" + soundKitFDID + ".ogg";
                                    NewFileManager.AddNewFile(soundKitFDID, filename);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static string CSDColToSoundType(string colName)
        {
            colName = colName.Replace("Sound", "").Replace("ID", "");

            switch (colName)
            {
                case "InjuryCritical":
                    return "woundcritical";
                case "Injury":
                    return "wound";
                case "Exertion":
                    return "attack";
                case "ExertionCritical":
                    return "attackcritical";
                case "Fidget":
                case "CustomAttack":
                case "Death":
                case "Aggro":
                case "Alert":
                case "BattleShout":
                case "BattleShoutCritical":
                case "Loop":
                case "Stand":
                case "Birth":
                case "JumpStart":
                case "WingFlap":
                    return colName.ToLower();
                default:
                    Console.WriteLine("WARNING: UNMAPPED SOUND: " + colName);
                    return "";
            }
        }

        public static readonly Dictionary<uint, string> StateType = new Dictionary<uint, string>()
        {
            { 0,   "Invalid" },
            { 1,   "Always" },
            { 2,   "Transport - Stopped" },
            { 3,   "Transport - Accelerating" },
            { 4,   "Transport - Moving" },
            { 5,   "Transport - Decelerating" },
            { 6,   "Movement - Ascending" },
            { 7,   "Movement - Descending" },
            { 8,   "Anim - AttackThrown" },
            { 9,   "Anim - HoldThrown" },
            { 10,  "Anim - LoadThrown" },
            { 11,  "Anim - ReadyThrown" },
            { 12,  "Anim - Run" },
            { 13,  "Anim - Walk" },
            { 14,  "Anim - CombatWound" },
            { 15,  "Anim - Death" },
            { 16,  "Anim - SpellCastDirected" },
            { 17,  "Anim - StandWound" },
            { 18,  "Anim - ReadyUnarmed" },
            { 19,  "Anim - Stand" },
            { 20,  "Anim - ShuffleLeft" },
            { 21,  "Anim - ShuffleRight" },
            { 22,  "Anim - WalkBackwards" },
            { 23,  "Anim - JumpStart" },
            { 24,  "Anim - JumpEnd" },
            { 25,  "Anim - Fall" },
            { 26,  "Anim - SwimIdle" },
            { 27,  "Anim - Swim" },
            { 28,  "Anim - SwimLeft" },
            { 29,  "Anim - SwimRight" },
            { 30,  "Anim - SwimBackwards" },
            { 31,  "Anim - RunLeft" },
            { 32,  "Anim - RunRight" },
            { 33,  "Anim - Fly" },
            { 34,  "Anim - Sprint" },
            { 35,  "Anim - JumpLandRun" },
            { 36,  "Anim - Jump" },
            { 37,  "Movement - Moving" },
            { 38,  "Movement - Not Moving" },
            { 39,  "Anim - CombatCritical" },
            { 40,  "Anim - AttackUnarmed" },
            { 41,  "Anim - Stand Var 1" },
            { 42,  "Anim - Swim Idle Var 1" },
            { 43,  "Anim - Sit Ground" },
            { 44,  "Anim - Sit Ground Up" },
            { 45,  "Anim - Sit Ground Down" },
            { 46,  "Anim - Hover" },
            { 47,  "Movement - Turning" },
            { 48,  "Movement - Not Turning" },
            { 49,  "Movement - Running" },
            { 50,  "Movement - Running Forward" },
            { 51,  "Movement - Running Backward" },
            { 52,  "Movement - Running Left" },
            { 53,  "Movement - Running Right" },
            { 54,  "Movement - Running Sideways" },
            { 55,  "Movement - Walking" },
            { 56,  "Movement - Walking Forward" },
            { 57,  "Movement - Walking Backward" },
            { 58,  "Movement - Walking Left" },
            { 59,  "Movement - Walking Right" },
            { 60,  "Movement - Walking Sideways" },
            { 61,  "Movement - Flying" },
            { 62,  "Movement - Flying Forward" },
            { 63,  "Movement - Flying Backward" },
            { 64,  "Movement - Flying Left" },
            { 65,  "Movement - Flying Right" },
            { 66,  "Movement - Flying Sideways" },
            { 67,  "Anim - Grab" },
            { 68,  "Anim - Ship Start" },
            { 69,  "Anim - Ship Moving" },
            { 70,  "Anim - Ship Stop" },
            { 71,  "Anim - Open" },
            { 72,  "Anim - Opened" },
            { 73,  "Anim - Close" },
            { 74,  "Anim - Closed" },
            { 75,  "Anim - Destroy" },
            { 76,  "Anim - Destroyed" },
            { 77,  "Anim - Custom 0" },
            { 78,  "Anim - Custom 1" },
            { 79,  "Anim - Custom 2" },
            { 80,  "Anim - Custom 3" },
            { 81,  "Anim - Dead" },
            { 82,  "Anim - MountFlightIdle" },
            { 83,  "Anim - MountFlightSprint" },
            { 84,  "Anim - MountFlightLeft" },
            { 85,  "Anim - MountFlightRight" },
            { 86,  "Anim - MountFlightBackwards" },
            { 87,  "Anim - MountFlightRun" },
            { 88,  "Anim - MountFlightWalk" },
            { 89,  "Anim - MountFlightWalkBackwards" },
            { 90,  "Anim - MountFlightStart" },
            { 91,  "Anim - MountFlightLand" },
            { 92,  "Anim - MountFlightLandRun" },
            { 93,  "Anim - MountSwimStart" },
            { 94,  "Anim - MountSwimLand" },
            { 95,  "Anim - MountSwimLandRun" },
            { 96,  "Anim - MountSwimIdle" },
            { 97,  "Anim - MountSwimBackwards" },
            { 98,  "Anim - MountSwimLeft" },
            { 99,  "Anim - MountSwimRight" },
            { 100, "Anim - MountSwimRun" },
            { 101, "Anim - MountSwimSprint" },
            { 102, "Anim - MountSwimWalk" },
            { 103, "Anim - MountSwimWalkBackwards" },
            { 104, "Anim - Birth" },
            { 105, "Anim - Decay" },
            { 106, "Anim - SPELL" },
            { 107, "Anim - STOP" },
            { 108, "Anim - RISE" },
            { 109, "Anim - STUN" },
            { 110, "Anim - HANDSCLOSED" },
            { 111, "Anim - ATTACK1H" },
            { 112, "Anim - ATTACK2H" },
            { 113, "Anim - ATTACK2HL" },
            { 114, "Anim - PARRYUNARMED" },
            { 115, "Anim - PARRY1H" },
            { 116, "Anim - PARRY2H" },
            { 117, "Anim - PARRY2HL" },
            { 118, "Anim - SHIELDBLOCK" },
            { 119, "Anim - READY1H" },
            { 120, "Anim - READY2H" },
            { 121, "Anim - READY2HL" },
            { 122, "Anim - READYBOW" },
            { 123, "Anim - DODGE" },
            { 124, "Anim - SPELLPRECAST" },
            { 125, "Anim - SPELLCAST" },
            { 126, "Anim - SPELLCASTAREA" },
            { 127, "Anim - NPCWELCOME" },
            { 128, "Anim - NPCGOODBYE" },
            { 129, "Anim - BLOCK" },
            { 130, "Anim - ATTACKBOW" },
            { 131, "Anim - FIREBOW" },
            { 132, "Anim - READYRIFLE" },
            { 133, "Anim - ATTACKRIFLE" },
            { 134, "Anim - LOOT" },
            { 135, "Anim - READYSPELLDIRECTED" },
            { 136, "Anim - READYSPELLOMNI" },
            { 137, "Anim - SPELLCASTOMNI" },
            { 138, "Anim - BATTLEROAR" },
            { 139, "Anim - READYABILITY" },
            { 140, "Anim - SPECIAL1H" },
            { 141, "Anim - SPECIAL2H" },
            { 142, "Anim - SHIELDBASH" },
            { 143, "Anim - EMOTETALK" },
            { 144, "Anim - EMOTEEAT" },
            { 145, "Anim - EMOTEWORK" },
            { 146, "Anim - EMOTEUSESTANDING" },
            { 147, "Anim - EMOTETALKEXCLAMATION" },
            { 148, "Anim - EMOTETALKQUESTION" },
            { 149, "Anim - EMOTEBOW" },
            { 150, "Anim - EMOTEWAVE" },
            { 151, "Anim - EMOTECHEER" },
            { 152, "Anim - EMOTEDANCE" },
            { 153, "Anim - EMOTELAUGH" },
            { 154, "Anim - EMOTESLEEP" },
            { 155, "Anim - EMOTESITGROUND" },
            { 156, "Anim - EMOTERUDE" },
            { 157, "Anim - EMOTEROAR" },
            { 158, "Anim - EMOTEKNEEL" },
            { 159, "Anim - EMOTEKISS" },
            { 160, "Anim - EMOTECRY" },
            { 161, "Anim - EMOTECHICKEN" },
            { 162, "Anim - EMOTEBEG" },
            { 163, "Anim - EMOTEAPPLAUD" },
            { 164, "Anim - EMOTESHOUT" },
            { 165, "Anim - EMOTEFLEX" },
            { 166, "Anim - EMOTESHY" },
            { 167, "Anim - EMOTEPOINT" },
            { 168, "Anim - ATTACK1HPIERCE" },
            { 169, "Anim - ATTACK2HLOOSEPIERCE" },
            { 170, "Anim - ATTACKOFF" },
            { 171, "Anim - ATTACKOFFPIERCE" },
            { 172, "Anim - SHEATH" },
            { 173, "Anim - HIPSHEATH" },
            { 174, "Anim - MOUNT" },
            { 175, "Anim - MOUNTSPECIAL" },
            { 176, "Anim - KICK" },
            { 177, "Anim - SLEEPDOWN" },
            { 178, "Anim - SLEEP" },
            { 179, "Anim - SLEEPUP" },
            { 180, "Anim - SITCHAIRLOW" },
            { 181, "Anim - SITCHAIRMED" },
            { 182, "Anim - SITCHAIRHIGH" },
            { 183, "Anim - LOADBOW" },
            { 184, "Anim - LOADRIFLE" },
            { 185, "Anim - HOLDBOW" },
            { 186, "Anim - HOLDRIFLE" },
            { 187, "Anim - EMOTESALUTE" },
            { 188, "Anim - KNEELSTART" },
            { 189, "Anim - KNEELLOOP" },
            { 190, "Anim - KNEELEND" },
            { 191, "Anim - ATTACKUNARMEDOFF" },
            { 192, "Anim - SPECIALUNARMED" },
            { 193, "Anim - STEALTHWALK" },
            { 194, "Anim - STEALTHSTAND" },
            { 195, "Anim - KNOCKDOWN" },
            { 196, "Anim - EATINGLOOP" },
            { 197, "Anim - USESTANDINGLOOP" },
            { 198, "Anim - CHANNELCASTDIRECTED" },
            { 199, "Anim - CHANNELCASTOMNI" },
            { 200, "Anim - WHIRLWIND" },
            { 201, "Anim - USESTANDINGSTART" },
            { 202, "Anim - USESTANDINGEND" },
            { 203, "Anim - CREATURESPECIAL" },
            { 204, "Anim - DROWN" },
            { 205, "Anim - DROWNED" },
            { 206, "Anim - FISHINGCAST" },
            { 207, "Anim - FISHINGLOOP" },
            { 208, "Anim - EMOTEWORKNOSHEATHE" },
            { 209, "Anim - EMOTESTUNNOSHEATHE" },
            { 210, "Anim - EMOTEUSESTANDINGNOSHEATHE" },
            { 211, "Anim - SPELLSLEEPDOWN" },
            { 212, "Anim - SPELLKNEELSTART" },
            { 213, "Anim - SPELLKNEELLOOP" },
            { 214, "Anim - SPELLKNEELEND" },
            { 215, "Anim - INFLIGHT" },
            { 216, "Anim - SPAWN" },
            { 217, "Anim - REBUILD" },
            { 218, "Anim - DESPAWN" },
            { 219, "Anim - HOLD" },
            { 220, "Anim - BOWPULL" },
            { 221, "Anim - BOWRELEASE" },
            { 222, "Anim - GROUPARROW" },
            { 223, "Anim - ARROW" },
            { 224, "Anim - CORPSEARROW" },
            { 225, "Anim - GUIDEARROW" },
            { 226, "Anim - SWAY" },
            { 227, "Anim - DRUIDCATPOUNCE" },
            { 228, "Anim - DRUIDCATRIP" },
            { 229, "Anim - DRUIDCATRAKE" },
            { 230, "Anim - DRUIDCATRAVAGE" },
            { 231, "Anim - DRUIDCATCLAW" },
            { 232, "Anim - DRUIDCATCOWER" },
            { 233, "Anim - DRUIDBEARSWIPE" },
            { 234, "Anim - DRUIDBEARBITE" },
            { 235, "Anim - DRUIDBEARMAUL" },
            { 236, "Anim - DRUIDBEARBASH" },
            { 237, "Anim - DRAGONTAIL" },
            { 238, "Anim - DRAGONSTOMP" },
            { 239, "Anim - DRAGONSPIT" },
            { 240, "Anim - DRAGONSPITHOVER" },
            { 241, "Anim - DRAGONSPITFLY" },
            { 242, "Anim - EMOTEYES" },
            { 243, "Anim - EMOTENO" },
            { 244, "Anim - LOOTHOLD" },
            { 245, "Anim - LOOTUP" },
            { 246, "Anim - STANDHIGH" },
            { 247, "Anim - IMPACT" },
            { 248, "Anim - LIFTOFF" },
            { 249, "Anim - SUCCUBUSENTICE" },
            { 250, "Anim - EMOTETRAIN" },
            { 251, "Anim - EMOTEDEAD" },
            { 252, "Anim - EMOTEDANCEONCE" },
            { 253, "Anim - DEFLECT" },
            { 254, "Anim - EMOTEEATNOSHEATHE" },
            { 255, "Anim - LAND" },
            { 256, "Anim - SUBMERGE" },
            { 257, "Anim - SUBMERGED" },
            { 258, "Anim - CANNIBALIZE" },
            { 259, "Anim - ARROWBIRTH" },
            { 260, "Anim - GROUPARROWBIRTH" },
            { 261, "Anim - CORPSEARROWBIRTH" },
            { 262, "Anim - GUIDEARROWBIRTH" },
            { 263, "Anim - EMOTETALKNOSHEATHE" },
            { 264, "Anim - EMOTEPOINTNOSHEATHE" },
            { 265, "Anim - EMOTESALUTENOSHEATHE" },
            { 266, "Anim - EMOTEDANCESPECIAL" },
            { 267, "Anim - MUTILATE" },
            { 268, "Anim - CUSTOMSPELL01" },
            { 269, "Anim - CUSTOMSPELL02" },
            { 270, "Anim - CUSTOMSPELL03" },
            { 271, "Anim - CUSTOMSPELL04" },
            { 272, "Anim - CUSTOMSPELL05" },
            { 273, "Anim - CUSTOMSPELL06" },
            { 274, "Anim - CUSTOMSPELL07" },
            { 275, "Anim - CUSTOMSPELL08" },
            { 276, "Anim - CUSTOMSPELL09" },
            { 277, "Anim - CUSTOMSPELL10" },
            { 278, "Anim - STEALTHRUN" },
            { 279, "Anim - EMERGE" },
            { 280, "Anim - COWER" },
            { 281, "Anim - GRABCLOSED" },
            { 282, "Anim - GRABTHROWN" },
            { 283, "Anim - FLYSTAND" },
            { 284, "Anim - FLYDEATH" },
            { 285, "Anim - FLYSPELL" },
            { 286, "Anim - FLYSTOP" },
            { 287, "Anim - FLYWALK" },
            { 288, "Anim - FLYRUN" },
            { 289, "Anim - FLYDEAD" },
            { 290, "Anim - FLYRISE" },
            { 291, "Anim - FLYSTANDWOUND" },
            { 292, "Anim - FLYCOMBATWOUND" },
            { 293, "Anim - FLYCOMBATCRITICAL" },
            { 294, "Anim - FLYSHUFFLELEFT" },
            { 295, "Anim - FLYSHUFFLERIGHT" },
            { 296, "Anim - FLYWALKBACKWARDS" },
            { 297, "Anim - FLYSTUN" },
            { 298, "Anim - FLYHANDSCLOSED" },
            { 299, "Anim - FLYATTACKUNARMED" },
            { 300, "Anim - FLYATTACK1H" },
            { 301, "Anim - FLYATTACK2H" },
            { 302, "Anim - FLYATTACK2HL" },
            { 303, "Anim - FLYPARRYUNARMED" },
            { 304, "Anim - FLYPARRY1H" },
            { 305, "Anim - FLYPARRY2H" },
            { 306, "Anim - FLYPARRY2HL" },
            { 307, "Anim - FLYSHIELDBLOCK" },
            { 308, "Anim - FLYREADYUNARMED" },
            { 309, "Anim - FLYREADY1H" },
            { 310, "Anim - FLYREADY2H" },
            { 311, "Anim - FLYREADY2HL" },
            { 312, "Anim - FLYREADYBOW" },
            { 313, "Anim - FLYDODGE" },
            { 314, "Anim - FLYSPELLPRECAST" },
            { 315, "Anim - FLYSPELLCAST" },
            { 316, "Anim - FLYSPELLCASTAREA" },
            { 317, "Anim - FLYNPCWELCOME" },
            { 318, "Anim - FLYNPCGOODBYE" },
            { 319, "Anim - FLYBLOCK" },
            { 320, "Anim - FLYJUMPSTART" },
            { 321, "Anim - FLYJUMP" },
            { 322, "Anim - FLYJUMPEND" },
            { 323, "Anim - FLYFALL" },
            { 324, "Anim - FLYSWIMIDLE" },
            { 325, "Anim - FLYSWIM" },
            { 326, "Anim - FLYSWIMLEFT" },
            { 327, "Anim - FLYSWIMRIGHT" },
            { 328, "Anim - FLYSWIMBACKWARDS" },
            { 329, "Anim - FLYATTACKBOW" },
            { 330, "Anim - FLYFIREBOW" },
            { 331, "Anim - FLYREADYRIFLE" },
            { 332, "Anim - FLYATTACKRIFLE" },
            { 333, "Anim - FLYLOOT" },
            { 334, "Anim - FLYREADYSPELLDIRECTED" },
            { 335, "Anim - FLYREADYSPELLOMNI" },
            { 336, "Anim - FLYSPELLCASTDIRECTED" },
            { 337, "Anim - FLYSPELLCASTOMNI" },
            { 338, "Anim - FLYBATTLEROAR" },
            { 339, "Anim - FLYREADYABILITY" },
            { 340, "Anim - FLYSPECIAL1H" },
            { 341, "Anim - FLYSPECIAL2H" },
            { 342, "Anim - FLYSHIELDBASH" },
            { 343, "Anim - FLYEMOTETALK" },
            { 344, "Anim - FLYEMOTEEAT" },
            { 345, "Anim - FLYEMOTEWORK" },
            { 346, "Anim - FLYEMOTEUSESTANDING" },
            { 347, "Anim - FLYEMOTETALKEXCLAMATION" },
            { 348, "Anim - FLYEMOTETALKQUESTION" },
            { 349, "Anim - FLYEMOTEBOW" },
            { 350, "Anim - FLYEMOTEWAVE" },
            { 351, "Anim - FLYEMOTECHEER" },
            { 352, "Anim - FLYEMOTEDANCE" },
            { 353, "Anim - FLYEMOTELAUGH" },
            { 354, "Anim - FLYEMOTESLEEP" },
            { 355, "Anim - FLYEMOTESITGROUND" },
            { 356, "Anim - FLYEMOTERUDE" },
            { 357, "Anim - FLYEMOTEROAR" },
            { 358, "Anim - FLYEMOTEKNEEL" },
            { 359, "Anim - FLYEMOTEKISS" },
            { 360, "Anim - FLYEMOTECRY" },
            { 361, "Anim - FLYEMOTECHICKEN" },
            { 362, "Anim - FLYEMOTEBEG" },
            { 363, "Anim - FLYEMOTEAPPLAUD" },
            { 364, "Anim - FLYEMOTESHOUT" },
            { 365, "Anim - FLYEMOTEFLEX" },
            { 366, "Anim - FLYEMOTESHY" },
            { 367, "Anim - FLYEMOTEPOINT" },
            { 368, "Anim - FLYATTACK1HPIERCE" },
            { 369, "Anim - FLYATTACK2HLOOSEPIERCE" },
            { 370, "Anim - FLYATTACKOFF" },
            { 371, "Anim - FLYATTACKOFFPIERCE" },
            { 372, "Anim - FLYSHEATH" },
            { 373, "Anim - FLYHIPSHEATH" },
            { 374, "Anim - FLYMOUNT" },
            { 375, "Anim - FLYRUNRIGHT" },
            { 376, "Anim - FLYRUNLEFT" },
            { 377, "Anim - FLYMOUNTSPECIAL" },
            { 378, "Anim - FLYKICK" },
            { 379, "Anim - FLYSITGROUNDDOWN" },
            { 380, "Anim - FLYSITGROUND" },
            { 381, "Anim - FLYSITGROUNDUP" },
            { 382, "Anim - FLYSLEEPDOWN" },
            { 383, "Anim - FLYSLEEP" },
            { 384, "Anim - FLYSLEEPUP" },
            { 385, "Anim - FLYSITCHAIRLOW" },
            { 386, "Anim - FLYSITCHAIRMED" },
            { 387, "Anim - FLYSITCHAIRHIGH" },
            { 388, "Anim - FLYLOADBOW" },
            { 389, "Anim - FLYLOADRIFLE" },
            { 390, "Anim - FLYATTACKTHROWN" },
            { 391, "Anim - FLYREADYTHROWN" },
            { 392, "Anim - FLYHOLDBOW" },
            { 393, "Anim - FLYHOLDRIFLE" },
            { 394, "Anim - FLYHOLDTHROWN" },
            { 395, "Anim - FLYLOADTHROWN" },
            { 396, "Anim - FLYEMOTESALUTE" },
            { 397, "Anim - FLYKNEELSTART" },
            { 398, "Anim - FLYKNEELLOOP" },
            { 399, "Anim - FLYKNEELEND" },
            { 400, "Anim - FLYATTACKUNARMEDOFF" },
            { 401, "Anim - FLYSPECIALUNARMED" },
            { 402, "Anim - FLYSTEALTHWALK" },
            { 403, "Anim - FLYSTEALTHSTAND" },
            { 404, "Anim - FLYKNOCKDOWN" },
            { 405, "Anim - FLYEATINGLOOP" },
            { 406, "Anim - FLYUSESTANDINGLOOP" },
            { 407, "Anim - FLYCHANNELCASTDIRECTED" },
            { 408, "Anim - FLYCHANNELCASTOMNI" },
            { 409, "Anim - FLYWHIRLWIND" },
            { 410, "Anim - FLYBIRTH" },
            { 411, "Anim - FLYUSESTANDINGSTART" },
            { 412, "Anim - FLYUSESTANDINGEND" },
            { 413, "Anim - FLYCREATURESPECIAL" },
            { 414, "Anim - FLYDROWN" },
            { 415, "Anim - FLYDROWNED" },
            { 416, "Anim - FLYFISHINGCAST" },
            { 417, "Anim - FLYFISHINGLOOP" },
            { 418, "Anim - FLYFLY" },
            { 419, "Anim - FLYEMOTEWORKNOSHEATHE" },
            { 420, "Anim - FLYEMOTESTUNNOSHEATHE" },
            { 421, "Anim - FLYEMOTEUSESTANDINGNOSHEATHE" },
            { 422, "Anim - FLYSPELLSLEEPDOWN" },
            { 423, "Anim - FLYSPELLKNEELSTART" },
            { 424, "Anim - FLYSPELLKNEELLOOP" },
            { 425, "Anim - FLYSPELLKNEELEND" },
            { 426, "Anim - FLYSPRINT" },
            { 427, "Anim - FLYINFLIGHT" },
            { 428, "Anim - FLYSPAWN" },
            { 429, "Anim - FLYCLOSE" },
            { 430, "Anim - FLYCLOSED" },
            { 431, "Anim - FLYOPEN" },
            { 432, "Anim - FLYOPENED" },
            { 433, "Anim - FLYDESTROY" },
            { 434, "Anim - FLYDESTROYED" },
            { 435, "Anim - FLYREBUILD" },
            { 436, "Anim - FLYCUSTOM0" },
            { 437, "Anim - FLYCUSTOM1" },
            { 438, "Anim - FLYCUSTOM2" },
            { 439, "Anim - FLYCUSTOM3" },
            { 440, "Anim - FLYDESPAWN" },
            { 441, "Anim - FLYHOLD" },
            { 442, "Anim - FLYDECAY" },
            { 443, "Anim - FLYBOWPULL" },
            { 444, "Anim - FLYBOWRELEASE" },
            { 445, "Anim - FLYSHIPSTART" },
            { 446, "Anim - FLYSHIPMOVING" },
            { 447, "Anim - FLYSHIPSTOP" },
            { 448, "Anim - FLYGROUPARROW" },
            { 449, "Anim - FLYARROW" },
            { 450, "Anim - FLYCORPSEARROW" },
            { 451, "Anim - FLYGUIDEARROW" },
            { 452, "Anim - FLYSWAY" },
            { 453, "Anim - FLYDRUIDCATPOUNCE" },
            { 454, "Anim - FLYDRUIDCATRIP" },
            { 455, "Anim - FLYDRUIDCATRAKE" },
            { 456, "Anim - FLYDRUIDCATRAVAGE" },
            { 457, "Anim - FLYDRUIDCATCLAW" },
            { 458, "Anim - FLYDRUIDCATCOWER" },
            { 459, "Anim - FLYDRUIDBEARSWIPE" },
            { 460, "Anim - FLYDRUIDBEARBITE" },
            { 461, "Anim - FLYDRUIDBEARMAUL" },
            { 462, "Anim - FLYDRUIDBEARBASH" },
            { 463, "Anim - FLYDRAGONTAIL" },
            { 464, "Anim - FLYDRAGONSTOMP" },
            { 465, "Anim - FLYDRAGONSPIT" },
            { 466, "Anim - FLYDRAGONSPITHOVER" },
            { 467, "Anim - FLYDRAGONSPITFLY" },
            { 468, "Anim - FLYEMOTEYES" },
            { 469, "Anim - FLYEMOTENO" },
            { 470, "Anim - FLYJUMPLANDRUN" },
            { 471, "Anim - FLYLOOTHOLD" },
            { 472, "Anim - FLYLOOTUP" },
            { 473, "Anim - FLYSTANDHIGH" },
            { 474, "Anim - FLYIMPACT" },
            { 475, "Anim - FLYLIFTOFF" },
            { 476, "Anim - FLYHOVER" },
            { 477, "Anim - FLYSUCCUBUSENTICE" },
            { 478, "Anim - FLYEMOTETRAIN" },
            { 479, "Anim - FLYEMOTEDEAD" },
            { 480, "Anim - FLYEMOTEDANCEONCE" },
            { 481, "Anim - FLYDEFLECT" },
            { 482, "Anim - FLYEMOTEEATNOSHEATHE" },
            { 483, "Anim - FLYLAND" },
            { 484, "Anim - FLYSUBMERGE" },
            { 485, "Anim - FLYSUBMERGED" },
            { 486, "Anim - FLYCANNIBALIZE" },
            { 487, "Anim - FLYARROWBIRTH" },
            { 488, "Anim - FLYGROUPARROWBIRTH" },
            { 489, "Anim - FLYCORPSEARROWBIRTH" },
            { 490, "Anim - FLYGUIDEARROWBIRTH" },
            { 491, "Anim - FLYEMOTETALKNOSHEATHE" },
            { 492, "Anim - FLYEMOTEPOINTNOSHEATHE" },
            { 493, "Anim - FLYEMOTESALUTENOSHEATHE" },
            { 494, "Anim - FLYEMOTEDANCESPECIAL" },
            { 495, "Anim - FLYMUTILATE" },
            { 496, "Anim - FLYCUSTOMSPELL01" },
            { 497, "Anim - FLYCUSTOMSPELL02" },
            { 498, "Anim - FLYCUSTOMSPELL03" },
            { 499, "Anim - FLYCUSTOMSPELL04" },
            { 500, "Anim - FLYCUSTOMSPELL05" },
            { 501, "Anim - FLYCUSTOMSPELL06" },
            { 502, "Anim - FLYCUSTOMSPELL07" },
            { 503, "Anim - FLYCUSTOMSPELL08" },
            { 504, "Anim - FLYCUSTOMSPELL09" },
            { 505, "Anim - FLYCUSTOMSPELL10" },
            { 506, "Anim - FLYSTEALTHRUN" },
            { 507, "Anim - FLYEMERGE" },
            { 508, "Anim - FLYCOWER" },
            { 509, "Anim - FLYGRAB" },
            { 510, "Anim - FLYGRABCLOSED" },
            { 511, "Anim - FLYGRABTHROWN" },
            { 512, "Anim - TOFLY" },
            { 513, "Anim - TOHOVER" },
            { 514, "Anim - TOGROUND" },
            { 515, "Anim - FLYTOFLY" },
            { 516, "Anim - FLYTOHOVER" },
            { 517, "Anim - FLYTOGROUND" },
            { 518, "Anim - SETTLE" },
            { 519, "Anim - FLYSETTLE" },
            { 520, "Anim - DEATHSTART" },
            { 521, "Anim - DEATHLOOP" },
            { 522, "Anim - DEATHEND" },
            { 523, "Anim - FLYDEATHSTART" },
            { 524, "Anim - FLYDEATHLOOP" },
            { 525, "Anim - FLYDEATHEND" },
            { 526, "Anim - DEATHENDHOLD" },
            { 527, "Anim - FLYDEATHENDHOLD" },
            { 528, "Anim - STRANGULATE" },
            { 529, "Anim - FLYSTRANGULATE" },
            { 530, "Anim - READYJOUST" },
            { 531, "Anim - LOADJOUST" },
            { 532, "Anim - HOLDJOUST" },
            { 533, "Anim - FLYREADYJOUST" },
            { 534, "Anim - FLYLOADJOUST" },
            { 535, "Anim - FLYHOLDJOUST" },
            { 536, "Anim - ATTACKJOUST" },
            { 537, "Anim - FLYATTACKJOUST" },
            { 538, "Anim - RECLINEDMOUNT" },
            { 539, "Anim - FLYRECLINEDMOUNT" },
            { 540, "Anim - TOALTERED" },
            { 541, "Anim - FROMALTERED" },
            { 542, "Anim - FLYTOALTERED" },
            { 543, "Anim - FLYFROMALTERED" },
            { 544, "Anim - INSTOCKS" },
            { 545, "Anim - FLYINSTOCKS" },
            { 546, "Anim - VEHICLEGRAB" },
            { 547, "Anim - VEHICLETHROW" },
            { 548, "Anim - FLYVEHICLEGRAB" },
            { 549, "Anim - FLYVEHICLETHROW" },
            { 550, "Anim - TOALTEREDPOSTSWAP" },
            { 551, "Anim - FROMALTEREDPOSTSWAP" },
            { 552, "Anim - FLYTOALTEREDPOSTSWAP" },
            { 553, "Anim - FLYFROMALTEREDPOSTSWAP" },
            { 554, "Anim - RECLINEDMOUNTPASSENGER" },
            { 555, "Anim - FLYRECLINEDMOUNTPASSENGER" },
            { 556, "Anim - CARRY2H" },
            { 557, "Anim - CARRIED2H" },
            { 558, "Anim - FLYCARRY2H" },
            { 559, "Anim - FLYCARRIED2H" },
            { 560, "Anim - EMOTESNIFF" },
            { 561, "Anim - EMOTEFLYSNIFF" },
            { 562, "Anim - ATTACKFIST1H" },
            { 563, "Anim - FLYATTACKFIST1H" },
            { 564, "Anim - ATTACKFIST1HOFF" },
            { 565, "Anim - FLYATTACKFIST1HOFF" },
            { 566, "Anim - PARRYFIST1H" },
            { 567, "Anim - FLYPARRYFIST1H" },
            { 568, "Anim - READYFIST1H" },
            { 569, "Anim - FLYREADYFIST1H" },
            { 570, "Anim - SPECIALFIST1H" },
            { 571, "Anim - FLYSPECIALFIST1H" },
            { 572, "Anim - EMOTEREADSTART" },
            { 573, "Anim - FLYEMOTEREADSTART" },
            { 574, "Anim - EMOTEREADLOOP" },
            { 575, "Anim - FLYEMOTEREADLOOP" },
            { 576, "Anim - EMOTEREADEND" },
            { 577, "Anim - FLYEMOTEREADEND" },
            { 578, "Anim - SWIMRUN" },
            { 579, "Anim - FLYSWIMRUN" },
            { 580, "Anim - SWIMWALK" },
            { 581, "Anim - FLYSWIMWALK" },
            { 582, "Anim - SWIMWALKBACKWARDS" },
            { 583, "Anim - FLYSWIMWALKBACKWARDS" },
            { 584, "Anim - SWIMSPRINT" },
            { 585, "Anim - FLYSWIMSPRINT" },
            { 586, "Anim - FLYMOUNTSWIMIDLE" },
            { 587, "Anim - FLYMOUNTSWIMBACKWARDS" },
            { 588, "Anim - FLYMOUNTSWIMLEFT" },
            { 589, "Anim - FLYMOUNTSWIMRIGHT" },
            { 590, "Anim - FLYMOUNTSWIMRUN" },
            { 591, "Anim - FLYMOUNTSWIMSPRINT" },
            { 592, "Anim - FLYMOUNTSWIMWALK" },
            { 593, "Anim - FLYMOUNTSWIMWALKBACKWARDS" },
            { 594, "Anim - FLYMOUNTFLIGHTIDLE" },
            { 595, "Anim - FLYMOUNTFLIGHTBACKWARDS" },
            { 596, "Anim - FLYMOUNTFLIGHTLEFT" },
            { 597, "Anim - FLYMOUNTFLIGHTRIGHT" },
            { 598, "Anim - FLYMOUNTFLIGHTRUN" },
            { 599, "Anim - FLYMOUNTFLIGHTSPRINT" },
            { 600, "Anim - FLYMOUNTFLIGHTWALK" },
            { 601, "Anim - FLYMOUNTFLIGHTWALKBACKWARDS" },
            { 602, "Anim - FLYMOUNTFLIGHTSTART" },
            { 603, "Anim - FLYMOUNTSWIMSTART" },
            { 604, "Anim - FLYMOUNTSWIMLAND" },
            { 605, "Anim - FLYMOUNTSWIMLANDRUN" },
            { 606, "Anim - FLYMOUNTFLIGHTLAND" },
            { 607, "Anim - FLYMOUNTFLIGHTLANDRUN" },
            { 608, "Anim - READYBLOWDART" },
            { 609, "Anim - FLYREADYBLOWDART" },
            { 610, "Anim - LOADBLOWDART" },
            { 611, "Anim - FLYLOADBLOWDART" },
            { 612, "Anim - HOLDBLOWDART" },
            { 613, "Anim - FLYHOLDBLOWDART" },
            { 614, "Anim - ATTACKBLOWDART" },
            { 615, "Anim - FLYATTACKBLOWDART" },
            { 616, "Anim - CARRIAGEMOUNT" },
            { 617, "Anim - FLYCARRIAGEMOUNT" },
            { 618, "Anim - CARRIAGEPASSENGERMOUNT" },
            { 619, "Anim - FLYCARRIAGEPASSENGERMOUNT" },
            { 620, "Anim - CARRIAGEMOUNTATTACK" },
            { 621, "Anim - FLYCARRIAGEMOUNTATTACK" },
            { 622, "Anim - BARTENDSTAND" },
            { 623, "Anim - FLYBARTENDSTAND" },
            { 624, "Anim - BARSERVERWALK" },
            { 625, "Anim - FLYBARSERVERWALK" },
            { 626, "Anim - BARSERVERRUN" },
            { 627, "Anim - FLYBARSERVERRUN" },
            { 628, "Anim - BARSERVERSHUFFLELEFT" },
            { 629, "Anim - FLYBARSERVERSHUFFLELEFT" },
            { 630, "Anim - BARSERVERSHUFFLERIGHT" },
            { 631, "Anim - FLYBARSERVERSHUFFLERIGHT" },
            { 632, "Anim - BARTENDEMOTETALK" },
            { 633, "Anim - FLYBARTENDEMOTETALK" },
            { 634, "Anim - BARTENDEMOTEPOINT" },
            { 635, "Anim - FLYBARTENDEMOTEPOINT" },
            { 636, "Anim - BARSERVERSTAND" },
            { 637, "Anim - FLYBARSERVERSTAND" },
            { 638, "Anim - BARSWEEPWALK" },
            { 639, "Anim - FLYBARSWEEPWALK" },
            { 640, "Anim - BARSWEEPRUN" },
            { 641, "Anim - FLYBARSWEEPRUN" },
            { 642, "Anim - BARSWEEPSHUFFLELEFT" },
            { 643, "Anim - FLYBARSWEEPSHUFFLELEFT" },
            { 644, "Anim - BARSWEEPSHUFFLERIGHT" },
            { 645, "Anim - FLYBARSWEEPSHUFFLERIGHT" },
            { 646, "Anim - BARSWEEPEMOTETALK" },
            { 647, "Anim - FLYBARSWEEPEMOTETALK" },
            { 648, "Anim - BARPATRONSITEMOTEPOINT" },
            { 649, "Anim - FLYBARPATRONSITEMOTEPOINT" },
            { 650, "Anim - MOUNTSELFIDLE" },
            { 651, "Anim - FLYMOUNTSELFIDLE" },
            { 652, "Anim - MOUNTSELFWALK" },
            { 653, "Anim - FLYMOUNTSELFWALK" },
            { 654, "Anim - MOUNTSELFRUN" },
            { 655, "Anim - FLYMOUNTSELFRUN" },
            { 656, "Anim - MOUNTSELFSPRINT" },
            { 657, "Anim - FLYMOUNTSELFSPRINT" },
            { 658, "Anim - MOUNTSELFRUNLEFT" },
            { 659, "Anim - FLYMOUNTSELFRUNLEFT" },
            { 660, "Anim - MOUNTSELFRUNRIGHT" },
            { 661, "Anim - FLYMOUNTSELFRUNRIGHT" },
            { 662, "Anim - MOUNTSELFSHUFFLELEFT" },
            { 663, "Anim - FLYMOUNTSELFSHUFFLELEFT" },
            { 664, "Anim - MOUNTSELFSHUFFLERIGHT" },
            { 665, "Anim - FLYMOUNTSELFSHUFFLERIGHT" },
            { 666, "Anim - MOUNTSELFWALKBACKWARDS" },
            { 667, "Anim - FLYMOUNTSELFWALKBACKWARDS" },
            { 668, "Anim - MOUNTSELFSPECIAL" },
            { 669, "Anim - FLYMOUNTSELFSPECIAL" },
            { 670, "Anim - MOUNTSELFJUMP" },
            { 671, "Anim - FLYMOUNTSELFJUMP" },
            { 672, "Anim - MOUNTSELFJUMPSTART" },
            { 673, "Anim - FLYMOUNTSELFJUMPSTART" },
            { 674, "Anim - MOUNTSELFJUMPEND" },
            { 675, "Anim - FLYMOUNTSELFJUMPEND" },
            { 676, "Anim - MOUNTSELFJUMPLANDRUN" },
            { 677, "Anim - FLYMOUNTSELFJUMPLANDRUN" },
            { 678, "Anim - MOUNTSELFSTART" },
            { 679, "Anim - FLYMOUNTSELFSTART" },
            { 680, "Anim - MOUNTSELFFALL" },
            { 681, "Anim - FLYMOUNTSELFFALL" },
            { 682, "Anim - STORMSTRIKE" },
            { 683, "Anim - FLYSTORMSTRIKE" },
            { 684, "Anim - READYJOUSTNOSHEATHE" },
            { 685, "Anim - FLYREADYJOUSTNOSHEATHE" },
            { 686, "Anim - SLAM" },
            { 687, "Anim - FLYSLAM" },
            { 688, "Anim - DEATHSTRIKE" },
            { 689, "Anim - FLYDEATHSTRIKE" },
            { 690, "Anim - SWIMATTACKUNARMED" },
            { 691, "Anim - FLYSWIMATTACKUNARMED" },
            { 692, "Anim - SPINNINGKICK" },
            { 693, "Anim - FLYSPINNINGKICK" },
            { 694, "Anim - ROUNDHOUSEKICK" },
            { 695, "Anim - FLYROUNDHOUSEKICK" },
            { 696, "Anim - ROLLSTART" },
            { 697, "Anim - FLYROLLSTART" },
            { 698, "Anim - ROLL" },
            { 699, "Anim - FLYROLL" },
            { 700, "Anim - ROLLEND" },
            { 701, "Anim - FLYROLLEND" },
            { 702, "Anim - PALMSTRIKE" },
            { 703, "Anim - FLYPALMSTRIKE" },
            { 704, "Anim - MONKOFFENSEATTACKUNARMED" },
            { 705, "Anim - FLYMONKOFFENSEATTACKUNARMED" },
            { 706, "Anim - MONKOFFENSEATTACKUNARMEDOFF" },
            { 707, "Anim - FLYMONKOFFENSEATTACKUNARMEDOFF" },
            { 708, "Anim - MONKOFFENSEPARRYUNARMED" },
            { 709, "Anim - FLYMONKOFFENSEPARRYUNARMED" },
            { 710, "Anim - MONKOFFENSEREADYUNARMED" },
            { 711, "Anim - FLYMONKOFFENSEREADYUNARMED" },
            { 712, "Anim - MONKOFFENSESPECIALUNARMED" },
            { 713, "Anim - FLYMONKOFFENSESPECIALUNARMED" },
            { 714, "Anim - MONKDEFENSEATTACKUNARMED" },
            { 715, "Anim - FLYMONKDEFENSEATTACKUNARMED" },
            { 716, "Anim - MONKDEFENSEATTACKUNARMEDOFF" },
            { 717, "Anim - FLYMONKDEFENSEATTACKUNARMEDOFF" },
            { 718, "Anim - MONKDEFENSEPARRYUNARMED" },
            { 719, "Anim - FLYMONKDEFENSEPARRYUNARMED" },
            { 720, "Anim - MONKDEFENSEREADYUNARMED" },
            { 721, "Anim - FLYMONKDEFENSEREADYUNARMED" },
            { 722, "Anim - MONKDEFENSESPECIALUNARMED" },
            { 723, "Anim - FLYMONKDEFENSESPECIALUNARMED" },
            { 724, "Anim - MONKHEALATTACKUNARMED" },
            { 725, "Anim - FLYMONKHEALATTACKUNARMED" },
            { 726, "Anim - MONKHEALATTACKUNARMEDOFF" },
            { 727, "Anim - FLYMONKHEALATTACKUNARMEDOFF" },
            { 728, "Anim - MONKHEALPARRYUNARMED" },
            { 729, "Anim - FLYMONKHEALPARRYUNARMED" },
            { 730, "Anim - MONKHEALREADYUNARMED" },
            { 731, "Anim - FLYMONKHEALREADYUNARMED" },
            { 732, "Anim - MONKHEALSPECIALUNARMED" },
            { 733, "Anim - FLYMONKHEALSPECIALUNARMED" },
            { 734, "Anim - FLYINGKICK" },
            { 735, "Anim - FLYFLYINGKICK" },
            { 736, "Anim - FLYINGKICKSTART" },
            { 737, "Anim - FLYFLYINGKICKSTART" },
            { 738, "Anim - FLYINGKICKEND" },
            { 739, "Anim - FLYFLYINGKICKEND" },
            { 740, "Anim - CRANESTART" },
            { 741, "Anim - FLYCRANESTART" },
            { 742, "Anim - CRANELOOP" },
            { 743, "Anim - FLYCRANELOOP" },
            { 744, "Anim - CRANEEND" },
            { 745, "Anim - FLYCRANEEND" },
            { 746, "Anim - DESPAWNED" },
            { 747, "Anim - FLYDESPAWNED" },
            { 748, "Anim - THOUSANDFISTS" },
            { 749, "Anim - FLYTHOUSANDFISTS" },
            { 750, "Anim - MONKHEALREADYSPELLDIRECTED" },
            { 751, "Anim - FLYMONKHEALREADYSPELLDIRECTED" },
            { 752, "Anim - MONKHEALREADYSPELLOMNI" },
            { 753, "Anim - FLYMONKHEALREADYSPELLOMNI" },
            { 754, "Anim - MONKHEALSPELLCASTDIRECTED" },
            { 755, "Anim - FLYMONKHEALSPELLCASTDIRECTED" },
            { 756, "Anim - MONKHEALSPELLCASTOMNI" },
            { 757, "Anim - FLYMONKHEALSPELLCASTOMNI" },
            { 758, "Anim - MONKHEALCHANNELCASTDIRECTED" },
            { 759, "Anim - FLYMONKHEALCHANNELCASTDIRECTED" },
            { 760, "Anim - MONKHEALCHANNELCASTOMNI" },
            { 761, "Anim - FLYMONKHEALCHANNELCASTOMNI" },
            { 762, "Anim - TORPEDO" },
            { 763, "Anim - FLYTORPEDO" },
            { 764, "Anim - MEDITATE" },
            { 765, "Anim - FLYMEDITATE" },
            { 766, "Anim - BREATHOFFIRE" },
            { 767, "Anim - FLYBREATHOFFIRE" },
            { 768, "Anim - RISINGSUNKICK" },
            { 769, "Anim - FLYRISINGSUNKICK" },
            { 770, "Anim - GROUNDKICK" },
            { 771, "Anim - FLYGROUNDKICK" },
            { 772, "Anim - KICKBACK" },
            { 773, "Anim - FLYKICKBACK" },
            { 774, "Anim - PETBATTLESTAND" },
            { 775, "Anim - FLYPETBATTLESTAND" },
            { 776, "Anim - PETBATTLEDEATH" },
            { 777, "Anim - FLYPETBATTLEDEATH" },
            { 778, "Anim - PETBATTLERUN" },
            { 779, "Anim - FLYPETBATTLERUN" },
            { 780, "Anim - PETBATTLEWOUND" },
            { 781, "Anim - FLYPETBATTLEWOUND" },
            { 782, "Anim - PETBATTLEATTACK" },
            { 783, "Anim - FLYPETBATTLEATTACK" },
            { 784, "Anim - PETBATTLEREADYSPELL" },
            { 785, "Anim - FLYPETBATTLEREADYSPELL" },
            { 786, "Anim - PETBATTLESPELLCAST" },
            { 787, "Anim - FLYPETBATTLESPELLCAST" },
            { 788, "Anim - PETBATTLECUSTOM0" },
            { 789, "Anim - FLYPETBATTLECUSTOM0" },
            { 790, "Anim - PETBATTLECUSTOM1" },
            { 791, "Anim - FLYPETBATTLECUSTOM1" },
            { 792, "Anim - PETBATTLECUSTOM2" },
            { 793, "Anim - FLYPETBATTLECUSTOM2" },
            { 794, "Anim - PETBATTLECUSTOM3" },
            { 795, "Anim - FLYPETBATTLECUSTOM3" },
            { 796, "Anim - PETBATTLEVICTORY" },
            { 797, "Anim - FLYPETBATTLEVICTORY" },
            { 798, "Anim - PETBATTLELOSS" },
            { 799, "Anim - FLYPETBATTLELOSS" },
            { 800, "Anim - PETBATTLESTUN" },
            { 801, "Anim - FLYPETBATTLESTUN" },
            { 802, "Anim - PETBATTLEDEAD" },
            { 803, "Anim - FLYPETBATTLEDEAD" },
            { 804, "Anim - PETBATTLEFREEZE" },
            { 805, "Anim - FLYPETBATTLEFREEZE" },
            { 806, "Anim - MONKOFFENSEATTACKWEAPON" },
            { 807, "Anim - FLYMONKOFFENSEATTACKWEAPON" },
            { 808, "Anim - BARTENDEMOTEWAVE" },
            { 809, "Anim - FLYBARTENDEMOTEWAVE" },
            { 810, "Anim - BARSERVEREMOTETALK" },
            { 811, "Anim - FLYBARSERVEREMOTETALK" },
            { 812, "Anim - BARSERVEREMOTEWAVE" },
            { 813, "Anim - FLYBARSERVEREMOTEWAVE" },
            { 814, "Anim - BARSERVERPOURDRINKS" },
            { 815, "Anim - FLYBARSERVERPOURDRINKS" },
            { 816, "Anim - BARSERVERPICKUP" },
            { 817, "Anim - FLYBARSERVERPICKUP" },
            { 818, "Anim - BARSERVERPUTDOWN" },
            { 819, "Anim - FLYBARSERVERPUTDOWN" },
            { 820, "Anim - BARSWEEPSTAND" },
            { 821, "Anim - FLYBARSWEEPSTAND" },
            { 822, "Anim - BARPATRONSIT" },
            { 823, "Anim - FLYBARPATRONSIT" },
            { 824, "Anim - BARPATRONSITEMOTETALK" },
            { 825, "Anim - FLYBARPATRONSITEMOTETALK" },
            { 826, "Anim - BARPATRONSTAND" },
            { 827, "Anim - FLYBARPATRONSTAND" },
            { 828, "Anim - BARPATRONSTANDEMOTETALK" },
            { 829, "Anim - FLYBARPATRONSTANDEMOTETALK" },
            { 830, "Anim - BARPATRONSTANDEMOTEPOINT" },
            { 831, "Anim - FLYBARPATRONSTANDEMOTEPOINT" },
            { 832, "Anim - CARRIONSWARM" },
            { 833, "Anim - FLYCARRIONSWARM" },
            { 834, "Anim - STANDVAR2" },
            { 835, "Anim - STANDVAR3" },
            { 836, "Anim - STANDVAR4" },
            { 837, "Anim - STANDVAR5" },
            { 838, "Anim - INSTOCKSVAR1" },
            { 839, "Anim - INSTOCKSVAR2" },
            { 840, "Anim - INSTOCKSVAR3" },
            { 841, "Anim - FLYINSTOCKSVAR1" },
            { 842, "Anim - FLYINSTOCKSVAR2" },
            { 843, "Anim - FLYINSTOCKSVAR3" },
            { 844, "Anim - BIRTHVAR0" },
            { 845, "Anim - BIRTHVAR1" },
            { 846, "Anim - DEATHVAR0" },
            { 847, "Anim - DEATHVAR1" },
            { 848, "Anim - DEATHVAR2" },
            { 849, "Anim - ATTACKUNARMEDVAR1" },
            { 850, "Anim - ATTACKUNARMEDVAR2" }
        };
    }
}
