using System;
using System.Collections.Generic;
using System.Linq;
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
    [StaticConstructorOnStartup]
    class Main
    {
        static Main()
        {
            var harmony = HarmonyInstance.Create("com.jkluch.HaulToStack");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    public class HaulToStack : ModBase
    {
        public static HaulToStack Instance { get; private set; }

        public override string ModIdentifier
        {
            get { return "com.jkluch.HaulToStack"; }
        }

        protected override bool HarmonyAutoPatch
        {
            get { return false; }
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
    static class PreToilReservations_Patch
    {

        static List<PlannedHauls> plannedHaulList = new List<PlannedHauls>();

        static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(Verse.AI.JobDriver_HaulToCell), "TryMakePreToilReservations", new Type[] { typeof(bool) });
        }


        static bool Prefix()
        {
            //Skip the TryMakePreToilReservations method
            return false;
        }

        static void Postfix(Verse.AI.JobDriver_HaulToCell __instance, ref bool __result, bool errorOnFailed)
        {
            Pawn pawn = __instance.pawn;
            Job job = __instance.job;
            const TargetIndex HaulableInd = TargetIndex.A;
            const TargetIndex StoreCellInd = TargetIndex.B;
            LocalTargetInfo thing = __instance.job.GetTarget(HaulableInd);
            LocalTargetInfo destination = __instance.job.GetTarget(StoreCellInd);

            //HaulToStack.Instance.Logger.Trace("FINDING STORAGE FOR ITEM " + job.targetA.Thing.def.defName);
            //HaulToStack.Instance.Logger.Trace("Error on fail: " + errorOnFailed.ToString());
            //HaulToStack.Instance.Logger.Trace("Initial result: " + __result.ToString());

            //If stacklimit for this item is 1, then reserve the tile and the thing
            if (job.targetA.Thing.def.stackLimit <= 1)
            {
                //HaulToStack.Instance.Logger.Trace("Inside full reservation mode");
                if (pawn.Reserve(destination, job, 1, -1, null, errorOnFailed))
                {
                    __result = pawn.Reserve(thing, job, 1, -1, null, errorOnFailed);
                    //HaulToStack.Instance.Logger.Trace("Result: " + __result.ToString());
                }
                else
                {
                    //HaulToStack.Instance.Logger.Trace("Failed to reserve, setting result to false");
                    __result = false;
                }
            }
            //We always want to reserve what we're hauling
            //Kluch: In the future we might want to set this based on whether or not the pawn can haul the whole stack or not
            else
            {
                //HaulToStack.Instance.Logger.Trace("Reserving just the thing");
                __result = pawn.Reserve(thing, job, 1, -1, null, errorOnFailed);
                //HaulToStack.Instance.Logger.Trace("Result: " + __result.ToString());
            }
        }

    }

}
