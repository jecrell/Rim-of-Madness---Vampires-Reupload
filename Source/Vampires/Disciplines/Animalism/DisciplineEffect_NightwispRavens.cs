﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Vampire
{
    public class DisciplineEffect_NightwispRavens : Verb_UseAbilityPawnEffect
    {
        public override void Effect(Pawn target)
        {
            base.Effect(target);
            VampireUtility.SummonEffect(target.PositionHeld, this.CasterPawn.Map, this.CasterPawn, 2f);

            HealthUtility.AdjustSeverity(target, VampDefOf.ROMV_NightwispRavens, 1.0f);
        }
    }
}