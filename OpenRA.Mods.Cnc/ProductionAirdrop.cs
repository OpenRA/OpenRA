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
using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.Cnc
{
	public class ProductionAirdropInfo : ProductionInfo
	{
		public override object Create(ActorInitializer init) { return new ProductionAirdrop(this); }
	}
	
	class ProductionAirdrop : Production
	{
		public ProductionAirdrop(ProductionAirdropInfo info) : base(info) {}

		public override bool Produce( Actor self, ActorInfo producee )
		{
			var owner = self.Owner;
			
			// Start and end beyond the edge of the map, to give a finite delay, and ability to land when AFLD is on map edge
			var startPos = new int2(owner.World.Map.XOffset + owner.World.Map.Width+5, self.Location.Y);
			var endPos = new int2(owner.World.Map.XOffset-5, self.Location.Y);		
			
			// Assume a single exit point for simplicity
			var exit = self.Info.Traits.WithInterface<ExitInfo>().First();
			
			var rb = self.Trait<RenderBuilding>();
			rb.PlayCustomAnimRepeating(self, "active");
			owner.World.AddFrameEndTask(w =>
			{
				var a = w.CreateActor("C17", new TypeDictionary 
				{
					new LocationInit( startPos ),
					new OwnerInit( owner ),
					new FacingInit( 64 ),
					new AltitudeInit( Rules.Info["c17"].Traits.Get<PlaneInfo>().CruiseAltitude ),
				});
				
				a.QueueActivity(new Fly(self.Location + new int2(6,0)));
				a.QueueActivity(new Land(Target.FromActor(self)));
				a.QueueActivity(new CallFunc(() => 
				{
					if (!self.IsInWorld || self.IsDead())
						return;
					
					rb.PlayCustomAnimRepeating(self, "idle");
					self.World.AddFrameEndTask(ww => DoProduction(self, producee, exit));
				}));
				a.QueueActivity(new Fly(endPos));
				a.QueueActivity(new RemoveSelf());
			});

			return true;
		}
	}
}
