using System.Collections.Generic;
using System.Linq;
using OpenRa.GameRules;

namespace OpenRa.Traits
{
	class ProductionInfo : ITraitInfo
	{
		public readonly int[] SpawnOffset = null;
		public readonly string[] Produces = { };

		public virtual object Create(Actor self) { return new Production(self); }
	}

	class Production : IIssueOrder, IResolveOrder, IProducer, ITags
	{
		bool isPrimary = false;
		public bool IsPrimary { get { return isPrimary; } }
		
		public Production( Actor self ) { }

		public virtual int2? CreationLocation( Actor self, ActorInfo producee )
		{
			return ( 1 / 24f * self.CenterLocation ).ToInt2();
		}

		public virtual int CreationFacing( Actor self, Actor newUnit )
		{
			return newUnit.Info.Traits.GetOrDefault<UnitInfo>().InitialFacing;
		}

		public bool Produce( Actor self, ActorInfo producee )
		{
			var location = CreationLocation( self, producee );
			if( location == null || self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt( location.Value ).Any() )
				return false;

			var newUnit = self.World.CreateActor( producee.Name, location.Value, self.Owner );
			newUnit.traits.Get<Unit>().Facing = CreationFacing( self, newUnit ); ;

			var rp = self.traits.GetOrDefault<RallyPoint>();
			if( rp != null )
			{
				var mobile = newUnit.traits.GetOrDefault<Mobile>();
				if( mobile != null )
					newUnit.QueueActivity( new Activities.Move( rp.rallyPoint, 1 ) );
			}

			var pi = self.Info.Traits.Get<ProductionInfo>();
			if (pi != null && pi.SpawnOffset != null)
				newUnit.CenterLocation = self.CenterLocation 
					+ new float2(pi.SpawnOffset[0], pi.SpawnOffset[1]);

			foreach (var t in self.traits.WithInterface<INotifyProduction>())
				t.UnitProduced(self, newUnit);

			return true;
		}

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

			Sound.PlayToPlayer(self.Owner, "pribldg1.aud");
		}
	}
}
