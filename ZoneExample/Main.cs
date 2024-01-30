using HarmonyLib;
using SRML;
using SRML.Console;
using SRML.SR;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using SRML.Utils.Enum;
using MonomiPark.SlimeRancher.Regions;
using System.Reflection.Emit;
using Console = SRML.Console.Console;
using Object = UnityEngine.Object;

namespace ZoneExample
{
    public class Main : ModEntryPoint
    {
        internal static Assembly modAssembly = Assembly.GetExecutingAssembly();
        internal static string modName = $"{modAssembly.GetName().Name}";
        internal static string modDir = $"{Environment.CurrentDirectory}\\SRML\\Mods\\{modName}";
        internal static Transform prefabParent;
        internal static GameObject zonePrefab;
        internal static Action<GameObject> onZoneInit;

        public override void PreLoad()
        {
            prefabParent = new GameObject("").transform;
            Object.DontDestroyOnLoad(prefabParent);
            prefabParent.gameObject.SetActive(false);
            ZoneDirector.zonePediaIdLookup.Add(ZoneIds.EXAMPLE, PediaIds.ZONE_EXAMPLE);
            PediaUI.WORLD_ENTRIES = PediaUI.WORLD_ENTRIES.AddToArray(PediaIds.ZONE_EXAMPLE);
            TranslationPatcher.AddTranslationKey("global", "l.presence.example", "Messing around in an example zone");
            TranslationPatcher.AddPediaTranslation("t.zone_example", "An Example");
            TranslationPatcher.AddPediaTranslation("m.intro.zone_example", "An Example Intro");
            TranslationPatcher.AddPediaTranslation("m.desc.zone_example", "An Example Description");
            RichPresence.Director.RICH_PRESENCE_ZONE_LOOKUP.Add(ZoneIds.EXAMPLE, "example");
            zonePrefab = new GameObject("zoneEXAMPLE");
            zonePrefab.transform.SetParent(prefabParent);
            var zone = zonePrefab.AddComponent<ZoneDirector>();
            zone.zone = ZoneIds.EXAMPLE;
            zone.maxCrates = 0;
            zone.minCrates = 0;
            zonePrefab.AddComponent<IdDirector>();

            var cellObj = new GameObject("cellExample_main");
            cellObj.transform.SetParent(zonePrefab.transform, false);
            var cell = cellObj.AddComponent<CellDirector>();
            cell.ambianceZone = AmbianceIds.EXAMPLE;
            cell.notShownOnMap = true;
            cell.targetSlimeCount = 5;
            cell.cullSlimesLimit = 25;
            cell.minPerSpawn = 1;
            cell.maxPerSpawn = 5;
            var region = cellObj.AddComponent<Region>();
            region.bounds = new Bounds(new Vector3(435.5f, 5.775f, 1.6f), new Vector3(60,10,60));
            var root = region.root = new GameObject("Sector");
            root.transform.SetParent(region.transform, false);

            var mesh = new Mesh();
            mesh.vertices = new Vector3[] {
                    (Vector3.forward + Vector3.right) * 30f,
                    (-Vector3.forward + Vector3.right) * 30f,
                    (-Vector3.forward - Vector3.right) * 30f,
                    (Vector3.forward - Vector3.right) * 30f
                };
            mesh.triangles = new int[] { 0, 1, 3, 1, 2, 3 };
            mesh.uv = new Vector2[] {
                    Vector2.up,
                    Vector2.up + Vector2.right,
                    Vector2.right,
                    Vector2.zero
                };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            var cellGround = new GameObject("Ground");
            cellGround.transform.SetParent(root.transform,false);
            cellGround.AddComponent<MeshFilter>().sharedMesh = mesh;
            cellGround.AddComponent<MeshCollider>().sharedMesh = mesh;
            cellGround.transform.position = new Vector3(435.5f, 0.75f, 1.6f);


            var siteId = SRML.SR.SaveSystem.ModdedStringRegistry.ClaimID("site", "1");
            var gordoId = SRML.SR.SaveSystem.ModdedStringRegistry.ClaimID("gordo", "1");
            var podId = SRML.SR.SaveSystem.ModdedStringRegistry.ClaimID("pod", "1");
            var gateId = SRML.SR.SaveSystem.ModdedStringRegistry.ClaimID("gate", "1");

            onZoneInit = (o) =>
            {
                var ground = o.transform.Find("cellExample_main/Sector/Ground");

                var site = Resources.FindObjectsOfTypeAll<GadgetSite>().First((x) => !x.name.EndsWith("(Clone)"));
                var newSite = Object.Instantiate(site, ground);
                newSite.name = "siteGadget";
                newSite.transform.localPosition = new Vector3(20, 0, 0);
                newSite.transform.localRotation = Quaternion.Euler(0, -90, 0);
                o.GetComponent<IdDirector>().persistenceDict.Add(newSite.GetComponent<GadgetSite>(), siteId);

                var gordo = Resources.FindObjectsOfTypeAll<GordoEat>().First((x) => x.name == "gordoPink");
                var newGordo = Object.Instantiate(gordo, ground).GetComponent<GordoEat>();
                newGordo.name = "gordoPink";
                newGordo.transform.localPosition = new Vector3(0, 0, 20);
                newGordo.transform.localRotation = Quaternion.Euler(0, 180, 0);
                o.GetComponent<IdDirector>().persistenceDict.Add(newGordo, gordoId);
                Object.DestroyImmediate(newGordo.GetComponent<GordoRewardsBase>());
                var newRewards = newGordo.gameObject.AddComponent<CustomGordoRewards>();
                newRewards.rewards = new Identifiable.Id[] {
                    Identifiable.Id.KEY, Identifiable.Id.GOLD_SLIME, Identifiable.Id.GOLD_SLIME
                };
                newRewards.slimeSpawnFXPrefab = SceneContext.Instance.fxPool.pooledObjects.Keys.First((x) => x.name == "FX Slime Spawn");
                newRewards.slimePrefab = GameContext.Instance.LookupDirector.GetPrefab(Identifiable.Id.POGO_FRUIT);
                newGordo.targetCount = 100;

                var pod = Resources.FindObjectsOfTypeAll<TreasurePod>().First((x) => x.name == "treasurePod Rank1");
                var newPod = Object.Instantiate(pod, ground).GetComponent<TreasurePod>();
                newPod.name = "treasurePod Rank1";
                newPod.transform.localPosition = new Vector3(0, 0, -20);
                newPod.transform.localRotation = Quaternion.Euler(-15, 0, 0);
                o.GetComponent<IdDirector>().persistenceDict.Add(newPod, podId);
                newPod.spawnObjs = new []
                {
                    GameContext.Instance.LookupDirector.GetPrefab(Identifiable.Id.GOLD_SLIME),
                    GameContext.Instance.LookupDirector.GetPrefab(Identifiable.Id.GOLD_SLIME),
                    GameContext.Instance.LookupDirector.GetPrefab(Identifiable.Id.TARR_SLIME)
                };

                var gate = Resources.FindObjectsOfTypeAll<AccessDoor>().First((x) => x.name == "doorSlimeGate02");
                var newGate = Object.Instantiate(gate, ground).GetComponent<AccessDoor>();
                newGate.name = "doorSlimeGate02";
                newGate.transform.localPosition = new Vector3(-30, 0, 5);
                newGate.transform.localRotation = Quaternion.Euler(0, -90, 0);
                newGate.lockedRegionId = PediaIds.ZONE_EXAMPLE;
                newGate.progress = new ProgressDirector.ProgressType[] { ProgressDirector.ProgressType.SLIME_DOORS, ProgressIds.UNLOCK_EXAMPLE };
                newGate.linkedDoors = new AccessDoor[0];
                o.GetComponent<IdDirector>().persistenceDict.Add(newGate.GetComponent<AccessDoor>(), gateId);
            };

            HarmonyInstance.PatchAll();
        }

