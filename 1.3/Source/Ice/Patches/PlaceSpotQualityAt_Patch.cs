using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using static Verse.GenPlace;

namespace Ice.Patches
{
    [HarmonyPatch(typeof(SteadyEnvironmentEffects))]
    [HarmonyPatch("FinalDeteriorationRate", new Type[]
    {
            typeof(Thing),
            typeof(bool),
            typeof(bool),
            typeof(bool),
            typeof(TerrainDef),
            typeof(List<string>)
    })]
    public static class FinalDeteriorationRatePatch
    {
        public static void Prefix(Thing t, bool roofed, bool roomUsesOutdoorTemperature, ref bool protectedByEdifice, TerrainDef terrain, ref float __result, List<string> reasons)
        {
            if (t?.Map != null && t.Position.GetThingList(t.Map).Any(x => x.def == Things.Cellar || x.def == Things.MedievalFridge && x.TryGetComp<CompRefuelable>().HasFuel))
            {
                protectedByEdifice = true;
            }
        }
    }

    [HarmonyPatch(typeof(SteadyEnvironmentEffects), "TryDoDeteriorate")]
    public static class TryDoDeteriorate_Patch
    {
        public static bool Prefix(Thing t, bool roofed, bool roomUsesOutdoorTemperature, bool protectedByEdifice, TerrainDef terrain)
        {
            if (t?.Map != null && t.Position.GetThingList(t.Map).Any(x => x.def == Things.Cellar || x.def == Things.MedievalFridge && x.TryGetComp<CompRefuelable>().HasFuel))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompRottable), "Active", MethodType.Getter)]
    public static class Active_Patch
    {
        public static bool Prefix(CompRottable __instance, ref bool __result)
        {
            if (__instance.parent?.Map != null && __instance.parent.Position.GetThingList(__instance.parent.Map)
                .Any(x => x.def == Things.Cellar || x.def == Things.MedievalFridge && x.TryGetComp<CompRefuelable>().HasFuel))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GenPlace))]
    [HarmonyPatch("PlaceSpotQualityAt")]
    public class PlaceSpotQualityAt_Patch
    {
        public static void Postfix(ref PlaceSpotQuality __result, IntVec3 c, Rot4 rot, Map map, Thing thing, IntVec3 center, bool allowStacking, Predicate<IntVec3> extraValidator = null)
        {
            foreach (var pawn in map.mapPawns.FreeColonists)
            {
                pawn.jobs.debugLog = true;
            }
            if (__result == PlaceSpotQuality.Bad)
            {
                if (c.GetFirstBuilding(map)?.def == Things.Cellar)
                {
                    __result = PlaceSpotQuality.Perfect;
                }
            }
        }
    }

    [HarmonyPatch(typeof(WorkGiverUtility))]
    [HarmonyPatch("HaulStuffOffBillGiverJob")]
    public class HaulStuffOffBillGiverJob_Patch
    {
        public static bool Prefix(Pawn pawn, IBillGiver giver, Thing thingToIgnore)
        {
            if (giver is Building_WorkTable workTable && workTable.def == Things.Cellar)
            {
                return false;
            }
            return true;
        }
    }

    //[HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    //public class StartJobPatch
    //{
    //    private static void Postfix(Pawn_JobTracker __instance, Pawn ___pawn, Job newJob, JobTag? tag)
    //    {
    //        try
    //        {
    //            if (___pawn.IsColonist)
    //            {
    //                ___pawn.jobs.debugLog = true;
    //                Log.Message(___pawn + " is starting " + newJob);
    //            }
    //        }
    //        catch { }
    //    }
    //}
    //
    //
    //[HarmonyPatch(typeof(Pawn_JobTracker), "EndCurrentJob")]
    //public class EndCurrentJobPatch
    //{
    //    private static void Prefix(Pawn_JobTracker __instance, Pawn ___pawn, JobCondition condition, ref bool startNewJob, bool canReturnToPool = true)
    //    {
    //        try
    //        {
    //            if (___pawn.IsColonist)
    //            {
    //                Log.Message(___pawn + " is ending " + ___pawn.CurJob);
    //            }
    //        }
    //        catch { };
    //    }
    //}
    //
    //[HarmonyPatch]
    //public class WorkGiverPatches
    //{
    //    [HarmonyTargetMethods]
    //    public static IEnumerable<MethodBase> TargetMethods()
    //    {
    //        foreach (var type in typeof(WorkGiver_Scanner).AllSubclassesNonAbstract())
    //        {
    //            var method = AccessTools.Method(type, "JobOnCell");
    //            if (method != null)
    //            {
    //                yield return method;
    //            }
    //        }
    //    }
    //    private static void Postfix(WorkGiver_Scanner __instance, Job __result, Pawn __0)
    //    {
    //        try
    //        {
    //            if (__0.IsColonist && __result != null)
    //            {
    //                Log.Message(__0 + " gets " + __result + " from " + __instance);
    //            }
    //        }
    //        catch { }
    //    }
    //}
    //
    //[HarmonyPatch]
    //public class WorkGiverPatches2
    //{
    //    [HarmonyTargetMethods]
    //    public static IEnumerable<MethodBase> TargetMethods()
    //    {
    //        foreach (var type in typeof(WorkGiver_Scanner).AllSubclassesNonAbstract())
    //        {
    //            var method = AccessTools.Method(type, "JobOnThing");
    //            if (method != null)
    //            {
    //                yield return method;
    //            }
    //        }
    //    }
    //    private static void Postfix(WorkGiver_Scanner __instance, Job __result, Pawn __0)
    //    {
    //        try
    //        {
    //            if (__0.IsColonist && __result != null)
    //            {
    //                Log.Message(__0 + " gets " + __result + " from " + __instance);
    //            }
    //        }
    //        catch { }
    //    }
    //}
    //
    //[HarmonyPatch]
    //public class TryIssueJobPackage
    //{
    //    [HarmonyTargetMethods]
    //    public static IEnumerable<MethodBase> TargetMethods()
    //    {
    //        foreach (var type in typeof(ThinkNode).AllSubclassesNonAbstract())
    //        {
    //            var method = AccessTools.Method(type, "TryIssueJobPackage");
    //            if (method != null)
    //            {
    //                yield return method;
    //            }
    //        }
    //    }
    //    private static void Postfix(ThinkNode __instance, ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
    //    {
    //        try
    //        {
    //            if (pawn.IsColonist && __result.Job != null)
    //            {
    //                Log.Message(pawn + " gets " + __result.Job + " from " + __instance);
    //            }
    //        }
    //        catch { }
    //    }
    //}
}
