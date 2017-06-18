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
            /**
             * Default Code
             **/
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
                            if (CellIsReachable(intVec2, map, t, carrier, faction))
                            {
                                String stackSituation = CellCanStack(intVec2, map, t);
                                if (stackSituation.Equals("stackable"))
                                {
                                    //This is the ideal situation
                                    //HaulToStack.Instance.Logger.Trace("We found a stacker!");
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
                                String stackSituation = CellCanStack(intVec2, map, t);

                                if (stackSituation.Equals("clear"))
                                {
                                    flag = true;
                                    intVec = intVec2;
                                    num = num3;
                                    storagePriority = priority;
                                }
                                else if (stackSituation.Equals("stackable"))
                                {
                                    //This is the ideal situation
                                    HaulToStack.Instance.Logger.Trace("We found a stacker!");
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
            }
            if (!flag)
            {
                foundCell = IntVec3.Invalid;
                //HaulToStack.Instance.Logger.Trace("Failed to find a stack location");
                return false;
            }
            foundCell = intVec;
            //HaulToStack.Instance.Logger.Trace("We found a stack location");
            return true;
            /**
             * Default Code
             **/
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

            //if (carrier != null)
            //{
            //    if (!carrier.CanReserve(c, 1, -1, null, false))
            //    {
            //        return false;
            //    }
            //}
            //else if (map.reservationManager.IsReserved(c, faction))
            //{
            //    return false;
            //}
            return !c.ContainsStaticFire(map) && (carrier == null || carrier.Map.reachability.CanReach((!t.SpawnedOrAnyParentSpawned) ? carrier.PositionHeld : t.PositionHeld, c, PathEndMode.ClosestTouch, TraverseParms.For(carrier, Danger.Deadly, TraverseMode.ByPawn, false)));
        }


        static String CellCanStack(IntVec3 c, Map map, Thing thing)
        {
            List<Thing> list = map.thingGrid.ThingsListAt(c);
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
                    return "stackable";
                }
            }

            return "clear";
        }


        static bool IsGoodStoreCell_Replacement(IntVec3 c, Map map, Thing t, Pawn carrier, Faction faction)
        {
            if (carrier != null && c.IsForbidden(carrier))
            {
                return false;
            }
            if (!NoStorageBlockersIn(c, map, t))
            {
                return false;
            }
            if (carrier != null)
            {
                if (!carrier.CanReserve(c, 1, -1, null, false))
                {
                    return false;
                }
            }
            else if (map.reservationManager.IsReserved(c, faction))
            {
                return false;
            }
            return !c.ContainsStaticFire(map) && (carrier == null || carrier.Map.reachability.CanReach((!t.SpawnedOrAnyParentSpawned) ? carrier.PositionHeld : t.PositionHeld, c, PathEndMode.ClosestTouch, TraverseParms.For(carrier, Danger.Deadly, TraverseMode.ByPawn, false)));
        }


        private static bool NoStorageBlockersIn(IntVec3 c, Map map, Thing thing)
        {
            List<Thing> list = map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing2 = list[i];
                if (thing2.def.EverStoreable)
                {
                    if (!thing2.CanStackWith(thing))
                    {
                        return false;
                    }
                    if (thing2.stackCount >= thing.def.stackLimit)
                    {
                        return false;
                    }
                }
                if (thing2.def.entityDefToBuild != null && thing2.def.entityDefToBuild.passability != Traversability.Standable)
                {
                    return false;
                }
                if (thing2.def.surfaceType == SurfaceType.None && thing2.def.passability != Traversability.Standable)
                {
                    return false;
                }
            }
            return true;
        }
    }

}
