#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	class ChronoshiftPowerInfo : SupportPowerInfo
	{
		[FieldLoader.Require]
		[Desc("Size of the footprint of the affected area.")]
		public readonly CVec Dimensions = CVec.Zero;

		[FieldLoader.Require]
		[Desc("Actual footprint. Cells marked as x will be affected.")]
		public readonly string Footprint = string.Empty;

		[Desc("Ticks until returning after teleportation.")]
		public readonly int Duration = 750;

		[PaletteReference]
		public readonly string TargetOverlayPalette = TileSet.TerrainPaletteInternalName;

		public readonly string FootprintImage = "overlay";

		[SequenceReference(nameof(FootprintImage), prefix: true)]
		public readonly string ValidFootprintSequence = "target-valid";

		[SequenceReference(nameof(FootprintImage))]
		public readonly string InvalidFootprintSequence = "target-invalid";

		[SequenceReference(nameof(FootprintImage))]
		public readonly string SourceFootprintSequence = "target-select";

		public readonly bool KillCargo = true;

		[Desc("Cursor to display when selecting targets for the chronoshift.")]
		public readonly string SelectionCursor = "chrono-select";

		[Desc("Cursor to display when targeting an area for the chronoshift.")]
		public readonly string TargetCursor = "chrono-target";

		[Desc("Cursor to display when the targeted area is blocked.")]
		public readonly string TargetBlockedCursor = "move-blocked";

		public override object Create(ActorInitializer init) { return new ChronoshiftPower(init.Self, this); }
	}

	class ChronoshiftPower : SupportPower
	{
		readonly char[] footprint;
		readonly CVec dimensions;

		public ChronoshiftPower(Actor self, ChronoshiftPowerInfo info)
			: base(self, info)
		{
			footprint = info.Footprint.Where(c => !char.IsWhiteSpace(c)).ToArray();
			dimensions = info.Dimensions;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectChronoshiftTarget(Self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			var info = (ChronoshiftPowerInfo)Info;
			var targetDelta = self.World.Map.CellContaining(order.Target.CenterPosition) - order.ExtraLocation;
			foreach (var target in UnitsInRange(order.ExtraLocation))
			{
				var cs = target.TraitsImplementing<Chronoshiftable>()
					.FirstEnabledTraitOrDefault();

				if (cs == null)
					continue;

				var targetCell = target.Location + targetDelta;

				if (self.Owner.Shroud.IsExplored(targetCell) && cs.CanChronoshiftTo(target, targetCell))
					cs.Teleport(target, targetCell, info.Duration, info.KillCargo, self);
			}
		}

		public IEnumerable<Actor> UnitsInRange(CPos xy)
		{
			var tiles = CellsMatching(xy, footprint, dimensions);
			var units = new HashSet<Actor>();
			foreach (var t in tiles)
				units.UnionWith(Self.World.ActorMap.GetActorsAt(t));

			return units.Where(a => a.TraitsImplementing<Chronoshiftable>().Any(cs => !cs.IsTraitDisabled));
		}

		public bool SimilarTerrain(CPos xy, CPos sourceLocation)
		{
			if (!Self.Owner.Shroud.IsExplored(xy))
				return false;

			var sourceTiles = CellsMatching(xy, footprint, dimensions);
			var destTiles = CellsMatching(sourceLocation, footprint, dimensions);

			if (!sourceTiles.Any() || !destTiles.Any())
				return false;

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

		class SelectChronoshiftTarget : OrderGenerator
		{
			readonly ChronoshiftPower power;
			readonly char[] footprint;
			readonly CVec dimensions;
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
				footprint = info.Footprint.Where(c => !char.IsWhiteSpace(c)).ToArray();
				dimensions = info.Dimensions;
				tile = world.Map.Rules.Sequences.GetSequence(info.FootprintImage, info.SourceFootprintSequence).GetSprite(0);
			}

			protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				world.CancelInputMode();
				if (mi.Button == MouseButton.Left)
					world.OrderGenerator = new SelectDestination(world, order, manager, power, cell);

				yield break;
			}

			protected override void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.ContainsKey(order))
					world.CancelInputMode();
			}

			protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

			protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var targetUnits = power.UnitsInRange(xy).Where(a => !world.FogObscures(a));

				foreach (var unit in targetUnits)
				{
					if (unit.CanBeViewedByPlayer(manager.Self.Owner))
					{
						var decorations = unit.TraitsImplementing<ISelectionDecorations>().FirstEnabledTraitOrDefault();
						if (decorations != null)
							foreach (var d in decorations.RenderSelectionAnnotations(unit, wr, Color.Red))
								yield return d;
					}
				}
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var tiles = power.CellsMatching(xy, footprint, dimensions);
				var palette = wr.Palette(((ChronoshiftPowerInfo)power.Info).TargetOverlayPalette);
				foreach (var t in tiles)
					yield return new SpriteRenderable(tile, wr.World.Map.CenterOfCell(t), WVec.Zero, -511, palette, 1f, true, TintModifiers.IgnoreWorldTint);
			}

			protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				return ((ChronoshiftPowerInfo)power.Info).SelectionCursor;
			}
		}

		class SelectDestination : OrderGenerator
		{
			readonly ChronoshiftPower power;
			readonly CPos sourceLocation;
			readonly char[] footprint;
			readonly CVec dimensions;
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
				footprint = info.Footprint.Where(c => !char.IsWhiteSpace(c)).ToArray();
				dimensions = info.Dimensions;

				var sequences = world.Map.Rules.Sequences;
				var tilesetValid = info.ValidFootprintSequence + "-" + world.Map.Tileset.ToLowerInvariant();
				if (sequences.HasSequence(info.FootprintImage, tilesetValid))
					validTile = sequences.GetSequence(info.FootprintImage, tilesetValid).GetSprite(0);
				else
					validTile = sequences.GetSequence(info.FootprintImage, info.ValidFootprintSequence).GetSprite(0);

				invalidTile = sequences.GetSequence(info.FootprintImage, info.InvalidFootprintSequence).GetSprite(0);
				sourceTile = sequences.GetSequence(info.FootprintImage, info.SourceFootprintSequence).GetSprite(0);
			}

			protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
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
					yield return new Order(order, manager.Self, Target.FromCell(manager.Self.World, xy), false)
					{
						ExtraLocation = sourceLocation,
						SuppressVisualFeedback = true
					};
			}

			protected override void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.ContainsKey(order))
					world.CancelInputMode();
			}

			protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var palette = wr.Palette(power.Info.IconPalette);

				// Destination tiles
				var delta = xy - sourceLocation;
				foreach (var t in power.CellsMatching(sourceLocation, footprint, dimensions))
				{
					var tile = manager.Self.Owner.Shroud.IsExplored(t + delta) ? validTile : invalidTile;
					yield return new SpriteRenderable(tile, wr.World.Map.CenterOfCell(t + delta), WVec.Zero, -511, palette, 1f, true, TintModifiers.IgnoreWorldTint);
				}

				// Unit previews
				foreach (var unit in power.UnitsInRange(sourceLocation))
				{
					if (unit.CanBeViewedByPlayer(manager.Self.Owner))
					{
						var targetCell = unit.Location + (xy - sourceLocation);
						var canEnter = manager.Self.Owner.Shroud.IsExplored(targetCell) &&
							unit.Trait<Chronoshiftable>().CanChronoshiftTo(unit, targetCell);
						var tile = canEnter ? validTile : invalidTile;
						yield return new SpriteRenderable(tile, wr.World.Map.CenterOfCell(targetCell), WVec.Zero, -511, palette, 1f, true, TintModifiers.IgnoreWorldTint);
					}

					var offset = world.Map.CenterOfCell(xy) - world.Map.CenterOfCell(sourceLocation);
					if (unit.CanBeViewedByPlayer(manager.Self.Owner))
						foreach (var r in unit.Render(wr))
							yield return r.OffsetBy(offset);
				}
			}

			protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
			{
				foreach (var unit in power.UnitsInRange(sourceLocation))
				{
					if (unit.CanBeViewedByPlayer(manager.Self.Owner))
					{
						var decorations = unit.TraitsImplementing<ISelectionDecorations>().FirstEnabledTraitOrDefault();
						if (decorations != null)
							foreach (var d in decorations.RenderSelectionAnnotations(unit, wr, Color.Red))
								yield return d;
					}
				}
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var palette = wr.Palette(power.Info.IconPalette);

				// Source tiles
				foreach (var t in power.CellsMatching(sourceLocation, footprint, dimensions))
					yield return new SpriteRenderable(sourceTile, wr.World.Map.CenterOfCell(t), WVec.Zero, -511, palette, 1f, true, TintModifiers.IgnoreWorldTint);
			}

			bool IsValidTarget(CPos xy)
			{
				// Don't teleport if there are no units in range (either all moved out of range, or none yet moved into range)
				var unitsInRange = power.UnitsInRange(sourceLocation);
				if (!unitsInRange.Any())
					return false;

				var canTeleport = false;
				foreach (var unit in unitsInRange)
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

			protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				var powerInfo = (ChronoshiftPowerInfo)power.Info;
				return IsValidTarget(cell) ? powerInfo.TargetCursor : powerInfo.TargetBlockedCursor;
			}
		}
	}
}
