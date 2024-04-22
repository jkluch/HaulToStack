using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace HaulToStack
{

    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);
        private static MethodInfo delegatedFailCondition = null;

        static HarmonyPatches()
        {
            //Harmony.DEBUG = true;
            var harmony = new Harmony("com.jkluch.HaulToStack");

            harmony.Patch(
                original: AccessTools.Method(typeof(JobDriver_HaulToCell), "TryMakePreToilReservations", new Type[] { typeof(bool) }),
                prefix: new HarmonyMethod(methodType: patchType, methodName: nameof(PreToilReservations_Prefix)),
                postfix: new HarmonyMethod(methodType: patchType, methodName: nameof(PreToilReservations_Postfix))
                );

            harmony.Patch(
                original: AccessTools.Method(typeof(StoreUtility), "TryFindBestBetterStoreCellForWorker"),
                prefix: null,
                postfix: new HarmonyMethod(methodType: patchType, methodName: nameof(TryFindBestBetterStoreCellForWorker_Postfix))
                );

#if DEBUG
            //Add a feature to CarryHauledThingToCell so if the haul - to tile is now reserved we look for a new location
            harmony.Patch(
                original: AccessTools.Method(typeof(Toils_Haul), "CarryHauledThingToCell"),
                prefix: null,
                transpiler: new HarmonyMethod(methodType: patchType, methodName: nameof(CarryHauledThingToCell_Transpiler))
                );

            harmony.Patch(
                original: delegatedFailCondition,
                prefix: null,
                transpiler: new HarmonyMethod(methodType: patchType, methodName: nameof(DelegatedFailCondition_Transpiler))
                );

            //harmony.Patch(
            //original: AccessTools.Method(method: typeof(MainTabWindow_Research), method: "ViewSize"),
            //prefix: null,
            //postfix: null,
            //transpiler: new HarmonyMethod(methodType: patchType, methodName: nameof(ResearchScreenTranspiler)));
#endif
        }


        static bool PreToilReservations_Prefix()
        {
            //Skip the TryMakePreToilReservations method
            return false;
        }

        static void PreToilReservations_Postfix(Verse.AI.JobDriver_HaulToCell __instance, ref bool __result, bool errorOnFailed)
        {
            Pawn pawn = __instance.pawn;
            Job job = __instance.job;
            const TargetIndex HaulableInd = TargetIndex.A;
            const TargetIndex StoreCellInd = TargetIndex.B;
            LocalTargetInfo thing = __instance.job.GetTarget(HaulableInd);
            LocalTargetInfo destination = __instance.job.GetTarget(StoreCellInd);
            HaulToStack.Instance.Logger.Trace("---------------- Start of PreToilReservations_Postfix ----------------");
            HaulToStack.Instance.Logger.Trace("Pawn " + pawn.Name + " finding storage for: " + job.targetA.Thing.def.defName);
            HaulToStack.Instance.Logger.Trace("Stack limit:" + job.targetA.Thing.def.stackLimit);
            HaulToStack.Instance.Logger.Trace("Job Count:" + job.count);
            HaulToStack.Instance.Logger.Trace("Placing at: " + destination.Cell.GetSlotGroup(pawn.Map));
            HaulToStack.Instance.Logger.Trace("Error on fail set to: " + errorOnFailed.ToString());
            HaulToStack.Instance.Logger.Trace("Initial result: " + __result.ToString());

            //If stacklimit for this item is 1, then reserve the tile and the thing
            if (job.targetA.Thing.def.stackLimit <= 1)
            {
                //HaulToStack.Instance.Logger.Trace("Inside full reservation mode");
                if (pawn.Reserve(destination, job, 1, -1, null, errorOnFailed))
                {
                    HaulToStack.Instance.Logger.Trace("Pawn reserve was successful on destination");
                    __result = pawn.Reserve(thing, job, 1, -1, null, errorOnFailed);
                    HaulToStack.Instance.Logger.Trace("Pawn reserve returned " + __result.ToString() + " on thing");
                }
                else
                {
                    HaulToStack.Instance.Logger.Trace("Failed to reserve destination, setting result to false");
                    __result = false;
                }
            }
            //We always want to reserve what we're hauling
            //Kluch: In the future we might want to set this based on whether or not the pawn can haul the whole stack or not
            //I tried not reserving the thing but other pawns wtill wouldn't haul from that stack so we're going to just reserve it
            else
            {
                //HaulToStack.Instance.Logger.Trace("Reserving just the thing");
                __result = pawn.Reserve(thing, job, 1, -1, null, errorOnFailed);
            }
            HaulToStack.Instance.Logger.Trace("Result: " + __result.ToString());
        }

        //See if closestSlot already contains Thing, if not double check the whole storage cell to see if there's an existing stack we want to force our Thing onto
        static void TryFindBestBetterStoreCellForWorker_Postfix(Thing t, Pawn carrier, Map map, Faction faction, SlotGroup slotGroup, ref IntVec3 closestSlot)
        {

            if (!closestSlot.InBounds(map))
            {
                HaulToStack.Instance.Logger.Message("The original location picked to place item is out of bounds for an unknown reason");
                HaulToStack.Instance.Logger.Message($"Thing {t.def.defName} Pawn: {carrier.Name}");
                HaulToStack.Instance.Logger.Message($"Pawn: {carrier.Name}");
                return;
            }

            if (closestSlot.GetThingList(map).Exists(item => item.def.defName == t.def.defName))
            {
                HaulToStack.Instance.Logger.Trace("The original location has: " + t.def.defName);
                return;
            }
            else
            {
                if (slotGroup == null || !slotGroup.parent.Accepts(t))
                {
                    //I don't think I needed this check in 1.0 but clearly this is needed in 1.1
                    HaulToStack.Instance.Logger.Message("slotgroup is null or doesn't accept item");
                    return;
                }
                HaulToStack.Instance.Logger.Trace("THE PLACE WE WERE GOING TO HAUL TO DOESN'T HAVE: " + t.def.defName);
                List<IntVec3> cellsList = slotGroup.CellsList;
                foreach (IntVec3 cell in cellsList)
                {
                    if (cell.InBounds(map))
                    {
                        HaulToStack.Instance.Logger.Trace("cell in the provided slotGroup is in bounds");
                        if (cell.GetThingList(map).Exists(item => item.def.defName == t.def.defName))
                        {
                            if (StoreUtility.IsGoodStoreCell(cell, map, t, carrier, faction))
                            {
                                HaulToStack.Instance.Logger.Trace("FOUND AN EXISTING TILE WITH " + t.def.defName);
                                closestSlot = cell;
                                return;
                            }
                        }
                    }
                    else
                    {
                        HaulToStack.Instance.Logger.Message("cell in the provided slotGroup is out of bounds");
                    }
                }

                HaulToStack.Instance.Logger.Trace("TILE WITH EXISTING STACK OF " + t.def.defName + " DOES NOT EXIST, USING PREDETERMINED STACK LOCATION");
                HaulToStack.Instance.Logger.Trace("----------------------------------------------------------------------------------------------------");

            }
        }

