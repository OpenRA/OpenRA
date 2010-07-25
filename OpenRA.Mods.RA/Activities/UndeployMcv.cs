#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class UndeployMcv : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool started;

		void DoUndeploy(World w, Actor self)
		{
			var selected = Game.controller.selection.Contains(self);
			w.Remove(self);
			
			var mcv = w.CreateActor("mcv", self.Location + new int2(1, 1), self.Owner);
			mcv.Health = TransformIntoActor.GetHealthToTransfer(self, mcv, true);
			mcv.traits.Get<Unit>().Facing = 96;
			
			if (selected)
				Game.controller.selection.Add(w, mcv);
		}

		public IActivity Tick(Actor self)
		{
			if (!started)
			{
				var rb = self.traits.Get<RenderBuilding>();
				rb.PlayCustomAnimBackwards(self, "make",
					() => self.World.AddFrameEndTask(w => DoUndeploy(w,self)));
				
				foreach (var s in self.Info.Traits.Get<BuildingInfo>().SellSounds)
					Sound.PlayToPlayer(self.Owner, s, self.CenterLocation);
				
				started = true;
			}

			return this;
		}

		public void Cancel(Actor self) {}
	}
}
