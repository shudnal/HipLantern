using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static HipLantern.HipLantern;

namespace HipLantern
{
    internal static class LanternItem
    {
        public const string itemName = "HipLantern";
        public static int itemHash = itemName.GetStableHashCode();
        public const string itemDropName = "$item_hiplantern";
        public const string itemDropDescription = "$item_hiplantern_description";

        public const string c_customDataState = "HipLanternState";
        public static readonly int s_lanternLightEnabled = "HipLanternLightEnabled".GetStableHashCode();
        public static readonly int s_lanternHeatEnabled = "HipLanternHeatEnabled".GetStableHashCode();

        public static int s_lightMaskNonPlayer;
        public static int s_lightMaskPlayer;

        public const string c_pointLightName = "Point Light";
        public const string c_spotLightName = "Spot Light";
        public const float c_lightLodDistance = 40f;

        [Serializable]
        private class LanternStateData
        {
            public bool lightEnabled = true;
            public bool heatEnabled = false;
        }

        internal static bool IsLanternType(ItemDrop.ItemData item) => item != null && item.m_shared.m_itemType == GetItemType();

        internal static ItemDrop.ItemData.ItemType GetItemType()
        {
            if (itemSlotUtility.Value)
                return ItemDrop.ItemData.ItemType.Utility;

            return (ItemDrop.ItemData.ItemType)itemSlotType.Value;
        }

        internal static bool IsLanternItem(ItemDrop item)
        {
            return item != null && (IsLanternItemName(item.GetPrefabName(item.name)) || IsLanternItem(item.m_itemData)) && IsLanternType(item.m_itemData);
        }

        public static bool IsLanternItem(ItemDrop.ItemData item)
        {
            return item != null && IsLanternItemByName(item) && IsLanternType(item);
        }

        public static bool IsLanternItem(ItemDrop.ItemData.SharedData item)
        {
            return item != null && item.m_itemType == GetItemType() && IsLanternItemDropName(item.m_name);
        }

        internal static bool IsLanternItemByName(ItemDrop.ItemData item)
        {
            return item != null && (item.m_dropPrefab != null && IsLanternItemName(item.m_dropPrefab.name) || IsLanternItemDropName(item.m_shared.m_name));
        }

        internal static bool IsLanternItemDropName(string name)
        {
            return name == itemDropName;
        }

        internal static bool IsLanternItemName(string name)
        {
            return name == itemName;
        }

        internal static bool IsLanternKnown()
        {
            if (!Player.m_localPlayer || Player.m_localPlayer.m_isLoading)
                return true;

            return Player.m_localPlayer.IsKnownMaterial(itemDropName);
        }

        internal static bool IsLanternSlotAvailable() => itemSlotExtraSlots.Value && (!itemSlotExtraSlotsDiscovery.Value || IsLanternKnown());

        internal static bool IsLightEnabled(ItemDrop.ItemData item)
        {
            if (item == null)
                return true;

            return GetLanternState(item).lightEnabled;
        }

        internal static bool SetLightEnabled(ItemDrop.ItemData item, bool enabled)
        {
            if (item == null)
                return false;

            bool changed = IsLightEnabled(item) != enabled;
            LanternStateData state = GetLanternState(item);
            state.lightEnabled = enabled;
            if (!enabled)
                state.heatEnabled = false;

            SaveLanternState(item, state);
            UpdateLanternVariant(item);
            return changed;
        }

        internal static bool IsHeatEnabled(ItemDrop.ItemData item)
        {
            if (item == null || !heatEnabled.Value || !IsLightEnabled(item))
                return false;

            return GetLanternState(item).heatEnabled;
        }

        internal static bool SetHeatEnabled(ItemDrop.ItemData item, bool enabled, bool force = false)
        {
            if (item == null)
                return false;

            bool expected = enabled && (force || (heatEnabled.Value && IsLightEnabled(item)));
            bool changed = IsHeatEnabled(item) != expected;
            LanternStateData state = GetLanternState(item);
            state.heatEnabled = expected;
            SaveLanternState(item, state);
            UpdateLanternVariant(item);
            return changed;
        }

        private static LanternStateData GetLanternState(ItemDrop.ItemData item)
        {
            LanternStateData state = new LanternStateData();

            if (item?.m_customData == null || !item.m_customData.TryGetValue(c_customDataState, out string json) || string.IsNullOrWhiteSpace(json))
                return state;

            return JsonUtility.FromJson<LanternStateData>(json) ?? state;
        }

