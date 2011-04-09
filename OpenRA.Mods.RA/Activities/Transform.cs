#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	class Transform : CancelableActivity
	{
		public readonly string ToActor = null;
		public int2 Offset = new int2(0,0);
		public int Facing = 96;
		public string[] Sounds = {};	
		
		public Transform(Actor self, string toActor)
		{
			this.ToActor = toActor;
		}

		public override IActivity Tick( Actor self )
		{
			if (IsCanceled) return NextActivity;
			
			self.World.AddFrameEndTask(w =>
			{
				var selected = w.Selection.Contains(self);

				self.Destroy();
				foreach (var s in Sounds)
					Sound.PlayToPlayer(self.Owner, s, self.CenterLocation);

				var init = new TypeDictionary
				{
					new LocationInit( self.Location + Offset ),
					new OwnerInit( self.Owner ),
					new FacingInit( Facing ),
				};
				var health = self.TraitOrDefault<Health>();
				// TODO: fix potential desync from HPFraction
				if (health != null)
					init.Add( new HealthInit( health.HPFraction ));
				
				var a = w.CreateActor( ToActor, init );
				
				if (selected)
					w.Selection.Add(w, a);
			});
			
			return this;
		}
	}
}
