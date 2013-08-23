﻿#region Copyright & License Information
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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CloakInfo : ITraitInfo
	{
		public readonly int InitialDelay = 10; // Ticks
		public readonly int CloakDelay = 30; // Ticks
		public readonly bool UncloakOnMove = false;
		public readonly bool RequiresCrate = false;

		public readonly string CloakSound = "subshow1.aud";
		public readonly string UncloakSound = "subshow1.aud";
		public readonly string Palette = "cloak";

		public object Create(ActorInitializer init) { return new Cloak(init.self, this); }
	}

	public class Cloak : IRenderModifier, INotifyDamageStateChanged, INotifyAttack, ITick, IVisibilityModifier, IRadarColorModifier, ISync
	{
		[Sync] int remainingTime;
		[Sync] bool damageDisabled;
		[Sync] bool crateDisabled;

		Actor self;
		CloakInfo info;
		CPos? lastPos;

		public Cloak(Actor self, CloakInfo info)
		{
			this.info = info;
			this.self = self;

			remainingTime = info.InitialDelay;
			crateDisabled = info.RequiresCrate;
		}

		public void Uncloak() { Uncloak(info.CloakDelay); }

		public void Uncloak(int time)
		{
			if (Cloaked)
				Sound.Play(info.UncloakSound, self.CenterPosition);

			remainingTime = Math.Max(remainingTime, time);
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel) { Uncloak(); }

		public bool Cloaked { get { return remainingTime <= 0; } }

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			damageDisabled = e.DamageState >= DamageState.Critical;
			if (damageDisabled)
				Uncloak();
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (remainingTime > 0)
				return r;

			if (Cloaked && IsVisible(self, self.World.RenderPlayer))
				if (string.IsNullOrEmpty(info.Palette))
					return r;
				else
					return r.Select(a => a.WithPalette(wr.Palette(info.Palette)));
			else
				return SpriteRenderable.None;
		}

		public void Tick(Actor self)
		{
			if (remainingTime > 0 && !crateDisabled && !damageDisabled && --remainingTime <= 0)
			{
				self.Generation++;
				Sound.Play(info.CloakSound, self.CenterPosition);
			}

			if (self.IsDisabled())
				Uncloak();

			if (info.UncloakOnMove && (lastPos == null || lastPos.Value != self.Location))
			{
				Uncloak();
				lastPos = self.Location;
			}
		}
		
		public bool IsVisible(Actor self, Player viewer)
		{
			if (!Cloaked || self.Owner.IsAlliedWith(viewer))
				return true;

			var centerPosition = self.CenterPosition;
			return self.World.ActorsWithTrait<DetectCloaked>().Any(a => a.Actor.Owner.IsAlliedWith(viewer) &&
				(centerPosition - a.Actor.CenterPosition).Length < WRange.FromCells(a.Actor.Info.Traits.Get<DetectCloakedInfo>().Range).Range);
		}

		public Color RadarColorOverride(Actor self)
		{
			var c = self.Owner.Color.RGB;
			if (self.Owner == self.World.LocalPlayer && Cloaked)
				c = Color.FromArgb(128, c);
			return c;
		}

		public bool AcceptsCloakCrate { get { return info.RequiresCrate && crateDisabled; } }

		public void ReceivedCloakCrate(Actor self)
		{
			crateDisabled = false;
		}
	}
}
