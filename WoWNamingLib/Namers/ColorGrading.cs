namespace WoWNamingLib.Namers
{
    class ColorGrading
    {
        public static void Name()
        {
            var screenEffectDB = Namer.LoadDBC("ScreenEffect");
            var seMap = new Dictionary<uint, List<string>>();
            foreach (var seRow in screenEffectDB.Values)
            {
                if (uint.Parse(seRow["Effect"].ToString()) != 6)
                    continue;

                var seParams = (uint[])seRow["Param"];
                if (seParams[0] == 0)
                    continue;

                if (!seMap.ContainsKey(seParams[0]))
                {
                    seMap.Add(seParams[0], new List<string>() { seRow["Name"].ToString() });
                }
                else
                {
                    seMap[seParams[0]].Add(seRow["Name"].ToString());
                }
            }

            var mapDB = Namer.LoadDBC("Map");
            var mapMap = new Dictionary<uint, string>();
            foreach (var mapRow in mapDB.Values)
            {
                mapMap.Add(uint.Parse(mapRow["ID"].ToString()), mapRow["Directory"].ToString());
            }

            var lightDataDB = Namer.LoadDBC("LightData");
            var lightDB = Namer.LoadDBC("Light");
            var zoneLightDB = Namer.LoadDBC("ZoneLight");
            var lightParamsDB = Namer.LoadDBC("LightParams");
            var lightSkyboxDB = Namer.LoadDBC("LightSkybox");
            var lightSkyboxMap = new Dictionary<uint, DBCD.DBCDRow>();
            foreach (var lsRow in lightSkyboxDB.Values)
            {
                lightSkyboxMap.Add(uint.Parse(lsRow["ID"].ToString()), lsRow);
            }

            foreach (var ldRow in lightDataDB.Values)
            {
                var needsCheck = false;

                var colorGradingFileDataID = int.Parse(ldRow["ColorGradingFileDataID"].ToString());
                if (colorGradingFileDataID != 0 && !Namer.IDToNameLookup.ContainsKey(colorGradingFileDataID))
                    needsCheck = true;

                var darkerColorGradingFileDataID = int.Parse(ldRow["DarkerColorGradingFileDataID"].ToString());
                if (darkerColorGradingFileDataID != 0 && !Namer.IDToNameLookup.ContainsKey(darkerColorGradingFileDataID))
                    needsCheck = true;

                if (!needsCheck)
                    continue;

                var lightParamID = ushort.Parse(ldRow["LightParamID"].ToString());

                foreach (var lRow in lightDB.Values)
                {
                    var lightParamArr = (ushort[])lRow["LightParamsID"];
                    for (var i = 0; i < lightParamArr.Length; i++)
                    {
                        if (lightParamArr[i] != lightParamID)
                            continue;

                        if (!mapMap.TryGetValue(uint.Parse(lRow["ContinentID"].ToString()), out var mapName))
                        {
                            Console.WriteLine("[ColorGrading] Map " + lRow["ContinentID"].ToString() + " is not known in Map.db2");
                            continue;
                        }

                        foreach (var zlRow in zoneLightDB.Values)
                        {
                            if (uint.Parse(zlRow["LightID"].ToString()) == uint.Parse(lRow["ID"].ToString()))
                            {
                                Console.WriteLine("[ColorGrading] Manual naming required, info: " + colorGradingFileDataID + " " + darkerColorGradingFileDataID + ": Matched ZoneLight " + zlRow["Name"].ToString());
                            }
                        }
                    }
                }

                foreach (var lpRow in lightParamsDB.Values)
                {
                    if (ushort.Parse(lpRow["ID"].ToString()) != lightParamID)
                        continue;

                    var lightSkyboxID = ushort.Parse(lpRow["LightSkyboxID"].ToString());

                    if (lightSkyboxID != 0 && lightSkyboxMap.TryGetValue(lightSkyboxID, out var lsRow))
                    {
                        Console.WriteLine("[ColorGrading] Manual naming required, info: " + colorGradingFileDataID + " " + darkerColorGradingFileDataID + ": Matched Skybox " + lsRow["Name"].ToString());
                    }
                }
            }
        }
    }
}
