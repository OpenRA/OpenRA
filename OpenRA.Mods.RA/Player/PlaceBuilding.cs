#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class PlaceBuildingInfo : TraitInfo<PlaceBuilding> { }

	class PlaceBuilding : IResolveOrder
	{
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PlaceBuilding" ||
				order.OrderString == "PlaceModule" ||
				order.OrderString == "LineBuild")
			{
				self.World.AddFrameEndTask(w =>
				{
					var prevItems = GetNumBuildables(self.Owner);

					// Find the queue with the target actor
					var queue = w.ActorsWithTrait<ProductionQueue>()
						.Where(p => p.Actor.Owner == self.Owner &&
									 p.Trait.CurrentItem() != null &&
									 p.Trait.CurrentItem().Item == order.TargetString &&
									 p.Trait.CurrentItem().RemainingTime == 0)
						.Select(p => p.Trait)
						.FirstOrDefault();

					if (queue == null)
						return;

					var unit = self.World.Map.Rules.Actors[order.TargetString];
					var buildingInfo = unit.Traits.Get<BuildingInfo>();

					if (order.OrderString == "LineBuild")
					{
						bool playSounds = true;
						foreach (var t in BuildingUtils.GetLineBuildCells(w, order.TargetLocation, order.TargetString, buildingInfo))
						{
							var building = w.CreateActor(order.TargetString, new TypeDictionary
							{
								new LocationInit(t),
								new OwnerInit(order.Player),
							});

							if (playSounds)
								foreach (var s in buildingInfo.BuildSounds)
									Sound.PlayToPlayer(order.Player, s, building.CenterPosition);

							playSounds = false;
						}
					}
					else if (order.OrderString == "PlaceModule")
					{
						var placeLocation = order.TargetLocation;
						var existing = self.World.ActorMap.GetUnitsAt(placeLocation).FirstOrDefault();

						if (!self.World.CanPlaceModule(existing, unit, placeLocation))
							return;

						var module = w.CreateActor(order.TargetString, new TypeDictionary
						{
							new LocationInit(placeLocation),
							new OwnerInit(order.Player)
						});

						foreach (var mod in existing.TraitsImplementing<Modular>())
							if (mod.Info.CellOffset + existing.Location == placeLocation)
								mod.UpgradeActor = module;

						foreach (var s in buildingInfo.BuildSounds)
							Sound.PlayToPlayer(order.Player, s, module.CenterPosition);
					}
					else
					{
						if (!self.World.CanPlaceBuilding(order.TargetString, buildingInfo, order.TargetLocation, null)
							|| !buildingInfo.IsCloseEnoughToBase(self.World, order.Player, order.TargetString, order.TargetLocation))
						{
							return;
						}

						var building = w.CreateActor(order.TargetString, new TypeDictionary
						{
							new LocationInit(order.TargetLocation),
							new OwnerInit(order.Player),
						});

						foreach (var s in buildingInfo.BuildSounds)
							Sound.PlayToPlayer(order.Player, s, building.CenterPosition);
					}

					PlayBuildAnim(self, unit);

					queue.FinishProduction();

					if (buildingInfo.RequiresBaseProvider)
					{
						// May be null if the build anywhere cheat is active
						// BuildingInfo.IsCloseEnoughToBase has already verified that this is a valid build location
						var producer = buildingInfo.FindBaseProvider(w, self.Owner, order.TargetLocation);
						if (producer != null)
							producer.Trait<BaseProvider>().BeginCooldown();
					}

					if (GetNumBuildables(self.Owner) > prevItems)
						w.Add(new DelayedAction(10,
							() => Sound.PlayNotification(self.World.Map.Rules, order.Player, "Speech", "NewOptions", order.Player.Country.Race)));
				});
			}
		}

		// finds a construction yard (or equivalent) and runs its "build" animation.
		static void PlayBuildAnim(Actor self, ActorInfo unit)
		{
			var bi = unit.Traits.GetOrDefault<BuildableInfo>();
			if (bi == null)
				return;

			var producers = self.World.ActorsWithTrait<Production>()
				.Where(x => x.Actor.Owner == self.Owner
					&& x.Actor.Info.Traits.Get<ProductionInfo>().Produces.Contains(bi.Queue))
					.ToList();
			var producer = producers.Where(x => x.Actor.IsPrimaryBuilding()).Concat(producers)
				.FirstOrDefault();

			if (producer.Actor == null)
				return;

			foreach (var nbp in producer.Actor.TraitsImplementing<INotifyBuildingPlaced>())
				nbp.BuildingPlaced(producer.Actor);
		}

		static int GetNumBuildables(Player p)
		{
			if (p != p.World.LocalPlayer) return 0;		// this only matters for local players.

			return p.World.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == p)
				.SelectMany(a => a.Trait.BuildableItems()).Distinct().Count();
		}
	}
}
