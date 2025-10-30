using DBCD;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using WoWNamingLib.Namers;

namespace WoWNamingLib.Utils
{
    public static class SceneScriptParser
    {
        public static bool DebugOutput = true;
        private static IDBCDStorage sceneScriptPackageDB;
        private static IDBCDStorage sceneScriptPackageMemberDB;
        private static IDBCDStorage sceneScriptDB;
        private static IDBCDStorage sceneScriptTextDB;

        public static string CompilePackage(uint sceneScriptPackageID, string name = "", bool includeSubPackages = true)
        {
            Console.WriteLine("Compiling script for SceneScriptPackageID " + sceneScriptPackageID + ": " + name);

            if (sceneScriptPackageDB == null)
                sceneScriptPackageDB = Namer.LoadDBC("SceneScriptPackage");

            if (sceneScriptPackageMemberDB == null)
                sceneScriptPackageMemberDB = Namer.LoadDBC("SceneScriptPackageMember");

            var package = new StringBuilder();
            //package.Append("\n\n-- WoW.tools debug output: Start of package " + sceneScriptPackageID + "\n");

            var members = sceneScriptPackageMemberDB.Values.Where(x => (int)x["SceneScriptPackageID"] == (int)sceneScriptPackageID).OrderBy(x => (int)x["OrderIndex"]).ToList();

            foreach (var member in members)
            {
                var childPackageID = (int)member["ChildSceneScriptPackageID"];
                if (childPackageID != 0 && includeSubPackages)
                {
                    package.Append(CompilePackage((uint)childPackageID) + "\n");
                    continue;
                }

                var sceneScriptID = (int)member["SceneScriptID"];
                if (sceneScriptID == 0)
                    continue;

                var script = CompileScript(sceneScriptID);
                package.Append(script);
            }

            //package.Append("\n\n--WoW.tools debug output: End of package " + sceneScriptPackageID + "\n\n");

            return package.ToString();
        }

        public static string CompileScript(int sceneScriptID)
        {
            if (sceneScriptDB == null)
                sceneScriptDB = Namer.LoadDBC("SceneScript");

            if (sceneScriptTextDB == null)
                sceneScriptTextDB = Namer.LoadDBC("SceneScriptText");

            var script = new StringBuilder();

            var sceneScriptRow = sceneScriptDB[sceneScriptID];
            var sceneScriptTextRow = sceneScriptTextDB[sceneScriptID];

            // script.Append("\n\n-- WoW.tools debug output: SceneScript name: " + (string)sceneScriptTextRow["Name"] + "\n\n");

            script.Append(sceneScriptTextRow["Script"]);
            while (int.Parse(sceneScriptRow["NextSceneScriptID"].ToString()) != 0)
            {
                if (!sceneScriptDB.ContainsKey(int.Parse(sceneScriptRow["NextSceneScriptID"].ToString())))
                {
                    script.Append("\n\n-- WoW.tools debug output: !!! SceneScript ID " + int.Parse(sceneScriptRow["NextSceneScriptID"].ToString()) + " not found, possibly encrypted\n\n");
                    continue;
                }

                sceneScriptTextRow = sceneScriptTextDB[int.Parse(sceneScriptRow["NextSceneScriptID"].ToString())];
                sceneScriptRow = sceneScriptDB[int.Parse(sceneScriptRow["NextSceneScriptID"].ToString())];

                script.Append(sceneScriptTextRow["Script"]);
            }

            return script.ToString();
        }

