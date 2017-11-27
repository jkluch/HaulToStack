using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace HaulToStack
{
    class PlannedHauls
    {
        Thing t;
        private IntVec3 plannedTile;
        private int count;

        public PlannedHauls(IntVec3 plannedTile, Thing t, int count)
        {
            this.plannedTile = plannedTile;
            this.t = t;
            this.count = count;
        }
    }
}