        private static void SaveLanternState(ItemDrop.ItemData item, LanternStateData state)
        {
            if (item?.m_customData == null)
                return;

            item.m_customData[c_customDataState] = JsonUtility.ToJson(state);
        }

        internal static void UpdateLanternVariant(ItemDrop.ItemData item)
        {
            if (item == null)
                return;

            item.m_variant = !IsLightEnabled(item) ? 1 : (IsHeatEnabled(item) ? 2 : 0);
        }

        private static ItemDrop.ItemData GetEquippedLantern(Humanoid humanoid)
        {
            ItemDrop.ItemData lantern = humanoid?.GetHipLantern();
            if (IsLanternItem(lantern))
                return lantern;

            if (humanoid?.GetInventory() == null)
                return null;

            return humanoid.GetInventory().GetEquippedItems().FirstOrDefault(IsLanternItem);
        }

        private static Transform AddCollider(Transform transform, string name, System.Type type)
        {
            Transform collider = new GameObject(name, type).transform;
            collider.SetParent(transform, worldPositionStays: false);

            return collider;
        }

        private static void CreateHipLanternPrefab()
        {
            GameObject lanternPrefab = ObjectDB.instance.GetItemPrefab("Lantern");
            if (lanternPrefab == null)
                return;

            if (s_lightMaskNonPlayer == 0)
                s_lightMaskNonPlayer = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle", "item");

            if (s_lightMaskPlayer == 0)
                s_lightMaskPlayer = LayerMask.GetMask("character");

            hipLanternPrefab = InitPrefabClone(lanternPrefab, itemName);

            UnityEngine.Object.DestroyImmediate(hipLanternPrefab.transform.Find("attach").gameObject);

            Transform attach_back = hipLanternPrefab.transform.Find("attach_back");

            attach_back.name = "attach_BackTool_attach";

            Transform attachPoint = attach_back.Find("default");

            attachPoint.localScale = Vector3.one * attachScale.Value;
            attachPoint.localPosition = attachPosition.Value;
            attachPoint.localEulerAngles = attachEuler.Value;

            MeshRenderer hipLanternMeshRenderer = attachPoint.GetComponent<MeshRenderer>();
            hipLanternMeshRenderer.sharedMaterial = new Material(hipLanternMeshRenderer.sharedMaterial)
            {
                name = $"{hipLanternPrefab.name}_mat"
            };

            Transform pointLight = attachPoint.Find(c_pointLightName);

            // Player only close range light
            GameObject spotLight = UnityEngine.Object.Instantiate(pointLight.gameObject, attachPoint);
            spotLight.name = c_spotLightName;
            
            Light playerLight = spotLight.GetComponent<Light>();
            playerLight.color = lightColor.Value;
            playerLight.cullingMask = s_lightMaskPlayer;
            playerLight.shadows = LightShadows.None;
            playerLight.range = 1.5f;
            playerLight.intensity = 2f;

            LightLod spotLod = spotLight.GetComponent<LightLod>();
            spotLod.m_lightDistance = c_lightLodDistance;
            spotLod.m_baseRange = playerLight.range;

            spotLight.GetComponent<LightFlicker>().m_baseIntensity = playerLight.intensity;

            Light nonPlayerLight = pointLight.GetComponent<Light>();
            nonPlayerLight.color = lightColor.Value;
            nonPlayerLight.cullingMask = s_lightMaskNonPlayer;
            nonPlayerLight.range = lightRangeOutdoors.Value;
            nonPlayerLight.intensity = lightIntensityOutdoors.Value;
            nonPlayerLight.shadowStrength = lightShadowsOutdoors.Value;

            LightFlicker nonPlayerLightFlicker = pointLight.GetComponent<LightFlicker>();
            nonPlayerLightFlicker.m_baseIntensity = nonPlayerLight.intensity;
            nonPlayerLightFlicker.m_flickerIntensity *= 0.6f;
            nonPlayerLightFlicker.m_flickerSpeed *= 0.6f;
            nonPlayerLightFlicker.m_movement = 0.02f;

            LightLod pointLod = pointLight.GetComponent<LightLod>();
            pointLod.m_lightDistance = c_lightLodDistance;
            pointLod.m_baseRange = nonPlayerLight.range;
            pointLod.m_baseShadowStrength = nonPlayerLight.shadowStrength;

            ParticleSystem.MainModule main = attachPoint.Find("flare").GetComponent<ParticleSystem>().main;
            main.startColor = new Color(main.startColor.color.r, main.startColor.color.g, main.startColor.color.b, 0.025f);

            attachPoint.gameObject.AddComponent<LanternLightController>();

            // Heat mode warmth effect
            GameObject heatWarmth = AddCollider(attachPoint, "HeatWarmth", typeof(SphereCollider)).gameObject;
            heatWarmth.layer = 14; // character_trigger
            heatWarmth.transform.localPosition = new Vector3(0, 0.22f, 0f); // center of lantern

            SphereCollider heatWarmthCollider = heatWarmth.GetComponent<SphereCollider>();
            heatWarmthCollider.radius = heatRadius.Value;
            heatWarmthCollider.isTrigger = true;
            heatWarmthCollider.enabled = true;

            EffectArea effectArea = heatWarmth.gameObject.AddComponent<EffectArea>();
            effectArea.m_type = EffectArea.Type.Fire | EffectArea.Type.Heat;
            effectArea.m_playerOnly = true;
            effectArea.m_isHeatType = true;

            if (attachPoint.Find("SFX") is Transform sfx)
            {
                sfx.SetParent(heatWarmth.transform);
                sfx.GetComponent<AudioSource>().volume = 2f;
            }

            if (ObjectDB.instance && ObjectDB.instance.GetItemPrefab("Torch") is GameObject torch)
            {
                Transform torchFlames = torch.transform.Find("attach/equiped/fx_Torch_Carried/Local Flames");
                Transform localFlames = UnityEngine.Object.Instantiate(torchFlames, heatWarmth.transform);
                localFlames.localScale = Vector3.one * 0.4f;
                localFlames.name = "Heat Flames";
            }

            // Attached object light controller
            Transform attach = hipLanternPrefab.transform.Find("default");
            attach.name = "attach";
            attach.localScale = Vector3.one * 0.57f;
            attach.localPosition = new Vector3(0f, 0.012f, 0f);
            attach.GetComponent<MeshRenderer>().sharedMaterial = hipLanternMeshRenderer.sharedMaterial;

            LightLod lod = attach.GetComponentInChildren<LightLod>(includeInactive: true);
            lod.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            lod.gameObject.SetActive(true);

            Transform flare = attach.Find("flare");
            flare.localPosition = new Vector3(0f, 0.2f, 0f);
            flare.gameObject.SetActive(true);

            Transform insects = Resources.FindObjectsOfTypeAll<Ship>().FirstOrDefault(ws => ws.name == "VikingShip")?.transform.Find("ship/visual/Customize/TraderLamp/insects");
            if (insects)
            {
                insects = UnityEngine.Object.Instantiate(insects, attach);
                insects.name = "insects";
                insects.gameObject.SetActive(false);
                insects.localPosition = new Vector3(0f, 0.2f, 0f);
            }

            attach.gameObject.AddComponent<LanternLightController>();

            LogInfo($"Created prefab {hipLanternPrefab.name}");
        }

