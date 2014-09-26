#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
	[Desc("This unit can cloak and uncloak in specific situations.")]
	public class CloakInfo : ITraitInfo
	{
		[Desc("Measured in game ticks.")]
		public readonly int InitialDelay = 10;

		[Desc("Measured in game ticks.")]
		public readonly int CloakDelay = 30;

		public readonly bool UncloakOnAttack = true;
		public readonly bool UncloakOnMove = false;
		public readonly bool UncloakOnUnload = true;

		[Desc("Enable only if this upgrade is enabled.")]
		public readonly string RequiresUpgrade = null;

		public readonly string CloakSound = null;
		public readonly string UncloakSound = null;
		public readonly string Palette = "cloak";

		public readonly string[] CloakTypes = { "Cloak" };

		public object Create(ActorInitializer init) { return new Cloak(init.self, this); }
	}

	public class Cloak : IUpgradable, IRenderModifier, INotifyDamageStateChanged, INotifyAttack, ITick, IVisibilityModifier, IRadarColorModifier, ISync
	{
		[Sync] int remainingTime;
		[Sync] bool damageDisabled;
		[Sync] bool disabled;

		Actor self;
		public readonly CloakInfo Info;
		CPos? lastPos;

		public Cloak(Actor self, CloakInfo info)
		{
			this.self = self;
			Info = info;

			remainingTime = info.InitialDelay;

			// Disable if an upgrade is required
			disabled = info.RequiresUpgrade != null;
		}

		public bool AcceptsUpgrade(string type)
		{
			return type == Info.RequiresUpgrade;
		}

		public void UpgradeAvailable(Actor self, string type, bool available)
		{
			if (type == Info.RequiresUpgrade)
			{
				disabled = !available;

				if (disabled)
				{
					Uncloak();
					remainingTime = Info.InitialDelay;
				}
			}
		}

		public void Uncloak() { Uncloak(Info.CloakDelay); }

		public void Uncloak(int time)
		{
			if (Cloaked)
				Sound.Play(Info.UncloakSound, self.CenterPosition);

			remainingTime = Math.Max(remainingTime, time);
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel) { if (Info.UncloakOnAttack) Uncloak(); }

		public bool Cloaked { get { return !disabled && remainingTime <= 0; } }

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			damageDisabled = e.DamageState >= DamageState.Critical;
			if (damageDisabled)
				Uncloak();
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (remainingTime > 0 || disabled)
				return r;

			if (Cloaked && IsVisible(self, self.World.RenderPlayer))
			{
				if (string.IsNullOrEmpty(Info.Palette))
					return r;
				else
					return r.Select(a => a.WithPalette(wr.Palette(Info.Palette)));
			}
			else
				return SpriteRenderable.None;
		}

		public void Tick(Actor self)
		{
			if (disabled)
				return;

			if (remainingTime > 0 && !disabled && !damageDisabled && --remainingTime <= 0)
				Sound.Play(Info.CloakSound, self.CenterPosition);

			if (self.IsDisabled())
				Uncloak();

			if (Info.UncloakOnMove && (lastPos == null || lastPos.Value != self.Location))
			{
				Uncloak();
				lastPos = self.Location;
			}
		}
		
		public bool IsVisible(Actor self, Player viewer)
		{
			if (!Cloaked || self.Owner.IsAlliedWith(viewer))
				return true;

			return self.World.ActorsWithTrait<DetectCloaked>().Any(a =>
			{
				var dc = a.Actor.Info.Traits.Get<DetectCloakedInfo>();

				return a.Actor.Owner.IsAlliedWith(viewer)
					&& Info.CloakTypes.Intersect(dc.CloakTypes).Any()
					&& (self.CenterPosition - a.Actor.CenterPosition).Length <= WRange.FromCells(dc.Range).Range;
			});
		}

		public Color RadarColorOverride(Actor self)
		{
			var c = self.Owner.Color.RGB;
			if (self.Owner == self.World.LocalPlayer && Cloaked)
				c = Color.FromArgb(128, c);
			return c;
		}
	}
}
