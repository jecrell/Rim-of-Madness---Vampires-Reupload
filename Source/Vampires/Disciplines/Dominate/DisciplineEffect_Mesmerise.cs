﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Vampire
{
    public class DisciplineEffect_Mesmerise : Verb_UseAbilityPawnEffect
    {
        public override void Effect(Pawn target)
        {
            base.Effect(target);
            if (target.Faction == this.CasterPawn.Faction) //To avoid throwing red errors
                target.ClearMind();
        }
    }
}