        internal static void PatchLanternItemData(ItemDrop.ItemData itemData, bool inventoryItemUpdate = true)
        {
            if (itemData == null)
                return;

            itemData.m_dropPrefab = hipLanternPrefab;

            PatchLanternSharedData(itemData.m_shared);
            UpdateLanternVariant(itemData);

            if (!inventoryItemUpdate)
                itemData.m_durability = itemData.m_shared.m_maxDurability;
        }

        internal static void PatchLanternSharedData(ItemDrop.ItemData.SharedData itemSharedData)
        {
            if (itemSharedData.m_icons == null || itemSharedData.m_icons.Length != 3)
                itemSharedData.m_icons = new Sprite[3];

            itemSharedData.m_icons[0] = itemIcon;
            itemSharedData.m_icons[1] = itemIconOff ? itemIconOff : itemIcon;
            itemSharedData.m_icons[2] = itemIconHeat ? itemIconHeat : itemIcon;
            itemSharedData.m_name = itemDropName;
            itemSharedData.m_description = itemDropDescription;
            itemSharedData.m_itemType = GetItemType();
            itemSharedData.m_maxStackSize = 1;
            itemSharedData.m_maxQuality = 1;
            itemSharedData.m_movementModifier = 0f;
            itemSharedData.m_equipDuration = equipDuration.Value;
            itemSharedData.m_attachOverride = ItemDrop.ItemData.ItemType.Tool;

            itemSharedData.m_useDurability = UseFuel();
            itemSharedData.m_maxDurability = UseFuel() ? fuelMinutes.Value : 200;
            itemSharedData.m_useDurabilityDrain = UseFuel() ? 1f : 0f;
            itemSharedData.m_durabilityDrain = UseFuel() ? Time.fixedDeltaTime * (50f / 60f) : 0f;
            itemSharedData.m_destroyBroken = false;
            itemSharedData.m_canBeReparied = !UseRefuel();
        }

