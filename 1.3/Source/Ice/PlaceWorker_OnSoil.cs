using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Ice
{
	public class PlaceWorker_OnSoil : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thingToPlace = null)
		{
			var terrain = loc.GetTerrain(map);
			if (terrain.IsSoil)
            {
				return true;
            }
			return "Ice.MustBeOnSoil".Translate();
		}
	}
}
