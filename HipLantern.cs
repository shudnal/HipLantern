﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HipLantern
{
    [BepInPlugin(pluginID, pluginName, pluginVersion)]
    public class HipLantern : BaseUnityPlugin
    {
        const string pluginID = "shudnal.HipLantern";
        const string pluginName = "Hip Lantern";
        const string pluginVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(pluginID);

        internal static readonly ConfigSync configSync = new ConfigSync(pluginID) { DisplayName = pluginName, CurrentVersion = pluginVersion, MinimumRequiredVersion = pluginVersion };

        internal static HipLantern instance;

        private static ConfigEntry<bool> configLocked;
        private static ConfigEntry<bool> loggingEnabled;

        public static ConfigEntry<string> itemCraftingStation;
        public static ConfigEntry<int> itemMinStationLevel;
        public static ConfigEntry<string> itemRecipe;

        public static ConfigEntry<string> refuelCraftingStation;
        public static ConfigEntry<string> refuelRecipe;
        public static ConfigEntry<int> fuelMinutes;

        public static ConfigEntry<int> itemSlotType;
        public static ConfigEntry<bool> itemSlotUtility;

        public static ConfigEntry<Color> lightColor;

        public static ConfigEntry<float> lightIntensityOutdoors;
        public static ConfigEntry<float> lightRangeOutdoors;
        public static ConfigEntry<float> lightShadowsOutdoors;

        public static ConfigEntry<float> lightIntensityIndoors;
        public static ConfigEntry<float> lightRangeIndoors;
        public static ConfigEntry<float> lightShadowsIndoors;

        private const string c_rootObjectName = "_shudnalRoot";
        private const string c_rootPrefabsName = "Prefabs";

        private static GameObject rootObject;
        private static GameObject rootPrefabs;

        public static GameObject hipLanternPrefab;
        public static Sprite itemIcon;

        public static bool prefabInit = false;

        private void Awake()
        {
            harmony.PatchAll();

            instance = this;

            ConfigInit();
            _ = configSync.AddLockingConfigEntry(configLocked);

            Game.isModded = true;

            LoadIcons();
        }

        private void OnDestroy()
        {
            Config.Save();
            instance = null;
            harmony?.UnpatchSelf();
        }

        public static void LogInfo(object data)
        {
            if (loggingEnabled.Value)
                instance.Logger.LogInfo(data);
        }

        public void ConfigInit()
        {
            config("General", "NexusID", 2748, "Nexus mod ID for updates", false);

            configLocked = config("General", "Lock Configuration", defaultValue: true, "Configuration is locked and can be changed by server admins only");
            loggingEnabled = config("General", "Logging enabled", defaultValue: false, "Enable logging. [Not Synced with Server]", false);

            itemCraftingStation = config("Item", "Crafting station", defaultValue: "$piece_forge", "Station to craft item. Leave empty to craft with hands. Restart required to apply.");
            itemMinStationLevel = config("Item", "Crafting station level", defaultValue: 3, "Minimum level of station required to craft. Restart required to apply.");
            itemRecipe = config("Item", "Recipe", defaultValue: "SurtlingCore:3,BronzeNails:10,FineWood:4", "Item recipe. Restart required to apply.");

            refuelCraftingStation = config("Item - Fuel", "Crafting station", defaultValue: "", "Station to refuel item. Leave empty to refuel with hands. Restart required to apply.");
            refuelRecipe = config("Item - Fuel", "Recipe refuel", defaultValue: "SurtlingCore:1", "Item recipe for refueling. Restart required to apply.");
            fuelMinutes = config("Item - Fuel", "Fuel minutes", defaultValue: 360, "Time in minutes required to consume all fuel. Restart required to apply.");

            itemSlotType = config("Item - Slot", "Slot type", defaultValue: 56, "Custom item slot type");
            itemSlotUtility = config("Item - Slot", "Use utility slot", defaultValue: false, "Use utility slot");

            lightColor = config("Light", "Color", defaultValue: new Color(1f, 0.62f, 0.48f), "Color of lantern light");

            lightIntensityOutdoors = config("Light - Outdoors", "Intensity", defaultValue: 1f, "Intensity of light");
            lightRangeOutdoors = config("Light - Outdoors", "Range", defaultValue: 30f, "Range of light");
            lightShadowsOutdoors = config("Light - Outdoors", "Shadows strength", defaultValue: 0.8f, "Strength of shadows");

            lightIntensityIndoors = config("Light - Indoors", "Intensity", defaultValue: 0.8f, "Intensity of light");
            lightRangeIndoors = config("Light - Indoors", "Range", defaultValue: 25f, "Range of light");
            lightShadowsIndoors = config("Light - Indoors", "Shadows strength", defaultValue: 0.9f, "Strength of shadows");

            lightColor.SettingChanged += (sender, args) => LanternLightController.UpdateLightState();
            lightIntensityOutdoors.SettingChanged += (sender, args) => LanternLightController.UpdateLightState();
            lightRangeOutdoors.SettingChanged += (sender, args) => LanternLightController.UpdateLightState();
            lightShadowsOutdoors.SettingChanged += (sender, args) => LanternLightController.UpdateLightState();
            lightIntensityIndoors.SettingChanged += (sender, args) => LanternLightController.UpdateLightState();
            lightRangeIndoors.SettingChanged += (sender, args) => LanternLightController.UpdateLightState();
            lightShadowsIndoors.SettingChanged += (sender, args) => LanternLightController.UpdateLightState();
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, defaultValue, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, string description, bool synchronizedSetting = true) => config(group, name, defaultValue, new ConfigDescription(description), synchronizedSetting);

        private void LoadIcons()
        {
            LoadIcon("lantern.png", ref itemIcon);
        }

        private void LoadIcon(string filename, ref Sprite icon)
        {
            Texture2D tex = new Texture2D(2, 2);
            if (LoadTexture(filename, ref tex))
                icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        }

        private bool LoadTexture(string filename, ref Texture2D tex)
        {
            string fileInPluginFolder = Path.Combine(Paths.PluginPath, filename);
            if (File.Exists(fileInPluginFolder))
            {
                LogInfo($"Loaded image: {fileInPluginFolder}");
                return tex.LoadImage(File.ReadAllBytes(fileInPluginFolder));
            }

            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            string name = executingAssembly.GetManifestResourceNames().Single(str => str.EndsWith(filename));

            Stream resourceStream = executingAssembly.GetManifestResourceStream(name);

            byte[] data = new byte[resourceStream.Length];
            resourceStream.Read(data, 0, data.Length);

            return tex.LoadImage(data, true);
        }

        private static void InitRootObject()
        {
            if (rootObject == null)
                rootObject = GameObject.Find(c_rootObjectName) ?? new GameObject(c_rootObjectName);

            DontDestroyOnLoad(rootObject);

            if (rootPrefabs == null)
            {
                rootPrefabs = rootObject.transform.Find(c_rootPrefabsName)?.gameObject;

                if (rootPrefabs == null)
                {
                    rootPrefabs = new GameObject(c_rootPrefabsName);
                    rootPrefabs.transform.SetParent(rootObject.transform, false);
                    rootPrefabs.SetActive(false);
                }
            }
        }

        internal static GameObject InitPrefabClone(GameObject prefabToClone, string prefabName)
        {
            InitRootObject();

            prefabInit = true;
            GameObject clonedPrefab = Instantiate(prefabToClone, rootPrefabs.transform, false);
            prefabInit = false;
            clonedPrefab.name = prefabName;

            return clonedPrefab;
        }

        [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.Awake))]
        public static class ZNetView_Awake_AddPrefab
        {
            [HarmonyPriority(Priority.First)]
            private static bool Prefix() => !prefabInit;
        }

        [HarmonyPatch(typeof(ZSyncTransform), nameof(ZSyncTransform.Awake))]
        public static class ZSyncTransform_Awake_AddPrefab
        {
            [HarmonyPriority(Priority.First)]
            private static bool Prefix() => !prefabInit;
        }

        [HarmonyPatch(typeof(ZSyncTransform), nameof(ZSyncTransform.OnEnable))]
        public static class ZSyncTransform_OnEnable_AddPrefab
        {
            [HarmonyPriority(Priority.First)]
            private static bool Prefix() => !prefabInit;
        }

        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
        public static class ItemDrop_Awake_AddPrefab
        {
            [HarmonyPriority(Priority.First)]
            private static bool Prefix() => !prefabInit;
        }

        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
        public static class ItemDrop_Start_AddPrefab
        {
            [HarmonyPriority(Priority.First)]
            private static bool Prefix() => !prefabInit;
        }
    }
}