        public override void PostLoad()
        {
            var mat = Resources.FindObjectsOfTypeAll<Material>().First((x) => x.name == "objRockReef01").Clone();
            mat.name = "ExampleMaterial";
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, Color.grey);
            t.Apply();
            mat.SetTexture("_Depth", t);
            mat.SetTexture("_PrimaryTex", LoadImage("image.png"));
            var ground = zonePrefab.transform.Find("cellExample_main/Sector/Ground");
            ground.gameObject.AddComponent<MeshRenderer>().material = mat;

            var spawnerObj = new GameObject("spawner");
            spawnerObj.transform.SetParent(ground,false);
            var spawner = spawnerObj.AddComponent<DirectedSlimeSpawner>();
            spawner.spawnFX = SceneContext.Instance.fxPool.pooledObjects.Keys.First((x) => x.name == "FX QuantumWarpIn"); 
            spawner.slimeSpawnFX = SceneContext.Instance.fxPool.pooledObjects.Keys.First((x) => x.name == "FX Slime Spawn");
            spawner.spawnLocs = new GameObject[] { new GameObject("SpawnPoint1"), new GameObject("SpawnPoint2"), new GameObject("SpawnPoint3"), new GameObject("SpawnPoint4") };
            foreach (var g in spawner.spawnLocs)
                g.transform.SetParent(spawner.transform,false);
            spawner.spawnLocs[0].transform.localPosition = new Vector3(-25, 0, -25);
            spawner.spawnLocs[1].transform.localPosition = new Vector3(-15, 0, -25);
            spawner.spawnLocs[2].transform.localPosition = new Vector3(-25, 0, -15);
            spawner.spawnLocs[3].transform.localPosition = new Vector3(-15, 0, -15);
            spawner.radius = 2;
            spawner.constraints = new DirectedActorSpawner.SpawnConstraint[]
            {
                new DirectedActorSpawner.SpawnConstraint()
                {
                    feral = false,
                    maxAgitation = false,
                    slimeset = new SlimeSet() { members = new SlimeSet.Member[]
                    {
                        new SlimeSet.Member()
                        {
                            prefab = GameContext.Instance.LookupDirector.GetPrefab(Identifiable.Id.FIRE_SLIME),
                            weight = 0.9f
                        },
                        new SlimeSet.Member()
                        {
                            prefab = GameContext.Instance.LookupDirector.GetPrefab(Identifiable.Id.PUDDLE_SLIME),
                            weight = 0.1f
                        }
                    }},
                    weight = 1,
                    window = new DirectedActorSpawner.TimeWindow() { timeMode = DirectedActorSpawner.TimeMode.DAY }
                },
                new DirectedActorSpawner.SpawnConstraint()
                {
                    feral = false,
                    maxAgitation = false,
                    slimeset = new SlimeSet() { members = new SlimeSet.Member[]
                    {
                        new SlimeSet.Member()
                        {
                            prefab = GameContext.Instance.LookupDirector.GetPrefab(Identifiable.Id.FIRE_SLIME),
                            weight = 0.1f
                        },
                        new SlimeSet.Member()
                        {
                            prefab = GameContext.Instance.LookupDirector.GetPrefab(Identifiable.Id.PUDDLE_SLIME),
                            weight = 0.2f
                        }
                    }},
                    weight = 1,
                    window = new DirectedActorSpawner.TimeWindow() { timeMode = DirectedActorSpawner.TimeMode.DAY }
                }
            };
        }
        public static void Log(string message) => Console.Log($"[{modName}]: " + message);
        public static void LogError(string message) => Console.LogError($"[{modName}]: " + message);
        public static void LogWarning(string message) => Console.LogWarning($"[{modName}]: " + message);
        public static void LogSuccess(string message) => Console.LogSuccess($"[{modName}]: " + message);

