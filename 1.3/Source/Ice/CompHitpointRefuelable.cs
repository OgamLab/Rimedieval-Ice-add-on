using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Ice
{
	public class CompHitpointRefuelable : CompRefuelable
	{
		private CompHeatPusher heatPusher;

		private static PropertyInfo ShouldPushHeatNow { get; } = AccessTools.Property(typeof(CompHeatPusher), "ShouldPushHeatNow");


		private static FieldInfo fuel { get; } = AccessTools.Field(typeof(CompRefuelable), "fuel");


		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			heatPusher = parent.GetComp<CompHeatPusher>();
		}

		public override void ReceiveCompSignal(string signal)
		{
			base.ReceiveCompSignal(signal);
			if (signal == "Refueled")
			{
				int maxHitPoints = parent.MaxHitPoints;
				int hP = (int)Math.Ceiling((float)fuel.GetValue(this) / base.Props.fuelCapacity * (float)maxHitPoints);
				SetHP(hP);
			}
		}

		public override string CompInspectStringExtra()
		{
			float num = (float)fuel.GetValue(this);
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

		public override void CompTick()
		{
			int maxHitPoints = parent.MaxHitPoints;
			int hitPoints = parent.HitPoints;
			float num = (float)hitPoints / (float)maxHitPoints * base.Props.fuelCapacity;
			if (num < (float)fuel.GetValue(this))
			{
				fuel.SetValue(this, num);
			}
			if (heatPusher == null || (bool)ShouldPushHeatNow.GetValue(heatPusher, null))
			{
				base.CompTick();
			}
			int num2 = (int)Math.Ceiling((float)fuel.GetValue(this) / base.Props.fuelCapacity * (float)maxHitPoints);
			if (num2 < hitPoints)
			{
				SetHP(num2);
			}
		}

		private void SetHP(int newHP)
		{
			parent.HitPoints = newHP;
		}
	}
}
