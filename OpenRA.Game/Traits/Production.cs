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
using OpenRA.GameRules;
using System.Drawing;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class ProductionInfo : ITraitInfo
	{
		public readonly float[] SpawnOffsets; // in px relative to CenterLocation
		public readonly int[] ExitCells; // in cells relative to TopLeft, supports a list for multiple exits
		public readonly bool EnablePrimary = true;
		public readonly string[] Produces = { };
		
		public virtual object Create(ActorInitializer init) { return new Production(this); }
	}

	public class Production : IIssueOrder, IResolveOrder, ITags, IOrderCursor
	{	
		public readonly Dictionary<float2, int2> Spawns = new Dictionary<float2, int2>();
		public Production(ProductionInfo info)
		{
			if (info.SpawnOffsets == null || info.ExitCells == null)
				return;
			
			if (info.SpawnOffsets.Length != info.ExitCells.Length)
				throw new System.InvalidOperationException("SpawnOffset, ExitCells length mismatch");
			
			for (int i = 0; i < info.ExitCells.Length; i+=2)
				Spawns.Add(new float2(info.SpawnOffsets[i],info.SpawnOffsets[i+1]), new int2(info.ExitCells[i], info.ExitCells[i+1]));
		}
		
		public void DoProduction(Actor self, Actor newUnit, int2 exit, float2 spawn)
		{
			Game.Debug("Creating actor {0}".F(newUnit.Info.Name));
			
			var mobile = newUnit.traits.Get<Mobile>();
			
			// Unit can be built; add to the world
			self.World.Add(newUnit);
			Game.Debug("Added to world");

			// Set the physical position of the unit as the exit cell
			mobile.SetPosition(newUnit,exit);
			var to = Util.CenterOfCell(exit);

			// Animate the spawn -> exit transition
			newUnit.CenterLocation = spawn;
			mobile.Facing = Util.GetFacing(to - spawn, mobile.Facing);
			var speed = mobile.MovementSpeedForCell(self, exit);
			var length = speed > 0 ? (int)( ( to - spawn ).Length*3 / speed ) : 0;
			newUnit.QueueActivity(new Activities.Drag(spawn, to, length));
			
			// For the target line
			var target = exit;
			var rp = self.traits.GetOrDefault<RallyPoint>();
			if (rp != null)
			{
				target = rp.rallyPoint;
				newUnit.QueueActivity(new Activities.Move(target, 1));
			}
			
			if (newUnit.Owner == self.World.LocalPlayer)
			{
				self.World.AddFrameEndTask(w =>
				{
					var line = newUnit.traits.GetOrDefault<DrawLineToTarget>();
					if (line != null)
						line.SetTargetSilently(newUnit, Target.FromCell(target), Color.Green);
				});
			}

			foreach (var t in self.traits.WithInterface<INotifyProduction>())
				t.UnitProduced(self, newUnit);

			Log.Write("debug", "{0} #{1} produced by {2} #{3}", newUnit.Info.Name, newUnit.ActorID, self.Info.Name, self.ActorID);
		}
		
		public virtual bool Produce( Actor self, ActorInfo producee )
		{			
			var newUnit = self.World.CreateActor(false, producee.Name, new TypeDictionary
			{
				new OwnerInit( self.Owner ),
			});
			
			// Todo: remove assumption on Mobile
			var mobile = newUnit.traits.Get<Mobile>();
	
			// Pick an exit that we can move to
			var exit = int2.Zero;
			var spawn = float2.Zero;
			var success = false;
			
			// Pick a spawn/exit point
			// Todo: Reorder in a synced random way
			foreach (var s in Spawns)
			{
				exit = self.Location + s.Value;
				spawn = self.CenterLocation + s.Key;
				if (mobile.CanEnterCell(exit,self,true))
				{
					success = true;
					break;
				}
			}
			
			if (!success)
			{
				// Hack around mobile being a tard; remove from UIM (we shouldn't be there in the first place)
				newUnit.traits.Get<Mobile>().RemoveInfluence();
				return false;
			}

			DoProduction(self, newUnit, exit, spawn);
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
			if (mi.Button == MouseButton.Right && underCursor == self && self.Info.Traits.Get<ProductionInfo>().EnablePrimary)
				return new Order("PrimaryProducer", self);
			return null;
		}
		
		public string CursorForOrder(Actor self, Order order)
		{
			return (order.OrderString == "PrimaryProducer") ? "deploy" : null;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PrimaryProducer")
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
