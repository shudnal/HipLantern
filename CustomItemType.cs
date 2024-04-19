using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
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

        public static HumanoidHipLantern GetHipLantern(this Humanoid humanoid) => data.GetOrCreateValue(humanoid);

        public static void AddData(this Humanoid humanoid, HumanoidHipLantern value)
        {
            try
            {
                data.Add(humanoid, value);
            }
            catch
            {
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

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupVisEquipment))]
        public static class Humanoid_SetupVisEquipment_CustomItemType
        {
            private static void Postfix(Humanoid __instance, VisEquipment visEq)
            {
                if (itemSlotUtility.Value)
                    return;

                ItemDrop.ItemData itemData = __instance.GetHipLantern().lantern;

                string itemName = itemData == null ? (__instance.m_utilityItem != null ? __instance.m_utilityItem.m_dropPrefab.name : "") : itemData.m_dropPrefab.name;
                visEq.SetUtilityItem(itemName);
            }
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
                    bool wasOn = __instance.GetHipLantern().lantern != null;

                    __instance.UnequipItem(__instance.GetHipLantern().lantern, triggerEquipEffects);

                    if (wasOn)
                        __instance.m_visEquipment.UpdateEquipmentVisuals();

                    __instance.GetHipLantern().lantern = item;
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

                if (item == null)
                    return;

                if (__instance.GetHipLantern().lantern == item || __instance.m_utilityItem == null)
                    __instance.GetHipLantern().lantern = null;

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
                __instance.UnequipItem(__instance.GetHipLantern().lantern, triggerEquipEffects: false);
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

                __result = __result || __instance.GetHipLantern().lantern == item;
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

    }
}