        public static Dictionary<string, TimelineScene> ParseTimelineScript(string sceneScript)
        {
            var timelineScripts = new Dictionary<string, TimelineScene>();

            System.Globalization.CultureInfo greatestCulture = (System.Globalization.CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            greatestCulture.NumberFormat.NumberDecimalSeparator = ".";
            Thread.CurrentThread.CurrentCulture = greatestCulture;

            var prepend = @"
function wid(id)
return id
end

function cdiid(id)
return id
end

function fid(id)
return id
end

function cid(id)
return id
end

function gdi(id)
return id
end

function iid(id)
return id
end

function skid(id)
return id
end

function adid(id)
return id
end

function cmid(id)
return id
end

function ceid(id)
return id
end

function dpid(id)
return id
end

function btid(id)
return id
end

function seid(id)
return id
end

function akid(id)
    return id
end

function svid(id)
    return id
end

local sceneFileData = {}
local sceneInputData = {}

function SceneTimelineAddFileData(name, table)
sceneFileData[name] = table 
end

function SceneTimlineAddInputData(name, table)
sceneInputData[name] = table 
end

function SceneTimelineAddInputData(name, table)
sceneInputData[name] = table 
end
            ";

            var script = new Script();

            try
            {
                // TODO: We only parse sceneFileData for now
                var mergedScript = prepend + "\n " + sceneScript + " \n return sceneFileData\n";
                try
                {
                    var rootScript = script.DoString(mergedScript);

                    foreach (var val in rootScript.Table.Keys)
                    {
                        var timelineScene = new TimelineScene();

                        // Don't crash on doc scenes
                        if (val.Type == DataType.Void)
                        {
                            timelineScripts.Add(val.String, timelineScene);
                            continue;
                        }

                        var table = rootScript.Table.Get(val).ToObject<Table>();
                        foreach (var key in table.Keys)
                        {
                            if (key.String == "actors")
                            {
                                timelineScene.actors = ParseActors(table.Get(key).ToObject<Table>());
                            }
                            else
                            {
                                Console.WriteLine("Unhandled root key " + key.String + ", assuming actors");
                                timelineScene.actors = ParseActors(table);
                            }
                        }

                        timelineScripts.Add(val.String, timelineScene);
                    }

                }
                catch (SyntaxErrorException e)
                {
                    Console.WriteLine("Syntax error during parsing SceneScript: " + e.DecoratedMessage);
                }
                catch (ScriptRuntimeException e)
                {
                    Console.WriteLine("Runtime error during running SceneScript: " + e.DecoratedMessage);
                }
            }
            catch (ScriptRuntimeException ex)
            {
                Console.WriteLine("Doh! An error occured! {0}", ex.DecoratedMessage);
            }

            return timelineScripts;
        }

        static Dictionary<string, Actor> ParseActors(Table table)
        {
            var actors = new Dictionary<string, Actor>();
            foreach (var actorName in table.Keys)
            {
                var actor = new Actor();

                //Console.WriteLine("New actor: " + actorName);

                var subTable = table.Get(actorName).ToObject<Table>();
                if (subTable.Keys.Count() != 1 || subTable.Keys.First().String != "properties")
                    throw new Exception("Actor table has unexpected number of keys (" + subTable.Keys.Count() + "), or the key is not 'properties' (got " + subTable.Keys.First().String + ")");

                actor.properties = new ActorProperties();

                var propertiesTable = subTable.Get("properties").ToObject<Table>();
                foreach (var propertyKey in propertiesTable.Keys)
                {
                    var propertyTable = propertiesTable.Get(propertyKey).ToObject<Table>();
                    if (propertyTable.Keys.Count() != 1 || propertyTable.Keys.First().String != "events")
                        throw new Exception("Property table has unexpected number of keys (" + propertyTable.Keys.Count() + "), or the key is not 'events' (got " + propertyTable.Keys.First().String + ")");

                    switch (propertyKey.String)
                    {
                        case "Appearance":
                            actor.properties.Appearance = ParseAppearanceProperty(propertyTable.Get("events").ToObject<Table>());
                            break;
                        case "Broadcast Text":
                            actor.properties.BroadcastText = ParseBroadcastTextProperty(propertyTable.Get("events").ToObject<Table>());
                            break;
                        case "CustomScript":
                            actor.properties.CustomScript = ParseCustomScriptProperty(propertyTable.Get("events").ToObject<Table>());
                            break;
                        case "EquipWeapon":
                            actor.properties.EquipWeapon = ParseEquipWeaponProperty(propertyTable.Get("events").ToObject<Table>());
                            break;
                        case "Fade":
                            actor.properties.Fade = ParseFadeProperty(propertyTable.Get("events").ToObject<Table>());
                            break;
                        case "FadeRegion":
                            actor.properties.FadeRegion = ParseFadeRegionProperty(propertyTable.Get("events").ToObject<Table>());
                            break;
                        case "GroundSnap":
                            actor.properties.GroundSnap = ParseGroundSnapProperty(propertyTable.Get("events").ToObject<Table>());
                            break;
                        case "MoveSpline":
                            actor.properties.MoveSpline = ParseMoveSplineProperty(propertyTable.Get("events").ToObject<Table>());
                            break;
                        case "Music":
                            actor.properties.Music = ParseMusicProperty(propertyTable.Get("events").ToObject<Table>());
                            break;
                        case "Scale":
                            actor.properties.Scale = ParseScaleProperty(propertyTable.Get("events").ToObject<Table>());
                            break;
                        case "Sheathe":
                            actor.properties.Sheathe = ParseSheatheProperty(propertyTable.Get("events").ToObject<Table>());
                            break;
                        case "SoundKit":
                            actor.properties.SoundKit = ParseSoundKitProperty(propertyTable.Get("events").ToObject<Table>());
                            break;
                        case "Transform":
                            actor.properties.Transform = ParseTransformProperty(propertyTable.Get("events").ToObject<Table>());
                            break;
                        default:
                            if (!DebugOutput)
                                break;

                            Console.WriteLine("Unhandled property: " + propertyKey.String);
                            ParseEvents(propertyTable.Get("events").ToObject<Table>());
                            break;
                    }
                }

                actors.Add(actorName.ToString().Replace("\"", ""), actor);
            }

            return actors;
        }

        static MusicProperty ParseMusicProperty(Table table)
        {
            var musicProperty = new MusicProperty { events = [] };

            foreach (var key in table.Keys)
            {
                foreach (var eventKey in table.Get(key).ToObject<Table>().Keys)
                {
                    var eventTime = (float)eventKey.Number;
                    var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                    musicProperty.events.Add(eventTime, new MusicEvent { soundKitID = int.Parse(subTable["soundKitID"].ToString()) });
                }
            }

            return musicProperty;
        }
        static GroundSnapProperty ParseGroundSnapProperty(Table table)
        {
            var groundSnapProperty = new GroundSnapProperty { events = [] };

            foreach (var key in table.Keys)
            {
                foreach (var eventKey in table.Get(key).ToObject<Table>().Keys)
                {
                    var eventTime = (float)eventKey.Number;
                    var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                    groundSnapProperty.events.TryAdd(eventTime, new GroundSnapEvent { snap = bool.Parse(subTable["snap"].ToString()) });
                }
            }

            return groundSnapProperty;
        }
        static TransformProperty ParseTransformProperty(Table table)
        {
            var property = new TransformProperty { events = [] };

            foreach (var key in table.Keys)
            {
                foreach (var eventKey in table.Get(key).ToObject<Table>().Keys)
                {
                    var eventTime = (float)eventKey.Number;
                    var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                    property.events.Add(eventTime, ParseTransform(((Table)subTable["transform"])));
                }
            }

            return property;
        }
        static AppearanceProperty ParseAppearanceProperty(Table table)
        {
            var property = new AppearanceProperty { events = [] };

            foreach (var key in table.Keys)
            {
                foreach (var eventKey in table.Get(key).ToObject<Table>().Keys)
                {
                    var appearanceEvent = new AppearanceEvent();
                    var eventTime = (float)eventKey.Number;
                    var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                    foreach (var propertyKey in subTable.Keys)
                    {
                        var value = subTable.Get(propertyKey);
                        switch (propertyKey.String)
                        {
                            case "creatureID":
                                appearanceEvent.creatureID = new CreatureID { ID = int.Parse(value.ToString().Replace("cid(", "").Replace(")", "")) };
                                break;
                            case "modelFileID":
                                appearanceEvent.modelFileID = new ModelFileID { ID = int.Parse(value.ToString().Replace("cid(", "").Replace(")", "")) };
                                break;
                            case "creatureDisplaySetIndex":
                                appearanceEvent.creatureDisplaySetIndex = int.Parse(value.ToString());
                                break;
                            case "creatureDisplayInfoID":
                                appearanceEvent.creatureDisplayInfoID = int.Parse(value.ToString());
                                break;
                            case "fileDataID":
                                appearanceEvent.fileDataID = new FileDataID { ID = int.Parse(value.ToString().Replace("fid(", "").Replace(")", "")) };
                                break;
                            case "wmoGameObjectDisplayID":
                                appearanceEvent.wmoGameObjectDisplayID = new GameObjectDisplayInfoID { ID = int.Parse(value.ToString().Replace("gdi(", "").Replace(")", "")) };
                                break;
                            case "itemID":
                                appearanceEvent.itemID = new ItemID { ID = int.Parse(value.ToString().Replace("iid(", "").Replace(")", "")) };
                                break;
                            case "isPlayerClone":
                                appearanceEvent.isPlayerClone = bool.Parse(value.ToString());
                                break;
                            case "isPlayerCloneNative":
                                appearanceEvent.isPlayerCloneNative = bool.Parse(value.ToString());
                                break;
                            case "playerSummon":
                                appearanceEvent.playerSummon = bool.Parse(value.ToString());
                                break;
                            case "dragonRidingMount":
                                appearanceEvent.dragonRidingMount = bool.Parse(value.ToString());
                                break;
                            case "playerGroupIndex":
                                appearanceEvent.playerGroupIndex = int.Parse(value.ToString());
                                break;
                            case "smoothPhase":
                                appearanceEvent.smoothPhase = bool.Parse(value.ToString());
                                break;
                            default:
                                throw new Exception("!!! Unhandled property: " + propertyKey.String);
                        }
                    }

                    property.events.TryAdd(eventTime, appearanceEvent);
                }
            }

            return property;
        }

        static BroadcastTextProperty ParseBroadcastTextProperty(Table table)
        {
            var property = new BroadcastTextProperty { events = [] };

            foreach (var key in table.Keys)
            {
                foreach (var eventKey in table.Get(key).ToObject<Table>().Keys)
                {
                    var broadcastTextEvent = new BroadcastTextEvent();
                    var eventTime = (float)eventKey.Number;
                    var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                    foreach (var propertyKey in subTable.Keys)
                    {
                        var value = subTable.Get(propertyKey);
                        switch (propertyKey.String)
                        {
                            case "broadcastTextID":
                                broadcastTextEvent.broadcastTextID = new BroadcastTextID { ID = int.Parse(value.ToString().Replace("btid(", "").Replace(")", "")) };
                                break;
                            case "target":
                                broadcastTextEvent.target = value.ToString();
                                break;
                            case "type":
                                broadcastTextEvent.type = int.Parse(value.ToString());
                                break;
                            case "stereoAudio":
                                broadcastTextEvent.stereoAudio = bool.Parse(value.ToString());
                                break;
                            default:
                                throw new Exception("!!! Unhandled property: " + propertyKey.String);
                        }
                    }

                    property.events.TryAdd(eventTime, broadcastTextEvent);
                }
            }

            return property;
        }

        static MoveSplineProperty ParseMoveSplineProperty(Table table)
        {
            var property = new MoveSplineProperty { events = [] };

            foreach (var key in table.Keys)
            {
                foreach (var eventKey in table.Get(key).ToObject<Table>().Keys)
                {
                    if (eventKey.Type == DataType.Table)
                    {
                        var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                        foreach (var propertyKey in subTable.Keys)
                        {
                            var value = subTable.Get(propertyKey);
                            switch (propertyKey.String)
                            {
                                case "overrideSpeed":
                                    property.overrideSpeed = float.Parse(value.ToString());
                                    break;
                                case "useModelRunSpeed":
                                    property.useModelRunSpeed = bool.Parse(value.ToString());
                                    break;
                                case "useModelWalkSpeed":
                                    property.useModelWalkSpeed = bool.Parse(value.ToString());
                                    break;
                                case "yawUsesSplineTangent":
                                    property.yawUsesSplineTangent = bool.Parse(value.ToString());
                                    break;
                                case "yawUsesNodeTransform":
                                    property.yawUsesNodeTransform = bool.Parse(value.ToString());
                                    break;
                                case "yawBlendDisabled":
                                    property.yawBlendDisabled = bool.Parse(value.ToString());
                                    break;
                                case "pitchUsesSplineTangent":
                                    property.pitchUsesSplineTangent = bool.Parse(value.ToString());
                                    break;
                                case "pitchUsesNodeTransform":
                                    property.pitchUsesNodeTransform = bool.Parse(value.ToString());
                                    break;
                                case "rollUsesNodeTransform":
                                    property.rollUsesNodeTransform = bool.Parse(value.ToString());
                                    break;
                                default:
                                    throw new Exception("!!! Unhandled property: " + propertyKey.String);
                            }
                        }
                    }
                    else if (eventKey.Type == DataType.Number)
                    {
                        var eventTime = (float)eventKey.Number;
                        var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                        property.events.TryAdd(eventTime, ParseTransform(((Table)subTable["position"])));
                    }
                }
            }

            return property;
        }
        static CustomScriptProperty ParseCustomScriptProperty(Table table)
        {
            var property = new CustomScriptProperty { events = [] };

            foreach (var key in table.Keys)
            {
                foreach (var eventKey in table.Get(key).ToObject<Table>().Keys)
                {
                    var eventTime = (float)eventKey.Number;
                    var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                    property.events.Add(eventTime, new CustomScriptEvent { script = subTable["script"].ToString() });
                }
            }

            return property;
        }
        static ScaleProperty ParseScaleProperty(Table table)
        {
            var property = new ScaleProperty { events = [] };

            foreach (var key in table.Keys)
            {
                foreach (var eventKey in table.Get(key).ToObject<Table>().Keys)
                {
                    var eventTime = (float)eventKey.Number;
                    var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                    property.events.TryAdd(eventTime, new ScaleEvent { scale = float.Parse(subTable["scale"].ToString()), duration = float.Parse(subTable["duration"].ToString()) });
                }
            }

            return property;
        }
        static FadeProperty ParseFadeProperty(Table table)
        {
            var property = new FadeProperty { events = [] };

            foreach (var key in table.Keys)
            {
                foreach (var eventKey in table.Get(key).ToObject<Table>().Keys)
                {
                    var eventTime = (float)eventKey.Number;
                    var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                    property.events.TryAdd(eventTime, new FadeEvent { alpha = float.Parse(subTable["alpha"].ToString()), time = float.Parse(subTable["time"].ToString()) });
                }
            }

            return property;
        }
        static FadeRegionProperty ParseFadeRegionProperty(Table table)
        {
            var property = new FadeRegionProperty { events = [] };

            foreach (var key in table.Keys)
            {
                foreach (var eventKey in table.Get(key).ToObject<Table>().Keys)
                {
                    var eventTime = (float)eventKey.Number;
                    var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                    property.events.Add(eventTime,
                        new FadeRegionEvent
                        {
                            enabled = bool.Parse(subTable["enabled"].ToString()),
                            radius = float.Parse(subTable["radius"].ToString()),
                            includePlayer = bool.Parse(subTable["includePlayer"].ToString()),
                            excludePlayers = bool.Parse(subTable["excludePlayers"].ToString()),
                            excludeNonPlayers = bool.Parse(subTable["excludeNonPlayers"].ToString()),
                            includeSounds = bool.Parse(subTable["includeSounds"].ToString()),
                            includeWMOs = bool.Parse(subTable["includeWMOs"].ToString())
                        }
                    );
                }
            }

            return property;
        }

        static SheatheProperty ParseSheatheProperty(Table table)
        {
            var property = new SheatheProperty { events = [] };

            foreach (var key in table.Keys)
            {
                foreach (var eventKey in table.Get(key).ToObject<Table>().Keys)
                {
                    var eventTime = (float)eventKey.Number;
                    var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                    property.events.Add(eventTime,
                        new SheatheEvent
                        {
                            isSheathed = bool.Parse(subTable["isSheathed"].ToString()),
                            isRanged = bool.Parse(subTable["isRanged"].ToString()),
                            animated = bool.Parse(subTable["animated"].ToString()),
                        }
                    );
                }
            }

            return property;
        }

        static SoundKitProperty ParseSoundKitProperty(Table table)
        {
            var property = new SoundKitProperty { events = [] };

            foreach (var key in table.Keys)
            {
                var soundKitEvent = new SoundKitEvent() { timestamps = [] };

                foreach (var eventKey in table.Get(key).ToObject<Table>().Keys)
                {
                    if (eventKey.Type == DataType.String)
                    {
                        var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                        foreach (var propertyKey in subTable.Keys)
                        {
                            var value = subTable.Get(propertyKey);
                            switch (propertyKey.String)
                            {
                                case "soundKitID":
                                    soundKitEvent.soundKitID = int.Parse(value.ToString().Replace("skitid(", "").Replace(")", ""));
                                    break;
                                case "stereoAudio":
                                    soundKitEvent.stereoAudio = bool.Parse(value.ToString());
                                    break;
                                case "looping":
                                    soundKitEvent.looping = bool.Parse(value.ToString());
                                    break;
                                case "sourceActor":
                                    soundKitEvent.sourceActor = value.ToString();
                                    break;
                                default:
                                    throw new Exception("!!! Unhandled property: " + propertyKey.String);
                            }
                        }
                    }
                    else if (eventKey.Type == DataType.Number)
                    {
                        var eventTime = (float)eventKey.Number;
                        //var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                        soundKitEvent.timestamps.Add(eventTime);
                    }
                    else
                    {
                        throw new Exception("!!! Unhandled key type");
                    }
                }

                property.events.Add(soundKitEvent);
            }

            return property;
        }

        static EquipWeaponProperty ParseEquipWeaponProperty(Table table)
        {
            var property = new EquipWeaponProperty { events = [] };

            foreach (var key in table.Keys)
            {
                foreach (var eventKey in table.Get(key).ToObject<Table>().Keys)
                {
                    var eventTime = (float)eventKey.Number;
                    var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                    property.events.Add(eventTime,
                        new EquipWeaponEvent
                        {
                            itemID = int.Parse(subTable["itemID"].ToString()),
                            MainHand = bool.Parse(subTable["MainHand"].ToString()),
                            OffHand = bool.Parse(subTable["OffHand"].ToString()),
                            Ranged = bool.Parse(subTable["Ranged"].ToString()),
                        }
                    );
                }
            }

            return property;
        }

        static void ParseEvents(Table table)
        {
            foreach (var key in table.Keys)
            {
                Console.WriteLine("\t \t Event: " + key);
                foreach (var eventKey in table.Get(key).ToObject<Table>().Keys)
                {
                    var subTable = table.Get(key).ToObject<Table>().Get(eventKey).ToObject<Table>();
                    Console.WriteLine("\t \t \t " + eventKey);
                    foreach (var propertyKey in subTable.Keys)
                    {
                        var value = subTable.Get(propertyKey);
                        object formattedValue = null;
                        switch (value.Type)
                        {
                            case DataType.String:
                                formattedValue = value.String;
                                break;
                            case DataType.Boolean:
                                formattedValue = value.Boolean;
                                break;
                            case DataType.Number:
                                formattedValue = value.Number;
                                break;
                            case DataType.Table:
                                switch (propertyKey.String)
                                {
                                    case "targetOffset":
                                    case "transform":
                                    case "position": // This is almost always the same as transform as it has another "position" inside
                                        var tf = ParseTransform(value.Table);
                                        formattedValue = "XYZ: " + tf.Position.ToString() + ", yaw: " + tf.Yaw + ", pitch: " + tf.Pitch + ", roll: " + tf.Roll;
                                        break;
                                    case "offset":
                                        var pos = ParsePosition(value.Table);
                                        formattedValue = pos.X + " " + pos.Y + " " + pos.Z;
                                        break;
                                    default:
                                        throw new Exception("!!! Unhandled table type: " + propertyKey.String);
                                }
                                break;
                            default:
                                throw new Exception("!!! Unhandled property type: " + value.Type);
                        }

                        Console.WriteLine("\t \t \t \t " + propertyKey.String + " = " + formattedValue.ToString());
                    }
                }
            }
        }

        static TransformEvent ParseTransform(Table table)
        {
            var transform = new TransformEvent();

            if (table.Keys.Count() != 4 || table.Keys.First().String != "position")
                throw new Exception("Property table has unexpected number of keys (" + table.Keys.Count() + "), or the 1st key is not 'position' (got " + table.Keys.First().String + ")");

            transform.Position = ParsePosition(table.Get("position").ToObject<Table>());
            transform.Yaw = (float)table.Get("yaw").Number;
            transform.Pitch = (float)table.Get("pitch").Number;
            transform.Roll = (float)table.Get("roll").Number;

            return transform;
        }

        static Vector3 ParsePosition(Table table)
        {
            var position = new Vector3();

            if (table.Keys.Count() != 3 || table.Keys.First().String != "x")
                throw new Exception("Property table has unexpected number of keys (" + table.Keys.Count() + "), or the 1st key is not 'x' (got " + table.Keys.First().String + ")");

            position.X = (float)table.Get("x").Number;
            position.Y = (float)table.Get("y").Number;
            position.Z = (float)table.Get("z").Number;
            return position;
        }
    }