        internal static Texture2D LoadImage(string filename)
        {
            var spriteData = modAssembly.GetManifestResourceStream(modName + "." + filename);
            var rawData = new byte[spriteData.Length];
            spriteData.Read(rawData, 0, rawData.Length);
            var tex = new Texture2D(1, 1);
            tex.LoadImage(rawData);
            tex.name = modName + "." + System.IO.Path.GetFileNameWithoutExtension(filename);
            return tex;
        }
    }

    class CustomGordoRewards : GordoRewardsBase
    {
        public Identifiable.Id[] rewards;
        protected override IEnumerable<GameObject> SelectActiveRewardPrefabs()
        {
            var r = new GameObject[rewards.Length];
            for (int i = 0; i < r.Length; i++)
                r[i] = GameContext.Instance.LookupDirector.GetPrefab(rewards[i]);
            return r;
        }
    }

    static class ExtentionMethods
    {
        public static Sprite CreateSprite(this Texture2D texture) => Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1);
        public static Material Clone(this Material material)
        {
            var m = new Material(material);
            m.CopyPropertiesFromMaterial(material);
            return m;
        }
    }

    [EnumHolder]
    static class ZoneIds
    {
        public static readonly ZoneDirector.Zone EXAMPLE;
    }

    [EnumHolder]
    static class RegionSetIds
    {
        public static readonly RegionRegistry.RegionSetId EXAMPLE;
    }

    [EnumHolder]
    static class PediaIds
    {
        public static readonly PediaDirector.Id ZONE_EXAMPLE;
    }

    [EnumHolder]
    static class AmbianceIds
    {
        public static readonly AmbianceDirector.Zone EXAMPLE;
    }

    [EnumHolder]
    static class ProgressIds
    {
        public static readonly ProgressDirector.ProgressType UNLOCK_EXAMPLE;
    }

    [HarmonyPatch(typeof(ZoneDirector), "GetRegionSetId" )]
    class Patch_GetZoneRegion
    {
        static bool Prefix(ZoneDirector.Zone zone, ref RegionRegistry.RegionSetId __result)
        {
            if (zone == ZoneIds.EXAMPLE)
            {
                __result = RegionSetIds.EXAMPLE;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PediaDirector),"Awake")]
    class Patch_PediaInit
    {
        static void Prefix(PediaDirector __instance)
        {
            __instance.entries = __instance.entries.AddToArray(new PediaDirector.IdEntry()
            {
                icon = Main.LoadImage("icon.png").CreateSprite(),
                id = PediaIds.ZONE_EXAMPLE
            });
        }
    }

    [HarmonyPatch(typeof(AutoSaveDirector), "BeginSceneSwitch")]
    class Patch_BeginWorldSceneLoad
    {
        static void Prefix()
        {
            SceneContext.onSceneLoaded += OnSceneLoaded;
        }
        static void OnSceneLoaded(SceneContext context)
        {
            SceneContext.onSceneLoaded -= OnSceneLoaded;
            var obj = new GameObject("");
            obj.SetActive(false);
            var zone = Object.Instantiate(Main.zonePrefab, Vector3.zero, new Quaternion(0,0,0,1), obj.transform);
            Main.onZoneInit(zone);
            zone.transform.SetParent(null, true);
        }
    }

    [HarmonyPatch(typeof(AmbianceDirector), "Awake")]
    class Patch_AmbianceInit
    {
        static void Prefix(AmbianceDirector __instance)
        {
            var setting = ScriptableObject.CreateInstance<AmbianceDirectorZoneSetting>();
            setting.dayAmbientColor = Color.red;
            setting.dayFogColor = Color.red;
            setting.dayFogDensity = 0.01f;
            setting.daySkyColor = Color.red;
            setting.daySkyHorizon = Color.black;
            setting.nightFogDensity = 0.05f;
            setting.nightAmbientColor = Color.black;
            setting.nightFogColor = Color.green;
            setting.nightSkyColor = Color.black;
            setting.nightSkyHorizon = Color.black;
            setting.zone = AmbianceIds.EXAMPLE;

            __instance.zoneSettings = __instance.zoneSettings.AddToArray(setting);
        }
    }

    [HarmonyPatch(typeof(Discord.RichPresenceHandlerImpl), "SetRichPresence", typeof(RichPresence.InZoneData))]
    class Patch_DiscordZonePresence
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            code.Insert(code.FindIndex((x) => x.opcode == OpCodes.Ldstr && (string)x.operand == "zone-{0}-large") + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_DiscordZonePresence),"CorrectImage")));
            return code;
        }
        static string CorrectImage(string original)
        {
            if (original == "example")
                return "ranch";
            return original;
        }
    }

    [HarmonyPatch(typeof(PlayerZoneTracker), "OnEntered")]
    class Patch_ZoneTracker
    {
        static void Postfix(ZoneDirector.Zone zone)
        {
            if (zone == ZoneIds.EXAMPLE)
                SceneContext.Instance.PediaDirector.MaybeShowPopup(PediaIds.ZONE_EXAMPLE);
        }
    }
}