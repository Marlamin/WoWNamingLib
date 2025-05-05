using System.Diagnostics;
using WoWNamingLib.Services;
using WoWNamingLib.Utils;

namespace WoWNamingLib.Namers
{
    class Interface
    {
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
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during MID naming: " + e.Message);
            }
        }
    }
}