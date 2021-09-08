using System.Linq;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Ice.Patches
{
	[HarmonyPatch(typeof(Tradeable))]
	[HarmonyPatch("PriceTypeFor")]
	public class DynamicPrice
	{
		public static void Postfix(Tradeable __instance, ref PriceType __result, TradeAction action)
		{
			if (__instance.ThingDef != Things.Resource_IceBlocks)
			{
				return;
			}
			int tile = TradeSession.playerNegotiator.Tile;
			float outdoorTemp = Current.Game.World.tileTemperatures.GetOutdoorTemp(tile);
			bool flag = Current.Game.World.worldObjects.AnySettlementAt(tile) && Current.Game.World.worldObjects.ObjectsAt(tile).Any((WorldObject x) => !x.Faction.IsPlayer);
			bool flag2 = Current.Game.World.worldObjects.AnySettlementAt(tile) && Current.Game.World.worldObjects.ObjectsAt(tile).Any((WorldObject x) => x.Faction.IsPlayer);
			if (outdoorTemp > 50f)
			{
				if (action == TradeAction.PlayerBuys)
				{
					__result = PriceType.Exorbitant;
				}
				else
				{
					__result = PriceType.Normal;
				}
			}
			else if (outdoorTemp > 30f)
			{
				if (action == TradeAction.PlayerBuys)
				{
					__result = PriceType.Expensive;
				}
				else
				{
					__result = PriceType.Cheap;
				}
			}
			else if (outdoorTemp > 10f)
			{
				if (action == TradeAction.PlayerBuys)
				{
					__result = PriceType.Normal;
				}
				else
				{
					__result = PriceType.VeryCheap;
				}
			}
			else
			{
				__result = PriceType.Undefined;
			}
		}
	}
}
