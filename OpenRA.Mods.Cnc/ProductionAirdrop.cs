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
using System.Drawing;
using OpenRA.GameRules;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.Cnc
{
	public class ProductionAirdropInfo : ProductionInfo
	{
		public override object Create(ActorInitializer init) { return new ProductionAirdrop(); }
	}
	
	class ProductionAirdrop : Production
	{
		public override bool Produce( Actor self, ActorInfo producee )
		{
			var owner = self.Owner;
			
			// Start and end beyond the edge of the map, to give a finite delay, and ability to land when AFLD is on map edge
			var startPos = new int2(owner.World.Map.XOffset + owner.World.Map.Width+15, self.Location.Y);
			var endPos = new int2(owner.World.Map.XOffset-15, self.Location.Y);
			var unloadOffset = new int2(1,1);
			var exitOffset = new int2(3,1);
			
			var rp = self.traits.GetOrDefault<RallyPoint>();
			owner.World.AddFrameEndTask(w =>
			{
				var a = w.CreateActor("C17", startPos, owner);
				var cargo = a.traits.Get<Cargo>();
				a.traits.Get<IFacing>().Facing = 64;
				a.traits.Get<IMove>().Altitude = a.Info.Traits.Get<PlaneInfo>().CruiseAltitude;

				var newUnit = new Actor(self.World, producee.Name, new int2(0, 0), self.Owner);
				cargo.Load(a, newUnit);
				
				a.CancelActivity();

				a.QueueActivity(new Land(Target.FromActor(self)));
				a.QueueActivity(new CallFunc(() => 
				{
					if (self.IsDead())
						return;
					
					var actor = cargo.Unload(self);
					self.World.AddFrameEndTask(ww =>
					{
						ww.Add(actor);
						actor.traits.Get<IMove>().SetPosition(actor, self.Location + unloadOffset);
						newUnit.traits.Get<IFacing>().Facing = 192;
						actor.CancelActivity();
						actor.QueueActivity(new Move(self.Location + exitOffset, self));
						actor.QueueActivity(new Move(rp.rallyPoint, 0));
						if (actor.Owner == self.World.LocalPlayer)
						{
							var line = actor.traits.GetOrDefault<DrawLineToTarget>();
							if (line != null)
								line.SetTargetSilently(actor, Target.FromCell(rp.rallyPoint), Color.Green);	
						}

						foreach (var t in self.traits.WithInterface<INotifyProduction>())
							t.UnitProduced(self, actor);
					});
				}));
				a.QueueActivity(new Fly(endPos));
				a.QueueActivity(new RemoveSelf());
			});

			return true;
		}
	}
}
