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
		public CVec Offset = new CVec(0, 0);
		public int Facing = 96;
		public string[] Sounds = {};
		public int ForceHealthPercentage = 0;
		public bool SkipMakeAnims = false;

		public Transform(Actor self, string toActor)
		{
			this.ToActor = toActor;
		}

		public override Activity Tick( Actor self )
		{
			if (IsCanceled) return NextActivity;

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead())
					return;

				foreach (var nt in self.TraitsImplementing<INotifyTransform>())
					nt.OnTransform(self);

				var selected = w.Selection.Contains(self);

				self.Destroy();
				foreach (var s in Sounds)
					Sound.PlayToPlayer(self.Owner, s, self.CenterPosition);

				var init = new TypeDictionary
				{
					new LocationInit( self.Location + Offset ),
					new OwnerInit( self.Owner ),
					new FacingInit( Facing ),
				};

				if (SkipMakeAnims) init.Add(new SkipMakeAnimsInit());

				var health = self.TraitOrDefault<Health>();
				if (health != null)
				{
					var newHP = (ForceHealthPercentage > 0)
						? ForceHealthPercentage / 100f
						: (float)health.HP / health.MaxHP;

					init.Add( new HealthInit(newHP) );
				}

				var cargo = self.TraitOrDefault<Cargo>();
				if (cargo != null)
					init.Add( new RuntimeCargoInit( cargo.Passengers.ToArray() ) );

				var a = w.CreateActor( ToActor, init );

				foreach (var nt in self.TraitsImplementing<INotifyTransformed>())
					nt.OnTransformed(a);

				if (selected)
					w.Selection.Add(w, a);
			});

			return this;
		}
	}
}
