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
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CloakInfo : ITraitInfo
	{
		public readonly float InitialDelay = .4f;	// seconds
		public readonly float CloakDelay = 1.2f; // Seconds
		public readonly string CloakSound = "subshow1.aud";
		public readonly string UncloakSound = "subshow1.aud";

		public CloakInfo() { }		/* only because we have other ctors */
		
		/* for CloakCrateAction */
		public CloakInfo(float initialDelay, float cloakDelay, string cloakSound, string uncloakSound)
		{
			InitialDelay = initialDelay;
			CloakDelay = cloakDelay;
			CloakSound = cloakSound;
			UncloakSound = uncloakSound;
		}

		public object Create(ActorInitializer init) { return new Cloak(init.self, this); }
	}

	public class Cloak : IRenderModifier, INotifyDamage, INotifyAttack, ITick, IVisibilityModifier, IRadarColorModifier
	{
		[Sync]
		int remainingTime;
		[Sync]
		bool canCloak = true;
		
		Actor self;
		CloakInfo info;
		public Cloak(Actor self, CloakInfo info)
		{
			this.info = info;
			this.self = self;

			remainingTime = (int)(info.InitialDelay * 25);
		}

		void DoUncloak()
		{
			if (remainingTime <= 0)
				OnCloak();

			remainingTime = Math.Max(remainingTime, (int)(info.CloakDelay * 25));
		}

		public void Attacking(Actor self, Target target) { DoUncloak(); }
		public void Damaged(Actor self, AttackInfo e)
		{			
			canCloak = (e.DamageState < DamageState.Critical);
			if (Cloaked && !canCloak)
				DoUncloak();
		}

		public IEnumerable<Renderable>
			ModifyRender(Actor self, IEnumerable<Renderable> rs)
		{
			if (remainingTime > 0)
				return rs;

			if (Cloaked && IsVisible(self))
				return rs.Select(a => a.WithPalette("shadow"));
			else
				return new Renderable[] { };
		}

		public void Tick(Actor self)
		{
			if (remainingTime > 0 && canCloak)
				if (--remainingTime <= 0)
					OnCloak();
		}

		void OnUncloak()
		{
			Sound.Play(info.UncloakSound, self.CenterLocation);
		}

		void OnCloak()
		{
			Sound.Play(info.CloakSound, self.CenterLocation);
		}

		public bool Cloaked { get { return remainingTime == 0; } }

		public bool IsVisible(Actor self)
		{
			return !Cloaked || self.Owner == self.World.LocalPlayer;
		}
		
		public Color RadarColorOverride(Actor self)
		{
			var c = self.Owner.Color;
			if (self.Owner == self.World.LocalPlayer && Cloaked)
				c = Color.FromArgb(128, c);
			return c;
		}
		
		public void Decloak(int time)
		{
			DoUncloak();
			remainingTime = Math.Max(remainingTime, time);
		}
	}
}
