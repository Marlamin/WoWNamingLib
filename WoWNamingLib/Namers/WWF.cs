using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    class WWF
    {
        public static void Name(bool fullRun = false)
        {
            var wxpDB = Namer.LoadDBC("WeatherXParticulate");
            foreach (var wxpEntry in wxpDB.Values)
            {
                var fileDataID = int.Parse(wxpEntry["FileDataID"].ToString());
                var parentWeatherID = uint.Parse(wxpEntry["ParentWeatherID"].ToString());
                if (!Namer.IDToNameLookup.ContainsKey(fileDataID))
                {
                    NewFileManager.AddNewFile(fileDataID, "particles/particulates/weather" + parentWeatherID + "_" + fileDataID + ".wwf");
                }
            }

            // todo: parse wwf
        }
    }
}
