﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace Vampire
{
    public class Building_Coffin : Building_Sarcophagus
    {
        // RimWorld.Building_Grave
        private Graphic cachedGraphicFull;


        //public override void Draw()

        //public override bool Accepts(Thing thing)


        // RimWorld.Building_Grave
        //public override Graphic Graphic

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
                yield return g;

            Pawn p = (Pawn)this.ContainedThing;
            if (p == null)
            {
                p = this?.Corpse?.InnerPawn ?? null;
            }
            if (p != null)
            {
                foreach (Gizmo y in HarmonyPatches.GraveGizmoGetter(p, this))
                    yield return y;
            }
        }


        //public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)


    }
}