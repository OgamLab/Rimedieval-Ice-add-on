using RimWorld;
using UnityEngine;
using Verse;

namespace Ice
{
	public class CompStackMelter : ThingComp
	{
		private const float MeltPerIntervalPer10Degrees = 0.3f;

		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
		}

		public override void CompTickRare()
		{
			if (parent.Position.GetThingList(this.parent.Map).Any(x => x.def == Things.Ice_Cellar))
            {
				return;
            }
			float ambientTemperature = parent.AmbientTemperature;
			if (ambientTemperature < 0f)
			{
				return;
			}
			float f = 0.3f * (ambientTemperature / 10f);
			int num = GenMath.RoundRandom(f);
			if (num > 0)
			{
				if ((float)(parent.HitPoints - num) <= 0.1f && parent.stackCount > 1)
				{
					parent.stackCount--;
					parent.HitPoints = parent.MaxHitPoints;
				}
				else
				{
					parent.TakeDamage(new DamageInfo(DamageDefOf.Rotting, (float)num, 0f, -1f, (Thing)null, (BodyPartRecord)null, (ThingDef)null, DamageInfo.SourceCategory.ThingOrUnknown, (Thing)null));
				}
			}
		}

		public override void PreAbsorbStack(Thing otherStack, int count)
		{
			float num = (float)count / (float)(parent.stackCount + count);
			parent.HitPoints = (int)Mathf.Lerp((float)parent.HitPoints, (float)otherStack.HitPoints, num);
		}
	}
}
