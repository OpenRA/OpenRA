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
	[Flags]
	public enum UncloakType
	{
		None = 0,
		Attack = 1,
		Move = 2,
		Unload = 4,
		Infiltrate = 8,
		Demolish = 16,
		Damage = 32
	}

	[Desc("This unit can cloak and uncloak in specific situations.")]
	public class CloakInfo : UpgradableTraitInfo
	{
		[Desc("Measured in game ticks.")]
		public readonly int InitialDelay = 10;

		[Desc("Measured in game ticks.")]
		public readonly int CloakDelay = 30;

		[Desc("Events leading to the actor getting uncloaked. Possible values are: Attack, Move, Unload, Infiltrate, Demolish and Damage")]
		public readonly UncloakType UncloakOn = UncloakType.Attack
			| UncloakType.Unload | UncloakType.Infiltrate | UncloakType.Demolish;

		public readonly string CloakSound = null;
		public readonly string UncloakSound = null;

		[PaletteReference("IsPlayerPalette")] public readonly string Palette = "cloak";
		public readonly bool IsPlayerPalette = false;

		public readonly HashSet<string> CloakTypes = new HashSet<string> { "Cloak" };

		[UpgradeGrantedReference]
		[Desc("The upgrades to grant to self while cloaked.")]
		public readonly string[] WhileCloakedUpgrades = { };

		public override object Create(ActorInitializer init) { return new Cloak(this); }
	}

	public class Cloak : UpgradableTrait<CloakInfo>, IRenderModifier, INotifyDamageStateChanged,
		INotifyAttack, ITick, IVisibilityModifier, IRadarColorModifier, INotifyCreated
	{
		[Sync] int remainingTime;
		[Sync] bool damageDisabled;
		UpgradeManager upgradeManager;

		CPos? lastPos;
		bool wasCloaked = false;

		public Cloak(CloakInfo info)
			: base(info)
		{
			remainingTime = info.InitialDelay;
		}

		void INotifyCreated.Created(Actor self)
		{
			upgradeManager = self.TraitOrDefault<UpgradeManager>();
			if (Cloaked)
				GrantUpgrades(self);
		}

		public bool Cloaked { get { return !IsTraitDisabled && remainingTime <= 0; } }

		public void Uncloak() { Uncloak(Info.CloakDelay); }

		public void Uncloak(int time) { remainingTime = Math.Max(remainingTime, time); }

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel) { if (Info.UncloakOn.HasFlag(UncloakType.Attack)) Uncloak(); }

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			damageDisabled = e.DamageState >= DamageState.Critical;
			if (damageDisabled || Info.UncloakOn.HasFlag(UncloakType.Damage))
				Uncloak();
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (remainingTime > 0 || IsTraitDisabled)
				return r;

			if (Cloaked && IsVisible(self, self.World.RenderPlayer))
			{
				var palette = string.IsNullOrEmpty(Info.Palette) ? null : Info.IsPlayerPalette ? wr.Palette(Info.Palette + self.Owner.InternalName) : wr.Palette(Info.Palette);
				if (palette == null)
					return r;
				else
					return r.Select(a => a.IsDecoration ? a : a.WithPalette(palette));
			}
			else
				return SpriteRenderable.None;
		}

		void ITick.Tick(Actor self)
		{
			if (!IsTraitDisabled)
			{
				if (remainingTime > 0 && !damageDisabled)
					remainingTime--;

				if (self.IsDisabled())
					Uncloak();

				if (Info.UncloakOn.HasFlag(UncloakType.Move) && (lastPos == null || lastPos.Value != self.Location))
				{
					Uncloak();
					lastPos = self.Location;
				}
			}

			var isCloaked = Cloaked;
			if (isCloaked && !wasCloaked)
			{
				GrantUpgrades(self);
				if (!self.TraitsImplementing<Cloak>().Any(a => a != this && a.Cloaked))
					Game.Sound.Play(Info.CloakSound, self.CenterPosition);
			}
			else if (!isCloaked && wasCloaked)
			{
				RevokeUpgrades(self);
				if (!self.TraitsImplementing<Cloak>().Any(a => a != this && a.Cloaked))
					Game.Sound.Play(Info.UncloakSound, self.CenterPosition);
			}

			wasCloaked = isCloaked;
		}

		public bool IsVisible(Actor self, Player viewer)
		{
			if (!Cloaked || self.Owner.IsAlliedWith(viewer))
				return true;

			return self.World.ActorsWithTrait<DetectCloaked>().Any(a => !a.Trait.IsTraitDisabled && a.Actor.Owner.IsAlliedWith(viewer)
				&& Info.CloakTypes.Overlaps(a.Trait.Info.CloakTypes)
				&& (self.CenterPosition - a.Actor.CenterPosition).LengthSquared <= a.Trait.Info.Range.LengthSquared);
		}

		Color IRadarColorModifier.RadarColorOverride(Actor self)
		{
			var c = self.Owner.Color.RGB;
			if (self.Owner == self.World.LocalPlayer && Cloaked)
				c = Color.FromArgb(128, c);
			return c;
		}

		void GrantUpgrades(Actor self)
		{
			if (upgradeManager != null)
				foreach (var u in Info.WhileCloakedUpgrades)
					upgradeManager.GrantUpgrade(self, u, this);
		}

		void RevokeUpgrades(Actor self)
		{
			if (upgradeManager != null)
				foreach (var u in Info.WhileCloakedUpgrades)
					upgradeManager.RevokeUpgrade(self, u, this);
		}
	}
}
