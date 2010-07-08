#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;

namespace OpenRA.Traits
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

					var queue = self.traits.Get<ProductionQueue>();
					var unit = Rules.Info[order.TargetString];
					var producing = queue.CurrentItem(unit.Category);

					if (producing == null || producing.Item != order.TargetString || producing.RemainingTime != 0)
						return;

					var buildingInfo = unit.Traits.Get<BuildingInfo>();

					if (order.OrderString == "LineBuild")
					{
						bool playSounds = true;
						foreach (var t in LineBuildUtils.GetLineBuildCells(w, order.TargetLocation, order.TargetString, buildingInfo))
						{
							var building = w.CreateActor(order.TargetString, t, order.Player);
							if (playSounds)
								foreach (var s in buildingInfo.BuildSounds)
									Sound.PlayToPlayer(order.Player, s, building.CenterLocation);
							playSounds = false;
						}
					}
					else
					{
						var building = w.CreateActor(order.TargetString, order.TargetLocation, order.Player);
						foreach (var s in buildingInfo.BuildSounds)
							Sound.PlayToPlayer(order.Player, s, building.CenterLocation);
					}

					PlayBuildAnim( self, unit );

					queue.FinishProduction(unit.Category);

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
			var producers = self.World.Queries.OwnedBy[ self.Owner ].WithTrait<Production>()
							   .Where( x => x.Actor.Info.Traits.Get<ProductionInfo>().Produces.Contains( unit.Category ) )
							   .ToList();
			var producer = producers.Where( x => x.Trait.IsPrimary ).Concat( producers )
				.FirstOrDefault();

			if( producer.Actor != null )
				producer.Actor.traits.WithInterface<RenderSimple>().First().PlayCustomAnim( producer.Actor, "build" );
		}

		static int GetNumBuildables(Player p)
		{
			if (p != p.World.LocalPlayer) return 0;		// this only matters for local players.
			return Rules.TechTree.BuildableItems(p, Rules.Categories().ToArray()).Count();
		}
	}
}