    public struct TimelineScene
    {
        public Dictionary<string, Actor> actors;
    }

    public struct Actor
    {
        public ActorProperties properties;
    }
    public struct ActorProperties
    {
        public AppearanceProperty? Appearance;
        public BroadcastTextProperty? BroadcastText;
        public CustomScriptProperty? CustomScript;
        public EquipWeaponProperty? EquipWeapon;
        public FadeProperty? Fade;
        public FadeRegionProperty? FadeRegion;
        public GroundSnapProperty? GroundSnap;
        public MusicProperty? Music;
        public MoveSplineProperty? MoveSpline;
        public ScaleProperty? Scale;
        public SheatheProperty? Sheathe;
        public SoundKitProperty? SoundKit;
        public TransformProperty? Transform;
    }

    public struct BroadcastTextProperty
    {
        public Dictionary<float, BroadcastTextEvent> events;
    }

    public struct BroadcastTextEvent
    {
        public BroadcastTextID broadcastTextID;
        public string target;
        public int type;
        public bool stereoAudio;
    }

    public struct SoundKitProperty
    {
        public List<SoundKitEvent> events;
    }

    public struct SoundKitEvent
    {
        public int soundKitID;
        public bool stereoAudio;
        public bool looping;
        public string sourceActor;
        public List<float> timestamps;
    }

