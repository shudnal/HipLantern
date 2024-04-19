﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static HipLantern.HipLantern;

namespace HipLantern
{
    internal static class LanternItem
    {
        public const string itemName = "HipLantern";
        public static int itemHash = itemName.GetStableHashCode();
        public const string itemDropName = "$item_hiplantern";
        public const string itemDropDescription = "$item_hiplantern_description";

        public static int s_lightMaskNonPlayer;
        public static int s_lightMaskPlayer;

        public const string c_pointLightName = "Point Light";
        public const string c_spotLightName = "Spot Light";

        private static void CreateHipLanternPrefab()
        {
            GameObject lanternPrefab = ObjectDB.instance.GetItemPrefab("Lantern");
            if (lanternPrefab == null)
                return;

            if (s_lightMaskNonPlayer == 0)
                s_lightMaskNonPlayer = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");

            if (s_lightMaskPlayer == 0)
                s_lightMaskPlayer = LayerMask.GetMask("character");

            hipLanternPrefab = InitPrefabClone(lanternPrefab, itemName);

            UnityEngine.Object.DestroyImmediate(hipLanternPrefab.transform.Find("attach").gameObject);

            Transform attach = hipLanternPrefab.transform.Find("attach_back");
            
            attach.name = "attach_BackTool_attach";

            Transform attachPoint = attach.Find("default");

            UnityEngine.Object.DestroyImmediate(attachPoint.Find("SFX").gameObject);

            attachPoint.localScale = Vector3.one * 0.25f;
            attachPoint.transform.localPosition = new Vector3(-0.22f, -0.1f, 0.1f);
            attachPoint.transform.localEulerAngles = new Vector3(306.55f, 215.5f, 117.64f);

            MeshRenderer hipLanternMeshRenderer = attachPoint.GetComponent<MeshRenderer>();
            hipLanternMeshRenderer.sharedMaterial = new Material(hipLanternMeshRenderer.sharedMaterial);

            Transform pointLight = attachPoint.Find(c_pointLightName);

            GameObject spotLight = UnityEngine.Object.Instantiate(pointLight.gameObject, attachPoint);
            spotLight.name = c_spotLightName;
            
            Light playerLight = spotLight.GetComponent<Light>();
            playerLight.color = lightColor.Value;
            playerLight.cullingMask = s_lightMaskPlayer;
            playerLight.shadows = LightShadows.None;
            playerLight.range = 1.5f;
            playerLight.intensity = 2f;

            LightLod spotLod = spotLight.GetComponent<LightLod>();
            spotLod.m_lightDistance = playerLight.range;
            spotLod.m_baseRange = playerLight.range;

            spotLight.GetComponent<LightFlicker>().m_baseIntensity = playerLight.intensity;

            Light nonPlayerLight = pointLight.GetComponent<Light>();
            nonPlayerLight.color = lightColor.Value;
            nonPlayerLight.cullingMask = s_lightMaskNonPlayer;

            LightFlicker nonPlayerLightFlicker = pointLight.GetComponent<LightFlicker>();
            nonPlayerLightFlicker.m_flickerIntensity *= 0.6f;
            nonPlayerLightFlicker.m_flickerSpeed *= 0.6f;
            nonPlayerLightFlicker.m_movement = 0.02f;

            ParticleSystem.MainModule main = attachPoint.Find("flare").GetComponent<ParticleSystem>().main;
            main.startColor = new Color(main.startColor.color.r, main.startColor.color.g, main.startColor.color.b, 0.025f);

            attachPoint.gameObject.AddComponent<LanternLightController>();

            ItemDrop HipLanternItem = hipLanternPrefab.GetComponent<ItemDrop>();
            HipLanternItem.m_itemData.m_dropPrefab = hipLanternPrefab;

            HipLanternItem.m_itemData.m_shared.m_icons[0] = itemIcon;
            HipLanternItem.m_itemData.m_shared.m_name = itemDropName;
            HipLanternItem.m_itemData.m_shared.m_description = itemDropDescription;
            HipLanternItem.m_itemData.m_shared.m_itemType = CustomItemType.GetItemType();
            HipLanternItem.m_itemData.m_shared.m_maxStackSize = 1;
            HipLanternItem.m_itemData.m_shared.m_maxQuality = 1;
            HipLanternItem.m_itemData.m_shared.m_movementModifier = 0f;
            HipLanternItem.m_itemData.m_shared.m_equipDuration = 0.5f;
            HipLanternItem.m_itemData.m_shared.m_attachOverride = ItemDrop.ItemData.ItemType.Tool;

            if (UseFuel())
            {
                HipLanternItem.m_itemData.m_durability = fuelMinutes.Value;
                HipLanternItem.m_itemData.m_shared.m_useDurability = true;
                HipLanternItem.m_itemData.m_shared.m_maxDurability = HipLanternItem.m_itemData.m_durability;
                HipLanternItem.m_itemData.m_shared.m_useDurabilityDrain = 1f;
                HipLanternItem.m_itemData.m_shared.m_durabilityDrain = Time.fixedDeltaTime * (50f / 60f);
                HipLanternItem.m_itemData.m_shared.m_destroyBroken = false;
                HipLanternItem.m_itemData.m_shared.m_canBeReparied = !UseRefuel();
            }

            LogInfo($"Created prefab {hipLanternPrefab.name}");
        }

