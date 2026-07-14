using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace BoatSailSaveFix
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "g1llez.boatsailsavefix";
        public const string PluginName = "Boat Sail Save Fix";
        public const string PluginVersion = "1.0.0";

        internal static BepInEx.Logging.ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Harmony.CreateAndPatchAll(typeof(SaveableBoatCustomization_GetData_Patch));
            Harmony.CreateAndPatchAll(typeof(SaveableBoatCustomization_LoadData_Patch));
            Log.LogInfo($"{PluginName} {PluginVersion} loaded");
        }
    }

    internal static class SailDataHelper
    {
        internal static SaveSailData FromSail(Sail sail, Mast mast)
        {
            return new SaveSailData
            {
                prefabIndex = sail.prefabIndex,
                mastIndex = mast.orderIndex,
                installHeight = sail.GetCurrentInstallHeight(),
                minAngle = sail.minAngle,
                maxAngle = sail.maxAngle,
                health = 100f,
                sailColor = sail.activeColor,
                scaleY = sail.GetScaleY(),
                scaleZ = sail.GetScaleZ()
            };
        }

        internal static string Key(SaveSailData sail) =>
            sail.prefabIndex + ":" + sail.mastIndex + ":" + sail.installHeight.ToString("R");

        internal static List<SaveSailData> CloneList(List<SaveSailData> source)
        {
            var copy = new List<SaveSailData>(source.Count);
            foreach (var sail in source)
            {
                copy.Add(new SaveSailData
                {
                    prefabIndex = sail.prefabIndex,
                    mastIndex = sail.mastIndex,
                    installHeight = sail.installHeight,
                    minAngle = sail.minAngle,
                    maxAngle = sail.maxAngle,
                    health = sail.health,
                    sailColor = sail.sailColor,
                    scaleY = sail.scaleY,
                    scaleZ = sail.scaleZ
                });
            }
            return copy;
        }
    }

    [HarmonyPatch(typeof(SaveableBoatCustomization), nameof(SaveableBoatCustomization.GetData))]
    internal static class SaveableBoatCustomization_GetData_Patch
    {
        static void Postfix(SaveableBoatCustomization __instance, ref SaveBoatCustomizationData __result)
        {
            if (__result?.sails == null)
                return;

            var refs = __instance.GetComponent<BoatRefs>();
            if (!refs)
                return;

            var seen = new HashSet<string>();
            foreach (var existing in __result.sails)
                seen.Add(SailDataHelper.Key(existing));

            var added = 0;
            foreach (var mast in refs.masts)
            {
                if (!mast || mast.gameObject.activeInHierarchy || mast.sails == null)
                    continue;

                foreach (var sailObject in mast.sails)
                {
                    if (!sailObject)
                        continue;

                    var sail = sailObject.GetComponent<Sail>();
                    if (!sail)
                        continue;

                    var saveSailData = SailDataHelper.FromSail(sail, mast);
                    if (!seen.Add(SailDataHelper.Key(saveSailData)))
                        continue;

                    __result.sails.Add(saveSailData);
                    added++;
                }
            }

            if (added > 0)
            {
                Plugin.Log.LogInfo(
                    $"Preserved {added} sail(s) from inactive masts while saving {__instance.gameObject.name}");
            }
        }
    }

    [HarmonyPatch(typeof(SaveableBoatCustomization), nameof(SaveableBoatCustomization.LoadData))]
    internal static class SaveableBoatCustomization_LoadData_Patch
    {
        static readonly Dictionary<int, List<SaveSailData>> Cache = new Dictionary<int, List<SaveSailData>>();

        static void Prefix(SaveableBoatCustomization __instance, SaveBoatCustomizationData data)
        {
            if (data?.sails == null || data.sails.Count > 0)
                return;

            var saveable = __instance.GetComponent<SaveableObject>();
            if (!saveable)
                return;

            if (!Cache.TryGetValue(saveable.sceneIndex, out var cached) || cached.Count == 0)
                return;

            data.sails = SailDataHelper.CloneList(cached);
            Plugin.Log.LogInfo(
                $"Restored {cached.Count} cached sail(s) for {__instance.gameObject.name} (sceneIndex {saveable.sceneIndex})");
        }

        static void Postfix(SaveableBoatCustomization __instance, SaveBoatCustomizationData data)
        {
            if (data?.sails == null || data.sails.Count == 0)
                return;

            var saveable = __instance.GetComponent<SaveableObject>();
            if (!saveable)
                return;

            Cache[saveable.sceneIndex] = SailDataHelper.CloneList(data.sails);
        }
    }
}
