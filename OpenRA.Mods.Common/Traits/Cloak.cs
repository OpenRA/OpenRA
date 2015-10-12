#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This unit can cloak and uncloak in specific situations.")]
	public class CloakInfo : UpgradableTraitInfo
	{
		[Desc("Measured in game ticks.")]
		public readonly int InitialDelay = 10;

		[Desc("Measured in game ticks.")]
		public readonly int CloakDelay = 30;

		public readonly bool UncloakOnAttack = true;
		public readonly bool UncloakOnMove = false;
		public readonly bool UncloakOnUnload = true;
		public readonly bool UncloakOnInfiltrate = true;
		public readonly bool UncloakOnDemolish = true;

		public readonly string CloakSound = null;
		public readonly string UncloakSound = null;

		[PaletteReference("IsPlayerPalette")] public readonly string Palette = "cloak";
		public readonly bool IsPlayerPalette = false;

		public readonly HashSet<string> CloakTypes = new HashSet<string> { "Cloak" };

		[UpgradeGrantedReference]
		[Desc("The upgrades to grant to self while cloaked.")]
		public readonly string[] WhileCloakedUpgrades = { };

		public override object Create(ActorInitializer init) { return new Cloak(init.Self, this); }
	}

	public class Cloak : UpgradableTrait<CloakInfo>, IRenderModifier, INotifyDamageStateChanged, INotifyAttack, ITick, IVisibilityModifier, IRadarColorModifier, INotifyCreated
	{
		[Sync] int remainingTime;
		[Sync] bool damageDisabled;
		UpgradeManager upgradeManager;

		Actor self;
		CPos? lastPos;

		public Cloak(Actor self, CloakInfo info)
			: base(info)
		{
			this.self = self;

			remainingTime = info.InitialDelay;
		}

		public void Created(Actor self)
		{
			upgradeManager = self.TraitOrDefault<UpgradeManager>();
			if (remainingTime == 0)
			{
				if (upgradeManager != null)
					foreach (var u in Info.WhileCloakedUpgrades)
						upgradeManager.GrantUpgrade(self, u, this);
			}
		}

		protected override void UpgradeDisabled(Actor self)
		{
			Uncloak();
			remainingTime = Info.InitialDelay;
		}

		public void Uncloak() { Uncloak(Info.CloakDelay); }

		public void Uncloak(int time)
		{
			if (Cloaked)
			{
				Game.Sound.Play(Info.UncloakSound, self.CenterPosition);
				if (upgradeManager != null)
					foreach (var u in Info.WhileCloakedUpgrades)
						upgradeManager.RevokeUpgrade(self, u, this);
			}

			remainingTime = Math.Max(remainingTime, time);
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel) { if (Info.UncloakOnAttack) Uncloak(); }

		public bool Cloaked { get { return !IsTraitDisabled && remainingTime <= 0; } }

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			damageDisabled = e.DamageState >= DamageState.Critical;
			if (damageDisabled)
				Uncloak();
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (remainingTime > 0 || IsTraitDisabled)
				return r;

			if (Cloaked && IsVisible(self, self.World.RenderPlayer))
			{
				var palette = string.IsNullOrEmpty(Info.Palette) ? null : Info.IsPlayerPalette ? wr.Palette(Info.Palette + self.Owner.InternalName) : wr.Palette(Info.Palette);
				if (palette == null)
					return r;
				else
					return r.Select(a => a.WithPalette(palette));
			}
			else
				return SpriteRenderable.None;
		}

		public void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (remainingTime > 0 && !IsTraitDisabled && !damageDisabled && --remainingTime <= 0)
			{
				Game.Sound.Play(Info.CloakSound, self.CenterPosition);
				if (upgradeManager != null)
					foreach (var u in Info.WhileCloakedUpgrades)
						upgradeManager.GrantUpgrade(self, u, this);
			}

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

			return self.World.ActorsWithTrait<DetectCloaked>().Any(a => !a.Trait.IsTraitDisabled && a.Actor.Owner.IsAlliedWith(viewer)
				&& Info.CloakTypes.Overlaps(a.Trait.Info.CloakTypes)
				&& (self.CenterPosition - a.Actor.CenterPosition).LengthSquared <= a.Trait.Info.Range.LengthSquared);
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
