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
using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	public class HarvesterInfo : ITraitInfo
	{
		public readonly int Capacity = 28;
		public readonly int PipCount = 7;
		public readonly PipType PipColor = PipType.Yellow;
		public readonly string[] Resources = { };

		public object Create(ActorInitializer init) { return new Harvester(init.self, this); }
	}

	public class Harvester : IIssueOrder, IResolveOrder, INotifyDamage, IPips, IRenderModifier, IExplodeModifier, IOrderCursor, IOrderVoice
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
		
		void ChooseNewProc(Actor self, Actor ignore)
		{
			LinkedProc = ClosestProc(self, ignore);
			if (LinkedProc != null)
				LinkedProc.traits.WithInterface<IAcceptOre>().FirstOrDefault().LinkHarvester(LinkedProc,self);
		}
		
		Actor ClosestProc(Actor self, Actor ignore)
		{
			var refs = self.World.Queries.OwnedBy[self.Owner]
				.Where(x => x != ignore && x.traits.Contains<IAcceptOre>())
				.ToList();
			
			var path = self.World.PathFinder.FindPath(PathSearch.FromPoints(self,
			                                                                refs.Select(r => r.Location + r.traits.Get<IAcceptOre>().DeliverOffset),
			                                                                self.Location,
			                                                                false));
			path.Reverse();
			if (path.Count != 0)
				return refs.FirstOrDefault(x => x.Location + x.traits.Get<IAcceptOre>().DeliverOffset == path[0]);
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
			proc.traits.Get<IAcceptOre>().GiveOre(contents.Sum(kv => kv.Key.ValuePerUnit * kv.Value));
			contents.Clear();
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;

			if (underCursor != null
				&& underCursor.Owner == self.Owner
				&& underCursor.traits.Contains<IAcceptOre>())
			{
				return new Order("Deliver", self, underCursor);
			}
			var res = self.World.WorldActor.traits.Get<ResourceLayer>().GetResource(xy);
			var info = self.Info.Traits.Get<HarvesterInfo>();

			if (underCursor == null && res != null && info.Resources.Contains(res.info.Name) && !IsFull)
				return new Order("Harvest", self, xy);

			return null;
		}
		
		public string CursorForOrder(Actor self, Order order)
		{
			if (order.OrderString == "Harvest") return "attackmove";
			if (order.OrderString == "Deliver") return IsEmpty ? "enter-blocked" : "enter";
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
					self.World.AddFrameEndTask(w => w.Add(new MoveFlash(self.World, order.TargetLocation)));
				
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetLocation, 0));
				self.QueueActivity(new Harvest());
			}
			else if (order.OrderString == "Deliver")
			{
				if (order.TargetActor != LinkedProc)
				{
					if (LinkedProc != null)
						LinkedProc.traits.WithInterface<IAcceptOre>().FirstOrDefault().UnlinkHarvester(LinkedProc,self);
					LinkedProc = order.TargetActor;
					LinkedProc.traits.WithInterface<IAcceptOre>().FirstOrDefault().LinkHarvester(LinkedProc,self);
				}
				
				if (IsEmpty)
					return;
				
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w => w.Add(new FlashTarget(order.TargetActor)));
				
				self.CancelActivity();
				self.QueueActivity(new DeliverResources());
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.IsDead)
				if (LinkedProc != null)
					LinkedProc.traits.WithInterface<IAcceptOre>().FirstOrDefault().UnlinkHarvester(LinkedProc,self);
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
	}
}
