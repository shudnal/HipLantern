using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static HipLantern.HipLantern;

namespace HipLantern
{
    [Serializable]
    public class HumanoidHipLantern
    {
        public ItemDrop.ItemData lantern;

        public HumanoidHipLantern()
        {
            lantern = null;
        }
    }

    public static class HumanoidExtension
    {
        private static readonly ConditionalWeakTable<Humanoid, HumanoidHipLantern> data = new ConditionalWeakTable<Humanoid, HumanoidHipLantern>();

        public static HumanoidHipLantern GetLanternData(this Humanoid humanoid) => data.GetOrCreateValue(humanoid);

        public static ItemDrop.ItemData GetHipLantern(this Humanoid humanoid) => humanoid.GetLanternData().lantern;

        public static ItemDrop.ItemData SetHipLantern(this Humanoid humanoid, ItemDrop.ItemData item) => humanoid.GetLanternData().lantern = item;

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupVisEquipment))]
        public static class Humanoid_SetupVisEquipment_CustomItemType
        {
            private static void Postfix(Humanoid __instance, VisEquipment visEq)
            {
                if (itemSlotUtility.Value)
                    return;

                ItemDrop.ItemData itemData = __instance.GetHipLantern();

                visEq.SetLanternItem((itemData != null) ? itemData.m_dropPrefab.name : "");
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetEquipmentWeight))]
        public static class Humanoid_GetEquipmentWeight_CustomItemType
        {
            private static void Postfix(Humanoid __instance, ref float __result)
            {
                if (itemSlotUtility.Value)
                    return;

                ItemDrop.ItemData itemData = __instance.GetHipLantern();
                if (itemData != null)
                    __result += itemData.m_shared.m_weight;
            }
        }
    }

    [Serializable]
    public class VisEquipmentHipLantern
    {
        public string m_lanternItem = "";
        public List<GameObject> m_lanternItemInstances;
        public int m_currentlanternItemHash = 0;

        public static readonly int s_lanternItem = "LanternItem".GetStableHashCode();
    }

    public static class VisEquipmentExtension
    {
        private static readonly ConditionalWeakTable<VisEquipment, VisEquipmentHipLantern> data = new ConditionalWeakTable<VisEquipment, VisEquipmentHipLantern>();

        public static VisEquipmentHipLantern GetLanternData(this VisEquipment visEquipment) => data.GetOrCreateValue(visEquipment);

        public static void SetLanternItem(this VisEquipment visEquipment, string name)
        {
            VisEquipmentHipLantern lanternData = visEquipment.GetLanternData();

            if (!(lanternData.m_lanternItem == name))
            {
                lanternData.m_lanternItem = name;
                if (visEquipment.m_nview.GetZDO() != null && visEquipment.m_nview.IsOwner())
                    visEquipment.m_nview.GetZDO().Set(VisEquipmentHipLantern.s_lanternItem, (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
            }
        }

        public static bool SetLanternEquipped(this VisEquipment visEquipment, int hash)
        {
            VisEquipmentHipLantern lanternData = visEquipment.GetLanternData();
            if (lanternData.m_currentlanternItemHash == hash)
            {
                return false;
            }

            if (lanternData.m_lanternItemInstances != null)
            {
                foreach (GameObject utilityItemInstance in lanternData.m_lanternItemInstances)
                {
                    if ((bool)visEquipment.m_lodGroup)
                    {
                        Utils.RemoveFromLodgroup(visEquipment.m_lodGroup, utilityItemInstance);
                    }

                    UnityEngine.Object.Destroy(utilityItemInstance);
                }

                lanternData.m_lanternItemInstances = null;
            }

            lanternData.m_currentlanternItemHash = hash;
            if (hash != 0)
            {
                lanternData.m_lanternItemInstances = visEquipment.AttachArmor(hash);
            }

            return true;
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals))]
        public static class VisEquipment_UpdateEquipmentVisuals_CustomItemType
        {
            private static void Prefix(VisEquipment __instance)
            {
                if (itemSlotUtility.Value)
                    return;

                int lanternEquipped = 0;
                ZDO zDO = __instance.m_nview.GetZDO();
                if (zDO != null)
                {
                    lanternEquipped = zDO.GetInt(VisEquipmentHipLantern.s_lanternItem);
                }
                else
                {
                    VisEquipmentHipLantern lanternData = __instance.GetLanternData();
                    if (!string.IsNullOrEmpty(lanternData.m_lanternItem))
                    {
                        lanternEquipped = lanternData.m_lanternItem.GetStableHashCode();
                    }
                }

                if (__instance.SetLanternEquipped(lanternEquipped))
                    __instance.UpdateLodgroup();
            }
        }
    }

    internal class CustomItemType
    {
        internal static ItemDrop.ItemData.ItemType GetItemType()
        {
            if (itemSlotUtility.Value)
                return ItemDrop.ItemData.ItemType.Utility;

            return (ItemDrop.ItemData.ItemType)itemSlotType.Value;
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        public static class Humanoid_EquipItem_CustomItemType
        {
            private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result, bool triggerEquipEffects)
            {
                if (itemSlotUtility.Value)
                    return;

                if (item.m_shared.m_itemType == GetItemType())
                {
                    bool wasOn = __instance.GetHipLantern() != null;

                    __instance.UnequipItem(__instance.GetHipLantern(), triggerEquipEffects);

                    if (wasOn)
                        __instance.m_visEquipment.UpdateEquipmentVisuals();

                    __instance.SetHipLantern(item);
                }

                if (__instance.IsItemEquiped(item))
                {
                    item.m_equipped = true;
                    __result = true;
                }

                __instance.SetupEquipment();

                if (triggerEquipEffects)
                    __instance.TriggerEquipEffect(item);
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        public static class Humanoid_UnequipItem_CustomItemType
        {
            private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects)
            {
                if (itemSlotUtility.Value)
                    return;

                if (item == null || item != __instance.GetHipLantern())
                    return;

                __instance.SetHipLantern(null);

                __instance.SetupEquipment();

                if (triggerEquipEffects)
                    __instance.TriggerEquipEffect(item);
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipAllItems))]
        public class Humanoid_UnequipAllItems_CustomItemType
        {
            public static void Postfix(Humanoid __instance)
            {
                __instance.UnequipItem(__instance.GetHipLantern(), triggerEquipEffects: false);
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped))]
        public static class Humanoid_IsItemEquiped_CustomItemType
        {
            private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
            {
                if (itemSlotUtility.Value)
                    return;

                if (item == null)
                    return;

                __result = __result || __instance.GetHipLantern() == item;
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.IsEquipable))]
        public static class ItemDropItemData_IsEquipable_CustomItemType
        {
            private static void Postfix(ItemDrop.ItemData __instance, ref bool __result)
            {
                if (itemSlotUtility.Value)
                    return;

                __result = __result || __instance.m_shared.m_itemType == GetItemType();
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new Type[] { typeof(ItemDrop.ItemData) })]
        public static class Inventory_RemoveItem_CustomItemType
        {
            private static void Postfix(Inventory __instance, ItemDrop.ItemData item)
            {
                if (__instance != Player.m_localPlayer?.GetInventory())
                    return;

                if (item == null || item != Player.m_localPlayer.GetHipLantern())
                    return;

                Player.m_localPlayer.SetHipLantern(null);

                Player.m_localPlayer.SetupEquipment();
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new Type[] { typeof(string), typeof(int), typeof(int), typeof(bool) })]
        public static class Inventory_RemoveItem_ByName_CustomItemType
        {
            private static void Postfix(Inventory __instance, string name)
            {
                if (__instance != Player.m_localPlayer?.GetInventory())
                    return;

                if (!LanternItem.IsLanternItemDropName(name))
                    return;

                if (Player.m_localPlayer.GetHipLantern() != null && __instance.ContainsItem(Player.m_localPlayer.GetHipLantern()))
                    return;

                Player.m_localPlayer.SetHipLantern(null);

                Player.m_localPlayer.SetupEquipment();
            }
        }
    }
}