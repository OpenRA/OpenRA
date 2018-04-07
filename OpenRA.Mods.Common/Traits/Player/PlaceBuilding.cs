#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Allows the player to execute build orders.", " Attach this to the player actor.")]
	public class PlaceBuildingInfo : ITraitInfo
	{
		[Desc("Palette to use for rendering the placement sprite.")]
		[PaletteReference] public readonly string Palette = TileSet.TerrainPaletteInternalName;

		[Desc("Palette to use for rendering the placement sprite for line build segments.")]
		[PaletteReference] public readonly string LineBuildSegmentPalette = TileSet.TerrainPaletteInternalName;

		[Desc("Play NewOptionsNotification this many ticks after building placement.")]
		public readonly int NewOptionsNotificationDelay = 10;

		[Desc("Notification to play after building placement if new construction options are available.")]
		public readonly string NewOptionsNotification = "NewOptions";

		public object Create(ActorInitializer init) { return new PlaceBuilding(this); }
	}

	public class PlaceBuilding : IResolveOrder, ITick
	{
		readonly PlaceBuildingInfo info;
		bool triggerNotification;
		int tick;

		public PlaceBuilding(PlaceBuildingInfo info)
		{
			this.info = info;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			var os = order.OrderString;
			if (os != "PlaceBuilding" &&
				os != "LineBuild" &&
				os != "PlacePlug")
				return;

			self.World.AddFrameEndTask(w =>
			{
				var prevItems = GetNumBuildables(self.Owner);
				var targetActor = w.GetActorById(order.ExtraData);

				if (targetActor == null || targetActor.IsDead)
					return;

				var actorInfo = self.World.Map.Rules.Actors[order.TargetString];
				var queue = targetActor.TraitsImplementing<ProductionQueue>()
					.FirstOrDefault(q => q.CanBuild(actorInfo) && q.CurrentItem() != null && q.CurrentItem().Item == order.TargetString && q.CurrentItem().RemainingTime == 0);

				if (queue == null)
					return;

				var producer = queue.MostLikelyProducer();
				var faction = producer.Trait != null ? producer.Trait.Faction : self.Owner.Faction.InternalName;
				var buildingInfo = actorInfo.TraitInfo<BuildingInfo>();

				var buildableInfo = actorInfo.TraitInfoOrDefault<BuildableInfo>();
				if (buildableInfo != null && buildableInfo.ForceFaction != null)
					faction = buildableInfo.ForceFaction;

				if (os == "LineBuild")
				{
					// Build the parent actor first
					var placed = w.CreateActor(order.TargetString, new TypeDictionary
					{
						new LocationInit(order.TargetLocation),
						new OwnerInit(order.Player),
						new FactionInit(faction),
					});

					foreach (var s in buildingInfo.BuildSounds)
						Game.Sound.PlayToPlayer(SoundType.World, order.Player, s, placed.CenterPosition);

					// Build the connection segments
					var segmentType = actorInfo.TraitInfo<LineBuildInfo>().SegmentType;
					if (string.IsNullOrEmpty(segmentType))
						segmentType = order.TargetString;

					foreach (var t in BuildingUtils.GetLineBuildCells(w, order.TargetLocation, actorInfo, buildingInfo))
					{
						if (t.First == order.TargetLocation)
							continue;

						w.CreateActor(t.First == order.TargetLocation ? order.TargetString : segmentType, new TypeDictionary
						{
							new LocationInit(t.First),
							new OwnerInit(order.Player),
							new FactionInit(faction),
							new LineBuildDirectionInit(t.First.X == order.TargetLocation.X ? LineBuildDirection.Y : LineBuildDirection.X),
							new LineBuildParentInit(new[] { t.Second, placed })
						});
					}
				}
				else if (os == "PlacePlug")
				{
					var host = self.World.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(order.TargetLocation);
					if (host == null)
						return;

					var plugInfo = actorInfo.TraitInfoOrDefault<PlugInfo>();
					if (plugInfo == null)
						return;

					var location = host.Location;
					var pluggable = host.TraitsImplementing<Pluggable>()
						.FirstOrDefault(p => location + p.Info.Offset == order.TargetLocation && p.AcceptsPlug(host, plugInfo.Type));

					if (pluggable == null)
						return;

					pluggable.EnablePlug(host, plugInfo.Type);
					foreach (var s in buildingInfo.BuildSounds)
						Game.Sound.PlayToPlayer(SoundType.World, order.Player, s, host.CenterPosition);
				}
				else
				{
					if (!self.World.CanPlaceBuilding(order.TargetLocation, actorInfo, buildingInfo, null)
						|| !buildingInfo.IsCloseEnoughToBase(self.World, order.Player, actorInfo, order.TargetLocation))
						return;

					var replacementInfo = actorInfo.TraitInfoOrDefault<ReplacementInfo>();
					if (replacementInfo != null)
					{
						var buildingInfluence = self.World.WorldActor.Trait<BuildingInfluence>();
						foreach (var t in buildingInfo.Tiles(order.TargetLocation))
						{
							var host = buildingInfluence.GetBuildingAt(t);
							if (host != null)
								host.World.Remove(host);
						}
					}

					var building = w.CreateActor(order.TargetString, new TypeDictionary
					{
						new LocationInit(order.TargetLocation),
						new OwnerInit(order.Player),
						new FactionInit(faction),
					});

					foreach (var s in buildingInfo.BuildSounds)
						Game.Sound.PlayToPlayer(SoundType.World, order.Player, s, building.CenterPosition);
				}

				if (producer.Actor != null)
					foreach (var nbp in producer.Actor.TraitsImplementing<INotifyBuildingPlaced>())
						nbp.BuildingPlaced(producer.Actor);

				queue.FinishProduction();

				if (buildingInfo.RequiresBaseProvider)
				{
					// May be null if the build anywhere cheat is active
					// BuildingInfo.IsCloseEnoughToBase has already verified that this is a valid build location
					var provider = buildingInfo.FindBaseProvider(w, self.Owner, order.TargetLocation);
					if (provider != null)
						provider.BeginCooldown();
				}

				if (GetNumBuildables(self.Owner) > prevItems)
					triggerNotification = true;
			});
		}

		void ITick.Tick(Actor self)
		{
			if (!triggerNotification)
				return;

			if (tick++ >= info.NewOptionsNotificationDelay)
				PlayNotification(self);
		}

		void PlayNotification(Actor self)
		{
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.NewOptionsNotification, self.Owner.Faction.InternalName);
			triggerNotification = false;
			tick = 0;
		}

		static int GetNumBuildables(Player p)
		{
			// This only matters for local players.
			if (p != p.World.LocalPlayer)
				return 0;

			return p.World.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == p)
				.SelectMany(a => a.Trait.BuildableItems()).Distinct().Count();
		}
	}
}
