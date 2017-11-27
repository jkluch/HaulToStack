using System;
using System.Collections.Generic;

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
        public static HaulToStack Instance { get; private set; }

        public override string ModIdentifier
        {
            get { return "HaulToStack"; }
        }

        public new ModLogger Logger
        {
            get { return base.Logger; }
        }

        private HaulToStack()
        {
            Instance = this;
        }

    }
    
    [HarmonyPatch]
    static class Haul_Patch
    {

        static List<PlannedHauls> plannedHaulList = new List<PlannedHauls>();

        static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(Verse.AI.HaulAIUtility), "HaulToStorageJob", new Type[] { typeof(Pawn), typeof(Thing) });
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> original)
        {
            var originalMethod = AccessTools.Method(typeof(StoreUtility), "TryFindBestBetterStoreCellFor");
            var modifiedMethod = AccessTools.Method(typeof(Haul_Patch), "TryFindBestBetterStoreCellForReplacement");
            return original.MethodReplacer(originalMethod, modifiedMethod);
        }


        static bool TryFindBestBetterStoreCellForReplacement(Thing t, Pawn carrier, Map map, StoragePriority currentPriority, Faction faction, out IntVec3 foundCell, bool needAccurateResult = true)
        {
            //HaulToStack.Instance.Logger.Trace("FINDING STORAGE FOR ITEM " + t.def.defName);

            List<SlotGroup> allGroupsListInPriorityOrder = map.slotGroupManager.AllGroupsListInPriorityOrder;
            if (allGroupsListInPriorityOrder.Count == 0)
            {
                foundCell = IntVec3.Invalid;
                return false;
            }
            IntVec3 a = (!t.SpawnedOrAnyParentSpawned) ? carrier.PositionHeld : t.PositionHeld;
            StoragePriority storagePriority = currentPriority;
            float num = 2.14748365E+09f;
            IntVec3 intVec = default(IntVec3);
            IntVec3 plannedTile = default(IntVec3);
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
                    //count2 is the size of the cell
                    int count2 = cellsList.Count;
                    int num2;
                    if (needAccurateResult)
                    {
                        num2 = Mathf.FloorToInt((float)count2 * Rand.Range(0.005f, 0.018f));
                        //Log.Message("num2: " + num2);
                    }
                    else
                    {
                        num2 = 0;
                    }
                    for (int j = 0; j < count2; j++)
                    {
                        IntVec3 intVec2 = cellsList[j];
                        float num3 = (float)(a - intVec2).LengthHorizontalSquared;

                        if (!(num3 <= num))
                        {
#if DEBUG
                            HaulToStack.Instance.Logger.Trace("Game usually doesn't attempt to haul on this condition");
#endif
                            if (CellIsReachable(intVec2, map, t, carrier, faction))
                            {
                                string stackSituation = CellCanStack(intVec2, map, t);
                                if (stackSituation.Equals("stackable"))
                                {
                                    //This is the ideal situation
#if DEBUG
                                    HaulToStack.Instance.Logger.Trace("We found a stacker!");
#endif
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
                        else
                        {
                            if (CellIsReachable(intVec2, map, t, carrier, faction))
                            {
                                string stackSituation = CellCanStack(intVec2, map, t);

                                if (stackSituation.Equals("clear"))
                                {
                                    flag = true;
                                    intVec = intVec2;
                                    plannedTile = intVec;
                                    num = num3;
                                    storagePriority = priority;
                                }
                                else if (stackSituation.Equals("stackable"))
                                {
                                    //This is the ideal situation
#if DEBUG
                                    HaulToStack.Instance.Logger.Trace("We found a stacker!");
#endif
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
                        //if (IsGoodStoreCell_Replacement(intVec2, map, t, carrier, faction))
                        //{
                        //    flag = true;
                        //    intVec = intVec2;
                        //    num = num3;
                        //    storagePriority = priority;
                        //    if (j >= num2)
                        //    {
                        //        break;
                        //    }
                        //}

                    }
                }
            }

            if (!flag)
            {
                foundCell = IntVec3.Invalid;
                //HaulToStack.Instance.Logger.Trace("Failed to find a stack location");
                return false;
            }
            foundCell = intVec;

            /**
             * TODO: continue working on this
             */
            //We're starting a new stack
            if (plannedTile.Equals(foundCell))
            {

#if DEBUG
                PlannedHauls ph = new PlannedHauls(plannedTile, t, t.stackCount);
                HaulToStack.Instance.Logger.Trace("Adding to Planned Hauls");
                HaulToStack.Instance.Logger.Trace(t.def.defName + " " + t.stackCount);
                plannedHaulList.Add(ph);
#endif

            }

            //HaulToStack.Instance.Logger.Trace("We found a stack location");
            return true;
        }

        static bool CellIsReachable(IntVec3 c, Map map, Thing t, Pawn carrier, Faction faction)
        {
            if (carrier != null && c.IsForbidden(carrier))
            {
                return false;
            }
            /**
             * Commented out code here stops a check for tile reservation.
             * It might be a good idea to turn this back on now that the job doesn't reserve the tile
             */
            if (carrier != null)
            {
                if (!carrier.CanReserveNew(c))
                {
                    return false;
                }
            }
            else if (faction != null && map.reservationManager.IsReservedByAnyoneOf(c, faction))
            {
                return false;
            }
            return !c.ContainsStaticFire(map) && (carrier == null || carrier.Map.reachability.CanReach((!t.SpawnedOrAnyParentSpawned) ? carrier.PositionHeld : t.PositionHeld, c, PathEndMode.ClosestTouch, TraverseParms.For(carrier, Danger.Deadly, TraverseMode.ByPawn, false)));
        }


        static string CellCanStack(IntVec3 c, Map map, Thing thing)
        {
            List<Thing> list = map.thingGrid.ThingsListAt(c);
            bool potentialStack = false;
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing2 = list[i];

                //HaulToStack.Instance.Logger.Trace("Item on tile is: " + thing2.def.defName);
                //HaulToStack.Instance.Logger.Trace("Item on hand is: " + thing.def.defName);

                if (thing2.def.EverStoreable)
                {
                    if (!thing2.CanStackWith(thing))
                    {
                        //HaulToStack.Instance.Logger.Trace("Can't stack on eachother");
                        return "unusable";
                    }
                    if (thing2.stackCount >= thing.def.stackLimit)
                    {
                        //HaulToStack.Instance.Logger.Trace("Stack count issue");
                        return "unusable";
                    }
                }
                if (thing2.def.entityDefToBuild != null && thing2.def.entityDefToBuild.passability != Traversability.Standable)
                {
                    //HaulToStack.Instance.Logger.Trace("impassible terrain");
                    return "unusable";
                }
                if (thing2.def.surfaceType == SurfaceType.None && thing2.def.passability != Traversability.Standable)
                {
                    //HaulToStack.Instance.Logger.Trace("different impassible terrain");
                    return "unusable";
                }
                if (thing2.def.defName.Equals(thing.def.defName))
                {
                    potentialStack = true;
                }
            }
            if(potentialStack)
            {
                return "stackable";
            }
            return "clear";
        }


        //static bool IsGoodStoreCell_Replacement(IntVec3 c, Map map, Thing t, Pawn carrier, Faction faction)
        //{
        //    if (carrier != null && c.IsForbidden(carrier))
        //    {
        //        HaulToStack.Instance.Logger.Trace("IsGoodStoreCell: " + 1);
        //        return false;
        //    }
        //    if (!NoStorageBlockersIn(c, map, t))
        //    {
        //        HaulToStack.Instance.Logger.Trace("IsGoodStoreCell: " + 2);
        //        return false;
        //    }
        //    if (carrier != null)
        //    {
        //        if (!carrier.CanReserve(c, 1, -1, null, false))
        //        {
        //            HaulToStack.Instance.Logger.Trace("IsGoodStoreCell: " + 3);
        //            return false;
        //        }
        //    }
        //    else if (map.reservationManager.IsReserved(c, faction))
        //    {
        //        HaulToStack.Instance.Logger.Trace("IsGoodStoreCell: " + 4);
        //        return false;
        //    }
        //    HaulToStack.Instance.Logger.Trace("IsGoodStoreCell: " + 5);
        //    return !c.ContainsStaticFire(map) && (carrier == null || carrier.Map.reachability.CanReach((!t.SpawnedOrAnyParentSpawned) ? carrier.PositionHeld : t.PositionHeld, c, PathEndMode.ClosestTouch, TraverseParms.For(carrier, Danger.Deadly, TraverseMode.ByPawn, false)));
        //}


        //private static bool NoStorageBlockersIn(IntVec3 c, Map map, Thing thing)
        //{
        //    List<Thing> list = map.thingGrid.ThingsListAt(c);
        //    for (int i = 0; i < list.Count; i++)
        //    {
        //        Thing thing2 = list[i];
        //        if (thing2.def.EverStoreable)
        //        {
        //            if (!thing2.CanStackWith(thing))
        //            {
        //                HaulToStack.Instance.Logger.Trace("NoStorageBlockersIn: " + 1);
        //                return false;
        //            }
        //            if (thing2.stackCount >= thing.def.stackLimit)
        //            {
        //                HaulToStack.Instance.Logger.Trace("NoStorageBlockersIn: " + 2);
        //                return false;
        //            }
        //        }
        //        if (thing2.def.entityDefToBuild != null && thing2.def.entityDefToBuild.passability != Traversability.Standable)
        //        {
        //            HaulToStack.Instance.Logger.Trace("NoStorageBlockersIn: " + 3);
        //            return false;
        //        }
        //        if (thing2.def.surfaceType == SurfaceType.None && thing2.def.passability != Traversability.Standable)
        //        {
        //            HaulToStack.Instance.Logger.Trace("NoStorageBlockersIn: " + 4);
        //            return false;
        //        }
        //    }
        //    HaulToStack.Instance.Logger.Trace("NoStorageBlockersIn: " + 5);
        //    return true;
        //}
    }

}
