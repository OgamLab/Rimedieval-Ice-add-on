using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Ice
{
	public class Designator_DigIce : Designator
	{
		public override int DraggableDimensions => 2;

		public override bool DragDrawMeasurements => true;

		public override DesignationDef Designation => Designations.Ice_DoDigIce;

		public Designator_DigIce()
		{
			defaultLabel = "Ice.Dig".Translate();
			defaultDesc = "Ice.DigDesc".Translate();
			icon = ContentFinder<Texture2D>.Get("UI/icesaw");
			useMouseIcon = true;
			soundDragSustain = SoundDefOf.Designate_DragStandard;
			soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			soundSucceeded = SoundDefOf.Designate_SmoothSurface;
			hotKey = KeyBindingDefOf.Misc1;
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 c)
		{
			if (!c.InBounds(base.Map) || c.Fogged(base.Map) || base.Map.designationManager.DesignationAt(c, Designation) != null)
			{
				return false;
			}
			if (c.InNoBuildEdgeArea(base.Map))
			{
				return "TooCloseToMapEdge".Translate();
			}
			Building edifice = c.GetEdifice(base.Map);
			if (edifice != null && edifice.def.Fillage == FillCategory.Full && edifice.def.passability == Traversability.Impassable)
			{
				return false;
			}
			TerrainDef terrainDef = base.Map.terrainGrid.TerrainAt(c);
			if (terrainDef != IceTerrain.Ice && terrainDef != IceTerrain.Ice_IceShallow)
			{
				return "Ice.MustBeIce".Translate();
			}
			return AcceptanceReport.WasAccepted;
		}

		public override void DesignateSingleCell(IntVec3 c)
		{
			base.Map.designationManager.AddDesignation(new Designation(c, Designation));
		}

		public override void SelectedUpdate()
		{
			GenUI.RenderMouseoverBracket();
		}

		public override void RenderHighlight(List<IntVec3> dragCells)
		{
			DesignatorUtility.RenderHighlightOverSelectableCells(this, dragCells);
		}
	}
}
