using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Ice
{
	public class JobDriver_DigIce : JobDriver_AffectFloor
	{
		private float workLeft;

		public override int BaseWorkAmount => 400;

		public override DesignationDef DesDef => Designations.DoDigIce;

		public override StatDef SpeedStat => StatDefOf.MiningSpeed;

		public override void DoEffect(IntVec3 c)
		{
			Thing thing = ThingMaker.MakeThing(Things.Resource_IceBlocks);
			thing.stackCount = 2;
			if (base.Map.terrainGrid.TerrainAt(c) == IceTerrain.IceShallow)
			{
				int mapIndex = base.Map.cellIndices.CellToIndex(c);
				IceMapComponent.Instance.RemoveIceFromTile(mapIndex);
			}
			GenPlace.TryPlaceThing(thing, base.TargetLocA, base.Map, ThingPlaceMode.Near);
		}

		public override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOn(() => (!job.ignoreDesignations && base.Map.designationManager.DesignationAt(base.TargetLocA, DesDef) == null) ? true : false);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
			Toil doWork = new Toil();
			doWork.initAction = delegate
			{
				workLeft = BaseWorkAmount;
			};
			doWork.tickAction = delegate
			{
				float num = ((SpeedStat == null) ? 1f : doWork.actor.GetStatValue(SpeedStat));
				workLeft -= num;
				if (doWork.actor.skills != null)
				{
					doWork.actor.skills.Learn(SkillDefOf.Mining, 0.03f);
				}
				base.Map.snowGrid.SetDepth(base.TargetLocA, 0f);
				if (workLeft <= 0f)
				{
					DoEffect(base.TargetLocA);
					base.Map.designationManager.DesignationAt(base.TargetLocA, DesDef)?.Delete();
					ReadyForNextToil();
				}
			};
			doWork.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			ToilEffects.WithProgressBar(doWork, TargetIndex.A, (Func<float>)(() => 1f - workLeft / (float)BaseWorkAmount), false, -0.5f);
			doWork.defaultCompleteMode = ToilCompleteMode.Never;
			doWork.activeSkill = () => SkillDefOf.Mining;
			yield return doWork;
		}
	}
}
