using BepInEx.Bootstrap;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using static HipLantern.HipLantern;

namespace HipLantern.Compatibility
{
    internal static class EpicLootCompat
    {
        public const string modGUID = "randyknapp.mods.epicloot";
        public static Assembly assembly;

        [HarmonyPatch]
        public static class EpicLoot_EnchantCostsHelper_CanBeMagicItem_TreatLanternAsUtility
        {
            public static List<MethodBase> targets;

            public static List<MethodBase> GetTargets()
            {
                assembly ??= Assembly.GetAssembly(Chainloader.PluginInfos[modGUID].Instance.GetType());

                List<MethodBase> list = new List<MethodBase>();

                if (AccessTools.Method(assembly.GetType("EpicLoot.Crafting.EnchantCostsHelper"), "GetSacrificeProducts", new System.Type[] { typeof(ItemDrop.ItemData) }) is MethodInfo method0)
                {
                    LogInfo("EpicLoot.Crafting.EnchantCostsHelper:GetSacrificeProducts method is patched to make it work with custom backpack item type");
                    list.Add(method0);
                }
                else
                    LogInfo("EpicLoot.Crafting.EnchantCostsHelper:GetSacrificeProducts method was not found");

                if (AccessTools.Method(assembly.GetType("EpicLoot.Crafting.EnchantCostsHelper"), "GetEnchantCost") is MethodInfo method2)
                {
                    LogInfo("EpicLoot.Crafting.EnchantCostsHelper:GetEnchantCost method is patched to make it work with custom backpack item type");
                    list.Add(method2);
                }
                else
                    LogInfo("EpicLoot.Crafting.EnchantCostsHelper:GetEnchantCost method was not found");

                if (AccessTools.Method(assembly.GetType("EpicLoot.Crafting.EnchantCostsHelper"), "GetAugmentCost") is MethodInfo method3)
                {
                    LogInfo("EpicLoot.Crafting.EnchantCostsHelper:GetAugmentCost method is patched to make it work with custom backpack item type");
                    list.Add(method3);
                }
                else
                    LogInfo("EpicLoot.Crafting.EnchantCostsHelper:GetAugmentCost method was not found");

                if (AccessTools.Method(assembly.GetType("EpicLoot.Crafting.EnchantCostsHelper"), "GetReAugmentCost") is MethodInfo method4)
                {
                    LogInfo("EpicLoot.Crafting.EnchantCostsHelper:GetReAugmentCost method is patched to make it work with custom backpack item type");
                    list.Add(method4);
                }
                else
                    LogInfo("EpicLoot.Crafting.EnchantCostsHelper:GetReAugmentCost method was not found");

                if (AccessTools.Method(assembly.GetType("EpicLoot.EpicLoot"), "CanBeMagicItem") is MethodInfo method5)
                {
                    LogInfo("EpicLoot.EpicLoot:CanBeMagicItem method is patched to make it work with custom backpack item type");
                    list.Add(method5);
                }
                else
                    LogInfo("EpicLoot.EpicLoot:CanBeMagicItem method was not found");

                return list;
            }

            public static bool Prepare() => Chainloader.PluginInfos.ContainsKey(modGUID) && (targets ??= GetTargets()).Count > 0;

            private static IEnumerable<MethodBase> TargetMethods() => targets;

            public static void Prefix(ItemDrop.ItemData item, ref bool __state)
            {
                if (!lanternEnchantableEpicLoot.Value)
                    return;

                if (__state = LanternItem.IsLanternItem(item))
                    item.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Utility;
            }

            public static void Postfix(ItemDrop.ItemData item, bool __state)
            {
                if (__state)
                    LanternItem.PatchLanternItemData(item);
            }
        }

        [HarmonyPatch]
        public static class EpicLoot_MagicItemEffectRequirements_argItemData_TreatLanternAsUtility
        {
            public static List<MethodBase> targets;

            public static List<MethodBase> GetTargets()
            {
                assembly ??= Assembly.GetAssembly(Chainloader.PluginInfos[modGUID].Instance.GetType());

                List<MethodBase> list = new List<MethodBase>();

                if (AccessTools.Method(assembly.GetType("EpicLoot.MagicItemEffectRequirements"), "AllowByItemType") is MethodInfo method6)
                {
                    LogInfo("EpicLoot.MagicItemEffectRequirements:AllowByItemType method is patched to make it work with custom backpack item type");
                    list.Add(method6);
                }
                else
                    LogInfo("EpicLoot.MagicItemEffectRequirements:AllowByItemType method was not found");

                if (AccessTools.Method(assembly.GetType("EpicLoot.MagicItemEffectRequirements"), "ExcludeByItemType") is MethodInfo method7)
                {
                    LogInfo("EpicLoot.MagicItemEffectRequirements:ExcludeByItemType method is patched to make it work with custom backpack item type");
                    list.Add(method7);
                }
                else
                    LogInfo("EpicLoot.MagicItemEffectRequirements:ExcludeByItemType method was not found");

                if (AccessTools.Method(assembly.GetType("EpicLoot.MagicItemEffectRequirements"), "CheckRequirements") is MethodInfo method8)
                {
                    LogInfo("EpicLoot.MagicItemEffectRequirements:CheckRequirements method is patched to make it work with custom backpack item type");
                    list.Add(method8);
                }
                else
                    LogInfo("EpicLoot.MagicItemEffectRequirements:CheckRequirements method was not found");

                return list;
            }

            public static bool Prepare() => Chainloader.PluginInfos.ContainsKey(modGUID) && (targets ??= GetTargets()).Count > 0;

            private static IEnumerable<MethodBase> TargetMethods() => targets;

            public static void Prefix(ItemDrop.ItemData itemData, ref bool __state)
            {
                if (!lanternEnchantableEpicLoot.Value)
                    return;

                if (__state = LanternItem.IsLanternItem(itemData))
                    itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Utility;
            }

            public static void Postfix(ItemDrop.ItemData itemData, bool __state)
            {
                if (__state)
                    LanternItem.PatchLanternItemData(itemData);
            }
        }
    }
}
