#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Allows to execute build orders.", " Attach this to the player actor.")]
	class PlaceBuildingInfo : TraitInfo<PlaceBuilding> { }

	class PlaceBuilding : IResolveOrder
	{
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PlaceBuilding" || order.OrderString == "LineBuild")
			{
				self.World.AddFrameEndTask(w =>
				{
					var prevItems = GetNumBuildables(self.Owner);

					if (order.TargetActor.IsDead)
						return;

					var unit = self.World.Map.Rules.Actors[order.TargetString];
					var queue = order.TargetActor.TraitsImplementing<ProductionQueue>()
						.FirstOrDefault(q => q.CanBuild(unit) && q.CurrentItem() != null && q.CurrentItem().Item == order.TargetString && q.CurrentItem().RemainingTime == 0);

					if (queue == null)
						return;

					var producer = queue.MostLikelyProducer();
					var race = producer.Trait != null ? producer.Trait.Race : self.Owner.Country.Race;
					var buildingInfo = unit.Traits.Get<BuildingInfo>();

					if (order.OrderString == "LineBuild")
					{
						var playSounds = true;
						foreach (var t in BuildingUtils.GetLineBuildCells(w, order.TargetLocation, order.TargetString, buildingInfo))
						{
							var building = w.CreateActor(order.TargetString, new TypeDictionary
							{
								new LocationInit(t),
								new OwnerInit(order.Player),
								new RaceInit(race)
							});

							if (playSounds)
								foreach (var s in buildingInfo.BuildSounds)
									Sound.PlayToPlayer(order.Player, s, building.CenterPosition);

							playSounds = false;
						}
					}
					else
					{
						if (!self.World.CanPlaceBuilding(order.TargetString, buildingInfo, order.TargetLocation, null)
							|| !buildingInfo.IsCloseEnoughToBase(self.World, order.Player, order.TargetString, order.TargetLocation))
							return;

						var building = w.CreateActor(order.TargetString, new TypeDictionary
						{
							new LocationInit(order.TargetLocation),
							new OwnerInit(order.Player),
							new RaceInit(race),
						});

						foreach (var s in buildingInfo.BuildSounds)
							Sound.PlayToPlayer(order.Player, s, building.CenterPosition);
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
							provider.Trait<BaseProvider>().BeginCooldown();
					}

					if (GetNumBuildables(self.Owner) > prevItems)
						w.Add(new DelayedAction(10,
							() => Sound.PlayNotification(self.World.Map.Rules, order.Player, "Speech", "NewOptions", order.Player.Country.Race)));
				});
			}
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
