#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	class ChronoshiftPowerInfo : SupportPowerInfo
	{
		[Desc("Target actor selection radius in cells.")]
		public readonly int Range = 1;

		[Desc("Seconds until returning after teleportation.")]
		public readonly int Duration = 30;

		[PaletteReference] public readonly string TargetOverlayPalette = TileSet.TerrainPaletteInternalName;

		public readonly string OverlaySpriteGroup = "overlay";
		[SequenceReference("OverlaySpriteGroup", true)] public readonly string ValidTileSequencePrefix = "target-valid-";
		[SequenceReference("OverlaySpriteGroup")] public readonly string InvalidTileSequence = "target-invalid";
		[SequenceReference("OverlaySpriteGroup")] public readonly string SourceTileSequence = "target-select";

		public readonly bool KillCargo = true;

		[Desc("Cursor sequence to use when selecting targets for the chronoshift.")]
		public readonly string SelectionCursor = "chrono-select";

		[Desc("Cursor sequence to use when targeting an area for the chronoshift.")]
		public readonly string TargetCursor = "chrono-target";

		[Desc("Cursor sequence to use when the targeted area is blocked.")]
		public readonly string TargetBlockedCursor = "move-blocked";

		public override object Create(ActorInitializer init) { return new ChronoshiftPower(init.Self, this); }
	}

	class ChronoshiftPower : SupportPower
	{
		public ChronoshiftPower(Actor self, ChronoshiftPowerInfo info) : base(self, info) { }

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			Game.Sound.PlayToPlayer(manager.Self.Owner, Info.SelectTargetSound);
			self.World.OrderGenerator = new SelectChronoshiftTarget(Self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			foreach (var target in UnitsInRange(order.ExtraLocation))
			{
				var cs = target.Trait<Chronoshiftable>();
				var targetCell = target.Location + (order.TargetLocation - order.ExtraLocation);
				var cpi = Info as ChronoshiftPowerInfo;

				if (self.Owner.Shroud.IsExplored(targetCell) && cs.CanChronoshiftTo(target, targetCell))
					cs.Teleport(target, targetCell, cpi.Duration * 25, cpi.KillCargo, self);
			}
		}

		public IEnumerable<Actor> UnitsInRange(CPos xy)
		{
			var range = ((ChronoshiftPowerInfo)Info).Range;
			var tiles = Self.World.Map.FindTilesInCircle(xy, range);
			var units = new HashSet<Actor>();
			foreach (var t in tiles)
				units.UnionWith(Self.World.ActorMap.GetActorsAt(t));

			return units.Where(a => a.Info.HasTraitInfo<ChronoshiftableInfo>() &&
				!a.TraitsImplementing<IPreventsTeleport>().Any(condition => condition.PreventsTeleport(a)));
		}

		public bool SimilarTerrain(CPos xy, CPos sourceLocation)
		{
			if (!Self.Owner.Shroud.IsExplored(xy))
				return false;

			var range = ((ChronoshiftPowerInfo)Info).Range;
			var sourceTiles = Self.World.Map.FindTilesInCircle(xy, range);
			var destTiles = Self.World.Map.FindTilesInCircle(sourceLocation, range);

			using (var se = sourceTiles.GetEnumerator())
			using (var de = destTiles.GetEnumerator())
				while (se.MoveNext() && de.MoveNext())
				{
					var a = se.Current;
					var b = de.Current;

					if (!Self.Owner.Shroud.IsExplored(a) || !Self.Owner.Shroud.IsExplored(b))
						return false;

					if (Self.World.Map.GetTerrainIndex(a) != Self.World.Map.GetTerrainIndex(b))
						return false;
				}

			return true;
		}

		class SelectChronoshiftTarget : IOrderGenerator
		{
			readonly ChronoshiftPower power;
			readonly int range;
			readonly Sprite tile;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectChronoshiftTarget(World world, string order, SupportPowerManager manager, ChronoshiftPower power)
			{
				// Clear selection if using Left-Click Orders
				if (Game.Settings.Game.UseClassicMouseStyle)
					manager.Self.World.Selection.Clear();

				this.manager = manager;
				this.order = order;
				this.power = power;

				var info = (ChronoshiftPowerInfo)power.Info;
				range = info.Range;
				tile = world.Map.Rules.Sequences.GetSequence(info.OverlaySpriteGroup, info.SourceTileSequence).GetSprite(0);
			}

			public IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				world.CancelInputMode();
				if (mi.Button == MouseButton.Left)
					world.OrderGenerator = new SelectDestination(world, order, manager, power, cell);

				yield break;
			}

			public void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.ContainsKey(order))
					world.CancelInputMode();
			}

			public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var targetUnits = power.UnitsInRange(xy).Where(a => !world.FogObscures(a));

				foreach (var unit in targetUnits)
					if (manager.Self.Owner.CanTargetActor(unit))
						yield return new SelectionBoxRenderable(unit, Color.Red);
			}

			public IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var tiles = world.Map.FindTilesInCircle(xy, range);
				var palette = wr.Palette(((ChronoshiftPowerInfo)power.Info).TargetOverlayPalette);
				foreach (var t in tiles)
					yield return new SpriteRenderable(tile, wr.World.Map.CenterOfCell(t), WVec.Zero, -511, palette, 1f, true);
			}

			public string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				return ((ChronoshiftPowerInfo)power.Info).SelectionCursor;
			}
		}

		class SelectDestination : IOrderGenerator
		{
			readonly ChronoshiftPower power;
			readonly CPos sourceLocation;
			readonly int range;
			readonly Sprite validTile, invalidTile, sourceTile;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectDestination(World world, string order, SupportPowerManager manager, ChronoshiftPower power, CPos sourceLocation)
			{
				this.manager = manager;
				this.order = order;
				this.power = power;
				this.sourceLocation = sourceLocation;

				var info = (ChronoshiftPowerInfo)power.Info;
				range = info.Range;

				var tileset = world.Map.Tileset.ToLowerInvariant();
				validTile = world.Map.Rules.Sequences.GetSequence(info.OverlaySpriteGroup, info.ValidTileSequencePrefix + tileset).GetSprite(0);
				invalidTile = world.Map.Rules.Sequences.GetSequence(info.OverlaySpriteGroup, info.InvalidTileSequence).GetSprite(0);
				sourceTile = world.Map.Rules.Sequences.GetSequence(info.OverlaySpriteGroup, info.SourceTileSequence).GetSprite(0);
			}

			public IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
				{
					world.CancelInputMode();
					yield break;
				}

				var ret = OrderInner(cell).FirstOrDefault();
				if (ret == null)
					yield break;

				world.CancelInputMode();
				yield return ret;
			}

			IEnumerable<Order> OrderInner(CPos xy)
			{
				// Cannot chronoshift into unexplored location
				if (IsValidTarget(xy))
					yield return new Order(order, manager.Self, false)
					{
						TargetLocation = xy,
						ExtraLocation = sourceLocation,
						SuppressVisualFeedback = true
					};
			}

			public void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.ContainsKey(order))
					world.CancelInputMode();
			}

			public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var palette = wr.Palette(power.Info.IconPalette);

				// Destination tiles
				foreach (var t in world.Map.FindTilesInCircle(xy, range))
				{
					var tile = manager.Self.Owner.Shroud.IsExplored(t) ? validTile : invalidTile;
					yield return new SpriteRenderable(tile, wr.World.Map.CenterOfCell(t), WVec.Zero, -511, palette, 1f, true);
				}

				// Unit previews
				foreach (var unit in power.UnitsInRange(sourceLocation))
				{
					if (manager.Self.Owner.CanTargetActor(unit))
					{
						var targetCell = unit.Location + (xy - sourceLocation);
						var canEnter = manager.Self.Owner.Shroud.IsExplored(targetCell) &&
							unit.Trait<Chronoshiftable>().CanChronoshiftTo(unit, targetCell);
						var tile = canEnter ? validTile : invalidTile;
						yield return new SpriteRenderable(tile, wr.World.Map.CenterOfCell(targetCell), WVec.Zero, -511, palette, 1f, true);
					}

					var offset = world.Map.CenterOfCell(xy) - world.Map.CenterOfCell(sourceLocation);
					if (manager.Self.Owner.CanTargetActor(unit))
						foreach (var r in unit.Render(wr))
							yield return r.OffsetBy(offset);
				}

				foreach (var unit in power.UnitsInRange(sourceLocation))
					if (manager.Self.Owner.CanTargetActor(unit))
						yield return new SelectionBoxRenderable(unit, Color.Red);
			}

			public IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var palette = wr.Palette(power.Info.IconPalette);

				// Source tiles
				foreach (var t in world.Map.FindTilesInCircle(sourceLocation, range))
					yield return new SpriteRenderable(sourceTile, wr.World.Map.CenterOfCell(t), WVec.Zero, -511, palette, 1f, true);
			}

			bool IsValidTarget(CPos xy)
			{
				var canTeleport = false;
				foreach (var unit in power.UnitsInRange(sourceLocation))
				{
					var targetCell = unit.Location + (xy - sourceLocation);
					if (manager.Self.Owner.Shroud.IsExplored(targetCell) && unit.Trait<Chronoshiftable>().CanChronoshiftTo(unit, targetCell))
					{
						canTeleport = true;
						break;
					}
				}

				if (!canTeleport)
				{
					// Check the terrain types. This will allow chronoshifts to occur on empty terrain to terrain of
					// a similar type. This also keeps the cursor from changing in non-visible property, alerting the
					// chronoshifter of enemy unit presence
					canTeleport = power.SimilarTerrain(sourceLocation, xy);
				}

				return canTeleport;
			}

			public string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				var powerInfo = (ChronoshiftPowerInfo)power.Info;
				return IsValidTarget(cell) ? powerInfo.TargetCursor : powerInfo.TargetBlockedCursor;
			}
		}
	}
}
