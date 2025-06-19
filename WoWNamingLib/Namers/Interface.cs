using System.Diagnostics;
using System.Text;
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

                // 6067012 - interface/ui-code-list.txt
                var codeList = CASCManager.GetFileByID(6067012).Result;
                if(codeList != null)
                {
                    using(var ms = new MemoryStream())
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
                if(tocList != null)
                {
                    using(var ms = new MemoryStream())
                    {
                        tocList.CopyTo(ms);
                        ms.Position = 0;
                        var asText = Encoding.ASCII.GetString(ms.ToArray());
                        var lines = asText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var line in lines)
                            NewFileManager.AddNewFileByname(line.Replace("\\", "/"));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during MID naming: " + e.Message);
            }
        }
    }
}