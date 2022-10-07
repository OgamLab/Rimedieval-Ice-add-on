using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Ice
{
	[HarmonyPatch(typeof(ThingDef), "HasComp")]
	internal class Patch_ThingDef_HasComp
	{
		private static bool Prefix(ref bool __result, ThingDef __instance, Type compType)
		{
			if (compType != typeof(CompRefuelable))
			{
				return true;
			}
			for (int i = 0; i < __instance.comps.Count; i++)
			{
				if (__instance.comps[i].compClass == compType || __instance.comps[i].compClass.IsSubclassOf(compType))
				{
					__result = true;
					return false;
				}
			}
			__result = false;
			return false;
		}
	}
	public class CompHitpointRefuelable : CompRefuelable
	{
		private CompHeatPusher heatPusher;
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			heatPusher = parent.GetComp<CompHeatPusher>();
			shouldPushHeat = heatPusher.ShouldPushHeatNow;
		}

		public override void ReceiveCompSignal(string signal)
		{
			base.ReceiveCompSignal(signal);
			if (signal == "Refueled")
			{
				int maxHitPoints = parent.MaxHitPoints;
				int hP = (int)Math.Ceiling(this.fuel / base.Props.fuelCapacity * (float)maxHitPoints);
				SetHP(hP);
			}
		}

		public override string CompInspectStringExtra()
		{
			float num = this.fuel;
			int num2 = (int)Math.Round(num / base.Props.fuelCapacity * 100f);
			string text = $"{base.Props.FuelLabel}: {num2}%";
			if (!base.Props.consumeFuelOnlyWhenUsed && base.HasFuel)
			{
				int numTicks = (int)(num / base.Props.fuelConsumptionRate * 60000f);
				text = text + " (" + numTicks.ToStringTicksToPeriod() + ")";
			}
			if (!base.HasFuel && !base.Props.outOfFuelMessage.NullOrEmpty())
			{
				text += $"\n{base.Props.outOfFuelMessage} ({GetFuelCountToFullyRefuel()}x {base.Props.fuelFilter.AnyAllowedDef.label})";
			}
			if (base.Props.targetFuelLevelConfigurable)
			{
				text = text + "\n" + "ConfiguredTargetFuelLevel".Translate(base.TargetFuelLevel.ToStringDecimalIfSmall());
			}
			return text;
		}

		private bool shouldPushHeat;
		public override void CompTick()
		{
			var pawn = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists.RandomElement();
			if (shouldPushHeat)
            {
				base.CompTick();
			}
			if (parent.IsHashIntervalTick(60))
            {
				int maxHitPoints = parent.MaxHitPoints;
				int hitPoints = parent.HitPoints;
				float num = (float)hitPoints / (float)maxHitPoints * base.Props.fuelCapacity;
				if (num < this.fuel)
				{
					this.fuel = num;
				}
				shouldPushHeat = heatPusher.ShouldPushHeatNow;
				int num2 = (int)Math.Ceiling(fuel / base.Props.fuelCapacity * (float)maxHitPoints);
				if (num2 < hitPoints)
				{
					SetHP(num2);
				}
			}
		}
		private void SetHP(int newHP)
		{
			parent.HitPoints = newHP;
		}
	}
}
