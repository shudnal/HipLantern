using BepInEx.Bootstrap;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using static HipLantern.HipLantern;

namespace HipLantern.Compatibility
{
    internal static class EpicLootCompat
    {
        public const string modGUID = "randyknapp.mods.epicloot";

        private const BindingFlags MethodFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private const string EnchantCostsHelperTypeName = "EpicLoot.Crafting.EnchantCostsHelper";
        private const string EpicLootTypeName = "EpicLoot.EpicLoot";
        private const string RequirementsTypeName = "EpicLoot.MagicItemEffectRequirements";
        private const string ItemTypeClassifierTypeName = "EpicLoot.GatedItemType.ItemTypeClassifier";

        private static Assembly assembly;
        private static EpicLootApiBranch apiBranch;
        private static bool apiBranchDetected;

        public static bool IsInstalled => Chainloader.PluginInfos.ContainsKey(modGUID);

        private enum EpicLootApiBranch
        {
            None,
            LegacyPre013,
            Post013
        }

        private struct ItemTypeState
        {
            public ItemDrop.ItemData Item;
            public ItemDrop.ItemData.ItemType OriginalItemType;
            public bool Restore;
        }

        // These methods exist on both EpicLoot API branches and inspect m_itemType directly.
        [HarmonyPatch]
        private static class EpicLoot_Common_ItemTypeCompatibility
        {
            private static List<MethodBase> targets;

            private static bool Prepare()
            {
                if (!IsInstalled)
                    return false;

                targets ??= GetCommonTargets();
                return targets.Count > 0;
            }

            private static IEnumerable<MethodBase> TargetMethods() => targets;

            private static void Prefix(ItemDrop.ItemData __0, ref ItemTypeState __state) =>
                TreatLanternAsUtility(__0, ref __state);

            private static Exception Finalizer(Exception __exception, ItemTypeState __state) =>
                RestoreItemType(__exception, __state);
        }

        // EpicLoot before 0.13 performs part of its item-type gating inside CheckRequirements.
        // The method is selected only on the legacy branch, so the new overloads introduced later
        // are never queried through an ambiguous name-only lookup.
        [HarmonyPatch]
        private static class EpicLoot_Legacy_CheckRequirementsCompatibility
        {
            private static List<MethodBase> targets;

            private static bool Prepare()
            {
                if (!IsInstalled || GetApiBranch() != EpicLootApiBranch.LegacyPre013)
                    return false;

                targets ??= GetLegacyTargets();
                return targets.Count > 0;
            }

            private static IEnumerable<MethodBase> TargetMethods() => targets;

            private static void Prefix(ItemDrop.ItemData __0, ref ItemTypeState __state) =>
                TreatLanternAsUtility(__0, ref __state);

            private static Exception Finalizer(Exception __exception, ItemTypeState __state) =>
                RestoreItemType(__exception, __state);
        }

        // EpicLoot 0.13 introduced ItemTypeClassifier. ClassifyFromFields is the new raw-field
        // classification path used for generated item information and shard-stone categories.
        [HarmonyPatch]
        private static class EpicLoot_Post013_ItemTypeClassifierCompatibility
        {
            private static List<MethodBase> targets;

            private static bool Prepare()
            {
                if (!IsInstalled || GetApiBranch() != EpicLootApiBranch.Post013)
                    return false;

                targets ??= GetPost013Targets();
                return targets.Count > 0;
            }

            private static IEnumerable<MethodBase> TargetMethods() => targets;

            private static void Prefix(ItemDrop.ItemData __0, ref ItemTypeState __state) =>
                TreatLanternAsUtility(__0, ref __state);

            private static Exception Finalizer(Exception __exception, ItemTypeState __state) =>
                RestoreItemType(__exception, __state);
        }

