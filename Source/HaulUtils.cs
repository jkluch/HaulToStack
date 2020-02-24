using System;
using System.Collections.Generic;

//using Harmony;
//using HugsLib;
//using HugsLib.Utils;
//using RimWorld;
//using Verse;
//using UnityEngine;
//using System.Reflection;
//using Verse.AI;

namespace HaulToStack
{
    class HaulUtils
    {
//        public static String CellCanStack(IntVec3 location, Map map, Thing thing)
//        {
//            List<Thing> list = map.thingGrid.ThingsListAt(location);
//            bool potentialStack = false;
//            for (int i = 0; i < list.Count; i++)
//            {
//                Thing thing2 = list[i];

//                //HaulToStack.Instance.Logger.Trace("Item on tile is: " + thing2.def.defName);
//                //HaulToStack.Instance.Logger.Trace("Item on hand is: " + thing.def.defName);

//                if (thing2.def.EverStoreable)
//                {
//                    if (!thing2.CanStackWith(thing))
//                    {
//                        //HaulToStack.Instance.Logger.Trace("Can't stack on eachother");
//                        return "unusable";
//                    }
//                    if (thing2.stackCount >= thing.def.stackLimit)
//                    {
//                        //HaulToStack.Instance.Logger.Trace("Stack count issue");
//                        return "unusable";
//                    }
//                }
//                if (thing2.def.entityDefToBuild != null && thing2.def.entityDefToBuild.passability != Traversability.Standable)
//                {
//                    //HaulToStack.Instance.Logger.Trace("impassible terrain");
//                    return "unusable";
//                }
//                if (thing2.def.surfaceType == SurfaceType.None && thing2.def.passability != Traversability.Standable)
//                {
//                    //HaulToStack.Instance.Logger.Trace("different impassible terrain");
//                    return "unusable";
//                }
//                if (thing2.def.defName.Equals(thing.def.defName))
//                {
//                    potentialStack = true;
//                }
//            }
//            if (potentialStack)
//            {
//                return "stackable";
//            }
//            return "clear";
//        }

//        internal static bool ShouldReserveHaulLocation(Thing thing, IntVec3 destination, Pawn pawn, Map map)
//        {
//            var destinationThing = map.thingGrid.ThingsListAt(destination).Find(x => x.def.defName == thing.def.defName);

//#if DEBUG
//            HaulToStack.Instance.Logger.Trace("Checking if we should reserve the destination");
//            HaulToStack.Instance.Logger.Trace("Pawn grabbing stack of: " + thing.stackCount);
//#endif
//            //If this is the case, we are carrying to a new (clear) location
//            //Technically we also would want to reserve if the max stack size of the item is one
//            //However we already are handling that case in TryMakePreToilReservations()
//            if (destinationThing == null)
//            {
//#if DEBUG
//                HaulToStack.Instance.Logger.Trace("NOT RESERVING DESTINATION");
//                HaulToStack.Instance.Logger.Trace("Destination empty, not reserving");
//#endif
//                return false;
//            }


//            //If pawn currently isn't holding anything just check the destination stack + hauling stack
//            if (pawn.carryTracker.CarriedThing == null)
//            {
//                if (destinationThing.stackCount + Math.Min(pawn.carryTracker.MaxStackSpaceEver(thing.def), thing.stackCount) >= thing.def.stackLimit)
//                {
//#if DEBUG
//                    HaulToStack.Instance.Logger.Trace("RESERVING DESTINATION");
//                    HaulToStack.Instance.Logger.Trace("Pawn not carrying anything but the stack they're grabbing is going to overfill the destination");
//#endif
//                    return true;
//                }
//                else
//                {
//#if DEBUG
//                    HaulToStack.Instance.Logger.Trace("NOT RESERVING DESTINATION");
//                    HaulToStack.Instance.Logger.Trace("Pawn can't carry enough to fill the destination stack");
//#endif
//                    return false;
//                }
//            }

//            //Destination has a stack
//            //Pawn is holding items
//#if DEBUG
//            HaulToStack.Instance.Logger.Trace("Pawn holding stack of: " + pawn.carryTracker.CarriedThing.stackCount);
//#endif

//            if (destinationThing.stackCount + pawn.carryTracker.CarriedThing.stackCount + thing.stackCount >= thing.def.stackLimit)
//            {
//#if DEBUG
//                HaulToStack.Instance.Logger.Trace("RESERVING DESTINATION");
//                HaulToStack.Instance.Logger.Trace("Pawn is going to overfill the destination");
//#endif
//                return true;
//            }   
//            else
//            {
//#if DEBUG
//                HaulToStack.Instance.Logger.Trace("NOT RESERVING DESTINATION");
//                HaulToStack.Instance.Logger.Trace("Pawn isn't carrying enough to fill the destination stack");
//#endif
//                return false;
//            }
            
//        }

//        internal static Toil CheckForGetOpportunityDuplicateReplace(Toil getHaulTargetToil, TargetIndex haulableInd, TargetIndex storeCellInd, bool takeFromValidStorage = false, Predicate<Thing> extraValidator = null)
//        {
//            Toil toil = new Toil();
//            toil.initAction = delegate
//            {
//                Pawn actor = toil.actor;
//                Job curJob = actor.jobs.curJob;
//                if (actor.carryTracker.CarriedThing.def.stackLimit == 1)
//                {
//                    return;
//                }
//                if (actor.carryTracker.Full)
//                {
//                    return;
//                }
//                if (curJob.count <= 0)
//                {
//                    return;
//                }
//                Predicate<Thing> validator = (Thing t) => t.Spawned && t.def == actor.carryTracker.CarriedThing.def && t.CanStackWith(actor.carryTracker.CarriedThing) && !t.IsForbidden(actor) && (takeFromValidStorage || !t.IsInValidStorage()) && (storeCellInd == TargetIndex.None || curJob.GetTarget(storeCellInd).Cell.IsValidStorageFor(actor.Map, t)) && actor.CanReserve(t, 1, -1, null, false) && (extraValidator == null || extraValidator(t));
//                Thing thing = GenClosest.ClosestThingReachable(actor.Position, actor.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableAlways), PathEndMode.ClosestTouch, TraverseParms.For(actor, Danger.Deadly, TraverseMode.ByPawn, false), 8f, validator, null, 0, -1, false, RegionType.Set_Passable, false);
//                if (thing != null)
//                {
//                    curJob.SetTarget(haulableInd, thing);
//                    actor.jobs.curDriver.JumpToToil(getHaulTargetToil);
//#if DEBUG
//                    HaulToStack.Instance.Logger.Trace("In opportunistic pickup");
//#endif
//                    if ( ShouldReserveHaulLocation(curJob.targetA.Thing, curJob.targetB.Cell, actor, actor.Map) )
//                        actor.Reserve(curJob.GetTarget(storeCellInd), curJob);
//                }
//            };
//            return toil;
//        }
    }
}
