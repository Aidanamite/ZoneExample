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

        public override void PreLoad()
        {
            prefabParent = new GameObject("").transform;
            Object.DontDestroyOnLoad(prefabParent);
            prefabParent.gameObject.SetActive(false);
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
    static class AmbianceIds
    {
        public static readonly AmbianceDirector.Zone EXAMPLE;
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
            Object.Instantiate(Main.zonePrefab, Vector3.zero, new Quaternion(0,0,0,1));
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
}