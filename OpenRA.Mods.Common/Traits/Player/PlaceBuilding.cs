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

using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	// Allows third party mods to detect whether an actor was created by PlaceBuilding.
	public class PlaceBuildingInit : RuntimeFlagInit { }

	[TraitLocation(SystemActors.Player)]
	[Desc("Allows the player to execute build orders.", " Attach this to the player actor.")]
	public class PlaceBuildingInfo : TraitInfo
	{
		[Desc("Play NewOptionsNotification this many ticks after building placement.")]
		public readonly int NewOptionsNotificationDelay = 10;

		[NotificationReference("Speech")]
		[Desc("Speech notification to play after building placement if new construction options are available.")]
		public readonly string NewOptionsNotification = null;

		[Desc("Text notification to display after building placement if new construction options are available.")]
		public readonly string NewOptionsTextNotification = null;

		[NotificationReference("Speech")]
		[Desc("Speech notification to play if building placement is not possible.")]
		public readonly string CannotPlaceNotification = null;

		[Desc("Text notification to display if building placement is not possible.")]
		public readonly string CannotPlaceTextNotification = null;

		[Desc("Hotkey to toggle between PlaceBuildingVariants when placing a structure.")]
		public readonly HotkeyReference ToggleVariantKey = new HotkeyReference();

		public override object Create(ActorInitializer init) { return new PlaceBuilding(this); }
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
				var faction = producer.Trait?.Faction ?? self.Owner.Faction.InternalName;
				var buildingInfo = actorInfo.TraitInfo<BuildingInfo>();

				var buildableInfo = actorInfo.TraitInfoOrDefault<BuildableInfo>();
				if (buildableInfo != null && buildableInfo.ForceFaction != null)
					faction = buildableInfo.ForceFaction;

				var replaceableTypes = actorInfo.TraitInfos<ReplacementInfo>()
					.SelectMany(r => r.ReplaceableTypes)
					.ToHashSet();

				if (replaceableTypes.Count > 0)
					foreach (var t in buildingInfo.Tiles(targetLocation))
						foreach (var a in self.World.ActorMap.GetActorsAt(t))
							if (a.TraitsImplementing<Replaceable>().Any(r => !r.IsTraitDisabled && r.Info.Types.Overlaps(replaceableTypes)))
								self.World.Remove(a);

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

					foreach (var t in BuildingUtils.GetLineBuildCells(w, targetLocation, actorInfo, buildingInfo, order.Player))
					{
						if (t.Cell == targetLocation)
							continue;

						var segment = self.World.Map.Rules.Actors[segmentType];
						var replaceableSegments = segment.TraitInfos<ReplacementInfo>()
							.SelectMany(r => r.ReplaceableTypes)
							.ToHashSet();

						if (replaceableSegments.Count > 0)
							foreach (var a in self.World.ActorMap.GetActorsAt(t.Cell))
								if (a.TraitsImplementing<Replaceable>().Any(r => !r.IsTraitDisabled && r.Info.Types.Overlaps(replaceableSegments)))
									self.World.Remove(a);

						w.CreateActor(segmentType, new TypeDictionary
						{
							new LocationInit(t.Cell),
							new OwnerInit(order.Player),
							new FactionInit(faction),
							new LineBuildDirectionInit(t.Cell.X == targetLocation.X ? LineBuildDirection.Y : LineBuildDirection.X),
							new LineBuildParentInit(new[] { t.Actor, placed }),
							new PlaceBuildingInit()
						});
					}
				}
				else if (os == "PlacePlug")
				{
					var plugInfo = actorInfo.TraitInfoOrDefault<PlugInfo>();
					if (plugInfo == null)
						return;

					foreach (var a in self.World.ActorMap.GetActorsAt(targetLocation))
					{
						var pluggables = a.TraitsImplementing<Pluggable>()
							.Where(p => p.AcceptsPlug(plugInfo.Type))
							.ToList();

						var pluggable = pluggables.FirstOrDefault(p => a.Location + p.Info.Offset == targetLocation)
							?? pluggables.FirstOrDefault();

						if (pluggable == null)
							return;

						pluggable.EnablePlug(a, plugInfo.Type);
						foreach (var s in buildingInfo.BuildSounds)
							Game.Sound.PlayToPlayer(SoundType.World, order.Player, s, a.CenterPosition);
					}
				}
				else
				{
					if (!self.World.CanPlaceBuilding(targetLocation, actorInfo, buildingInfo, null)
						|| !buildingInfo.IsCloseEnoughToBase(self.World, order.Player, actorInfo, targetLocation))
						return;

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

				// FindBaseProvider may return null if the build anywhere cheat is active
				// BuildingInfo.IsCloseEnoughToBase has already verified that this is a valid build location
				if (buildingInfo.RequiresBaseProvider)
					buildingInfo.FindBaseProvider(w, self.Owner, targetLocation)?.BeginCooldown();

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
			TextNotificationsManager.AddTransientLine(info.NewOptionsTextNotification, self.Owner);

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
