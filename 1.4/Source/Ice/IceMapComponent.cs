using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Ice
{
	[StaticConstructorOnStartup]
	public class IceMapComponent : MapComponent
	{
		private Dictionary<int, float> IceDepth = new Dictionary<int, float>();

		private Dictionary<int, float> ThawSpeed = new Dictionary<int, float>();

		private Dictionary<int, TerrainDef> TemporarilyRemovedTerrain = new Dictionary<int, TerrainDef>();

		private int currentIndex;

		private const int TicksPerMap = 360;

		private const float MinThawRate = 0.9f;

		private const float MaxThawRate = 1f;

		public static float MaximumIceDepthPerIceTile = -300f;

		public static float ShallowIceThreshold = -150f;

		private uint ticks;

		private uint nextWarmupTick;
		public IceMapComponent(Map map)
			: base(map)
		{
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref IceDepth, "iceDepth", LookMode.Value, LookMode.Value);
			Scribe_Collections.Look(ref TemporarilyRemovedTerrain, "tempTerrain", LookMode.Value, LookMode.Def);
			if (ThawSpeed == null)
			{
				ThawSpeed = new Dictionary<int, float>();
			}
		}

		public override void MapComponentTick()
		{
			ticks++;
			int num = map.cellIndices.NumGridCells / 360;
			for (int i = currentIndex; i < map.cellIndices.NumGridCells && i < currentIndex + num; i++)
			{
				var type = map.terrainGrid.topGrid[i];
				var c = map.cellIndices.IndexToCell(i);
				float num2 = map.regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(c)?.Room?.Temperature ?? map.mapTemperature.OutdoorTemp;
				if (num2 < 0f && CanCooldown(type))
				{
					CooldownTile(i, num2, type);
				}
			}
			currentIndex += num;
			if (currentIndex >= map.cellIndices.NumGridCells)
			{
				currentIndex -= map.cellIndices.NumGridCells;
			}
			if (ticks <= nextWarmupTick)
			{
				return;
			}
			nextWarmupTick += (uint)Rand.RangeInclusive(300, 460);
			var array = IceDepth.ToArray();
			for (int j = 0; j < array.Length; j++)
			{
				var keyValuePair = array[j];
				int key = keyValuePair.Key;
				_ = keyValuePair.Value;
				var c2 = map.cellIndices.IndexToCell(key);
				float num3 = map.regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(c2)?.Room?.Temperature ?? map.mapTemperature.OutdoorTemp;
				if (num3 > 0f)
				{
					WarmupTile(key, num3);
				}
			}
		}

		public static bool CanCooldown(TerrainDef type)
		{
			return type == IceTerrain.WaterDeep || type == IceTerrain.WaterShallow || type == IceTerrain.Marsh || type == IceTerrain.Ice_IceShallow || type == IceTerrain.Ice_FrozenMarsh || (type.defName.Contains("Water") && !type.defName.Contains("Salt") && !type.defName.Contains("Moving") && !type.defName.Contains("Ocean"));
		}

		public static bool CanFreeze(TerrainDef type)
		{
			return type == IceTerrain.WaterDeep || type == IceTerrain.WaterShallow || type == IceTerrain.Marsh || (type.defName.Contains("Water") && !type.defName.Contains("Salt") && !type.defName.Contains("Moving") && !type.defName.Contains("Ocean"));
		}

		public static bool IsLand(TerrainDef type)
		{
			return type != IceTerrain.WaterDeep && type != IceTerrain.WaterShallow && type != IceTerrain.Marsh && !type.defName.Contains("Water");
		}

		public static bool IsIce(TerrainDef type)
		{
			return type == IceTerrain.Ice || type == IceTerrain.Ice_IceShallow;
		}

		public static bool IsFrozen(TerrainDef type)
		{
			return IsIce(type) || type == IceTerrain.Ice_FrozenMarsh;
		}

		private void WarmupTile(int mapIndex, float temp)
		{
			if (!IceDepth.ContainsKey(mapIndex))
			{
				IceDepth.Add(mapIndex, 0f);
			}
			if (!ThawSpeed.ContainsKey(mapIndex))
			{
				ThawSpeed.Add(mapIndex, Rand.Range(0.9f, 1f));
			}
			IceDepth[mapIndex] += temp * ThawSpeed[mapIndex] / 2f;
			if (IceDepth[mapIndex] > 0f)
			{
				RemoveIceFromTile(mapIndex);
			}
		}

		public override void MapComponentOnGUI()
		{
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			if (!DebugButtonsPatch.DrawDebugOverlay)
			{
				return;
			}
			foreach (var item in Find.CameraDriver.CurrentViewRect)
			{
				if (item.InBounds(map))
				{
					int num = map.cellIndices.CellToIndex(item);
					var type = map.terrainGrid.TerrainAt(num);
					if (CanCooldown(type) && IceDepth.TryGetValue(num, out float value))
					{
						var screenPos = GenMapUI.LabelDrawPosFor(item);
						GenMapUI.DrawThingLabel(screenPos, Math.Abs(Mathf.RoundToInt(value)).ToStringCached(), Color.white);
					}
				}
			}
		}

		public void RemoveIceFromTile(int cellIndex)
		{
			try
			{
				var vec = map.cellIndices.IndexToCell(cellIndex);
				TerrainDef terrainDef = null;
				if (TemporarilyRemovedTerrain.TryGetValue(cellIndex, out var value))
				{
					TemporarilyRemovedTerrain.Remove(cellIndex);
					map.terrainGrid.SetTerrain(vec, value);
					map.designationManager.AllDesignations.Remove(map.designationManager.AllDesignations.SingleOrDefault((Designation x) => x.target == vec && x.def == Designations.Ice_DoDigIce));
					terrainDef = value;
				}
				else if (IsFrozen(map.terrainGrid.TerrainAt(vec)))
				{
					map.terrainGrid.SetTerrain(vec, IceTerrain.WaterShallow);
					terrainDef = IceTerrain.WaterShallow;
				}
				ThawSpeed.Remove(cellIndex);
				IceDepth.Remove(cellIndex);

				var array = map.thingGrid.ThingsAt(vec).ToArray();
				foreach (var thing in array)
				{
					if (thing != null)
					{
						if (thing is Building && !GenConstruct.TerrainCanSupport(CellRect.SingleCell(vec), map, thing.def))
						{
							thing.Destroy();
						}
						else if (!(thing is Pawn) && (terrainDef == IceTerrain.WaterDeep || (terrainDef?.defName?.Contains("Deep") ?? false)))
						{
							thing.Destroy();
						}
					}

				}
			}
			catch { }
		}

		private void CooldownTile(int mapIndex, float temp, TerrainDef type)
		{
			var vec = map.cellIndices.IndexToCell(mapIndex);
			var source = from x in GenAdj.AdjacentCells
						 select vec + x into x
						 where x.InBounds(map)
						 select map.terrainGrid.TerrainAt(map.cellIndices.CellToIndex(x));
			int num = source.Count((TerrainDef x) => IsLand(x));
			bool flag = type == IceTerrain.Marsh;
			if (flag)
			{
				num = Math.Max(num, 1);
			}
			if (num <= 0)
			{
				return;
			}
			float num2 = source.Count((TerrainDef x) => IsIce(x));
			float val = Math.Min(MaximumIceDepthPerIceTile * (num2 / 2f), MaximumIceDepthPerIceTile);
			if (!IceDepth.ContainsKey(mapIndex))
			{
				IceDepth.Add(mapIndex, 0f);
			}
			if (!ThawSpeed.ContainsKey(mapIndex))
			{
				ThawSpeed.Add(mapIndex, Rand.Range(0.9f, 1f));
			}
			float val2 = IceDepth[mapIndex] + (temp * (Rand.Value + 0.05f) * num);
			IceDepth[mapIndex] = Math.Max(val2, val);
			if (!CanFreeze(type))
			{
				return;
			}
			double num3 = type.defName.Contains("Deep") ? 3.0 : 1.0;
			if (flag)
			{
				num3 += 0.5;
			}
			if ((double)IceDepth[mapIndex] < ShallowIceThreshold * num3 && !TemporarilyRemovedTerrain.ContainsKey(mapIndex))
			{
				TemporarilyRemovedTerrain.Add(mapIndex, type);
				if (flag)
				{
					map.terrainGrid.SetTerrain(vec, IceTerrain.Ice_FrozenMarsh);
				}
				else
				{
					map.terrainGrid.SetTerrain(vec, IceTerrain.Ice_IceShallow);
				}
			}
		}
	}
}
