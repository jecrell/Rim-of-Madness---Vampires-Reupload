﻿using RimWorld;
using Verse;

namespace Vampire
{
    public class Building_HideyHole : Building_Grave
    {
        public override void Open()
        {
            base.Open();
            Destroy();
        }
    }
}