        private static void RegisterHipLanternPrefab()
        {
            ClearPrefabReferences();

            if (!(bool)hipLanternPrefab)
                CreateHipLanternPrefab();

            if (!(bool)hipLanternPrefab)
                return;

            ItemDrop.ItemData itemData = hipLanternPrefab.GetComponent<ItemDrop>()?.m_itemData;
            PatchLanternItemData(itemData, inventoryItemUpdate: false);

            if (ObjectDB.instance && !ObjectDB.instance.m_itemByHash.ContainsKey(itemHash))
            {
                ObjectDB.instance.m_items.Add(hipLanternPrefab);
                ObjectDB.instance.m_itemByHash.Add(itemHash, hipLanternPrefab);
                if (itemData != null)
                    ObjectDB.instance.m_itemByData[itemData.m_shared] = hipLanternPrefab;
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
                if (ObjectDB.instance.m_recipes.RemoveAll(x => IsLanternItemName(x.name)) > 0)
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

            inventory.GetAllItems().DoIf(IsLanternItemByName, item => PatchLanternItemData(item));
        }

        internal static void PatchLanternItemOnConfigChange()
        {
            PatchLanternItemData(hipLanternPrefab?.GetComponent<ItemDrop>()?.m_itemData, inventoryItemUpdate: false);

            PatchInventory(Player.m_localPlayer?.GetInventory());
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int))]
        private class ItemDropItemData_GetTooltip_ItemTooltip
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(ItemDrop.ItemData item, ref string __result)
            {
                if (!IsLanternItem(item))
                    return;

                __result = __result.Replace("$item_durability", "$piece_fire_fuel");
                UpdateLanternVariant(item);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Player_Update_ToggleLanternModes
        {
            private static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer)
                    return;

                if (!__instance.TakeInput())
                    return;

                bool heatPressed = heatEnabled.Value && IsShortcutDown(toggleLanternHeatShortcut.Value);
                bool lightPressed = !heatPressed && IsShortcutDown(toggleLanternShortcut.Value);
                if (!lightPressed && !heatPressed)
                    return;

                ItemDrop.ItemData lantern = GetEquippedLantern(__instance);
                if (!IsLanternItem(lantern))
                    return;

                bool lightEnabled = IsLightEnabled(lantern);
                bool heatEnabledNow = IsHeatEnabled(lantern);

                bool newLightEnabled = lightEnabled;
                bool newHeatEnabled = heatEnabledNow;

                if (lightPressed)
                {
                    newLightEnabled = !newLightEnabled;

                    if (!newLightEnabled)
                        newHeatEnabled = false;
                }

                if (heatPressed)
                {
                    newHeatEnabled = !newHeatEnabled;

                    if (newHeatEnabled)
                        newLightEnabled = true;
                }

                bool changed = false;
                changed |= SetLightEnabled(lantern, newLightEnabled);
                changed |= SetHeatEnabled(lantern, newHeatEnabled);

                if (!changed)
                    return;

                __instance.SetupEquipment();
                __instance.GetInventory()?.Changed();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        private static class InventoryGui_Awake_AddLanternTooltipHint
        {
            private static bool s_initialized;

            private static void Postfix(InventoryGui __instance)
            {
                if (s_initialized)
                    return;

                UITooltip tooltip = __instance.m_playerGrid?.m_elementPrefab?.GetComponent<UITooltip>();
                if (tooltip == null)
                    return;

                Transform bkg = tooltip.m_tooltipPrefab?.transform.Find("Bkg");
                TextMeshProUGUI template = bkg?.Find("Text")?.GetComponent<TextMeshProUGUI>();
                if (bkg == null || template == null)
                    return;

                GameObject extra = new GameObject("HipLanternSwitchHint");
                extra.transform.SetParent(bkg, false);
                TextMeshProUGUI hint = extra.AddComponent<TextMeshProUGUI>();
                hint.font = template.font;
                hint.fontSize = template.fontSize;
                hint.alignment = TextAlignmentOptions.Center;
                hint.color = template.color;
                hint.raycastTarget = false;

                ContentSizeFitter fitter = extra.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                extra.SetActive(false);
                tooltip.gameObject.AddComponent<HipLanternTooltipState>();
                s_initialized = true;
            }
        }

        private static string GetHotkeyText(KeyboardShortcut shortcut) => $"[<color=yellow><b>{shortcut}</b></color>]";

        private static string BuildLanternTooltipHint(ItemDrop.ItemData item)
        {
            if (!IsLanternItem(item))
                return null;

            if (!heatEnabled.Value)
                return Localization.instance.Localize("$hiplantern_switch_light", GetHotkeyText(toggleLanternShortcut.Value));

            string heatState = IsHeatBlockedForPlayer(Player.m_localPlayer) ? "$hiplantern_heat_mode_blocked" : IsHeatEnabled(item) ? "$hiplantern_heat_mode" : "";
            return Localization.instance.Localize($"$hiplantern_switch_light\n$hiplantern_switch_heat\n{heatState}", GetHotkeyText(toggleLanternShortcut.Value), GetHotkeyText(toggleLanternHeatShortcut.Value)).TrimEnd();
        }

        private static void ApplyLanternTooltipHint(UITooltip tooltip, string text)
        {
            Transform hintTransform = UITooltip.m_tooltip?.transform.Find("Bkg/HipLanternSwitchHint");
            if (hintTransform == null)
                return;

            TextMeshProUGUI hint = hintTransform.GetComponent<TextMeshProUGUI>();
            if (hint == null)
                return;

            bool active = !string.IsNullOrEmpty(text);
            hintTransform.gameObject.SetActive(active);
            if (active)
                hint.text = text;
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip))]
        private static class InventoryGrid_CreateItemTooltip_LanternHint
        {
            private static void Postfix(ItemDrop.ItemData item, UITooltip tooltip)
            {
                HipLanternTooltipState state = tooltip?.GetComponent<HipLanternTooltipState>();
                if (state == null)
                    return;

                state.text = BuildLanternTooltipHint(item);
                ApplyLanternTooltipHint(tooltip, state.text);
            }
        }

        [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.UpdateTextElements))]
        private static class UITooltip_UpdateTextElements_LanternHint
        {
            private static void Postfix(UITooltip __instance)
            {
                Transform hintTransform = UITooltip.m_tooltip?.transform.Find("Bkg/HipLanternSwitchHint");
                if (hintTransform == null)
                    return;

                HipLanternTooltipState state = __instance.GetComponent<HipLanternTooltipState>();
                TextMeshProUGUI hint = hintTransform.GetComponent<TextMeshProUGUI>();
                if (hint == null)
                    return;

                if (state != null)
                    ApplyLanternTooltipHint(__instance, state.text);
                else
                    hintTransform.gameObject.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.UseItem))]
        private static class ItemStand_UseItem_SyncLanternLight
        {
            private static void Postfix(ItemStand __instance, bool __result, ItemDrop.ItemData item)
            {
                if (!__result || !IsLanternItem(item) || __instance.m_nview.GetZDO() is not ZDO zdo || !__instance.m_nview.IsOwner())
                    return;

                zdo.Set(s_lanternLightEnabled, IsLightEnabled(item));
                zdo.Set(s_lanternHeatEnabled, IsHeatEnabled(item));
            }
        }

        [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.DropItem))]
        private static class ItemStand_DropItem_ClearLanternLightState
        {
            private static void Prefix(ItemStand __instance)
            {
                if (__instance.m_nview.GetZDO() is not ZDO zdo || !__instance.m_nview.IsOwner())
                    return;

                if (LanternItem.IsLanternItemName(zdo.GetString(ZDOVars.s_item)))
                {
                    zdo.Set(s_lanternLightEnabled, true);
                    zdo.Set(s_lanternHeatEnabled, false);
                }
            }
        }

        private class HipLanternTooltipState : MonoBehaviour
        {
            public string text;
        }

        internal static bool IsHeatBlockedForPlayer(Player player)
        {
            if (player == null)
                return false;

            Heightmap.Biome biome = player.GetCurrentBiome();
            bool coldBiome = biome == Heightmap.Biome.Mountain && preventHeatInMountains.Value
                          || biome == Heightmap.Biome.DeepNorth && preventHeatInDeepNorth.Value;

            if (!coldBiome)
                return false;

            if (player.InInterior() || player.InShelter() || ShieldGenerator.IsInsideShield(player.transform.position))
                return false;

            if (!keepHeatWhenColdProtected.Value)
                return true;

            HitData.DamageModifier modifier = player.GetDamageModifiers().GetModifier(HitData.DamageType.Frost);
            if (modifier == HitData.DamageModifier.Resistant
                || modifier == HitData.DamageModifier.VeryResistant
                || modifier == HitData.DamageModifier.SlightlyResistant
                || modifier == HitData.DamageModifier.Immune)
            {
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipment))]
        public static class Humanoid_UpdateEquipment_CustomItemType
        {
            private static void Finalizer(Humanoid __instance, float dt)
            {
                if (__instance.IsPlayer() && __instance.GetHipLantern() is ItemDrop.ItemData lantern && lantern.m_shared.m_useDurability && (!lantern.m_shared.m_canBeReparied || (__instance as Player).GetCurrentCraftingStation() == null))
                {
                    if (IsLightEnabled(lantern))
                    {
                        bool activeHeatMode = IsHeatEnabled(lantern) && !IsHeatBlockedForPlayer(__instance as Player);
                        float durabilityMultiplier = activeHeatMode ? Mathf.Max(1f, heatDurabilityMultiplier.Value) : 1f;
                        __instance.DrainEquipedItemDurability(lantern, dt * durabilityMultiplier);
                    }
                    else if (fuelAutoChargeSpeed.Value > 0f)
                    {
                        float chargeStep = dt * fuelAutoChargeSpeed.Value * lantern.m_shared.m_durabilityDrain;

                        if (lantern.m_durability < lantern.m_shared.m_maxDurability - chargeStep)
                            __instance.DrainEquipedItemDurability(lantern, -dt * fuelAutoChargeSpeed.Value);
                    }
                }
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
                if (!IsLanternItem(item))
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
                if (!IsLanternItem(__instance))
                    return;

                PatchLanternItemData(__instance.m_itemData);
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        public class InventoryGui_DoCrafting_PreserveCustomData
        {
            public static readonly List<ItemDrop.ItemData> lanternsBefore = new List<ItemDrop.ItemData>();
            public static readonly List<ItemDrop.ItemData> lanternsAfter = new List<ItemDrop.ItemData>();

            // Get all lanterns before crafting
            // Compare it with all lanterns after crafting
            // Find removed lantern and new lantern
            // Move custom data from removed to new lantern
            // Repick item if EpicLoot is there to update enchanted state

            public static void Prefix(InventoryGui __instance)
            {
                if (__instance.m_craftUpgradeItem != null)
                    return;

                if (__instance.m_craftRecipe == null)
                    return;

                if (!IsLanternItem(__instance.m_craftRecipe.m_item))
                    return;

                lanternsBefore.AddRange(Player.m_localPlayer.GetInventory().GetAllItems().Where(IsLanternItem));
                lanternsAfter.Clear();
            }

            [HarmonyPriority(Priority.Last)]
            public static void Postfix()
            {
                if (lanternsBefore.Count == 0)
                    return;

                if (lanternsBefore.Find(item => !Player.m_localPlayer.GetInventory().m_inventory.Contains(item)) is ItemDrop.ItemData recraftedLantern && recraftedLantern.m_customData.Any())
                {
                    lanternsAfter.AddRange(Player.m_localPlayer.GetInventory().GetAllItems().Where(IsLanternItem));
                    if (lanternsAfter.Find(item => !lanternsBefore.Contains(item)) is ItemDrop.ItemData newLantern)
                    { 
                        recraftedLantern.m_customData.Do(kvp => newLantern.m_customData[kvp.Key] = kvp.Value); 
                        if (Compatibility.EpicLootCompat.IsInstalled)
                        {
                            // It's easier to repick item from ItemDrop than call reflections
                            Player.m_localPlayer.GetInventory().RemoveItem(newLantern);
                            ItemDrop itemDrop = ItemDrop.DropItem(newLantern, 1, Player.m_localPlayer.transform.position, Player.m_localPlayer.transform.rotation);
                            itemDrop.OnPlayerDrop();
                            Player.m_localPlayer.Pickup(itemDrop.gameObject, true, false);
                        }
                    }
                }

                lanternsBefore.Clear();
                lanternsAfter.Clear();
            }
        }
    }
}
