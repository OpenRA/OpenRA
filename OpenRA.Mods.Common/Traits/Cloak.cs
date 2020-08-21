#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
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
		Damage = 32,
		Heal = 64,
		SelfHeal = 128,
		Dock = 256
	}

	// Type tag for cloaktypes
	public class CloakType { }

	[Desc("This unit can cloak and uncloak in specific situations.")]
	public class CloakInfo : PausableConditionalTraitInfo
	{
		[Desc("Measured in game ticks.")]
		public readonly int InitialDelay = 10;

		[Desc("Measured in game ticks.")]
		public readonly int CloakDelay = 30;

		[Desc("Events leading to the actor getting uncloaked. Possible values are: Attack, Move, Unload, Infiltrate, Demolish, Dock, Damage, Heal and SelfHeal.",
			"'Dock' is triggered when docking to a refinery or resupplying.")]
		public readonly UncloakType UncloakOn = UncloakType.Attack
			| UncloakType.Unload | UncloakType.Infiltrate | UncloakType.Demolish | UncloakType.Dock;

		public readonly string CloakSound = null;
		public readonly string UncloakSound = null;

		[PaletteReference(nameof(IsPlayerPalette))]
		public readonly string Palette = "cloak";
		public readonly bool IsPlayerPalette = false;

		public readonly BitSet<CloakType> CloakTypes = new BitSet<CloakType>("Cloak");

		[GrantedConditionReference]
		[Desc("The condition to grant to self while cloaked.")]
		public readonly string CloakedCondition = null;

		public override object Create(ActorInitializer init) { return new Cloak(this); }
	}

	public class Cloak : PausableConditionalTrait<CloakInfo>, IRenderModifier, INotifyDamage, INotifyUnload, INotifyDemolition, INotifyInfiltration,
		INotifyAttack, ITick, IVisibilityModifier, IRadarColorModifier, INotifyCreated, INotifyHarvesterAction, INotifyBeingResupplied
	{
		[Sync]
		int remainingTime;

		bool isDocking;
		Cloak[] otherCloaks;

		CPos? lastPos;
		bool wasCloaked = false;
		bool firstTick = true;
		int cloakedToken = Actor.InvalidConditionToken;

		public Cloak(CloakInfo info)
			: base(info)
		{
			remainingTime = info.InitialDelay;
		}

		protected override void Created(Actor self)
		{
			otherCloaks = self.TraitsImplementing<Cloak>()
				.Where(c => c != this)
				.ToArray();

			if (Cloaked)
			{
				wasCloaked = true;
				if (cloakedToken == Actor.InvalidConditionToken)
					cloakedToken = self.GrantCondition(Info.CloakedCondition);
			}

			base.Created(self);
		}

		public bool Cloaked { get { return !IsTraitDisabled && !IsTraitPaused && remainingTime <= 0; } }

		public void Uncloak() { Uncloak(Info.CloakDelay); }

		public void Uncloak(int time) { remainingTime = Math.Max(remainingTime, time); }

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel) { if (Info.UncloakOn.HasFlag(UncloakType.Attack)) Uncloak(); }

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage.Value == 0)
				return;

			var type = e.Damage.Value < 0
				? (e.Attacker == self ? UncloakType.SelfHeal : UncloakType.Heal)
				: UncloakType.Damage;
			if (Info.UncloakOn.HasFlag(type))
				Uncloak();
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (remainingTime > 0 || IsTraitDisabled || IsTraitPaused)
				return r;

			if (Cloaked && IsVisible(self, self.World.RenderPlayer))
			{
				var palette = string.IsNullOrEmpty(Info.Palette) ? null : Info.IsPlayerPalette ? wr.Palette(Info.Palette + self.Owner.InternalName) : wr.Palette(Info.Palette);
				if (palette == null)
					return r;
				else
					return r.Select(a => !a.IsDecoration && a is IPalettedRenderable ? ((IPalettedRenderable)a).WithPalette(palette) : a);
			}
			else
				return SpriteRenderable.None;
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			return bounds;
		}

		void ITick.Tick(Actor self)
		{
			if (!IsTraitDisabled && !IsTraitPaused)
			{
				if (remainingTime > 0 && !isDocking)
					remainingTime--;

				if (Info.UncloakOn.HasFlag(UncloakType.Move) && (lastPos == null || lastPos.Value != self.Location))
				{
					Uncloak();
					lastPos = self.Location;
				}
			}

			var isCloaked = Cloaked;
			if (isCloaked && !wasCloaked)
			{
				if (cloakedToken == Actor.InvalidConditionToken)
					cloakedToken = self.GrantCondition(Info.CloakedCondition);

				// Sounds shouldn't play if the actor starts cloaked
				if (!(firstTick && Info.InitialDelay == 0) && !otherCloaks.Any(a => a.Cloaked))
					Game.Sound.Play(SoundType.World, Info.CloakSound, self.CenterPosition);
			}
			else if (!isCloaked && wasCloaked)
			{
				if (cloakedToken != Actor.InvalidConditionToken)
					cloakedToken = self.RevokeCondition(cloakedToken);

				if (!(firstTick && Info.InitialDelay == 0) && !otherCloaks.Any(a => a.Cloaked))
					Game.Sound.Play(SoundType.World, Info.UncloakSound, self.CenterPosition);
			}

			wasCloaked = isCloaked;
			firstTick = false;
		}

		protected override void TraitEnabled(Actor self)
		{
			remainingTime = Info.InitialDelay;
		}

		protected override void TraitDisabled(Actor self) { Uncloak(); }

		public bool IsVisible(Actor self, Player viewer)
		{
			if (!Cloaked || self.Owner.IsAlliedWith(viewer))
				return true;

			return self.World.ActorsWithTrait<DetectCloaked>().Any(a => a.Actor.Owner.IsAlliedWith(viewer)
				&& Info.CloakTypes.Overlaps(a.Trait.Info.CloakTypes)
				&& (self.CenterPosition - a.Actor.CenterPosition).LengthSquared <= a.Trait.Range.LengthSquared);
		}

		Color IRadarColorModifier.RadarColorOverride(Actor self, Color color)
		{
			if (self.Owner == self.World.LocalPlayer && Cloaked)
				color = Color.FromArgb(128, color);

			return color;
		}

		void INotifyHarvesterAction.MovingToResources(Actor self, CPos targetCell) { }

		void INotifyHarvesterAction.MovingToRefinery(Actor self, Actor refineryActor) { }

		void INotifyHarvesterAction.MovementCancelled(Actor self) { }

		void INotifyHarvesterAction.Harvested(Actor self, ResourceType resource) { }

		void INotifyHarvesterAction.Docked()
		{
			if (Info.UncloakOn.HasFlag(UncloakType.Dock))
			{
				isDocking = true;
				Uncloak();
			}
		}

		void INotifyHarvesterAction.Undocked()
		{
			isDocking = false;
		}

		void INotifyUnload.Unloading(Actor self)
		{
			if (Info.UncloakOn.HasFlag(UncloakType.Unload))
				Uncloak();
		}

		void INotifyDemolition.Demolishing(Actor self)
		{
			if (Info.UncloakOn.HasFlag(UncloakType.Demolish))
				Uncloak();
		}

		void INotifyInfiltration.Infiltrating(Actor self)
		{
			if (Info.UncloakOn.HasFlag(UncloakType.Infiltrate))
				Uncloak();
		}

		void INotifyBeingResupplied.StartingResupply(Actor self, Actor host)
		{
			if (Info.UncloakOn.HasFlag(UncloakType.Dock))
			{
				isDocking = true;
				Uncloak();
			}
		}

		void INotifyBeingResupplied.StoppingResupply(Actor self, Actor host)
		{
			if (Info.UncloakOn.HasFlag(UncloakType.Dock))
				isDocking = false;
		}
	}
}
