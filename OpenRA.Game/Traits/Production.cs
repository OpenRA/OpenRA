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

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	public class ProductionInfo : TraitInfo<Production>
	{
		public readonly int[] SpawnOffset = null;
		public readonly int[] ProductionOffset = null;
		public readonly int[] ExitOffset = null;
		public readonly string[] Produces = { };
	}

	public class Production : IIssueOrder, IResolveOrder, ITags, IProvideCursor
	{	
		public virtual int2? CreationLocation( Actor self, ActorInfo producee )
		{
			var pos = Util.CellContaining(self.CenterLocation);
			var pi = self.Info.Traits.Get<ProductionInfo>();
			if (pi.ProductionOffset != null)
				pos += pi.ProductionOffset.AsInt2();
			return pos;
		}

		public virtual int2? ExitLocation(Actor self, ActorInfo producee)
		{
			var pos = Util.CellContaining(self.CenterLocation);
			var pi = self.Info.Traits.Get<ProductionInfo>();
			if (pi.ExitOffset != null)
				pos += pi.ExitOffset.AsInt2();
			return pos;
		}
		
		public virtual int CreationFacing( Actor self, Actor newUnit )
		{
			return newUnit.Info.Traits.GetOrDefault<UnitInfo>().InitialFacing;
		}

		public virtual bool Produce( Actor self, ActorInfo producee )
		{
			var location = CreationLocation( self, producee );
			if( location == null || self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt( location.Value ).Any() )
				return false;

			var newUnit = self.World.CreateActor( producee.Name, location.Value, self.Owner );
			newUnit.traits.Get<Unit>().Facing = CreationFacing( self, newUnit ); ;

			var pi = self.Info.Traits.Get<ProductionInfo>();
			var rp = self.traits.GetOrDefault<RallyPoint>();
			if (rp != null || pi.ExitOffset != null)
			{
				var mobile = newUnit.traits.GetOrDefault<Mobile>();
				if (mobile != null)
				{
					if (pi.ExitOffset != null)
						newUnit.QueueActivity(new Activities.Move(ExitLocation(self, producee).Value, 1));

					if (rp != null)
						newUnit.QueueActivity(new Activities.Move(rp.rallyPoint, 1));
				}
			}

			if (pi != null && pi.SpawnOffset != null)
				newUnit.CenterLocation = self.CenterLocation + pi.SpawnOffset.AsInt2();

			foreach (var t in self.traits.WithInterface<INotifyProduction>())
				t.UnitProduced(self, newUnit);

			Log.Write("debug", "{0} #{1} produced by {2} #{3}", newUnit.Info.Name, newUnit.ActorID, self.Info.Name, self.ActorID);

			return true;
		}

		// "primary building" crap - perhaps this should be split?

		bool isPrimary = false;
		public bool IsPrimary { get { return isPrimary; } }

		public IEnumerable<TagType> GetTags()
		{
			yield return (isPrimary) ? TagType.Primary : TagType.None;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Right && underCursor == self)
				return new Order("Deploy", self);
			return null;
		}
		
		public string CursorForOrderString(string s, Actor a, int2 location)
		{
			return (s == "Deploy") ? "deploy" : null;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Deploy")
				SetPrimaryProducer(self, !isPrimary);
		}
		
		public void SetPrimaryProducer(Actor self, bool state)
		{
			if (state == false)
			{
				isPrimary = false;
				return;
			}
			
			// Cancel existing primaries
			foreach (var p in self.Info.Traits.Get<ProductionInfo>().Produces)
			{
				foreach (var b in self.World.Queries.OwnedBy[self.Owner]
					.WithTrait<Production>()
					.Where(x => x.Trait.IsPrimary
						&& (x.Actor.Info.Traits.Get<ProductionInfo>().Produces.Contains(p))))
				{
					b.Trait.SetPrimaryProducer(b.Actor, false);
				}
			}
			isPrimary = true;
			
			var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			Sound.PlayToPlayer(self.Owner,eva.PrimaryBuildingSelected);
		}
	}
}
