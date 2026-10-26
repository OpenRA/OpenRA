#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.TerrainProbe
{
	public sealed class MutableProbeLayerInfo : TraitInfo, ICustomMovementLayerInfo
	{
		public override object Create(ActorInitializer init) => new MutableProbeLayer(init.World);
	}

	public sealed class MutableProbeLayer : ICustomMovementLayer
#if LAYER_API
		, ICustomMovementLayerTerrainChanges, ICustomMovementLayerConnectivityChanges, ICustomMovementLayerMoveOrders
#endif
	{
		readonly World world;
		bool connected;
		bool broken;
		public event Action<CPos>? CellTerrainChanged;
		public event Action<CPos>? CellConnectivityChanged;
		public MutableProbeLayer(World world)
		{
			this.world = world;
		}
		public byte Index => CustomMovementLayerType.ElevatedBridge;
		public bool InteractsWithDefaultLayer => true;
		public bool ReturnToGroundLayerOnIdle => false;
		public bool EnabledForLocomotor(LocomotorInfo locomotor) => true;
		public bool SupportsMoveOrders(LocomotorInfo locomotor) => true;
		public short EntryMovementCost(LocomotorInfo locomotor, CPos cell) => ExitMovementCost(locomotor, cell);
		public short ExitMovementCost(LocomotorInfo locomotor, CPos cell) =>
			connected && cell.Y == 20 && cell.X is 53 or 57 ? (short)0 : short.MaxValue;
		public byte GetTerrainIndex(CPos cell) => cell.Y == 20 && cell.X >= 53 && cell.X <= 57 && !(broken && cell.X == 55)
			? world.Map.Rules.TerrainInfo.GetTerrainIndex("Clear") : byte.MaxValue;
		public WPos CenterOfCell(CPos cell) => world.Map.CenterOfCell(cell) + new WVec(0, 0, 2 * world.Map.CellHeightStep.Length);
		public void Connect(bool enabled)
		{
			connected = enabled;
			foreach (var x in new[] { 53, 57 })
				CellConnectivityChanged?.Invoke(new CPos(x, 20));
		}
		public void Break(bool enabled)
		{
			broken = enabled;
			CellTerrainChanged?.Invoke(new CPos(55, 20));
		}
	}

	public static class DynamicLayerChecks
	{
		public static void Run(World world, Actor actor, Action<string, bool, string> check)
		{
			var layer = world.WorldActor.Trait<MutableProbeLayer>();
			var mobile = actor.Trait<Mobile>();
			var column = world.Map.AllCells.Where(c => c.X == 55).ToArray();
			var old = column.ToDictionary(c => c, c => world.Map.Height[c]);
			foreach (var cell in column)
				world.Map.Height[cell] = 7;
			var start = new CPos(52, 20);
			var end = new CPos(58, 20);
			bool Path() => mobile.PathFinder.FindPathToTargetCell(actor, [start], end, BlockedByActor.None).Count > 0;
			layer.Connect(false);
			check("layer-initially-disconnected", !Path(), "prime abstract domains while closed");
			layer.Connect(true);
			check("layer-initial-route", Path(), "");
			foreach (var connected in new[] { false, true, false, true })
			{
				layer.Connect(connected);
				check("layer-endpoints-" + connected, Path() == connected, "unchanged cell costs");
			}
			foreach (var broken in new[] { true, false, true, false })
			{
				layer.Break(broken);
				check("layer-terrain-" + broken, Path() != broken, "");
			}
			var saved = mobile.ToCell;
			var subcell = mobile.ToSubCell;
			var deck = new CPos(54, 20, layer.Index);
			mobile.SetPosition(actor, deck);
			check("layer-set-position-z", actor.CenterPosition.Z == layer.CenterOfCell(deck).Z, actor.CenterPosition.ToString());
			var target = new CPos(56, 20, layer.Index);
			check("layer-direct-target", mobile.NearestMoveableCell(target, 0, 3) == target, "");
			layer.Break(true);
			var nearest = mobile.NearestMoveableCell(new CPos(55, 20, layer.Index), 1, 3);
			check("layer-nearest-target", nearest.Layer == layer.Index && nearest.X != 55, nearest.ToString());
			layer.Break(false);
			mobile.SetPosition(actor, saved, subcell);
			layer.Connect(false);
			foreach (var cell in column)
				world.Map.Height[cell] = old[cell];
			SpriteOverrideChecks.Run(world, check);
		}
	}
}
