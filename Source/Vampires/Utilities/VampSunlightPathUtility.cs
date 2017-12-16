﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Vampire
{
    public static class VampSunlightPathUtility
    {
        public static readonly int TicksFromDarknessToSunlight = 9700; //1 minute 37 seconds from darkness (0% lit) to sunrise (60% lit)
        public static readonly int TicksBetweenLightChanges = 161; //TicksFromDarknessToSunlight by 60. This is one unit between percent changes.
        public static readonly int TicksOfSurvivingSunlight = 1800; //30 real seconds in sunlight is when burning begins.

        /// <summary>
        /*
            No overriding path is made if...
                .1. Character(C) is indoors.
                .2. C drafted.
                .3. C's job's destination is ""safe""
                ""Safe"" jobs meet all these criteria...
                    .3a.a job's destination is indoors or under a roof AND
                       .i. it's nighttime OR
                       .ii. C can arrive before sunlight OR
                       .iii. C can arrive before sunlight damage.
        
            If distance to home is greater than or equal to the time until sunrise,
               start heading home.

            Special sunlight pather determines if a path goes into the sunlight.
               if a path through the sunlight home takes longer than VARIABLE X,
               X being how long it takes to crisp C,
               then do not take the path, and look for a closer safe place.
        */
        ///</summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static Job GetSunlightPathJob(Pawn pawn)
        {
            if (pawn.IsVampire() && pawn.VampComp() is CompVampire vampComp)
            {
                if (pawn.Drafted)
                    return null;
                if (pawn.pather != null && pawn.pather.Destination != null && pawn.pather.Destination.IsSunlightSafeFor(pawn))
                    return null;
                if (pawn.GetRoom(RegionType.Set_Passable) is Room room && room.PsychologicallyOutdoors)
                {
                    Job surviveJob;
                    if (TryGoingToHomePoint(pawn, out surviveJob))
                        return surviveJob;

                    if (TryGoingToSafePoint(pawn, out surviveJob))
                        return surviveJob;

                    if (TryDiggingHideyHole(pawn, out surviveJob))
                        return surviveJob;
                }
            }
            return null;
        }

        /// <summary>
        /// If C is of player faction..
        ///    Check if homePoint is valid AND
        ///    if homePoint is...
        ///      roofed OR
        ///      arrivable before sunlight OR
        ///      arrivable before burning one's flesh
        ///    ... then go home
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="newJob"></param>
        /// <returns></returns>
        public static bool TryGoingToHomePoint(Pawn pawn, out Job gotoJob)
        {
            gotoJob = null;
            if (pawn.Faction == Faction.OfPlayerSilentFail && pawn.VampComp().VampLastHomePoint is IntVec3 homePoint && homePoint.IsValid &&
            IsSunlightSafeFor(homePoint, pawn))
            {
                //Log.Message("VSPU :: Going home");
                gotoJob = new Job(JobDefOf.Goto, homePoint) { locomotionUrgency = LocomotionUrgency.Sprint };
                return true;
            }
            return false;
        }

        /// <summary>
        /// If no "home point" exists, go to the nearest safe point underneath a roof.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="gotoJob"></param>
        /// <returns></returns>
        public static bool TryGoingToSafePoint(Pawn pawn, out Job gotoJob)
        {
            gotoJob = null;
            if (FindSafeZoneFor(pawn) is IntVec3 safePoint && safePoint.IsValid)
            {
                if (IsSunlightSafeFor(safePoint, pawn))
                {
                    //Log.Message("VSPU :: Going to safe point");
                    gotoJob = new Job(JobDefOf.Goto, safePoint) { locomotionUrgency = LocomotionUrgency.Sprint };
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Finds an area with a roof -- a "safe zone"
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static IntVec3 FindSafeZoneFor(Pawn pawn)
        {
            IntVec3 result = IntVec3.Invalid;
            Region region;
            CellFinder.TryFindClosestRegionWith(pawn.GetRegion(RegionType.Set_Passable), TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn), (x => !x.Room.PsychologicallyOutdoors), 9999, out region, RegionType.Set_All);   //.ClosestRegionIndoors(pawn.Position, pawn.Map, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), RegionType.Set_Passable);
            if (region != null)
            {
                region.TryFindRandomCellInRegion(x => x.IsValid && x.x > 0 && x.z > 0 && x.InBounds(pawn.MapHeld) && x.GetDoor(pawn.MapHeld) == null, out result);
            }
            return result;
        }

        /// <summary>
        /// When no safe areas exist and sunlight is going to kill the vampire before it can reach safety, it must resort to digging
        /// a hole into the ground for survival.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="gotoJob"></param>
        /// <returns></returns>
        public static bool TryDiggingHideyHole(Pawn pawn, out Job gotoJob)
        {
            gotoJob = null;
            if (pawn?.health?.capacities?.CapableOf(PawnCapacityDefOf.Manipulation) ?? false)
            {
                gotoJob = null;
                if (FindHideyHoleSpot(VampDefOf.ROMV_HideyHole, Rot4.Random, pawn.PositionHeld, pawn.MapHeld) is IntVec3 iv && iv.IsValid)
                {
                    if (pawn?.CurJob != null && pawn?.CurJob?.def != VampDefOf.ROMV_DigAndHide)
                    {
                        //Log.Message("VSPU :: Hidey Place");
                        gotoJob = new Job(VampDefOf.ROMV_DigAndHide, iv) { locomotionUrgency = LocomotionUrgency.Sprint };
                        return true;
                    }

                }
            }
            return false;
        }


        /// <summary>
        /// Checks to find a point nearby to dig a hole for survival
        /// </summary>
        /// <param name="holeDef"></param>
        /// <param name="rot"></param>
        /// <param name="center"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static IntVec3 FindHideyHoleSpot(ThingDef holeDef, Rot4 rot, IntVec3 center, Map map)
        {
            if (GenConstruct.CanPlaceBlueprintAt(holeDef, center, rot, map, false, null).Accepted)
            {
                return center;
            }
            CellRect cellRect = CellRect.CenteredOn(center, 8);
            cellRect.ClipInsideMap(map);
            IntVec3 randomCell = cellRect.RandomCell;
            if (!CellFinder.TryFindRandomCellNear(center, map, 8, (IntVec3 c) => c.Standable(map) &&
                (GenConstruct.CanPlaceBlueprintAt(holeDef, c, rot, map, false, null).Accepted) &&
                (map?.reachability?.CanReach(c, randomCell, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)) ?? false), out randomCell))
            {
                //Log.Error("Found no place to build hideyhole for burning vampire.");
                randomCell = IntVec3.Invalid;
            }
            return randomCell;
        }

        /// <summary>
        /// Checks the current job of the character, to see if what they're doing is sunlight safe.
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static bool IsAlreadyDoingSunlightPathJob(this Pawn pawn)
        {
            return pawn?.CurJob != null && pawn.CurJob.IsSunlightSafeFor(pawn);
        }

        /// <summary>
        /// Checks jobs for sunlight safety.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static bool IsSunlightSafeFor(this Job job, Pawn pawn)
        {
            if (job != null)
            {
                if (job?.def == VampDefOf.ROMV_DigAndHide) return true;
                if (job?.targetA.Cell is IntVec3 loc && loc.IsValid && loc.IsSunlightSafeFor(pawn))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Grabs a job, checks its targets, and sees if those destinations are safe from sunlight.
        /// </summary>
        /// <param name="curJob"></param>
        /// <returns></returns>
        public static bool IsSunlightSafeFor(this LocalTargetInfo targ, Pawn pawn)
        {
            return IsSunlightSafeFor(targ.Cell, pawn);
        }

        /// <summary>
        /// Return safe for pawn if...
        ///    .1. Area has a roof AND...
        ///       .a. it's nighttime OR
        ///       .b. C can arrive before sunlight OR
        ///       .c. C can arrive before sunlight damage.
        ///    
        /// </summary>
        /// <param name="curJob"></param>
        /// <returns></returns>
        public static bool IsSunlightSafeFor(this IntVec3 targ, Pawn pawn)
        {
            return targ.Roofed(pawn.MapHeld) && 
                (!VampireUtility.IsDaylight(pawn.MapHeld) ||
                  CanArriveBeforeSunlight(targ, pawn) ||
                  CanSurviveTimeInSunlight(targ, pawn));
        }

        /// <summary>
        /// Creates a fake path to test the number of cells in sunlight vs ticks to survive sunlight damage.
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static bool CanSurviveTimeInSunlight(IntVec3 dest, Pawn pawn)
        {
            PawnPath path = pawn.MapHeld.pathFinder.FindPath(pawn.PositionHeld, dest, pawn, PathEndMode.OnCell);
            IntVec3 curVec;
            int cellsInSunlight = 0;
            while (path.NodesLeftCount > 1)
            {
                curVec = path.ConsumeNextNode();
                if (!curVec.Roofed(pawn.MapHeld))
                    cellsInSunlight++;
            }
            path.Dispose();
            if (cellsInSunlight > 0)
            {

                int sunExpTicks = 0;
                if (pawn?.health?.hediffSet?.GetFirstHediffOfDef(VampDefOf.ROMV_SunExposure) is HediffWithComps_SunlightExposure sunExp)
                {
                    sunExpTicks = (int)((TicksOfSurvivingSunlight * 0.75f) * sunExp.CurStageIndex);
                }
                int ticksToArrive = (cellsInSunlight * pawn.TicksPerMoveDiagonal) + sunExpTicks;
                if (ticksToArrive > TicksOfSurvivingSunlight)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Uses some guestimates to figure out if going home is safe now.
        /// </summary>
        /// <param name="targ"></param>
        /// <param name="pawn"></param>
        /// <param name="leewayFactor"></param>
        /// <returns></returns>
        public static bool CanArriveBeforeSunlight(this IntVec3 targ, Pawn pawn, float leewayFactor = 0.5f)
        {
            int ticksUntilDaylight = DetermineTicksUntilDaylight(pawn.MapHeld);
            int ticksPerMove = pawn.TicksPerMoveCardinal;
            int distanceToPoint = (int)pawn.PositionHeld.DistanceTo(targ);
            int ticksUntilArrival = distanceToPoint * ticksPerMove;
            int ticksLeeway = (int)(ticksUntilArrival * leewayFactor);

            if ((ticksUntilArrival + ticksLeeway) <= ticksUntilDaylight)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Daylight occurs when glow levels rise above 60%.
        /// I used a stopwatch to determine the average time between
        /// the percentage raises is about 161 ticks.
        /// For instance, if the current glow is 40%, 60% - 40% = 20%.
        ///   Then 20 multiplied by 161, which yields 3220 total ticks until daylight.
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public static int DetermineTicksUntilDaylight(Map map)
        {
            int result = -1;
            if (GenCelestial.CurCelestialSunGlow(map) > 0)
            {
                int curLightLevel = (int)(GenCelestial.CurCelestialSunGlow(map) * 100);
                int maxLightLevel = 60;
                int diffLightLevel = maxLightLevel - curLightLevel;
                int ticksLeftForTravel = TicksBetweenLightChanges * diffLightLevel;
                result = ticksLeftForTravel;
            }
            return result;
        }

        /// <summary>
        /// Returns a cell space that has a roof over it, is walkable, and reachable for a character.
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static IntVec3 FindCellSafeFromSunlight(Pawn pawn)
        {
            return CellFinderLoose.RandomCellWith(x => !IsZero(x) && x.IsValid && x.InBounds(pawn.MapHeld) && x.Roofed(pawn.MapHeld) && x.Walkable(pawn.MapHeld)
                    && pawn.CanReach(x, PathEndMode.OnCell, Danger.Deadly), pawn.MapHeld, 1000);
        }

        /// <summary>
        /// Finds a pont on the map that can be designated as a safe point for vampires to rest from daylight.
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static IntVec3? DetermineHomePoint(Pawn pawn)
        {
            IntVec3? result = null;
            if (pawn.MapHeld is Map map)
            {
                if (map.IsPlayerHome)
                {
                    if (RestUtility.FindBedFor(pawn) is Building_Bed bed)
                    {
                        result = bed.PositionHeld;
                    }
                }
                if (result == null)
                {
                    result = FindCellSafeFromSunlight(pawn);
                }
            }
            return result;
        }

        public static bool IsZero(IntVec3 loc)
        {
            return loc.x == 0 && loc.y == 0 && loc.z == 0;
        }

    }
}