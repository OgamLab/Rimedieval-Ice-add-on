using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Ice
{
	public class IceStockGenerator : StockGenerator
	{
		public IceStockGenerator()
		{
			countRange = new IntRange(25, 400);
			totalPriceRange = new FloatRange(0f, 700f);
		}

		public override bool HandlesThingDef(ThingDef thingDef)
		{
			return thingDef == Things.Ice_Resource_IceBlocks;
		}

		public override IEnumerable<Thing> GenerateThings(int mapTileIndex, Faction faction)
		{
			Current.Game.World.tileTemperatures.GetOutdoorTemp(mapTileIndex);
			bool isColony = Current.Game.World.worldObjects.AnySettlementAt(mapTileIndex);
			foreach (Thing thing in StockGeneratorUtility.TryMakeForStock(Things.Ice_Resource_IceBlocks, RandomCountOf(Things.Ice_Resource_IceBlocks), faction))
			{
				if (!isColony)
				{
					yield return thing;
				}
			}
		}
	}
}
