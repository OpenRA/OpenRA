#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Transform : Activity
	{
		public readonly string ToActor = null;
		public int2 Offset = new int2(0,0);
		public int Facing = 96;
		public string[] Sounds = {};
		public int ForceHealthPercentage = 0;

		public Transform(Actor self, string toActor)
		{
			this.ToActor = toActor;
		}

		public override Activity Tick( Actor self )
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
				if (health != null)
				{
					// TODO: Fix bogus health init
					if (ForceHealthPercentage > 0)
						init.Add( new HealthInit( ForceHealthPercentage * 1f / 100 ));
					else
						init.Add( new HealthInit( (float)health.HP / health.MaxHP ));
				}

				var cargo = self.TraitOrDefault<Cargo>();
				if (cargo != null)
					init.Add( new CargoInit( cargo.Passengers.ToArray() ) );

				var a = w.CreateActor( ToActor, init );

				if (selected)
					w.Selection.Add(w, a);
			});

			return this;
		}
	}
}
