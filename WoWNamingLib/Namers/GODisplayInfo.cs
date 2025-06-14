using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class GODisplayInfo
    {
        public static void Name()
        {
            var objectEffectPackageElemDB = Namer.LoadDBC("ObjectEffectPackageElem");
            var objectEffectPackageMap = new Dictionary<uint, List<(uint GroupID, uint StateType)>>();
            foreach (var oepeRow in objectEffectPackageElemDB.Values)
            {
                var objectEffectPackageID = uint.Parse(oepeRow["ObjectEffectPackageID"].ToString());
                var objectEffectGroupID = uint.Parse(oepeRow["ObjectEffectGroupID"].ToString());
                var stateType = uint.Parse(oepeRow["StateType"].ToString());

                if (!objectEffectPackageMap.TryGetValue(objectEffectPackageID, out List<(uint GroupID, uint StateType)>? objectEffectGroupStates))
                    objectEffectPackageMap.Add(objectEffectPackageID, new List<(uint GroupID, uint StateType)>() { (objectEffectGroupID, stateType) });
                else
                    objectEffectGroupStates.Add((objectEffectGroupID, stateType));
            }

            var objectEffectGroupMap = new Dictionary<uint, List<DBCD.DBCDRow>>();
            var objectEffectDB = Namer.LoadDBC("ObjectEffect");

            foreach (var oeRow in objectEffectDB.Values)
            {
                var objectEffectGroupID = uint.Parse(oeRow["ObjectEffectGroupID"].ToString());
                if (!objectEffectGroupMap.TryGetValue(objectEffectGroupID, out List<DBCD.DBCDRow>? objectEffectGroup))
                    objectEffectGroupMap.Add(objectEffectGroupID, new List<DBCD.DBCDRow>() { oeRow });
                else
                    objectEffectGroup.Add(oeRow);
            }

            var gameObjectDisplayInfoDB = Namer.LoadDBC("GameObjectDisplayInfo");
            foreach (var gdiRow in gameObjectDisplayInfoDB.Values)
            {
                var goModelFDID = uint.Parse(gdiRow["FileDataID"].ToString());

                if (!Namer.IDToNameLookup.TryGetValue((int)goModelFDID, out var modelFileName))
                {
                    Console.WriteLine("!!! Unnamed GameObject FDID " + goModelFDID + " for GDI " + gdiRow["ID"].ToString());
                    continue;
                }

                var objectEffectPackageID = uint.Parse(gdiRow["ObjectEffectPackageID"].ToString());
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
                                    if (Sound.StateType.TryGetValue(objectEffectPackageRef.StateType, out var stateName))
                                    {
                                        var animName = stateName.Replace("Anim", "").Replace("Movement", "").Replace("Transport", "").Replace(" ", "").Replace("-", "");
                                        NewFileManager.AddNewFile(soundFDID, "Sound/Doodad/" + Path.GetFileNameWithoutExtension(modelFileName) + "_" + animName.ToLower() + "_" + soundFDID + ".ogg", modelFileName.Contains(goModelFDID.ToString()));
                                    }
                                    else
                                    {
                                        Console.WriteLine("!!!! GO " + modelFileName + " has unnamed sound " + soundFDID + " for unknown state " + objectEffectPackageRef.StateType);
                                    }
                                }
                            }
                        }
                    }
                }

            }
        }
    }
}
