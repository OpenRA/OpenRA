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
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	class CloakInfo : ITraitInfo
	{
		public readonly float InitialDelay = .4f;	// seconds
		public readonly float CloakDelay = 1.2f; // Seconds
		public readonly string CloakSound = "subshow1.aud";
		public readonly string UncloakSound = "subshow1.aud";

		public object Create(ActorInitializer init) { return new Cloak(init.self); }
	}

	public class Cloak : IRenderModifier, INotifyAttack, ITick, INotifyDamage
	{
		[Sync]
		int remainingTime;
		
		Actor self;
		public Cloak(Actor self)
		{
			remainingTime = (int)(self.Info.Traits.Get<CloakInfo>().InitialDelay * 25);
			this.self = self;
		}

		void DoSurface()
		{
			if (remainingTime <= 0)
				OnSurface();

			remainingTime = Math.Max(remainingTime, (int)(self.Info.Traits.Get<CloakInfo>().CloakDelay * 25));
		}

		public void Attacking(Actor self) { DoSurface(); }
		public void Damaged(Actor self, AttackInfo e) { DoSurface(); }

		public IEnumerable<Renderable>
			ModifyRender(Actor self, IEnumerable<Renderable> rs)
		{
			if (remainingTime > 0)
				return rs;

			if (self.Owner == self.World.LocalPlayer)
				return rs.Select(a => a.WithPalette("shadow"));
			else
				return new Renderable[] { };
		}

		public void Tick(Actor self)
		{
			if (remainingTime > 0)
				if (--remainingTime <= 0)
					OnDive();
		}

		void OnSurface()
		{
			Sound.Play(self.Info.Traits.Get<CloakInfo>().UncloakSound, self.CenterLocation);
		}

		void OnDive()
		{
			Sound.Play(self.Info.Traits.Get<CloakInfo>().CloakSound, self.CenterLocation);
		}

		public bool Cloaked { get { return remainingTime > 0; } }

		public void Decloak(int time)
		{
			DoSurface();
			remainingTime = Math.Max(remainingTime, time);
		}
	}
}
