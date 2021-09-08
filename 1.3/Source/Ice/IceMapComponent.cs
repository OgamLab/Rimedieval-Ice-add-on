using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
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

		public static IceMapComponent Instance { get; private set; }

		public IceMapComponent(Map map)
			: base(map)
		{
			Instance = this;
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
			if (Instance != this)
			{
				map.components.Remove(this);
				return;
			}
			ticks++;
			int num = map.cellIndices.NumGridCells / 360;
			for (int i = currentIndex; i < map.cellIndices.NumGridCells && i < currentIndex + num; i++)
			{
				TerrainDef type = map.terrainGrid.topGrid[i];
				IntVec3 c = map.cellIndices.IndexToCell(i);
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
			KeyValuePair<int, float>[] array = IceDepth.ToArray();
			for (int j = 0; j < array.Length; j++)
			{
				KeyValuePair<int, float> keyValuePair = array[j];
				int key = keyValuePair.Key;
				float value = keyValuePair.Value;
				IntVec3 c2 = map.cellIndices.IndexToCell(key);
				float num3 = map.regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(c2)?.Room?.Temperature ?? map.mapTemperature.OutdoorTemp;
				if (num3 > 0f)
				{
					WarmupTile(key, num3);
				}
			}
		}

		public static bool CanCooldown(TerrainDef type)
		{
			return type == IceTerrain.WaterDeep || type == IceTerrain.WaterShallow || type == IceTerrain.Marsh || type == IceTerrain.IceShallow || type == IceTerrain.FrozenMarsh || (type.defName.Contains("Water") && !type.defName.Contains("Salt") && !type.defName.Contains("Moving") && !type.defName.Contains("Ocean"));
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
			return type == IceTerrain.Ice || type == IceTerrain.IceShallow;
		}

		public static bool IsFrozen(TerrainDef type)
		{
			return IsIce(type) || type == IceTerrain.FrozenMarsh;
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
			foreach (IntVec3 item in Find.CameraDriver.CurrentViewRect)
			{
				if (item.InBounds(map))
				{
					int num = map.cellIndices.CellToIndex(item);
					TerrainDef type = map.terrainGrid.TerrainAt(num);
					if (CanCooldown(type) && IceDepth.TryGetValue(num, out var value))
					{
						Vector2 screenPos = GenMapUI.LabelDrawPosFor(item);
						GenMapUI.DrawThingLabel(screenPos, Math.Abs(Mathf.RoundToInt(value)).ToStringCached(), Color.white);
					}
				}
			}
		}

		public void RemoveIceFromTile(int mapIndex)
		{
			IntVec3 vec = map.cellIndices.IndexToCell(mapIndex);
			TerrainDef terrainDef = null;
			if (TemporarilyRemovedTerrain.TryGetValue(mapIndex, out var value))
			{
				TemporarilyRemovedTerrain.Remove(mapIndex);
				map.terrainGrid.SetTerrain(vec, value);
				map.designationManager.allDesignations.Remove(map.designationManager.allDesignations.SingleOrDefault((Designation x) => x.target == vec && x.def == Designations.DoDigIce));
				terrainDef = value;
			}
			else if (IsFrozen(map.terrainGrid.TerrainAt(mapIndex)))
			{
				map.terrainGrid.SetTerrain(vec, IceTerrain.WaterShallow);
				terrainDef = IceTerrain.WaterShallow;
			}
			ThawSpeed.Remove(mapIndex);
			IceDepth.Remove(mapIndex);
			Thing[] array = map.thingGrid.ThingsAt(vec).ToArray();
			foreach (Thing thing in array)
			{
				if (thing is Building && !GenConstruct.TerrainCanSupport(CellRect.SingleCell(vec), map, thing.def))
				{
					thing.Destroy();
				}
				else if (!(thing is Pawn) && (terrainDef == IceTerrain.WaterDeep || terrainDef.defName.Contains("Deep")))
				{
					thing.Destroy();
				}
			}
		}

		private void CooldownTile(int mapIndex, float temp, TerrainDef type)
		{
			IntVec3 vec = map.cellIndices.IndexToCell(mapIndex);
			IEnumerable<TerrainDef> source = from x in GenAdj.AdjacentCells
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
			float val2 = IceDepth[mapIndex] + temp * (Rand.Value + 0.05f) * (float)num;
			IceDepth[mapIndex] = Math.Max(val2, val);
			if (!CanFreeze(type))
			{
				return;
			}
			double num3 = (type.defName.Contains("Deep") ? 3.0 : 1.0);
			if (flag)
			{
				num3 += 0.5;
			}
			if ((double)IceDepth[mapIndex] < (double)ShallowIceThreshold * num3 && !TemporarilyRemovedTerrain.ContainsKey(mapIndex))
			{
				TemporarilyRemovedTerrain.Add(mapIndex, type);
				if (flag)
				{
					map.terrainGrid.SetTerrain(vec, IceTerrain.FrozenMarsh);
				}
				else
				{
					map.terrainGrid.SetTerrain(vec, IceTerrain.IceShallow);
				}
			}
		}
	}
}
