#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
		public int InitialDelay = 10; // Ticks
		public int CloakDelay = 30; // Ticks
		public string CloakSound = "subshow1.aud";
		public string UncloakSound = "subshow1.aud";

		public object Create(ActorInitializer init) { return new Cloak(init.self, this); }
	}

	public class Cloak : IRenderModifier, INotifyDamageStateChanged, INotifyAttack, ITick, IVisibilityModifier, IRadarColorModifier, ISync
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

			remainingTime = info.InitialDelay;
		}

		void DoUncloak()
		{
			if (remainingTime <= 0)
				OnCloak();

			remainingTime = Math.Max(remainingTime, info.CloakDelay);
		}

		public void Attacking(Actor self, Target target) { DoUncloak(); }
		public void DamageStateChanged(Actor self, AttackInfo e)
		{			
			canCloak = (e.DamageState < DamageState.Critical);
			if (Cloaked && !canCloak)
				DoUncloak();
		}

		static readonly Renderable[] Nothing = { };
		public IEnumerable<Renderable>
			ModifyRender(Actor self, IEnumerable<Renderable> rs)
		{
			if (remainingTime > 0)
				return rs;

			if (Cloaked && IsVisible(self))
				return rs.Select(a => a.WithPalette("shadow"));
			else
				return Nothing;
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
			if (!Cloaked || self.Owner == self.World.LocalPlayer || 
				self.World.LocalPlayer == null || 
				self.Owner.Stances[self.World.LocalPlayer] == Stance.Ally)
				return true;

			return self.World.ActorsWithTrait<DetectCloaked>().Any(a =>
				a.Actor.Owner.Stances[self.Owner] != Stance.Ally &&
				(self.Location - a.Actor.Location).Length < a.Actor.Info.Traits.Get<DetectCloakedInfo>().Range);
		}
		
		public Color RadarColorOverride(Actor self)
		{
			var c = self.Owner.ColorRamp.GetColor(0);
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
