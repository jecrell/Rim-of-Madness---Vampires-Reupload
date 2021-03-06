﻿using UnityEngine;
using Verse;
using RimWorld;

namespace Vampire
{
    public class ScenPart_GameStartNight : ScenPart
    {
        private string text;

        private string textKey;

        private SoundDef closeSound;

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, RowHeight);
            //text = Widgets.TextArea(scenPartRect, text);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref text, "text");
            Scribe_Values.Look(ref textKey, "textKey");
            Scribe_Defs.Look(ref closeSound, "closeSound");
        }

        public override void PostGameStart()
        {
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 32500);
        }
    }
}
