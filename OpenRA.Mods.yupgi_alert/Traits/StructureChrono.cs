#region Copyright & License Information
/*
 * Modded by Boolbada of OP Mod.
 * Modified from PortableChrono by OpenRA devs.
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

/* Works without base engine modification */

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	class StructureChronoInfo : ITraitInfo
	{
		[Desc("Cooldown in ticks until the unit can teleport.")]
		public readonly int ChargeDelay = 500;

		[Desc("Can the unit teleport only a certain distance?")]
		public readonly bool HasDistanceLimit = true;

		[Desc("The maximum distance in cells this unit can teleport (only used if HasDistanceLimit = true).")]
		public readonly int MaxDistance = 12;

		[Desc("Sound to play when teleporting.")]
		public readonly string ChronoshiftSound = "chrotnk1.aud";

		[Desc("Cursor to display when able to deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[Desc("Cursor to display when unable to deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[VoiceReference]
		public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new StructureChrono(this); }
	}

	class StructureChrono : IIssueOrder, IResolveOrder, ITick, ISelectionBar, IOrderVoice, ISync
	{
		[Sync]
		int chargeTick = 0;
		public readonly StructureChronoInfo Info;

		public StructureChrono(StructureChronoInfo info)
		{
			Info = info;
		}

		public void Tick(Actor self)
		{
			if (chargeTick > 0)
				chargeTick--;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new DeployOrderTargeter("StructureChronoDeploy", 5,
					() => CanTeleport ? Info.DeployCursor : Info.DeployBlockedCursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "StructureChronoDeploy" && CanTeleport)
			{
				// Assuming I'm a building.
				self.World.OrderGenerator = new StructureChronoOrderGenerator(self, Info);
			}

			if (order.OrderID == "StructureChronoTeleport")
				return new Order(order.OrderID, self, queued) { TargetLocation = self.World.Map.CellContaining(target.CenterPosition) };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "StructureChronoTeleport" && CanTeleport)
			{
				self.CancelActivity();

				self.World.AddFrameEndTask(w =>
				{
					var init = new TypeDictionary
					{
						new LocationInit(order.TargetLocation),
						new OwnerInit(order.Player),
						new FactionInit(self.Owner.Faction.InternalName)
					};

					var health = self.TraitOrDefault<Health>();
					if (health != null)
					{
						var newHP = (health.HP * 100) / health.MaxHP;
						init.Add(new HealthInit(newHP));
					}

					// TODO: I'm assuming that structures don't get veterrancy.
					// If necessary, add veterrancy to the new actor.
					var building = w.CreateActor(self.Info.Name, init);

					// Chronoshift is "used".
					building.Trait<StructureChrono>().chargeTick = self.Info.TraitInfo<StructureChronoInfo>().ChargeDelay;

					// "sell" the old one.
					self.Dispose();

					// Play chrono sound
					Game.Sound.Play(SoundType.World, Info.ChronoshiftSound, building.CenterPosition);
				});
			}
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "StructureChronoTeleport" && CanTeleport ? Info.Voice : null;
		}

		public void ResetChargeTime()
		{
			chargeTick = Info.ChargeDelay;
		}

		public bool CanTeleport
		{
			get { return chargeTick <= 0; }
		}

		float ISelectionBar.GetValue()
		{
			return (float)(Info.ChargeDelay - chargeTick) / Info.ChargeDelay;
		}

		Color ISelectionBar.GetColor() { return Color.Magenta; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}

	class StructureChronoOrderTargeter : IOrderTargeter
	{
		readonly string targetCursor;

		public StructureChronoOrderTargeter(string targetCursor)
		{
			this.targetCursor = targetCursor;
		}

		public string OrderID { get { return "StructureChronoTeleport"; } }
		public int OrderPriority { get { return 5; } }
		public bool IsQueued { get; protected set; }
		public bool TargetOverridesSelection(TargetModifiers modifiers) { return true; }

		public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
		{
			// TODO: When target modifiers are configurable this needs to be revisited
			if (modifiers.HasModifier(TargetModifiers.ForceMove) || modifiers.HasModifier(TargetModifiers.ForceQueue))
			{
				var xy = self.World.Map.CellContaining(target.CenterPosition);

				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				if (self.IsInWorld && self.Owner.Shroud.IsExplored(xy))
				{
					cursor = targetCursor;
					return true;
				}

				return false;
			}

			return false;
		}
	}

	class StructureChronoOrderGenerator : IOrderGenerator
	{
		readonly string building; // name in rules definition
		readonly BuildingInfo buildingInfo;
		readonly Sprite buildOk;
		readonly Sprite buildBlocked;
		readonly Actor self;
		readonly StructureChronoInfo info;

		IActorPreview[] preview;
		bool initialized;

		public StructureChronoOrderGenerator(Actor self, StructureChronoInfo info)
		{
			var world = self.World;
			this.self = self;
			this.info = info;

			buildingInfo = self.Info.TraitInfo<BuildingInfo>();
			var map = world.Map;
			var tileset = world.Map.Tileset.ToLowerInvariant();
			buildOk = map.Rules.Sequences.GetSequence("overlay", "build-valid-{0}".F(tileset)).GetSprite(0);
			buildBlocked = map.Rules.Sequences.GetSequence("overlay", "build-invalid").GetSprite(0);

			this.building = self.Info.Name; // name in the rules definition
		}

		public IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			// Cancel Chrono of the building
			if (mi.Button == MouseButton.Right)
			{
				world.CancelInputMode();
				yield break;
			}

			var ret = InnerOrder(world, cell, mi);
			if (ret.Count() == 0)
			{
				// Unbuildable area or out of range.
				world.CancelInputMode();
				yield break;
			}

			if (self.IsInWorld && self.Location != cell
				&& self.Trait<StructureChrono>().CanTeleport && self.Owner.Shroud.IsExplored(cell))
			{
				world.CancelInputMode();
				yield return new Order("StructureChronoTeleport", self, mi.Modifiers.HasModifier(Modifiers.Shift)) { TargetLocation = cell };
			}
		}

		IEnumerable<CPos> InnerOrder(World world, CPos cell, MouseInput mi)
		{
			if (world.Paused)
				yield break;

			var owner = self.Owner;
			if (mi.Button == MouseButton.Left)
			{
				var topLeft = cell - FootprintUtils.AdjustForBuildingSize(buildingInfo);
				var selfPos = self.Trait<IOccupySpace>().TopLeft;
				var isCloseEnough = (topLeft - selfPos).Length <= info.MaxDistance;

				if (!world.CanPlaceBuilding(building, buildingInfo, topLeft, null) || !isCloseEnough)
				{
					Game.Sound.PlayNotification(world.Map.Rules, owner, "Speech", "BuildingCannotPlaceAudio", owner.Faction.InternalName);
					yield break;
				}

				yield return new CPos(topLeft.X, topLeft.Y);
			}
		}

		public void Tick(World world)
		{
			if (preview == null)
				return;

			foreach (var p in preview)
				p.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
		{
			// Draw chrono range
			yield return new RangeCircleRenderable(
				self.CenterPosition,
				WDist.FromCells(self.Trait<StructureChrono>().Info.MaxDistance),
				0,
				Color.FromArgb(128, Color.LawnGreen),
				Color.FromArgb(96, Color.Black));

			var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
			var topLeft = xy - FootprintUtils.AdjustForBuildingSize(buildingInfo);
			var offset = world.Map.CenterOfCell(topLeft) + FootprintUtils.CenterOffset(world, buildingInfo);
			var rules = world.Map.Rules;

			var actorInfo = self.Info; // rules.Actors[building];
			foreach (var dec in actorInfo.TraitInfos<IPlaceBuildingDecorationInfo>())
				foreach (var r in dec.Render(wr, world, actorInfo, offset))
					yield return r;

			// Cells, we are about to construct and occupy.
			var cells = new Dictionary<CPos, bool>();

			if (!initialized)
			{
				var td = new TypeDictionary()
				{
					new OpenRA.Mods.Common.FactionInit(""),
					new OwnerInit(self.Owner),
					new HideBibPreviewInit()
				};

				var init = new ActorPreviewInitializer(actorInfo, wr, td);
				preview = actorInfo.TraitInfos<IRenderActorPreviewInfo>()
					.SelectMany(rpi => rpi.RenderPreview(init))
					.ToArray();

				initialized = true;
			}

			var previewRenderables = preview
				.SelectMany(p => p.Render(wr, offset))
				.OrderBy(WorldRenderer.RenderableScreenZPositionComparisonKey);

			foreach (var r in previewRenderables)
				yield return r;

			var res = world.WorldActor.Trait<ResourceLayer>();
			var selfPos = self.Trait<IOccupySpace>().TopLeft;
			var isCloseEnough = (topLeft - selfPos).Length <= info.MaxDistance;
			foreach (var t in FootprintUtils.Tiles(rules, building, buildingInfo, topLeft))
				cells.Add(t, isCloseEnough && world.IsCellBuildable(t, buildingInfo) && res.GetResource(t) == null);

			var placeBuildingInfo = self.Owner.PlayerActor.Info.TraitInfo<PlaceBuildingInfo>();
			var pal = wr.Palette(placeBuildingInfo.Palette);
			var topLeftPos = world.Map.CenterOfCell(topLeft);

			// draw red or white buildable cell indicator.
			foreach (var c in cells)
			{
				var tile = c.Value ? buildOk : buildBlocked;
				var pos = world.Map.CenterOfCell(c.Key);
				yield return new SpriteRenderable(tile, pos, new WVec(0, 0, topLeftPos.Z - pos.Z),
					-511, pal, 1f, true);
			}
		}

		public string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi) { return "default"; }

		IEnumerable<Order> ClearBlockersOrders(World world, CPos topLeft)
		{
			var allTiles = FootprintUtils.Tiles(world.Map.Rules, building, buildingInfo, topLeft).ToArray();
			var neightborTiles = OpenRA.Mods.Common.Util.ExpandFootprint(allTiles, true).Except(allTiles)
				.Where(world.Map.Contains).ToList();

			var blockers = allTiles.SelectMany(world.ActorMap.GetActorsAt)
				.Where(a => a.Owner == self.Owner && a.IsIdle)
				.Select(a => new TraitPair<Mobile>(a, a.TraitOrDefault<Mobile>()));

			foreach (var blocker in blockers.Where(x => x.Trait != null))
			{
				var availableCells = neightborTiles.Where(t => blocker.Trait.CanEnterCell(t)).ToList();
				if (availableCells.Count == 0)
					continue;

				yield return new Order("Move", blocker.Actor, false)
				{
					TargetLocation = blocker.Actor.ClosestCell(availableCells)
				};
			}
		}
	}
}
