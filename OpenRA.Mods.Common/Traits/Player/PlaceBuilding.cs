#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	// Allows third party mods to detect whether an actor was created by PlaceBuilding.
	public class PlaceBuildingInit : IActorInit { }

	[Desc("Allows the player to execute build orders.", " Attach this to the player actor.")]
	public class PlaceBuildingInfo : ITraitInfo
	{
		[Desc("Play NewOptionsNotification this many ticks after building placement.")]
		public readonly int NewOptionsNotificationDelay = 10;

		[NotificationReference("Speech")]
		[Desc("Notification to play after building placement if new construction options are available.")]
		public readonly string NewOptionsNotification = null;

		[NotificationReference("Speech")]
		public readonly string CannotPlaceNotification = null;

		[Desc("Hotkey to toggle between PlaceBuildingVariants when placing a structure.")]
		public HotkeyReference ToggleVariantKey = new HotkeyReference();

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
				var targetLocation = w.Map.CellContaining(order.Target.CenterPosition);

				if (targetActor == null || targetActor.IsDead)
					return;

				var actorInfo = self.World.Map.Rules.Actors[order.TargetString];
				var queue = targetActor.TraitsImplementing<ProductionQueue>()
					.FirstOrDefault(q => q.CanBuild(actorInfo) && q.AllQueued().Any(i => i.Done && i.Item == order.TargetString));

				if (queue == null)
					return;

				// Find the ProductionItem associated with the building that we are trying to place
				var item = queue.AllQueued().FirstOrDefault(i => i.Done && i.Item == order.TargetString);

				if (item == null)
					return;

				// Override with the alternate actor
				if (order.ExtraLocation.X > 0)
				{
					var variant = actorInfo.TraitInfos<PlaceBuildingVariantsInfo>()
						.SelectMany(p => p.Actors)
						.Skip(order.ExtraLocation.X - 1)
						.FirstOrDefault();

					if (variant != null)
						actorInfo = self.World.Map.Rules.Actors[variant];
				}

				var producer = queue.MostLikelyProducer();
				var faction = producer.Trait != null ? producer.Trait.Faction : self.Owner.Faction.InternalName;
				var buildingInfo = actorInfo.TraitInfo<BuildingInfo>();

				var buildableInfo = actorInfo.TraitInfoOrDefault<BuildableInfo>();
				if (buildableInfo != null && buildableInfo.ForceFaction != null)
					faction = buildableInfo.ForceFaction;

				if (os == "LineBuild")
				{
					// Build the parent actor first
					var placed = w.CreateActor(actorInfo.Name, new TypeDictionary
					{
						new LocationInit(targetLocation),
						new OwnerInit(order.Player),
						new FactionInit(faction),
						new PlaceBuildingInit()
					});

					foreach (var s in buildingInfo.BuildSounds)
						Game.Sound.PlayToPlayer(SoundType.World, order.Player, s, placed.CenterPosition);

					// Build the connection segments
					var segmentType = actorInfo.TraitInfo<LineBuildInfo>().SegmentType;
					if (string.IsNullOrEmpty(segmentType))
						segmentType = actorInfo.Name;

					foreach (var t in BuildingUtils.GetLineBuildCells(w, targetLocation, actorInfo, buildingInfo))
					{
						if (t.First == targetLocation)
							continue;

						w.CreateActor(t.First == targetLocation ? actorInfo.Name : segmentType, new TypeDictionary
						{
							new LocationInit(t.First),
							new OwnerInit(order.Player),
							new FactionInit(faction),
							new LineBuildDirectionInit(t.First.X == targetLocation.X ? LineBuildDirection.Y : LineBuildDirection.X),
							new LineBuildParentInit(new[] { t.Second, placed }),
							new PlaceBuildingInit()
						});
					}
				}
				else if (os == "PlacePlug")
				{
					var host = self.World.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(targetLocation);
					if (host == null)
						return;

					var plugInfo = actorInfo.TraitInfoOrDefault<PlugInfo>();
					if (plugInfo == null)
						return;

					var location = host.Location;
					var pluggable = host.TraitsImplementing<Pluggable>()
						.FirstOrDefault(p => location + p.Info.Offset == targetLocation && p.AcceptsPlug(host, plugInfo.Type));

					if (pluggable == null)
						return;

					pluggable.EnablePlug(host, plugInfo.Type);
					foreach (var s in buildingInfo.BuildSounds)
						Game.Sound.PlayToPlayer(SoundType.World, order.Player, s, host.CenterPosition);
				}
				else
				{
					if (!self.World.CanPlaceBuilding(targetLocation, actorInfo, buildingInfo, null)
						|| !buildingInfo.IsCloseEnoughToBase(self.World, order.Player, actorInfo, targetLocation))
						return;

					var replacementInfo = actorInfo.TraitInfoOrDefault<ReplacementInfo>();
					if (replacementInfo != null)
					{
						var buildingInfluence = self.World.WorldActor.Trait<BuildingInfluence>();
						foreach (var t in buildingInfo.Tiles(targetLocation))
						{
							var host = buildingInfluence.GetBuildingAt(t);
							if (host != null)
								host.World.Remove(host);
						}
					}

					var building = w.CreateActor(actorInfo.Name, new TypeDictionary
					{
						new LocationInit(targetLocation),
						new OwnerInit(order.Player),
						new FactionInit(faction),
						new PlaceBuildingInit()
					});

					foreach (var s in buildingInfo.BuildSounds)
						Game.Sound.PlayToPlayer(SoundType.World, order.Player, s, building.CenterPosition);
				}

				if (producer.Actor != null)
					foreach (var nbp in producer.Actor.TraitsImplementing<INotifyBuildingPlaced>())
						nbp.BuildingPlaced(producer.Actor);

				queue.EndProduction(item);

				if (buildingInfo.RequiresBaseProvider)
				{
					// May be null if the build anywhere cheat is active
					// BuildingInfo.IsCloseEnoughToBase has already verified that this is a valid build location
					var provider = buildingInfo.FindBaseProvider(w, self.Owner, targetLocation);
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