        private static void RegisterHipLanternPrefab()
        {
            ClearPrefabReferences();

            if (!(bool)hipLanternPrefab)
                CreateHipLanternPrefab();

            if (!(bool)hipLanternPrefab)
                return;

            if (ObjectDB.instance && !ObjectDB.instance.m_itemByHash.ContainsKey(itemHash))
            {
                ObjectDB.instance.m_items.Add(hipLanternPrefab);
                ObjectDB.instance.m_itemByHash.Add(itemHash, hipLanternPrefab);
            }

            if (ZNetScene.instance && !ZNetScene.instance.m_namedPrefabs.ContainsKey(itemHash))
            {
                ZNetScene.instance.m_prefabs.Add(hipLanternPrefab);
                ZNetScene.instance.m_namedPrefabs.Add(itemHash, hipLanternPrefab);
            }

            if (ObjectDB.instance)
            {
                if (ObjectDB.instance.m_recipes.RemoveAll(x => x.name == itemName) > 0)
                    LogInfo($"Removed recipe {itemName}");

                CraftingStation station = string.IsNullOrWhiteSpace(itemCraftingStation.Value) ? null : ObjectDB.instance.m_recipes.FirstOrDefault(rec => rec.m_craftingStation?.m_name == itemCraftingStation.Value)?.m_craftingStation;

                ItemDrop item = hipLanternPrefab.GetComponent<ItemDrop>();

                Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
                recipe.name = itemName;
                recipe.m_amount = 1;
                recipe.m_minStationLevel = itemMinStationLevel.Value;
                recipe.m_item = item;
                recipe.m_enabled = true;

                if (station != null)
                    recipe.m_craftingStation = station;

                List<Piece.Requirement> requirements = new List<Piece.Requirement>();
                foreach (string requirement in itemRecipe.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] req = requirement.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (req.Length != 2)
                        continue;

                    int amount = int.Parse(req[1]);
                    if (amount <= 0)
                        continue;

                    var prefab = ObjectDB.instance.GetItemPrefab(req[0].Trim());
                    if (prefab == null)
                        continue;

                    requirements.Add(new Piece.Requirement()
                    {
                        m_amount = amount,
                        m_resItem = prefab.GetComponent<ItemDrop>(),
                    });
                };
                recipe.m_resources = requirements.ToArray();

                ObjectDB.instance.m_recipes.Add(recipe);

                if (UseRefuel())
                {
                    Recipe recipeRefuel = ScriptableObject.CreateInstance<Recipe>();
                    recipeRefuel.name = itemName;
                    recipeRefuel.m_amount = 1;
                    recipeRefuel.m_minStationLevel = 1;
                    recipeRefuel.m_item = item;
                    recipeRefuel.m_enabled = true;

                    CraftingStation stationRefuel = string.IsNullOrWhiteSpace(refuelCraftingStation.Value) ? null : ObjectDB.instance.m_recipes.FirstOrDefault(rec => rec.m_craftingStation?.m_name == refuelCraftingStation.Value)?.m_craftingStation;

                    if (stationRefuel != null)
                        recipeRefuel.m_craftingStation = stationRefuel;

                    List<Piece.Requirement> requirementsRefuel = new List<Piece.Requirement>
                    {
                        new Piece.Requirement()
                        {
                            m_amount = 1,
                            m_resItem = item,
                        }
                    };

                    foreach (string requirement in refuelRecipe.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] req = requirement.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        if (req.Length != 2)
                            continue;

                        int amount = int.Parse(req[1]);
                        if (amount <= 0)
                            continue;

                        var prefab = ObjectDB.instance.GetItemPrefab(req[0].Trim());
                        if (prefab == null)
                            continue;

                        requirementsRefuel.Add(new Piece.Requirement()
                        {
                            m_amount = amount,
                            m_resItem = prefab.GetComponent<ItemDrop>(),
                        });
                    };

                    recipeRefuel.m_resources = requirementsRefuel.ToArray();

                    ObjectDB.instance.m_recipes.Add(recipeRefuel);
                }
            }
        }

        private static void ClearPrefabReferences()
        {
            if (ObjectDB.instance && ObjectDB.instance.m_itemByHash.ContainsKey(itemHash))
            {
                ObjectDB.instance.m_items.Remove(ObjectDB.instance.m_itemByHash[itemHash]);
                ObjectDB.instance.m_itemByHash.Remove(itemHash);
            }

            if (ZNetScene.instance && ZNetScene.instance.m_namedPrefabs.ContainsKey(itemHash))
            {
                ZNetScene.instance.m_prefabs.Remove(ZNetScene.instance.m_namedPrefabs[itemHash]);
                ZNetScene.instance.m_namedPrefabs.Remove(itemHash);
            }
        }

        internal static bool UseFuel()
        {
            return fuelMinutes.Value > 0;
        }

        internal static bool UseRefuel()
        {
            return UseFuel() && !String.IsNullOrEmpty(refuelRecipe.Value);
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float))]
        private class ItemDropItemData_GetTooltip_ItemTooltip
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(ItemDrop.ItemData item, ref string __result)
            {
                if (item.m_shared.m_name != itemDropName)
                    return;

                __result = __result.Replace("$item_durability", "$piece_fire_fuel");
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipment))]
        public static class Humanoid_UpdateEquipment_CustomItemType
        {
            private static void Postfix(Humanoid __instance, float dt)
            {
                if (__instance.IsPlayer() && UseFuel() &&  __instance.GetHipLantern().lantern != null)
                    __instance.DrainEquipedItemDurability(__instance.GetHipLantern().lantern, dt);
            }
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        public static class ObjectDB_Awake_AddPrefab
        {
            private static void Postfix(ObjectDB __instance)
            {
                if (__instance.m_items.Count == 0 || __instance.GetItemPrefab("Wood") == null)
                    return;

                RegisterHipLanternPrefab();
            }
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        public static class ObjectDB_CopyOtherDB_AddPrefab
        {
            private static void Postfix(ObjectDB __instance)
            {
                if (__instance.m_items.Count == 0 || __instance.GetItemPrefab("Wood") == null)
                    return;

                RegisterHipLanternPrefab();
            }
        }

        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnDestroy))]
        public static class FejdStartup_OnDestroy_AddPrefab
        {
            private static void Prefix()
            {
                ClearPrefabReferences();
            }
        }

        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.SetupGui))]
        public static class FejdStartup_SetupGui_AddLocalizedWords
        {
            private static void Postfix()
            {
                Localization_SetupLanguage_AddLocalizedWords.AddTranslations(Localization.instance, PlayerPrefs.GetString("language", "English"));
            }
        }

        [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
        public static class Localization_SetupLanguage_AddLocalizedWords
        {
            private static void Postfix(Localization __instance, string language)
            {
                AddTranslations(__instance, language);
            }

            public static void AddTranslations(Localization localization, string language)
            {
                localization.AddWord(itemDropName.Replace("$", ""), GetItemName(language));
                localization.AddWord(itemDropDescription.Replace("$", ""), GetItemDescription(language));
            }

            private static string GetItemName(string language)
            {
                return "Hip lantern";
                return language switch
                {
                    "Russian" => "Светлячок",
                    "Chinese" => "萤火虫",
                    "Chinese_Trad" => "螢火蟲",
                    "French" => "Luciole",
                    "German" => "Glühwürmchen",
                    "Polish" => "Świetlik",
                    "Korean" => "반딧불이",
                    "Spanish" => "Luciérnaga",
                    "Turkish" => "Ateşböceği",
                    "Dutch" => "Glimworm",
                    "Portuguese_Brazilian" => "Vaga-lume",
                    "Japanese" => "ホタル",
                    "Ukrainian" => "Світлячок",
                    _ => "HipLantern"
                };
            }

            private static string GetItemDescription(string language)
            {
                return "A hip lantern";
                return language switch
                {
                    "Russian" => "Светлячок, который проведет вас через самые темные ночи",
                    "Chinese" => "一只被束缚的萤火虫，引导你度过最黑暗的夜晚",
                    "Chinese_Trad" => "一隻被束縛的螢火蟲，引導你度過最黑暗的夜晚",
                    "French" => "Une luciole liée pour vous guider à travers les nuits les plus sombres",
                    "German" => "Ein gebundenes Glühwürmchen, das Sie durch die dunkelste Nacht führt",
                    "Polish" => "Związany świetlik, który poprowadzi Cię przez najciemniejsze noce",
                    "Korean" => "가장 어두운 밤을 안내할 묶인 반딧불이",
                    "Spanish" => "Una luciérnaga atada que te guiará a través de las noches más oscuras.",
                    "Turkish" => "En karanlık gecelerde size rehberlik edecek bağlı bir ateş böceği",
                    "Dutch" => "Een gebonden vuurvliegje om je door de donkerste nachten te leiden",
                    "Portuguese_Brazilian" => "Um vaga-lume preso para guiá-lo nas noites mais escuras",
                    "Japanese" => "縛られたホタルがあなたを最も暗い夜へと導きます",
                    "Ukrainian" => "Прив’язаний світлячок проведе вас у найтемніші ночі",
                    _ => "A hip lantern"
                };
            }
        }

    }
}
