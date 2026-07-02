using System.Diagnostics;
using System.Text;
using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class Interface
    {
        private static string CleanName(string name)
        {
            return name.Replace(" ", "").Replace("'", "").Replace("-", "").Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "").Replace(":", "").Replace(";", "").Replace("DNT", "").Replace("+", "").Replace("<", "").Replace(">", "").Replace("!", "");
        }
        public static void Name()
        {
            // Interface files
            try
            {
                var manifestInterfaceData = Namer.LoadDBC("ManifestInterfaceData");
                foreach (var midRow in manifestInterfaceData.Values)
                {
                    var fdid = int.Parse(midRow["ID"].ToString());
                    var filename = midRow["FilePath"].ToString() + midRow["FileName"].ToString();
                    filename = filename.Replace("\\", "/");

                    if (!Namer.IDToNameLookup.TryGetValue(fdid, out string? currentFilename) || currentFilename != filename)
                        NewFileManager.AddNewFile(fdid, filename, true, true);
                }

                // 6067012 - interface/ui-code-list.txt
                var codeList = CASCManager.GetFileByID(6067012).Result;
                if (codeList != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        codeList.CopyTo(ms);
                        ms.Position = 0;
                        var asText = Encoding.ASCII.GetString(ms.ToArray());
                        var lines = asText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var line in lines)
                            NewFileManager.AddNewFileByname(line.Replace("\\", "/"));
                    }
                }

                // 6067013 - interface/ui-toc-list.txt
                var tocList = CASCManager.GetFileByID(6067013).Result;
                if (tocList != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        tocList.CopyTo(ms);
                        ms.Position = 0;
                        var asText = Encoding.ASCII.GetString(ms.ToArray());
                        var lines = asText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var line in lines)
                            NewFileManager.AddNewFileByname(line.Replace("\\", "/"));
                    }
                }

                // 6076661 - interface/ui-gen-addon-list.txt
                var genList = CASCManager.GetFileByID(6076661).Result;
                if (genList != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        genList.CopyTo(ms);
                        ms.Position = 0;
                        var asText = Encoding.ASCII.GetString(ms.ToArray());
                        var lines = asText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var line in lines)
                            NewFileManager.AddNewFileByname(line.Replace("\\", "/"));
                    }
                }

                #region LoadingScreen
                try
                {
                    // 	Interface/Glues/LOADINGSCREENS/ExpansionXX/Main/Loadscreen_X.blp
                    var mapDB = Namer.LoadDBC("Map");
                    if (!mapDB.AvailableColumns.Contains("LoadingScreenID") || !mapDB.AvailableColumns.Contains("ExpansionID") || !mapDB.AvailableColumns.Contains("MapName_lang"))
                        throw new Exception("Missing LoadingScreenID or ExpansionID column in Map.db2");

                    var mapLoadingScreenDB = Namer.LoadDBC("MapLoadingScreen");
                    if (!mapLoadingScreenDB.AvailableColumns.Contains("MapID") || !mapLoadingScreenDB.AvailableColumns.Contains("LoadingScreenID"))
                        throw new Exception("Missing MapID or LoadingScreenID column in MapLoadingScreen.db2");

                    var loadingScreenDB = Namer.LoadDBC("LoadingScreens");
                    if (!loadingScreenDB.AvailableColumns.Contains("MainImageFileDataID"))
                        throw new Exception("Missing MainImageFileDataID column in LoadingScreen.db2");

                    foreach (var mapRow in mapDB.Values)
                    {
                        var loadingScreenID = int.Parse(mapRow["LoadingScreenID"].ToString()!);
                        if (loadingScreenDB.TryGetValue(loadingScreenID, out var loadingScreenRow))
                        {
                            var mainImage = int.Parse(loadingScreenRow["MainImageFileDataID"].ToString()!);
                            if (mainImage != 0 && (!Namer.IDToNameLookup.ContainsKey(mainImage) || Namer.placeholderNames.Contains(mainImage)))
                            {
                                var expansion = int.Parse(mapRow["ExpansionID"].ToString()!);
                                var mapName = CleanName(mapRow["MapName_lang"].ToString()!);
                                var loadingScreenName = "Interface/Glues/LOADINGSCREENS/Expansion" + expansion + "/Main/Loadscreen_" + mapName + "_" + mainImage + ".blp";
                                NewFileManager.AddNewFile(mainImage, loadingScreenName, true);
                            }
                        }

                        foreach (var mapLoadingScreenRow in mapLoadingScreenDB.Values)
                        {
                            if (int.Parse(mapLoadingScreenRow["MapID"].ToString()!) == int.Parse(mapRow["ID"].ToString()!))
                            {
                                var loadingScreenID2 = int.Parse(mapLoadingScreenRow["LoadingScreenID"].ToString()!);
                                if (loadingScreenDB.TryGetValue(loadingScreenID2, out var loadingScreenRow2))
                                {
                                    var mainImage = int.Parse(loadingScreenRow2["MainImageFileDataID"].ToString()!);
                                    if (mainImage != 0 && (!Namer.IDToNameLookup.ContainsKey(mainImage) || Namer.placeholderNames.Contains(mainImage)))
                                    {
                                        var expansion = int.Parse(mapRow["ExpansionID"].ToString()!);
                                        var mapName = CleanName(mapRow["MapName_lang"].ToString()!);
                                        var loadingScreenName = "Interface/Glues/LOADINGSCREENS/Expansion" + expansion + "/Main/Loadscreen_" + mapName + "_" + mainImage + ".blp";
                                        NewFileManager.AddNewFile(mainImage, loadingScreenName, true);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error during loading screen naming: " + e.Message);
                }

                #endregion

                #region UIMap
                // System:
                // 0 => Interface/WorldMap
                // 1 => Deprecated, ignore
                // 2 => Interface/AdventureMap
                // 3 => Minimap, ignore probably

                // Type (exceptions):
                // 5 => Interface/WorldMap/MicroDungeon
                try
                {
                    var uiMapDB = Namer.LoadDBC("UiMap");
                    if (!uiMapDB.AvailableColumns.Contains("System") || !uiMapDB.AvailableColumns.Contains("Type") || !uiMapDB.AvailableColumns.Contains("Name_lang"))
                        throw new Exception("Missing required column in UiMap.db2");

                    var uiMapXMapArtDB = Namer.LoadDBC("UiMapXMapArt");
                    if (!uiMapXMapArtDB.AvailableColumns.Contains("UiMapID") || !uiMapXMapArtDB.AvailableColumns.Contains("UiMapArtID"))
                        throw new Exception("Missing required column in UiMapXMapArt.db2");

                    var uiMapArtTileDB = Namer.LoadDBC("UiMapArtTile");
                    if (!uiMapArtTileDB.AvailableColumns.Contains("UiMapArtID") || !uiMapArtTileDB.AvailableColumns.Contains("FileDataID") || !uiMapArtTileDB.AvailableColumns.Contains("RowIndex") || !uiMapArtTileDB.AvailableColumns.Contains("ColIndex"))
                        throw new Exception("Missing required column in UiMapArtTile.db2");

                    foreach (var uiMapRow in uiMapDB.Values)
                    {
                        var system = int.Parse(uiMapRow["System"].ToString()!);
                        var type = int.Parse(uiMapRow["Type"].ToString()!);
                        var name = CleanName(uiMapRow["Name_lang"].ToString()!);
                        var uiMapID = int.Parse(uiMapRow["ID"].ToString()!);

                        var basename = system switch
                        {
                            0 => "Interface/WorldMap",
                            2 => "Interface/AdventureMap",
                            _ => null
                        };

                        // Skip other map types for now
                        if (basename == null)
                        {
                            if (system != 1 && system != 3) // only bother warning for unexpected situations
                                Console.WriteLine("Skipping UIMapID " + uiMapID + " with unsupported system " + system);

                            continue;
                        }

                        if (type == 5)
                            basename += "/MicroDungeon";

                        foreach (var uiMapXMapArtRow in uiMapXMapArtDB.Values)
                        {
                            if (int.Parse(uiMapXMapArtRow["UiMapID"].ToString()!) != uiMapID)
                                continue;

                            var uiMapArtID = int.Parse(uiMapXMapArtRow["UiMapArtID"].ToString()!);
                            var tileList = new List<(int rowIndex, int colIndex, int fileDataID)>();

                            foreach (var uiMapArtTileRow in uiMapArtTileDB.Values)
                            {
                                if (int.Parse(uiMapArtTileRow["UiMapArtID"].ToString()!) == uiMapArtID)
                                {
                                    var fileDataID = int.Parse(uiMapArtTileRow["FileDataID"].ToString()!);
                                    var rowIndex = int.Parse(uiMapArtTileRow["RowIndex"].ToString()!);
                                    var colIndex = int.Parse(uiMapArtTileRow["ColIndex"].ToString()!);

                                    tileList.Add((rowIndex, colIndex, fileDataID));
                                }
                            }

                            var totalTiles = tileList.Count;
                            var maxTilesPerRow = tileList.GroupBy(x => x.rowIndex).Max(g => g.Count());
                            var tilesPerRow = maxTilesPerRow > 0 ? maxTilesPerRow : 1;

                            foreach (var (rowIndex, colIndex, fileDataID) in tileList)
                            {
                                if (!Namer.IDToNameLookup.ContainsKey(fileDataID) || Namer.placeholderNames.Contains(fileDataID))
                                {
                                    var tileIndex = rowIndex * tilesPerRow + colIndex + 1; // +1 because thats how blizzard does it
                                    var newName = basename + "/" + name + tileIndex + "_" + fileDataID + ".blp";
                                    NewFileManager.AddNewFile(fileDataID, newName, true);
                                }
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    throw new Exception("Error during UIMap naming: " + e.Message);
                }

                #endregion

                #region Icons
                // Interface/Icons
                // TODO
                #endregion

                #region LFGArt
                // Interface/LFGFRAME/LFGIcon-x.blp
                // Interface/LFGFRAME/UI-LFG-BACKGROUND-x.blp
                // TODO
                #endregion

                #region UITextureAtlas
                #endregion

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during MID naming: " + e.Message);
            }
        }
    }
}