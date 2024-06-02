using HarmonyLib;
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

            hipLanternPrefab.transform.Find("default").localScale = Vector3.one * 0.5f;

            Transform attach = hipLanternPrefab.transform.Find("attach_back");
            
            attach.name = "attach_BackTool_attach";

            Transform attachPoint = attach.Find("default");

            UnityEngine.Object.DestroyImmediate(attachPoint.Find("SFX").gameObject);

            attachPoint.localScale = Vector3.one * attachScale.Value;
            attachPoint.localPosition = attachPosition.Value;
            attachPoint.localEulerAngles = attachEuler.Value;

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

            LogInfo($"Created prefab {hipLanternPrefab.name}");
        }

        internal static void PatchLanternItemData(ItemDrop.ItemData itemData, bool inventoryItemUpdate = true)
        {
            if (itemData == null)
                return;

            itemData.m_dropPrefab = hipLanternPrefab;

            itemData.m_shared.m_icons[0] = itemIcon;
            itemData.m_shared.m_name = itemDropName;
            itemData.m_shared.m_description = itemDropDescription;
            itemData.m_shared.m_itemType = CustomItemType.GetItemType();
            itemData.m_shared.m_maxStackSize = 1;
            itemData.m_shared.m_maxQuality = 1;
            itemData.m_shared.m_movementModifier = 0f;
            itemData.m_shared.m_equipDuration = equipDuration.Value;
            itemData.m_shared.m_attachOverride = ItemDrop.ItemData.ItemType.Tool;

            if (!inventoryItemUpdate)
                itemData.m_durability = UseFuel() ? fuelMinutes.Value : 200;

            itemData.m_shared.m_useDurability = UseFuel();
            itemData.m_shared.m_maxDurability = UseFuel() ? fuelMinutes.Value : 200;
            itemData.m_shared.m_useDurabilityDrain = UseFuel() ? 1f : 0f;
            itemData.m_shared.m_durabilityDrain = UseFuel() ? Time.fixedDeltaTime * (50f / 60f) : 0f;
            itemData.m_shared.m_destroyBroken = false;
            itemData.m_shared.m_canBeReparied = !UseRefuel();
        }

        private static void RegisterHipLanternPrefab()
        {
            ClearPrefabReferences();

            if (!(bool)hipLanternPrefab)
                CreateHipLanternPrefab();

            if (!(bool)hipLanternPrefab)
                return;

            PatchLanternItemData(hipLanternPrefab.GetComponent<ItemDrop>()?.m_itemData, inventoryItemUpdate: false);

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

            SetLanternRecipes();
        }

        internal static void SetLanternRecipes()
        {
            if (ObjectDB.instance)
            {
                if (ObjectDB.instance.m_recipes.RemoveAll(x => x.name == itemName) > 0)
                    LogInfo($"Replaced recipe {itemName}");

                CraftingStation workbench = ObjectDB.instance.m_recipes.FirstOrDefault(rec => rec.m_craftingStation?.m_name == "$piece_workbench")?.m_craftingStation;
                CraftingStation station = string.IsNullOrWhiteSpace(itemCraftingStation.Value) ? null : ObjectDB.instance.m_recipes.FirstOrDefault(rec => rec.m_craftingStation?.m_name == itemCraftingStation.Value)?.m_craftingStation;
                CraftingStation stationRefuel = string.IsNullOrWhiteSpace(refuelCraftingStation.Value) ? null : ObjectDB.instance.m_recipes.FirstOrDefault(rec => rec.m_craftingStation?.m_name == refuelCraftingStation.Value)?.m_craftingStation;

                ItemDrop item = hipLanternPrefab.GetComponent<ItemDrop>();

                Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
                recipe.name = itemName;
                recipe.m_amount = 1;
                recipe.m_item = item;
                recipe.m_enabled = true;
                recipe.m_craftingStation = station;
                recipe.m_minStationLevel = station ? itemMinStationLevel.Value : 1;
                recipe.m_repairStation = station ? null : stationRefuel ?? workbench;

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
                    recipeRefuel.m_craftingStation = stationRefuel;

                    List<Piece.Requirement> requirementsRefuel = new List<Piece.Requirement>
                    {
                        new Piece.Requirement()
                        {
                            m_amount = 1,
                            m_resItem = item,
                            m_recover = false
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
                            m_recover = false
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

        internal static void PatchInventory(Inventory inventory)
        {
            if (inventory == null)
                return;

            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>();
            inventory.GetAllItems(itemDropName, items);

            foreach (ItemDrop.ItemData item in items)
                PatchLanternItemData(item);
        }

        internal static void PatchLanternItemOnConfigChange()
        {
            PatchLanternItemData(hipLanternPrefab?.GetComponent<ItemDrop>()?.m_itemData, inventoryItemUpdate: false);

            PatchInventory(Player.m_localPlayer?.GetInventory());
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
                if (__instance.IsPlayer() && UseFuel() &&  __instance.GetHipLantern() != null)
                    __instance.DrainEquipedItemDurability(__instance.GetHipLantern(), dt);
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

        [HarmonyPatch(typeof(Player), nameof(Player.AddKnownItem))]
        public static class Player_AddKnownItem_LanternStats
        {
            private static void Postfix(ref ItemDrop.ItemData item)
            {
                if (item.m_shared.m_name != itemDropName)
                    return;

                PatchLanternItemData(item);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        public class Player_OnSpawned_LanternStats
        {
            public static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer)
                    return;

                PatchInventory(__instance.GetInventory());
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.Load))]
        public class Inventory_Load_LanternStats
        {
            public static void Postfix(Inventory __instance)
            {
                PatchInventory(__instance);
            }
        }

        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
        public static class ItemDrop_Start_LanternStats
        {
            private static void Postfix(ref ItemDrop __instance)
            {
                if (__instance.GetPrefabName(__instance.name) != itemName)
                    return;

                PatchLanternItemData(__instance.m_itemData);
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
                return language switch
                {
                    "Russian" => "Набедренный фонарь",
                    "Chinese" => "髋关节灯",
                    "Chinese_Trad" => "大腿燈",                  
                    "French" => "Lanterne de cuisse",
                    "German" => "Oberschenkellaterne",
                    "Polish" => "Latarnia uda",
                    "Korean" => "랜턴",
                    "Spanish" => "Linterna de cadera",
                    "Turkish" => "Kalça Feneri",
                    "Dutch" => "Hippe Lantaarn",
                    "Portuguese_Brazilian" => "Lanterna",
                    "Japanese" => "ヒップランタン",
                    "Ukrainian" => "Стегновий ліхтарик",
                    _ => "Hip Lantern"
                };
            }

            private static string GetItemDescription(string language)
            {
                return language switch
                {
                    "Russian" => "Небольшой портативный фонарь, который можно прикрепить к бедру.\nЭтот аксессуар обеспечивает тусклый свет, оставляя обе руки свободными для оружия.",
                    "Chinese" => "一种可以挂在臀部的小型便携式灯笼。 \n该配件提供昏暗的光线，同时可以腾出双手来拿武器。",
                    "Chinese_Trad" => "一種可以掛在臀部的小型便攜式燈籠。 \n此配件提供昏暗的光線，同時可以騰出雙手來拿武器。",
                    "French" => "Une petite lanterne portable qui peut être fixée à la hanche.\nCet accessoire fournit une lumière tamisée tout en laissant les deux mains libres pour les armes.",
                    "German" => "Eine kleine tragbare Laterne, die an der Hüfte befestigt werden kann.\nDieses Zubehör sorgt für gedämpftes Licht und lässt gleichzeitig beide Hände für Waffen frei.",
                    "Polish" => "Mała przenośna latarka, którą można przymocować do biodra.\nTo akcesorium zapewnia przyćmione światło, pozostawiając obie ręce wolne dla broni.",
                    "Korean" => "엉덩이에 부착할 수 있는 소형 휴대용 랜턴.\n이 액세서리는 양손을 자유롭게 사용하면서 희미한 조명을 제공합니다.",
                    "Spanish" => "Una pequeña linterna portátil que se puede colocar en la cadera.\nEste accesorio proporciona una luz tenue y deja ambas manos libres para usar las armas.",
                    "Turkish" => "Kalçaya takılabilen küçük, taşınabilir bir fener.\nBu aksesuar, her iki elinizi de silahlar için serbest bırakırken loş ışık sağlar.",
                    "Dutch" => "Een kleine draagbare lantaarn die op de heup kan worden bevestigd.\nDit accessoire zorgt voor gedimd licht en laat beide handen vrij voor wapens.",
                    "Portuguese_Brazilian" => "Uma pequena lanterna portátil que pode ser fixada no quadril.\nEste acessório fornece pouca luz enquanto deixa ambas as mãos livres para pegar armas.",
                    "Japanese" => "腰に装着できる小型の携帯用ランタン。\n このアクセサリーは、両手を自由にして武器を扱えるようにしながら、薄暗い光を提供します。",
                    "Ukrainian" => "Невеликий портативний ліхтар, який можна прикріпити до стегна.\nЦей аксесуар забезпечує приглушене світло, залишаючи обидві руки вільними для зброї.",
                    _ => "A small portable lantern that can be attached to the hip.\nThis accessory provides dim light while leaving both hands free for weapons."
                };
            }
        }

    }
}
