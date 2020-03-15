﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vampire
{
    static partial class HarmonyPatches
    {
        // Verse.Dialog_DebugActionsMenu
        [DebugAction("Vampirism", "Spawn Vampire (Random)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugCommand_SpawnVampire(Pawn p)
        {
            Pawn randomVampire =
            VampireGen.GenerateVampire(VampireGen.RandHigherGeneration, VampireUtility.RandBloodline, null);
            GenSpawn.Spawn(randomVampire, UI.MouseCell(), Find.CurrentMap);
        }

        [DebugAction("Vampirism", "Give Vampirism (Default)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugCommand_GiveVampirism(Pawn pawn)
        {
            if (pawn != null)
            {
                if (!pawn.IsVampire())
                {
                    pawn.health.AddHediff(VampDefOf.ROM_Vampirism, null, null);
                    pawn.Drawer.Notify_DebugAffected();
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, pawn.LabelShort + " is now a vampire");
                }
                else
                    Messages.Message(pawn.LabelCap + " is already a vampire.", MessageTypeDefOf.RejectInput);
            }
        }

        [DebugAction("Vampirism", "Give Vampirism (w/Settings)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugCommand_GiveVampirismWithSettings(Pawn pawn)
        {
            if (pawn != null)
            {
                //pawn.health.AddHediff(VampDefOf.ROM_Vampirism, null, null);
                Find.WindowStack.Add(new Dialog_DebugOptionListLister(Options_Bloodlines(pawn)));
                //DebugTools.curTool = null;
            }
        }


        [DebugAction("Vampirism", "Remove Vampirism", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugCommand_RemoveVampirism(Pawn pawn)
        {
            if (pawn != null)
            {
                if (pawn.IsVampire())
                {
                    if (pawn.health.hediffSet.GetFirstHediffOfDef(VampDefOf.ROM_Vampirism) is HediffVampirism vampirism)
                    {
                        pawn.health.RemoveHediff(vampirism);
                    }
                    if (pawn?.health?.hediffSet?.GetHediffs<Hediff_AddedPart>()?.First() is Hediff_AddedPart_Fangs fangs)
                    {
                        BodyPartRecord rec = fangs.Part;
                        pawn.health.RemoveHediff(fangs);
                        pawn.health.RestorePart(rec);
                    }
                    pawn.Drawer.Notify_DebugAffected();
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, pawn.LabelShort + " is no longer a vampire");
                }
                else
                    Messages.Message(pawn.LabelCap + " is already a vampire.", MessageTypeDefOf.RejectInput);
            }
        }


        [DebugAction("Vampirism", "Add Blood (1)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugCommand_AddBlood(Pawn pawn)
        {
            if (pawn != null && pawn?.BloodNeed() is Need_Blood b)
            {
                b.AdjustBlood(1);
                pawn.Drawer.Notify_DebugAffected();
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "+1 Blood");
            }
        }

        [DebugAction("Vampirism", "Drain Blood (1)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugCommand_DrainBlood(Pawn pawn)
        {

            if (pawn != null && pawn?.BloodNeed() is Need_Blood b)
            {
                b.AdjustBlood(-1);
                pawn.Drawer.Notify_DebugAffected();
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "-1 Blood");
            }
        }


        [DebugAction("Vampirism", "Add Ghoul Blood (1)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugCommand_AddGoulBlood(Pawn pawn)
        {
            if (pawn != null && pawn.IsGhoul() && pawn?.BloodNeed() is Need_Blood b)
            {
                b.AdjustBlood(1, true, true);
                pawn.Drawer.Notify_DebugAffected();
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "+1 Ghoul Vitae");
            }
        }

        [DebugAction("Vampirism", "Drain Ghoul Blood (1)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugCommand_DrainGhoulBlood(Pawn pawn)
        {
            if (pawn != null && pawn.IsGhoul() && pawn?.BloodNeed() is Need_Blood b)
            {
                b.AdjustBlood(-1, true, true);
                pawn.Drawer.Notify_DebugAffected();
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "-1 Ghoul Blood");
            }
        }

        [DebugAction("Vampirism", "Add XP (100)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugCommand_AddXPOneHundred(Pawn pawn)
        {
                if (pawn != null && pawn?.VampComp() is CompVampire v)
                {
                        v.XP += 100;
                        pawn.Drawer.Notify_DebugAffected();
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "+100 XP");
                }
        }

        // Verse.DebugTools_Health
        private static List<DebugMenuOption> Options_Bloodlines(Pawn p)
        {
            if (p == null)
            {
                throw new ArgumentNullException("p");
            }
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (BloodlineDef current in DefDatabase<BloodlineDef>.AllDefs)
            {
                list.Add(new DebugMenuOption(current.LabelCap, DebugMenuOptionMode.Action, delegate
                {
                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(Options_Generation(p, current)));

                }));
            }
            return list;
        }

        private static List<DebugMenuOption> Options_Generation(Pawn p, BloodlineDef bloodline)
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            for (int i = 1; i < 14; i++)
            {
                int curGen = i;
                list.Add(new DebugMenuOption(curGen.ToString(), DebugMenuOptionMode.Action, delegate
                {
                    p.VampComp().InitializeVampirism(null, bloodline, curGen, curGen == 1);
                    //Log.Message("0" + p.LabelShort + " " + i.ToString());
                    p.Drawer.Notify_DebugAffected();
                    MoteMaker.ThrowText(p.DrawPos, p.Map, p.LabelShort + " is now a vampire");
                }));
            }
            return list;
        }

    }
}
