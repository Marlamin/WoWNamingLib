using WoWNamingLib.Services;

namespace WoWNamingLib
{
    class CharCustomization
    {
        public static void Name()
        {
            var tfdMap = new Dictionary<int, List<int>>();
            var tfdMapS = new Dictionary<int, int>();
            var tfdMapE = new Dictionary<int, int>();

            var textureFileData = Namer.LoadDBC("TextureFileData");

            foreach (var tfdRow in textureFileData.Values)
            {
                var usageType = uint.Parse(tfdRow["UsageType"].ToString());
                var materialResourcesID = int.Parse(tfdRow["MaterialResourcesID"].ToString());

                if (usageType == 0)
                {
                    if (tfdMap.ContainsKey(materialResourcesID))
                    {
                        tfdMap[materialResourcesID].Add(int.Parse(tfdRow["FileDataID"].ToString()));
                    }
                    else
                    {
                        tfdMap.TryAdd(materialResourcesID, new List<int>() { int.Parse(tfdRow["FileDataID"].ToString()) });
                    }
                }
                else if (usageType == 1)
                {
                    tfdMapS.TryAdd(int.Parse(tfdRow["MaterialResourcesID"].ToString()), int.Parse(tfdRow["FileDataID"].ToString()));
                }
                else if (usageType == 2)
                {
                    tfdMapE.TryAdd(int.Parse(tfdRow["MaterialResourcesID"].ToString()), int.Parse(tfdRow["FileDataID"].ToString()));
                }
            }

            var cmdDB = Namer.LoadDBC("CreatureModelData");
            var cmdIDToFDIDMap = new Dictionary<uint, int>();

            foreach (var cmdEntry in cmdDB.Values)
            {
                var mFDID = int.Parse(cmdEntry["FileDataID"].ToString());
                cmdIDToFDIDMap.Add(uint.Parse(cmdEntry["ID"].ToString()), mFDID);
            }

            var creatureDisplayInfoDB = Namer.LoadDBC("CreatureDisplayInfo");
            var cdiToFDIDMap = new Dictionary<uint, int>();
            foreach (var cdiRow in creatureDisplayInfoDB.Values)
            {
                if (cmdIDToFDIDMap.TryGetValue(uint.Parse(cdiRow["ModelID"].ToString()), out var fdid))
                {
                    cdiToFDIDMap.Add(uint.Parse(cdiRow["ID"].ToString()), fdid);
                }
            }

            var chrModelDB = Namer.LoadDBC("ChrModel");
            var chrModelMap = new Dictionary<uint, DBCD.DBCDRow>();
            foreach (var chrModelRow in chrModelDB.Values)
            {
                chrModelMap.Add(uint.Parse(chrModelRow["ID"].ToString()), chrModelRow);
            }

            var chrCustomizationMaterialDB = Namer.LoadDBC("ChrCustomizationMaterial");
            var chrCustomizationMaterialMap = new Dictionary<int, DBCD.DBCDRow>();
            foreach (var chrCustMaterialRow in chrCustomizationMaterialDB.Values)
            {
                chrCustomizationMaterialMap.Add(int.Parse(chrCustMaterialRow["ID"].ToString()), chrCustMaterialRow);
            }

            var chrCustomizationOptionDB = Namer.LoadDBC("ChrCustomizationOption");
            var chrCustomizationOptionMap = new Dictionary<int, DBCD.DBCDRow>();
            foreach (var chrCustOptionRow in chrCustomizationOptionDB.Values)
            {
                chrCustomizationOptionMap.Add(int.Parse(chrCustOptionRow["ID"].ToString()), chrCustOptionRow);
            }

            var chrCustomizationChoiceDB = Namer.LoadDBC("ChrCustomizationChoice");
            var chrCustomizationChoiceMap = new Dictionary<int, DBCD.DBCDRow>();
            foreach (var chrCustChoiceRow in chrCustomizationChoiceDB.Values)
            {
                chrCustomizationChoiceMap.Add(int.Parse(chrCustChoiceRow["ID"].ToString()), chrCustChoiceRow);
            }

            var chrCustomizationCategoryDB = Namer.LoadDBC("ChrCustomizationCategory");
            var chrCustomizationCategoryMap = new Dictionary<int, DBCD.DBCDRow>();
            foreach (var chrCustCategoryRow in chrCustomizationCategoryDB.Values)
            {
                chrCustomizationCategoryMap.Add(int.Parse(chrCustCategoryRow["ID"].ToString()), chrCustCategoryRow);
            }

            var chrCustomizationElementDB = Namer.LoadDBC("ChrCustomizationElement");

            foreach (var chrCustElementRow in chrCustomizationElementDB.Values)
            {
                var chrCustMaterialID = int.Parse(chrCustElementRow["ChrCustomizationMaterialID"].ToString());
                if (chrCustMaterialID == 0)
                    continue;

                if (!chrCustomizationMaterialMap.TryGetValue(chrCustMaterialID, out var chrCustMatRow))
                    continue;

                var chrCustMaterialResID = int.Parse(chrCustMatRow["MaterialResourcesID"].ToString());

                var continueNaming = false;

                if (tfdMap.TryGetValue(chrCustMaterialResID, out var chrCustFDIDs))
                {
                    foreach (var chrCustFDID in chrCustFDIDs)
                    {
                        if (!Namer.IDToNameLookup.ContainsKey(chrCustFDID))
                            continueNaming = true;
                    }
                }

                if (tfdMapE.TryGetValue(chrCustMaterialResID, out var chrCustFDIDE))
                {
                    if (!Namer.IDToNameLookup.ContainsKey(chrCustFDIDE))
                        continueNaming = true;
                }

                if (tfdMapS.TryGetValue(chrCustMaterialResID, out var chrCustFDIDS))
                {
                    if (!Namer.IDToNameLookup.ContainsKey(chrCustFDIDS))
                        continueNaming = true;
                }

                if (!continueNaming)
                    continue;

                var chrCustChoiceID = int.Parse(chrCustElementRow["ChrCustomizationChoiceID"].ToString());
                if (!chrCustomizationChoiceMap.TryGetValue(chrCustChoiceID, out var choiceRow))
                    continue;

                var chrCustOptionID = int.Parse(choiceRow["ChrCustomizationOptionID"].ToString());
                if (!chrCustomizationOptionMap.TryGetValue(chrCustOptionID, out var optionRow))
                    continue;

                var chrModelID = uint.Parse(optionRow["ChrModelID"].ToString());
                if (!chrModelMap.TryGetValue(chrModelID, out var modelRow))
                    continue;

                var cdiID = uint.Parse(modelRow["DisplayID"].ToString());
                if (!cdiToFDIDMap.TryGetValue(cdiID, out var chrModelFileDataID))
                    continue;

                if (!Namer.IDToNameLookup.TryGetValue(chrModelFileDataID, out var chrModelFilename))
                    continue;

                if (chrCustFDIDs != null)
                {
                    foreach (var chrCustFDID in chrCustFDIDs)
                    {
                        if (chrCustFDID != 0 && (!Namer.IDToNameLookup.ContainsKey(chrCustFDID) || Namer.IDToNameLookup[chrCustFDID].Contains("exp09")))
                            NewFileManager.AddNewFile(chrCustFDID, Path.GetDirectoryName(chrModelFilename) + "/" + Path.GetFileNameWithoutExtension(chrModelFilename) + "_" + optionRow["Name_lang"].ToString().Replace(" ", "_").ToLower() + "_" + chrCustFDID + ".blp");
                    }
                }

                if (chrCustFDIDE != 0 && (!Namer.IDToNameLookup.ContainsKey(chrCustFDIDE) || Namer.IDToNameLookup[chrCustFDIDE].Contains("exp09")))
                    NewFileManager.AddNewFile(chrCustFDIDE, Path.GetDirectoryName(chrModelFilename) + "/" + Path.GetFileNameWithoutExtension(chrModelFilename) + "_" + optionRow["Name_lang"].ToString().Replace(" ", "_").ToLower() + "_e_" + chrCustFDIDE + ".blp");

                if (chrCustFDIDS != 0 && (!Namer.IDToNameLookup.ContainsKey(chrCustFDIDS) || Namer.IDToNameLookup[chrCustFDIDS].Contains("exp09")))
                    NewFileManager.AddNewFile(chrCustFDIDS, Path.GetDirectoryName(chrModelFilename) + "/" + Path.GetFileNameWithoutExtension(chrModelFilename) + "_" + optionRow["Name_lang"].ToString().Replace(" ", "_").ToLower() + "_s_" + chrCustFDIDS + ".blp");
            }
        }
    }
}
