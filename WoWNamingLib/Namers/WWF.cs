using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class WWF
    {
        public static void Name(bool fullRun = false)
        {
            var wxpDB = Namer.LoadDBC("WeatherXParticulate");
            if (!wxpDB.AvailableColumns.Contains("FileDataID") || !wxpDB.AvailableColumns.Contains("ParentWeatherID"))
            {
                Console.WriteLine("WeatherXParticulate is missing required columns, skipping..."); 
                return;
            }

            foreach (var wxpEntry in wxpDB.Values)
            {
                var fileDataID = int.Parse(wxpEntry["FileDataID"]!.ToString()!);
                var parentWeatherID = uint.Parse(wxpEntry["ParentWeatherID"]!.ToString()!);
                if (!Namer.IDToNameLookup.ContainsKey(fileDataID))
                    NewFileManager.AddNewFile(fileDataID, "Environments/ParticulateVolumes/pvdata/unkweather" + parentWeatherID + "_" + fileDataID + ".pvdata");
            }

            // todo: parse wwf
        }
    }
}
