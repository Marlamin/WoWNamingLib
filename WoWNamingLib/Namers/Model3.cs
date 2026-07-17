using WoWFormatLib.FileReaders;
using WoWNamingLib.Services;

namespace WoWNamingLib.Namers
{
    public static class Model3
    {
        public static void Name(List<int> fileDataIDs, bool forceFullRun = false)
        {
            foreach (int fdid in fileDataIDs)
            {
                Console.WriteLine("Naming M3 " + fdid);

                var encrypted = false;

                using (var ms = new MemoryStream())
                {
                    try
                    {
                        var file = CASCManager.GetFileByID((uint)fdid).Result;
                        file.CopyTo(ms);
                        ms.Position = 0;

                        var bin = new BinaryReader(ms);
                        if (bin.ReadUInt64() == 0)
                            encrypted = true;

                        ms.Position = 0;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error retrieving M3 " + fdid + ": " + e.Message);
                        continue;
                    }

                    try
                    {
                        var reader = new M3Reader();
                        reader.LoadM3(ms, loadSkins: false);

                        var model = reader.model;

                        if (!Namer.IDToNameLookup.TryGetValue(fdid, out var name))
                        {
                            var expansion = NewFileManager.GetExpansionForFileDataID((uint)fdid);
                            name = "models/unknown/unk_" + expansion + "_" + fdid + "/" + fdid + ".m3";
                            NewFileManager.AddNewFile(fdid, name, true);
                        }

                        var folder = Path.GetDirectoryName(name);

                        foreach (var instance in model.Instances.Instances)
                        {
                            var instanceName = Path.Combine(folder, fdid + "_" + instance.FileDataID + ".mtl3lib");
                            NewFileManager.AddNewFile(instance.FileDataID, instanceName, true);

                            if (instance.shaderData.SamplerTextureFileIDs != null && instance.shaderData.SamplerTextureFileIDs.Count > 0)
                            {
                                foreach (var texID in instance.shaderData.SamplerTextureFileIDs)
                                {
                                    var texName = Path.Combine(folder, fdid + "_" + texID + ".blp");
                                    NewFileManager.AddNewFile(texID, texName, true);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error naming M3 " + fdid + ": " + e.Message);
                    }
                }
            }
        }
    }
}
