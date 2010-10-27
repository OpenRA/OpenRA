#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class PlaceBuildingInfo : TraitInfo<PlaceBuilding> {}

	class PlaceBuilding : IResolveOrder
	{
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PlaceBuilding" || order.OrderString == "LineBuild")
			{
				self.World.AddFrameEndTask(w =>
				{
					var prevItems = GetNumBuildables(self.Owner);
					
					// Find the queue with the target actor
					var queue = w.Queries.WithTraitMultiple<ProductionQueue>()
						.Where(p => p.Actor.Owner == self.Owner &&
						       		 p.Trait.CurrentItem() != null && 
					                 p.Trait.CurrentItem().Item == order.TargetString && 
					                 p.Trait.CurrentItem().RemainingTime == 0)
						.Select(p => p.Trait)
						.FirstOrDefault();
					
					if (queue == null)
						return;
					
					var unit = Rules.Info[order.TargetString];
					var buildingInfo = unit.Traits.Get<BuildingInfo>();

					if (order.OrderString == "LineBuild")
					{
						bool playSounds = true;
						foreach (var t in LineBuildUtils.GetLineBuildCells(w, order.TargetLocation, order.TargetString, buildingInfo))
						{
							var building = w.CreateActor(order.TargetString, new TypeDictionary
							{
								new LocationInit( t ),
								new OwnerInit( order.Player ),
							});
							                             
							if (playSounds)
								foreach (var s in buildingInfo.BuildSounds)
									Sound.PlayToPlayer(order.Player, s, building.CenterLocation);
							playSounds = false;
						}
					}
					else
					{
						if (!self.World.CanPlaceBuilding(order.TargetString, buildingInfo, order.TargetLocation, null))
						{
							return;
						}

						var building = w.CreateActor(order.TargetString, new TypeDictionary
						{
							new LocationInit( order.TargetLocation ),
							new OwnerInit( order.Player ),
						});
						foreach (var s in buildingInfo.BuildSounds)
							Sound.PlayToPlayer(order.Player, s, building.CenterLocation);
					}

					PlayBuildAnim( self, unit );

					queue.FinishProduction();

					if (GetNumBuildables(self.Owner) > prevItems)
						w.Add(new DelayedAction(10,
							() => Sound.PlayToPlayer(order.Player,
								w.WorldActor.Info.Traits.Get<EvaAlertsInfo>().NewOptions)));
				});
			}
		}

		// finds a construction yard (or equivalent) and runs its "build" animation.
		static void PlayBuildAnim( Actor self, ActorInfo unit )
		{
			var bi = unit.Traits.GetOrDefault<BuildableInfo>();
			if (bi == null)
				return;
			
			var producers = self.World.Queries.OwnedBy[ self.Owner ].WithTrait<Production>()
							   .Where( x => x.Actor.Info.Traits.Get<ProductionInfo>().Produces.Contains( bi.Queue ) )
							   .ToList();
			var producer = producers.Where( x => x.Actor.IsPrimaryBuilding() ).Concat( producers )
				.FirstOrDefault();

			if( producer.Actor != null )
				producer.Actor.TraitsImplementing<RenderSimple>().First().PlayCustomAnim( producer.Actor, "build" );
		}

		static int GetNumBuildables(Player p)
		{
			if (p != p.World.LocalPlayer) return 0;		// this only matters for local players.
			
			return p.World.Queries.WithTraitMultiple<ProductionQueue>()
				.Where(a => a.Actor.Owner == p)
				.SelectMany(a => a.Trait.BuildableItems()).Distinct().Count();
		}
	}
}
