using BepInEx.Bootstrap;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using static HipLantern.HipLantern;

namespace HipLantern.Compatibility
{
    internal class JewelcraftingCompat
    {
        public const string modGUID = "org.bepinex.plugins.jewelcrafting";
        public static Assembly assembly;

        private struct ItemTypeState
        {
            public ItemDrop.ItemData.SharedData Item;
            public ItemDrop.ItemData.ItemType OriginalItemType;
            public bool Restore;
        }

        private static void TreatLanternAsUtility(ItemDrop.ItemData.SharedData item, ref ItemTypeState state)
        {
            if (!lanternSocketableJewelcrafting.Value || !LanternItem.IsLanternItem(item))
                return;

            state.Item = item;
            state.OriginalItemType = item.m_itemType;
            state.Restore = true;
            item.m_itemType = ItemDrop.ItemData.ItemType.Utility;
        }

        private static Exception RestoreItemType(Exception exception, ItemTypeState state)
        {
            if (state.Restore && state.Item != null)
                state.Item.m_itemType = state.OriginalItemType;

            return exception;
        }

        [HarmonyPatch]
        public static class Jewelcrafting_Utils_GetGemLocation_TreatLanternAsUtility
        {
            public static List<MethodBase> targets;

            public static List<MethodBase> GetTargets()
            {
                assembly ??= Assembly.GetAssembly(Chainloader.PluginInfos[modGUID].Instance.GetType());

                List<MethodBase> list = new List<MethodBase>();

                if (AccessTools.Method(assembly.GetType("Jewelcrafting.Utils"), "GetGemLocation") is MethodInfo method0)
                {
                    LogInfo("Jewelcrafting.Utils:GetGemLocation method is patched to make it work with lantern");
                    list.Add(method0);
                }
                else
                    LogInfo("Jewelcrafting.Utils:GetGemLocation method was not found");

               return list;
            }

            public static bool Prepare() => Chainloader.PluginInfos.ContainsKey(modGUID) && (targets ??= GetTargets()).Count > 0;

            private static IEnumerable<MethodBase> TargetMethods() => targets;

            private static void Prefix(ItemDrop.ItemData.SharedData item, ref ItemTypeState __state) =>
                TreatLanternAsUtility(item, ref __state);

            private static Exception Finalizer(Exception __exception, ItemTypeState __state) =>
                RestoreItemType(__exception, __state);
        }

        [HarmonyPatch]
        public static class Jewelcrafting_Utils_IsSocketableItem_TreatLanternAsUtility
        {
            public static List<MethodBase> targets;

            public static List<MethodBase> GetTargets()
            {
                assembly ??= Assembly.GetAssembly(Chainloader.PluginInfos[modGUID].Instance.GetType());

                List<MethodBase> list = new List<MethodBase>();

                if (AccessTools.Method(assembly.GetType("Jewelcrafting.Utils"), "IsSocketableItem", new System.Type[] { typeof(ItemDrop.ItemData) }) is MethodInfo method0)
                {
                    LogInfo("Jewelcrafting.Utils:IsSocketableItem method is patched to make it work with lantern");
                    list.Add(method0);
                }
                else
                    LogInfo("Jewelcrafting.Utils:IsSocketableItem method was not found");

                return list;
            }

            public static bool Prepare() => Chainloader.PluginInfos.ContainsKey(modGUID) && (targets ??= GetTargets()).Count > 0;

            private static IEnumerable<MethodBase> TargetMethods() => targets;

            private static void Prefix(ItemDrop.ItemData item, ref ItemTypeState __state) =>
                TreatLanternAsUtility(item?.m_shared, ref __state);

            private static Exception Finalizer(Exception __exception, ItemTypeState __state) =>
                RestoreItemType(__exception, __state);
        }
    }
}
