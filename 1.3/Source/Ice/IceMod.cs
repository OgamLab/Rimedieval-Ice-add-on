using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Ice
{
	[StaticConstructorOnStartup]
	public static class IceModStartup
	{
		static IceModStartup()
		{
			List<Designator> value = Traverse.Create((object)DesignationCategories.Orders).Field("resolvedDesignators").GetValue<List<Designator>>();
			if (!value.Any((Designator item) => item is Designator_DigIce))
			{
				int num = value.FindIndex((Designator item) => item is Designator_PlantsHarvestWood);
				Designator_DigIce item2 = new Designator_DigIce();
				value.Insert(num + 1, item2);
			}
			new Harmony("Ice.Mod").PatchAll();
		}
	}
}