    public struct GroundSnapProperty
    {
        public Dictionary<float, GroundSnapEvent> events;
    }
    public struct GroundSnapEvent
    {
        public bool snap;
    }

    public struct TransformProperty
    {
        public Dictionary<float, TransformEvent> events;
    }

    public struct TransformEvent
    {
        public Vector3 Position;
        public float Yaw;
        public float Pitch;
        public float Roll;
    }

    public struct MoveSplineProperty
    {
        public float overrideSpeed;
        public bool useModelRunSpeed;
        public bool useModelWalkSpeed;
        public bool yawUsesSplineTangent;
        public bool yawUsesNodeTransform;
        public bool yawBlendDisabled;
        public bool pitchUsesSplineTangent;
        public bool pitchUsesNodeTransform;
        public bool rollUsesNodeTransform;
        public Dictionary<float, TransformEvent> events;
    }

    public struct CustomScriptProperty
    {
        public Dictionary<float, CustomScriptEvent> events;
    }

    public struct CustomScriptEvent
    {
        public string script;
    }

    public struct FadeRegionProperty
    {
        public Dictionary<float, FadeRegionEvent> events;
    }

    public struct FadeRegionEvent
    {
        public bool enabled;
        public float radius;
        public bool includePlayer;
        public bool excludePlayers;
        public bool excludeNonPlayers;
        public bool includeSounds;
        public bool includeWMOs;
    }

