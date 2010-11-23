#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class HarvesterInfo : ITraitInfo
	{
		public readonly int Capacity = 28;
		public readonly int PipCount = 7;
		public readonly PipType PipColor = PipType.Yellow;
		public readonly string[] Resources = { };
		public readonly decimal FullyLoadedSpeed = .85m;

		public object Create(ActorInitializer init) { return new Harvester(init.self, this); }
	}

	public class Harvester : IIssueOrder, IResolveOrder, INotifyDamage, IPips, 
		IRenderModifier, IExplodeModifier, IOrderVoice,
		ISpeedModifier
	{
		Dictionary<ResourceTypeInfo, int> contents = new Dictionary<ResourceTypeInfo, int>();
		
		[Sync]
		public bool Visible = true;
		
		[Sync]
		public Actor LinkedProc = null;
		
		[Sync]
		public int2 LastHarvestedCell = int2.Zero;
		
		readonly HarvesterInfo Info;
		public Harvester(Actor self, HarvesterInfo info)
		{
			Info = info;
			self.QueueActivity( new CallFunc( () => ChooseNewProc(self, null)));
		}
		
		public void ChooseNewProc(Actor self, Actor ignore)
		{
			LinkedProc = ClosestProc(self, ignore);
			if (LinkedProc != null)
				LinkedProc.TraitsImplementing<IAcceptOre>().FirstOrDefault().LinkHarvester(LinkedProc,self);
		}
		
		Actor ClosestProc(Actor self, Actor ignore)
		{
			var refs = self.World.Queries.OwnedBy[self.Owner]
				.Where(x => x != ignore && x.HasTrait<IAcceptOre>())
				.ToList();
			var mi = self.Info.Traits.Get<MobileInfo>();
			var path = self.World.WorldActor.Trait<PathFinder>().FindPath(PathSearch.FromPoints(self.World, mi,
			                                                                refs.Select(r => r.Location + r.Trait<IAcceptOre>().DeliverOffset),
			                                                                self.Location,
			                                                                false));
			path.Reverse();
			if (path.Count != 0)
				return refs.FirstOrDefault(x => x.Location + x.Trait<IAcceptOre>().DeliverOffset == path[0]);
			else
				return null;
		}

		public bool IsFull { get { return contents.Values.Sum() == Info.Capacity; } }
		public bool IsEmpty { get { return contents.Values.Sum() == 0; } }
		
		public void AcceptResource(ResourceType type)
		{
			if (!contents.ContainsKey(type.info)) contents[type.info] = 1;
			else contents[type.info]++;
		}

		public void Deliver(Actor self, Actor proc)
		{
			if (!proc.IsInWorld)
				return;	// fail to deliver if there is no proc.

			proc.Trait<IAcceptOre>().GiveOre(contents.Sum(kv => kv.Key.ValuePerUnit * kv.Value));
			contents.Clear();
		}
		
		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterOrderTargeter<IAcceptOre>( "Deliver", 5, false, true, _ => true, _ => !IsEmpty );
				yield return new HarvestOrderTargeter();
			}
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "Deliver" )
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			if( order.OrderID == "Harvest" )
				return new Order( order.OrderID, self, Util.CellContaining( target.CenterLocation ), queued );

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Harvest" || (order.OrderString == "Deliver" && !IsEmpty)) ? "Move" : null;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Harvest")
			{
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						if (self.Destroyed) return;
						w.Add(new MoveFlash(self.World, order.TargetLocation));
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, Target.FromOrder(order), Color.Red);
					});
				
				var mobile = self.Trait<Mobile>();
				self.CancelActivity();
				self.QueueActivity(mobile.MoveTo(order.TargetLocation, 0));
				self.QueueActivity(new Harvest());
			}
			else if (order.OrderString == "Deliver")
			{
				if (order.TargetActor != LinkedProc)
				{
					if (LinkedProc != null && LinkedProc.IsInWorld)
						LinkedProc.TraitsImplementing<IAcceptOre>().FirstOrDefault().UnlinkHarvester(LinkedProc,self);
					LinkedProc = order.TargetActor;
					LinkedProc.TraitsImplementing<IAcceptOre>().FirstOrDefault().LinkHarvester(LinkedProc,self);
				}
				
				if (IsEmpty)
					return;
				
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						if (self.Destroyed) return;
						w.Add(new FlashTarget(order.TargetActor));
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, Target.FromOrder(order), Color.Green);
					});
				
				self.CancelActivity();
				self.QueueActivity(new DeliverResources());
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				if (LinkedProc != null && !LinkedProc.Destroyed)
					LinkedProc.TraitsImplementing<IAcceptOre>().FirstOrDefault().UnlinkHarvester(LinkedProc,self);
		}
		
		public void LinkProc(Actor self, Actor proc)
		{
			LinkedProc = proc;
		}
		
		public void UnlinkProc(Actor self, Actor proc)
		{
			if (LinkedProc != proc)
				return;

			ChooseNewProc(self, proc);
		}
		
		public IEnumerable<PipType> GetPips(Actor self)
		{
			int numPips = Info.PipCount;
			int n = contents.Values.Sum();

			for (int i = 0; i < numPips; i++)
			{
				if (n * 1.0f / Info.Capacity > i * 1.0f / numPips)
					yield return Info.PipColor;
				else
					yield return PipType.Transparent;
			}
		}
				
		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			return Visible ? r : new Renderable[] { };
		}

		public bool ShouldExplode(Actor self) { return !IsEmpty; }

		public decimal GetSpeedModifier()
		{
			return 1m - ( 1m - Info.FullyLoadedSpeed ) * contents.Values.Sum() / Info.Capacity;
		}

		class HarvestOrderTargeter : IOrderTargeter
		{
			public string OrderID { get { return "Harvest";}}
			public int OrderPriority { get { return 10; } }

			public bool CanTargetUnit(Actor self, Actor target, bool forceAttack, bool forceMove, bool forceQueued, ref string cursor)
			{
				return false;
			}

			public bool CanTargetLocation(Actor self, int2 location, List<Actor> actorsAtLocation, bool forceAttack, bool forceMove, bool forceQueued, ref string cursor)
			{
				// Don't leak info about resources under the shroud
				if (!self.World.LocalPlayer.Shroud.IsExplored(location)) return false;

				var res = self.World.WorldActor.Trait<ResourceLayer>().GetResource( location );
				var info = self.Info.Traits.Get<HarvesterInfo>();

				if( res == null ) return false;
				if( !info.Resources.Contains( res.info.Name ) ) return false;
				cursor = "attackmove";
				IsQueued = forceQueued;

				return true;
			}
			public bool IsQueued { get; protected set; }
		}
	}
}