#if DEBUG
        static IEnumerable<CodeInstruction> CarryHauledThingToCell_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo defListInfo = AccessTools.Method(type: typeof(Toil), name: "AddFailCondition");

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                
                //codes[i].Calls(defListInfo)
                if (codes[i].Calls(defListInfo))
                {
                    Log.Message("-------Found AddFailCondition---------");
                    //Log.Message(codes[i - 2].ToString());
                    //Log.Message(codes[i - 1].ToString());
                    //Log.Message(codes[i].ToString());
                    delegatedFailCondition = (MethodInfo)codes[i - 2].operand;
                    yield return codes[i];
                    //yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: patchType, name: nameof(CarryHauledThingToCell_Fix)));
                }
                else
                {
                    yield return codes[i];
                }
            }
        }

        static IEnumerable<CodeInstruction> DelegatedFailCondition_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            Log.Message("-------Found DelegatedFailCondition---------");
            for (int i = 0; i < codes.Count; i++)
            {
                Log.Message(codes[i].ToString());
                yield return codes[i];
            }
            Log.Message("-------Done DelegatedFailCondition---------");
        }

        //Issue here is.. I want to alter the toil.AddFailCondition of CarryHauledThingToCell.. but I really only want to change the delegate not the AddFailCondition method
        //static IEnumerable<CodeInstruction> CarryHauledThingToCell_Fix(Func<bool> newFailCondition)
        //{
        //    Toil toil = new Toil();
        //    toil.initAction = delegate ()
        //    {
        //        IntVec3 cell = toil.actor.jobs.curJob.GetTarget(squareIndex).Cell;
        //        toil.actor.pather.StartPath(cell, PathEndMode.ClosestTouch);
        //    };
        //    toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
        //    toil.AddFailCondition(delegate
        //    {
        //        Pawn actor = toil.actor;
        //        IntVec3 cell = actor.jobs.curJob.GetTarget(squareIndex).Cell;
        //        return actor.jobs.curJob.haulMode == HaulMode.ToCellStorage && !cell.IsValidStorageFor(actor.Map, actor.carryTracker.CarriedThing);
        //    });
        //    return toil;
        //}
#endif
    }
}