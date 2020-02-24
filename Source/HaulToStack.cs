using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using HugsLib;
using HugsLib.Utils;
using RimWorld;
using Verse;
using System.Reflection;
using Verse.AI;

namespace HaulToStack
{

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

}
