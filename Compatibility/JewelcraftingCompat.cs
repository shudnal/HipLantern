using BepInEx.Bootstrap;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using static HipLantern.HipLantern;

namespace HipLantern.Compatibility
{
    internal class JewelcraftingCompat
    {
        public const string modGUID = "org.bepinex.plugins.jewelcrafting";
        public static Assembly assembly;

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

            public static void Prefix(ItemDrop.ItemData.SharedData item, ref bool __state)
            {
                if (!lanternSocketableJewelcrafting.Value)
                    return;

                if (__state = LanternItem.IsLanternItem(item))
                    item.m_itemType = ItemDrop.ItemData.ItemType.Utility;
            }

            public static void Postfix(ItemDrop.ItemData.SharedData item, bool __state)
            {
                if (__state)
                    LanternItem.PatchLanternSharedData(item);
            }
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

            public static void Prefix(ItemDrop.ItemData item, ref bool __state)
            {
                if (!lanternSocketableJewelcrafting.Value)
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
    }
}