        private static EpicLootApiBranch GetApiBranch()
        {
            if (apiBranchDetected)
                return apiBranch;

            apiBranchDetected = true;

            Assembly epicLootAssembly = GetAssembly();
            if (epicLootAssembly == null)
                return apiBranch = EpicLootApiBranch.None;

            Type classifierType = epicLootAssembly.GetType(ItemTypeClassifierTypeName, throwOnError: false);
            apiBranch = FindItemDataMethod(classifierType, "ClassifyFromFields") != null
                ? EpicLootApiBranch.Post013
                : EpicLootApiBranch.LegacyPre013;

            return apiBranch;
        }

        private static Assembly GetAssembly()
        {
            if (assembly != null)
                return assembly;

            if (!Chainloader.PluginInfos.TryGetValue(modGUID, out var pluginInfo) || pluginInfo?.Instance == null)
                return null;

            return assembly = pluginInfo.Instance.GetType().Assembly;
        }

        private static List<MethodBase> GetCommonTargets()
        {
            Assembly epicLootAssembly = GetAssembly();
            List<MethodBase> methods = new List<MethodBase>();

            if (epicLootAssembly == null)
                return methods;

            AddMethodsWithItemDataFirstParameter(methods, epicLootAssembly.GetType(EnchantCostsHelperTypeName, throwOnError: false),
                "GetSacrificeProducts", "GetEnchantCost", "GetAugmentCost", "GetReAugmentCost");
            AddMethodsWithItemDataFirstParameter(methods, epicLootAssembly.GetType(EpicLootTypeName, throwOnError: false),
                "CanBeMagicItem");
            AddMethodsWithItemDataFirstParameter(methods, epicLootAssembly.GetType(RequirementsTypeName, throwOnError: false),
                "AllowByItemType", "ExcludeByItemType");

            return methods;
        }

        private static List<MethodBase> GetLegacyTargets()
        {
            Assembly epicLootAssembly = GetAssembly();
            List<MethodBase> methods = new List<MethodBase>();

            if (epicLootAssembly == null)
                return methods;

            AddMethodsWithItemDataFirstParameter(methods, epicLootAssembly.GetType(RequirementsTypeName, throwOnError: false),
                "CheckRequirements");

            return methods;
        }

        private static List<MethodBase> GetPost013Targets()
        {
            Assembly epicLootAssembly = GetAssembly();
            List<MethodBase> methods = new List<MethodBase>();

            if (epicLootAssembly == null)
                return methods;

            AddMethodsWithItemDataFirstParameter(methods, epicLootAssembly.GetType(ItemTypeClassifierTypeName, throwOnError: false),
                "ClassifyFromFields");

            return methods;
        }

        private static MethodInfo FindItemDataMethod(Type type, string methodName)
        {
            if (type == null)
                return null;

            foreach (MethodInfo method in type.GetMethods(MethodFlags))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name == methodName && parameters.Length > 0 && parameters[0].ParameterType == typeof(ItemDrop.ItemData))
                    return method;
            }

            return null;
        }

        private static void AddMethodsWithItemDataFirstParameter(List<MethodBase> methods, Type type, params string[] methodNames)
        {
            if (type == null)
                return;

            foreach (MethodInfo method in type.GetMethods(MethodFlags))
            {
                if (!Contains(methodNames, method.Name))
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 0 && parameters[0].ParameterType == typeof(ItemDrop.ItemData) && !methods.Contains(method))
                    methods.Add(method);
            }
        }

        private static bool Contains(string[] values, string value)
        {
            for (int index = 0; index < values.Length; ++index)
            {
                if (string.Equals(values[index], value, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static void TreatLanternAsUtility(ItemDrop.ItemData item, ref ItemTypeState state)
        {
            if (!lanternEnchantableEpicLoot.Value || item?.m_shared == null || !LanternItem.IsLanternItemByName(item))
                return;

            state.Item = item;
            state.OriginalItemType = item.m_shared.m_itemType;
            state.Restore = true;

            item.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Utility;
        }

        private static Exception RestoreItemType(Exception exception, ItemTypeState state)
        {
            if (state.Restore && state.Item?.m_shared != null)
                state.Item.m_shared.m_itemType = state.OriginalItemType;

            return exception;
        }
    }
}
