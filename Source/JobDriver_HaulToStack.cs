/**
 * This JobDriver is almost entirely the game's source code for JobDriver_HaulToCell
 * I made slight modifications to prevent haulers from reserving the tile they haul to
 */

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace HaulToStack
{
    class JobDriver_HaulToStack : JobDriver
    {
        //Constants
        private const TargetIndex HaulableInd = TargetIndex.A;
        private const TargetIndex StoreCellInd = TargetIndex.B;

        public override string GetReport()
        {
            IntVec3 destLoc = pawn.jobs.curJob.targetB.Cell;

            Thing hauledThing = null;
            if (pawn.carryTracker.CarriedThing != null)
                hauledThing = pawn.carryTracker.CarriedThing;
            else
                hauledThing = TargetThingA;

            string destName = null;
            var destGroup = destLoc.GetSlotGroup(Map);
            if (destGroup != null)
                destName = destGroup.parent.SlotYielderLabel();

            string repString;
            if (destName != null)
                repString = "ReportHaulingTo".Translate(hauledThing.LabelCap, destName);
            else
                repString = "ReportHauling".Translate(hauledThing.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Set fail conditions
            this.FailOnDestroyedOrNull(HaulableInd);
            this.FailOnBurningImmobile(StoreCellInd);

            //Note we only fail on forbidden if the target doesn't start that way
            //This helps haul-aside jobs on forbidden items
            //
            // TODO instead of this, just use Job.ignoreForbidden where appropriate
            //
            if (!TargetThingA.IsForbidden(pawn))
                this.FailOnForbidden(HaulableInd);


            //Reserve target storage cell
            if(pawn.RaceProps.Animal)
            {
                HaulToStack.Instance.Logger.Trace("Animal is hauling!");
                yield return Toils_Reserve.Reserve(StoreCellInd);
            }
            //yield return Toils_Reserve.Reserve(StoreCellInd);

            //Reserve thing to be stored
            Toil reserveTargetA = Toils_Reserve.Reserve(HaulableInd);
            yield return reserveTargetA;

            Toil toilGoto = null;
            toilGoto = Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(HaulableInd)
                .FailOn(() =>
                {
                //Note we don't fail on losing hauling designation
                //Because that's a special case anyway

                //While hauling to cell storage, ensure storage dest is still valid
                Pawn actor = toilGoto.actor;
                    Job curJob = actor.jobs.curJob;
                    if (curJob.haulMode == HaulMode.ToCellStorage)
                    {
                        Thing haulThing = curJob.GetTarget(HaulableInd).Thing;

                        IntVec3 destLoc = actor.jobs.curJob.GetTarget(TargetIndex.B).Cell;
                        if (!destLoc.IsValidStorageFor(Map, haulThing))
                            return true;
                    }

                    return false;
                });
            yield return toilGoto;


            yield return Toils_Haul.StartCarryThing(HaulableInd, subtractNumTakenFromJobCount: true);

            if (CurJob.haulOpportunisticDuplicates)
                yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveTargetA, HaulableInd, StoreCellInd);

            Toil carryToCell = Toils_Haul.CarryHauledThingToCell(StoreCellInd);
            yield return carryToCell;

            yield return Toils_Haul.PlaceHauledThingInCell(StoreCellInd, carryToCell, true);
        }
    }
}
