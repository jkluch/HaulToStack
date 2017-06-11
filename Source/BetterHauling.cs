using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Harmony;
using HugsLib;
using HugsLib.Utils;
using RimWorld;
using Verse;
using UnityEngine;
using System.Reflection;
using Verse.AI;

namespace HaulToStack
{
    public class HaulToStack : ModBase
    {
        public override string ModIdentifier
        {
            get { return "HaulToStack"; }
        }

        public new ModLogger Logger
        {
            get { return base.Logger; }
        }

    }

    [HarmonyPatch(typeof(StoreUtility))]
    [HarmonyPatch("TryFindBestBetterStoreCellFor")]
    public static class Haul_Patch
    {
        [HarmonyPrefix]
        public static bool TryFindBestBetterStoreCellFor_Prefix()
        {
            return false;
        }

        [HarmonyPostfix]
        public static void TryFindBestBetterStoreCellFor_Postfix(bool __result, Thing t, Pawn carrier, Map map, StoragePriority currentPriority, Faction faction, ref IntVec3 foundCell, bool needAccurateResult = true)
        {
            /**
             * Default Code
             **/
            List<SlotGroup> allGroupsListInPriorityOrder = map.slotGroupManager.AllGroupsListInPriorityOrder;
            if (allGroupsListInPriorityOrder.Count == 0)
            {
                foundCell = IntVec3.Invalid;
                __result = false;
            }
            IntVec3 a = (!t.SpawnedOrAnyParentSpawned) ? carrier.PositionHeld : t.PositionHeld;
            StoragePriority storagePriority = currentPriority;
            float num = 2.14748365E+09f;
            IntVec3 intVec = default(IntVec3);
            bool flag = false;
            int count = allGroupsListInPriorityOrder.Count;
            for (int i = 0; i < count; i++)
            {
                SlotGroup slotGroup = allGroupsListInPriorityOrder[i];
                StoragePriority priority = slotGroup.Settings.Priority;
                if (priority < storagePriority || priority <= currentPriority)
                {
                    break;
                }
                if (slotGroup.Settings.AllowedToAccept(t))
                {
                    List<IntVec3> cellsList = slotGroup.CellsList;
                    int count2 = cellsList.Count;
                    int num2;
                    if (needAccurateResult)
                    {
                        num2 = Mathf.FloorToInt((float)count2 * Rand.Range(0.005f, 0.018f));
                        Log.Message("num2: " + num2 + ", count2: " + count2);
                    }
                    else
                    {
                        num2 = 0;
                    }
                    for (int j = 0; j < count2; j++)
                    {
                        IntVec3 intVec2 = cellsList[j];
                        float num3 = (float)(a - intVec2).LengthHorizontalSquared;
                        if (num3 <= num)
                        {
                            if (StoreUtility.IsGoodStoreCell(intVec2, map, t, carrier, faction))
                            {
                                flag = true;
                                intVec = intVec2;
                                num = num3;
                                storagePriority = priority;
                                if (j >= num2)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if (!flag)
            {
                foundCell = IntVec3.Invalid;
                __result = false;
                Log.Message("Modified Haul code, false");
            }
            foundCell = intVec;
            __result = true;
            Log.Message("Modified Haul code, true");
            /**
             * Default Code
             **/
        }

    }


    //[HarmonyPatch]
    //public static class Haul_Patch
    //{
    //    [HarmonyTargetMethod]
    //    static MethodBase TargetMethod()
    //    {
    //        var predicateClass = typeof(HaulAIUtility).GetNestedTypes(AccessTools.all)
    //            .FirstOrDefault(t => t.FullName.Contains("HaulToStorageJob"));
    //        return predicateClass.GetMethods(AccessTools.all).FirstOrDefault(m => m.ReturnType == typeof(bool));
    //    }

    //    [HarmonyTranspiler]
    //    static IEnumerable<CodeInstruction> MyTranspiler(IEnumerable<CodeInstruction> instr)
    //    {
    //        return instr
    //            .MethodReplacer(
    //                AccessTools.Method(typeof(StoreUtility), "TryFindBestBetterStoreCellFor"),
    //                AccessTools.Method(typeof(Haul_Patch), "TryFindBestBetterStoreCellFor")
    //             );

    //    }


    //    public static bool TryFindBestBetterStoreCellFor(Thing t, Pawn carrier, Map map, StoragePriority currentPriority, Faction faction, out IntVec3 foundCell, bool needAccurateResult = true)
    //    {
    //        List<SlotGroup> allGroupsListInPriorityOrder = map.slotGroupManager.AllGroupsListInPriorityOrder;
    //        if (allGroupsListInPriorityOrder.Count == 0)
    //        {
    //            foundCell = IntVec3.Invalid;
    //            return false;
    //        }
    //        IntVec3 a = (!t.SpawnedOrAnyParentSpawned) ? carrier.PositionHeld : t.PositionHeld;
    //        StoragePriority storagePriority = currentPriority;
    //        float num = 2.14748365E+09f;
    //        IntVec3 intVec = default(IntVec3);
    //        bool flag = false;
    //        int count = allGroupsListInPriorityOrder.Count;
    //        for (int i = 0; i < count; i++)
    //        {
    //            SlotGroup slotGroup = allGroupsListInPriorityOrder[i];
    //            StoragePriority priority = slotGroup.Settings.Priority;
    //            if (priority < storagePriority || priority <= currentPriority)
    //            {
    //                break;
    //            }
    //            if (slotGroup.Settings.AllowedToAccept(t))
    //            {
    //                List<IntVec3> cellsList = slotGroup.CellsList;
    //                int count2 = cellsList.Count;
    //                int num2;
    //                if (needAccurateResult)
    //                {
    //                    num2 = Mathf.FloorToInt((float)count2 * Rand.Range(0.005f, 0.018f));
    //                }
    //                else
    //                {
    //                    num2 = 0;
    //                }
    //                for (int j = 0; j < count2; j++)
    //                {
    //                    IntVec3 intVec2 = cellsList[j];
    //                    float num3 = (float)(a - intVec2).LengthHorizontalSquared;
    //                    if (num3 <= num)
    //                    {
    //                        if (StoreUtility.IsGoodStoreCell(intVec2, map, t, carrier, faction))
    //                        {
    //                            flag = true;
    //                            intVec = intVec2;
    //                            num = num3;
    //                            storagePriority = priority;
    //                            if (j >= num2)
    //                            {
    //                                break;
    //                            }
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //        if (!flag)
    //        {
    //            foundCell = IntVec3.Invalid;
    //            Log.Message("Modified Haul code, false");
    //            return false;
    //        }
    //        foundCell = intVec;
    //        Log.Message("Modified Haul code, true");
    //        return true;
    //    }


    //}

}
