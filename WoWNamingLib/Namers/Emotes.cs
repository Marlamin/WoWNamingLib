using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    internal class Emotes
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

            var chrRacesDB = Namer.LoadDBC("ChrRaces");
            var chrRaceMap = new Dictionary<int, string>();
            foreach (var chrRaceRow in chrRacesDB.Values)
            {
                var raceID = (int)chrRaceRow["ID"];
                chrRaceMap.Add(raceID, (string)chrRaceRow["ClientFileString"] + "_" + (string)chrRaceRow["Short_name_lower_lang"]);
            }

            var emotesTextDB = Namer.LoadDBC("EmotesText");
            var emoteNameMap = new Dictionary<int, string>();
            foreach (var emotesTextRow in emotesTextDB.Values)
            {
                var emoteID = (int)emotesTextRow["ID"];
                var nameID = (string)emotesTextRow["Name"];
                emoteNameMap.Add(emoteID, nameID);
            }

            var emotesTextSoundDB = Namer.LoadDBC("EmotesTextSound");
            foreach (var emotesTextSoundRow in emotesTextSoundDB.Values)
            {
                var emotesTextID = (int)emotesTextSoundRow["EmotesTextID"];
                var raceID = (byte)emotesTextSoundRow["RaceID"];
                var sexID = (byte)emotesTextSoundRow["SexID"];
                var soundKitID = (uint)emotesTextSoundRow["SoundID"];

                if (soundKitFDIDMap.TryGetValue(soundKitID, out var soundFDIDs))
                {
                    foreach (var soundFDID in soundFDIDs)
                    {
                        if (Namer.IDToNameLookup.ContainsKey(soundFDID))
                            continue;

                        if (soundFDID == 0)
                            continue;

                        var sex = "male";
                        if (sexID == 1)
                            sex = "female";

                        var emoteName = emoteNameMap[(int)emotesTextID];
                        NewFileManager.AddNewFile(soundFDID, "sound/character/" + chrRaceMap[(int)raceID] + "_" + sex + "/vo_" + chrRaceMap[(int)raceID] + "_" + sex + "_" + emoteName + "_" + soundFDID + ".ogg");
                    }
                }
            }

            var vocalUiNames = new Dictionary<byte, string>
            {
                { 0, "INVENTORYFULL" },
                { 1, "OUTOFAMMO" },
                { 2, "NOEQUIP_LEVEL" },
                { 3, "NOEQUIP_EVER" },
                { 4, "BOUND_NODROP" },
                { 5, "ITEMCOOLING" },
                { 6, "CANTDRINKMORE" },
                { 7, "CANTEATMORE" },
                { 8, "CANTINVITE" },
                { 9, "INVITEEBUSY" },
                { 10, "TARGETTOOFAR" },
                { 11, "INVALIDTARGET" },
                { 12, "SPELLCOOLING" },
                { 13, "CANTLEARN_LEVEL" },
                { 14, "LOCKED" },
                { 15, "NOMANA" },
                { 16, "NOTWHILEDEAD" },
                { 17, "CANTLOOT" },
                { 18, "CANTCREATE" },
                { 19, "DECLINEGROUP" },
                { 20, "ALREADYINGROUP" },
                { 21, "ALREADYINGUILD" },
                { 22, "CANTAFFORDBANKSLOT" },
                { 23, "TOOMANYBANKSLOTS" },
                { 24, "CANTEAT_MOVING" },
                { 25, "NOTABAG" },
                { 26, "CANTPUTBAG" },
                { 27, "WRONGSLOT" },
                { 28, "AMMOONLYINBAG" },
                { 29, "BAGFULL" },
                { 30, "ITEMMAXCOUNT" },
                { 31, "CANTLOOT_DIDNTKILL" },
                { 32, "CANTLOOT_WRONGFACING" },
                { 33, "CANTLOOT_LOCKED" },
                { 34, "CANTLOOT_NOTSTANDING" },
                { 35, "CANTLOOT_TOOFAR" },
                { 36, "CANTATTACKRONGDIRECTION" },
                { 37, "CANTATTACK_NOTSTANDING" },
                { 38, "CANTATTACK_NOTARGET" },
                { 39, "NOTENOUGHGOLD" },
                { 40, "NOTENOUGHMONEY" },
                { 41, "CANTEQUIP2H_SKILL" },
                { 42, "CANTEQUIP_2HEQUIPPED" },
                { 43, "CANTEQUIP2H_NOSKILL" },
                { 44, "NOTEQUIPPABLE" },
                { 45, "GENERICNOTARGET" },
                { 46, "CANTCAST_OUTOFRANGE" },
                { 47, "POTIONCOOLING" },
                { 48, "PROFICIENCYNEEDED" },
                { 49, "MUSTEQUIPPITEM" },
                { 50, "ABILITYCOOLING" },
                { 51, "CANTUSEITEM" },
                { 52, "CHESTINUSE" },
                { 53, "FOODCOOLING" },
                { 54, "CANTTAXI_NOMONEY" },
                { 55, "CANTUSELOCKED" },
                { 56, "NOEQUIPSLOTAVAILABLE" },
                { 57, "CANTUSETOOFAR" },
                { 58, "CANTSWAP" },
                { 59, "CANTTRADE_SOULBOUND" },
                { 60, "NOTOWNER" },
                { 61, "ITEMLOCKED" },
                { 62, "GUILDPERMISSIONS" },
                { 63, "NORAGE" },
                { 64, "NOENERGY" },
                { 65, "NOFOCUS" },
                { 66, "INVALIDATTACKTARGET" },
                { 67, "OUTOFRANGE2" }
            };

            var vocalUiSoundsDB = Namer.LoadDBC("VocalUISounds");
            foreach (var vocalUiSoundsRow in vocalUiSoundsDB.Values)
            {
                var normalSoundIDs = ((uint[])vocalUiSoundsRow["NormalSoundID"]);
                var vocalUIEnum = (byte)vocalUiSoundsRow["VocalUIEnum"];
                var raceID = (byte)vocalUiSoundsRow["RaceID"];
                for (int i = 0; i < normalSoundIDs.Length; i++)
                {
                    if (soundKitFDIDMap.TryGetValue(normalSoundIDs[i], out var soundFDIDs))
                    {
                        foreach (var soundFDID in soundFDIDs)
                        {
                            if (Namer.IDToNameLookup.ContainsKey(soundFDID))
                                continue;

                            if (soundFDID == 0)
                                continue;

                            var sex = "male";
                            if (i == 1)
                                sex = "female";

                            var vocalUIName = vocalUiNames[vocalUIEnum];
                            if (vocalUIName.Contains(vocalUIName))
                            {
                                NewFileManager.AddNewFile(soundFDID, "sound/character/" + chrRaceMap[(int)raceID] + "_" + sex + "/vo_" + chrRaceMap[(int)raceID] + "_" + sex + "_" + vocalUIName + "_" + soundFDID + ".ogg");
                            }
                            else
                            {
                                Console.WriteLine("[Emotes] !!! Unknown vocal UI name for enum " + vocalUIEnum);
                            }
                        }
                    }
                }

            }

        }
    }
}
