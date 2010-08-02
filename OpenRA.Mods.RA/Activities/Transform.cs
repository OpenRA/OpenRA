#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using OpenRA.Traits;
using OpenRA.Mods.RA.Render;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA.Activities
{
	class Transform : IActivity
	{
		string actor = null;
		int2 offset;
		string[] sounds = null;		
		bool isCanceled;
		int facing;
		
		RenderBuilding rb;
		public Transform(Actor self, string toActor, int2 offset, int facing, string[] sounds)
		{
			this.actor = toActor;
			this.offset = offset;
			this.sounds = sounds;
			this.facing = facing;
			rb = self.traits.GetOrDefault<RenderBuilding>();
		}
		
		public IActivity NextActivity { get; set; }

		
		void DoTransform(Actor self)
		{
			// Hack: repeat the first frame of the make anim instead
			// of flashing the full structure for a frame
			if (rb != null)
				rb.PlayCustomAnim(self, "make");
			
			self.World.AddFrameEndTask(w =>
			{
				var selected = w.Selection.Contains(self);
				
				self.World.Remove(self);
				foreach (var s in sounds)
					Sound.PlayToPlayer(self.Owner, s, self.CenterLocation);
								
				var init = new TypeDictionary
				{
					new LocationInit( self.Location + offset ),
					new OwnerInit( self.Owner ),
					new FacingInit( facing ),
				};
				if (self.traits.Contains<Health>())
					init.Add( new HealthInit( self.traits.Get<Health>().HPFraction ));
				
				var a = w.CreateActor( actor, init );
				
				if (selected)
					w.Selection.Add(w, a);
			});
		}
		
		bool started = false;
		public IActivity Tick( Actor self )
		{
			if (isCanceled) return NextActivity;
			if (started) return this;
			
			if (rb == null)
				DoTransform(self);
			else
			{
				rb.PlayCustomAnimBackwards(self, "make", () => DoTransform(self));
				
				foreach (var s in self.Info.Traits.Get<BuildingInfo>().SellSounds)
					Sound.PlayToPlayer(self.Owner, s, self.CenterLocation);
				
				started = true;	
			}
			return this;
		}
		
		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