    public struct MusicProperty
    {
        public Dictionary<float, MusicEvent> events;
    }

    public struct MusicEvent
    {
        public int soundKitID;
    }

    public struct AppearanceProperty
    {
        public Dictionary<float, AppearanceEvent> events;
    }

    public struct AppearanceEvent
    {
        public CreatureID creatureID;
        public ModelFileID modelFileID;
        public int creatureDisplaySetIndex;
        public int creatureDisplayInfoID;
        public FileDataID fileDataID;
        public GameObjectDisplayInfoID wmoGameObjectDisplayID;
        public ItemID itemID;
        public bool isPlayerClone;
        public bool isPlayerCloneNative;
        public bool playerSummon;
        public bool dragonRidingMount;
        public int playerGroupIndex;
        public bool smoothPhase;
    }
    public struct ScaleProperty
    {
        public Dictionary<float, ScaleEvent> events;
    }
    public struct ScaleEvent
    {
        public float scale;
        public float duration;
    }
    public struct FadeProperty
    {
        public Dictionary<float, FadeEvent> events;
    }
    public struct FadeEvent
    {
        public float alpha;
        public float time;
    }

    public struct SheatheProperty
    {
        public Dictionary<float, SheatheEvent> events;
    }
    public struct SheatheEvent
    {
        public bool isSheathed;
        public bool isRanged;
        public bool animated;
    }

    public struct EquipWeaponProperty
    {
        public Dictionary<float, EquipWeaponEvent> events;
    }
    public struct EquipWeaponEvent
    {
        public int itemID;
        public bool MainHand;
        public bool OffHand;
        public bool Ranged;
    }

    public struct CreatureID
    {
        public int ID;
    }
    public struct FileDataID
    {
        public int ID;
    }
    public struct GameObjectDisplayInfoID
    {
        public int ID;
    }
    public struct ItemID
    {
        public int ID;
    }

    public struct ModelFileID
    {
        public int ID;
    }

    public struct BroadcastTextID
    {
        public int ID;
    }
}
