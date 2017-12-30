﻿using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Verse.AI;

namespace Vampire
{
    /// <summary>
    /// Spawns invisible beds on top of objects that are not actually beds.
    /// This provids a good way to make sleeping points for vampires.
    /// E.g. Caskets, Coffins, Sarcophagi
    /// </summary>
    public class CompVampBed : ThingComp
    {
        private Building_Bed bed;
        public Building_Bed Bed { get => bed; set => bed = value; }
        public CompProperties_VampBed Props => props as CompProperties_VampBed;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            SpawnBedAsNeeded();
        }

        /// <summary>
        /// Adds 'Enter Torpor' option.
        /// </summary>
        /// <param name="selPawn"></param>
        /// <returns></returns>
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            //return base.CompFloatMenuOptions(selPawn);

            if ((selPawn?.IsVampire() ?? false) && parent is Building_Grave g && !g.HasCorpse)
            {
                yield return new FloatMenuOption("ROMV_EnterTorpor".Translate(new object[]
                {
                    selPawn.Label
                }), delegate
                {
                    selPawn.jobs.TryTakeOrderedJob(new Job(VampDefOf.ROMV_EnterTorpor, parent));
                });
            }
        }

        /// <summary>
        /// Checks to add / remove the bed and assign / unassign bed users as needed.
        /// </summary>
        public override void CompTickRare()
        {
            base.CompTickRare();
            if (parent is Building_Grave g)
            {
                if (g.HasAnyContents)
                {
                    if (bed != null && bed.Spawned)
                    {
                        bed.Destroy();
                        bed = null;
                    }
                    if (g.TryGetInnerInteractableThingOwner().FirstOrDefault(x => x is MinifiedThing) is MinifiedThing t)
                    {
                        g.TryGetInnerInteractableThingOwner().Remove(t);
                        //Log.Message("Removed " + t.Label);
                    }
                    return;
                }

                SpawnBedAsNeeded();
                
                //Remove and add characters to the bed.
                if (bed == null || !bed.Spawned) return;
                var bedPawns = bed?.AssignedPawns?.ToList();
                if (bedPawns == null) return;
                AssignBedPawnsAsNeeded(g, bedPawns);
            }
        }

        /// <summary>
        /// If no bed is spawned above the furniture, spawn a bed. This acts as an easy way
        /// to avoid patching a lot of RimWorld methods.
        /// </summary>
        private void SpawnBedAsNeeded()
        {
            if (bed == null && parent.TryGetInnerInteractableThingOwner().Count == 0)
            {
                ThingDef stuff = null;
                if (parent is Building b)
                {
                    stuff = b.Stuff;
                }
                bed = (Building_Bed) ThingMaker.MakeThing(Props.bedDef, stuff);
                GenSpawn.Spawn(bed, parent.Position, parent.Map, parent.Rotation);
                bed.SetFaction(parent.Faction);
            }
        }

        /// <summary>
        /// Remove and add assignments to the bed as needed.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="bedPawns"></param>
        private void AssignBedPawnsAsNeeded(Building_Grave g, List<Pawn> bedPawns)
        {
            if (g.AssignedPawns != null && g.AssignedPawns.Any())
            {
                if (bedPawns.Any())
                {
                    var tempBedPawns = new List<Pawn>(bedPawns);
                    foreach (var bedPawn in tempBedPawns)
                    {
                        if (!g.AssignedPawns.Contains(bedPawn))
                            bed.TryUnassignPawn(bedPawn);
                    }
                }
                foreach (var gravePawn in g.AssignedPawns)
                    bed.TryAssignPawn(gravePawn);
            }
            else if (bed.AssignedPawns.Any())
                foreach (var pawn in bedPawns)
                    bed.TryUnassignPawn(pawn);
        }


        /// <summary>
        /// Destroy the bed when we despawn.
        /// </summary>
        /// <param name="map"></param>
        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (bed != null && bed.Spawned)
            {
                bed.Destroy();
                bed = null;
            }
        }

        /// <summary>
        /// Save the bed info.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref bed, "bed");
        }
    }
}
