using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class Movie
    {
        public static void Name()
        {
            var movieDB = Namer.LoadDBC("Movie");
            if (!movieDB.AvailableColumns.Contains("AudioFileDataID") || !movieDB.AvailableColumns.Contains("SubtitleFileDataID") || !movieDB.AvailableColumns.Contains("SubtitleFileFormat"))
                throw new Exception("Movie.db2 missing required cols.");


            var movieFileDataDB = Namer.LoadDBC("MovieFileData");
            if (!movieFileDataDB.AvailableColumns.Contains("ID") || !movieFileDataDB.AvailableColumns.Contains("Resolution"))
                throw new Exception("MovieFileData.db2 missing required cols.");

            var fdidToRes = new Dictionary<int, string>();
            foreach (var movieFDIDRow in movieFileDataDB.Values)
            {
                var movieFDID = int.Parse(movieFDIDRow["ID"].ToString()!);
                var resolution = movieFDIDRow["Resolution"].ToString()!;
                fdidToRes[movieFDID] = resolution;
            }

            var movieVariationDB = Namer.LoadDBC("MovieVariation");
            if(!movieVariationDB.AvailableColumns.Contains("MovieID") || !movieVariationDB.AvailableColumns.Contains("FileDataID"))
                throw new Exception("MovieVariation.db2 missing required cols.");

            var movieIDToFDIDs = new Dictionary<int, List<int>>();
            foreach (var movieVarRow in movieVariationDB.Values)
            {
                var movieID = int.Parse(movieVarRow["MovieID"].ToString()!);
                var fileDataID = int.Parse(movieVarRow["FileDataID"].ToString()!);

                if (!movieIDToFDIDs.ContainsKey(movieID))
                    movieIDToFDIDs[movieID] = new List<int>();

                movieIDToFDIDs[movieID].Add(fileDataID);
            }

            foreach (var movieRow in movieDB.Values)
            {
                var movieID = int.Parse(movieRow["ID"].ToString()!);

                var audioFDID = int.Parse(movieRow["AudioFileDataID"].ToString()!);
                if (audioFDID != 0 && Namer.NeedsName(audioFDID))
                    NewFileManager.AddNewFile(audioFDID, "Interface/Cinematics/Movie_" + movieID + "/Movie_" + movieID + "_Audio_" + audioFDID + ".mp3");

                var subtitleFDID = int.Parse(movieRow["SubtitleFileDataID"].ToString()!);
                var subtitleFormat = int.Parse(movieRow["SubtitleFileFormat"].ToString()!);

                if (subtitleFDID != 0 && Namer.NeedsName(subtitleFDID) && subtitleFormat != 0)
                {
                    var sbtExt = subtitleFormat switch
                    {
                        7 => "sbt",
                        118 => "srt",
                        _ => throw new Exception("Unknown subtitle format: " + subtitleFormat)
                    };

                    NewFileManager.AddNewFile(subtitleFDID, "Interface/Cinematics/Movie_" + movieID + "/Movie_" + movieID + "_Subtitles_" + subtitleFDID + "." + sbtExt);
                }

                if(movieIDToFDIDs.TryGetValue(movieID, out var fdids))
                {
                    foreach (var fdid in fdids)
                    {
                        if (Namer.NeedsName(fdid))
                        {
                            if (fdidToRes.TryGetValue(fdid, out var res))
                            {
                                NewFileManager.AddNewFile(fdid, "Interface/Cinematics/Movie_" + movieID + "/Movie_" + movieID + "_" + res + "_" + fdid + ".avi");
                            }
                        }
                    }
                }
            }
        }
    }
}
