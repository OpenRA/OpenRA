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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	class SandwormInfo : WandersInfo, Requires<MobileInfo>, Requires<RenderUnitInfo>, Requires<AttackBaseInfo>
	{
		[Desc("Time between rescanning for targets (in ticks).")]
		public readonly int TargetRescanInterval = 32;

		[Desc("The radius in which the worm \"searches\" for targets.")]
		public readonly WRange MaxSearchRadius = WRange.FromCells(27);

		[Desc("The range at which the worm launches an attack regardless of noise levels.")]
		public readonly WRange IgnoreNoiseAttackRange = WRange.FromCells(3);

		[Desc("The chance this actor has of disappearing after it attacks (in %).")]
		public readonly int ChanceToDisappear = 80;

		[Desc("Name of the sequence that is used when the actor is idle or moving (not attacking).")]
		public readonly string IdleSequence = "idle";

		public override object Create(ActorInitializer init) { return new Sandworm(init.Self, this); }
	}

	class Sandworm : Wanders, ITick, INotifyKilled
	{
		public readonly SandwormInfo Info;

		readonly WormManager manager;
		readonly Lazy<Mobile> mobile;
		readonly Lazy<RenderUnit> renderUnit;
		readonly Lazy<AttackBase> attackTrait;

		public bool IsMovingTowardTarget { get; private set; }

		public bool IsAttacking;

		int targetCountdown;

		public Sandworm(Actor self, SandwormInfo info)
			: base(self, info)
		{
			Info = info;
			mobile = Exts.Lazy(self.Trait<Mobile>);
			renderUnit = Exts.Lazy(self.Trait<RenderUnit>);
			attackTrait = Exts.Lazy(self.Trait<AttackBase>);
			manager = self.World.WorldActor.Trait<WormManager>();
		}

		public override void OnBecomingIdle(Actor self)
		{
			if (renderUnit.Value.DefaultAnimation.CurrentSequence.Name != Info.IdleSequence)
				renderUnit.Value.DefaultAnimation.PlayRepeating("idle");

			base.OnBecomingIdle(self);
		}

		public override void DoAction(Actor self, CPos targetCell)
		{
			IsMovingTowardTarget = false;

			RescanForTargets(self);

			if (IsMovingTowardTarget)
				return;

			self.QueueActivity(mobile.Value.MoveWithinRange(Target.FromCell(self.World, targetCell, SubCell.Any), WRange.FromCells(1)));
		}

		public void Tick(Actor self)
		{
			if (--targetCountdown > 0 || IsAttacking || !self.IsInWorld)
				return;

			RescanForTargets(self);
		}

		void RescanForTargets(Actor self)
		{
			targetCountdown = Info.TargetRescanInterval;

			var actorsInRange = self.World.FindActorsInCircle(self.CenterPosition, Info.MaxSearchRadius);
			var noiseDirection = WVec.Zero;

			foreach (var actor in actorsInRange)
			{
				if (!actor.IsInWorld)
					continue;

				// TODO: Test if we really want to ignore actors that are on rock
				if (!mobile.Value.CanEnterCell(actor.Location, null, false))
					continue;

				var noise = actor.TraitOrDefault<AttractsWorms>();
				if (noise == null)
					continue;

				var distance = actor.CenterPosition - self.CenterPosition;
				var length = distance.Length;

				// Actor is too far to be heard
				if (noise.Info.Range[noise.Info.Range.Length - 1].Range < length)
					continue;

				// If close enough, we don't care about other actors
				if (length <= Info.IgnoreNoiseAttackRange.Range)
				{
					self.CancelActivity();
					attackTrait.Value.ResolveOrder(self, new Order("Attack", actor, true) { TargetActor = actor });
					return;
				}

				var direction = 1024 * distance / length;
				var percentage = noise.GetNoisePercentageAtDistance(length);

				noiseDirection += direction * noise.Info.Intensity * percentage / 100;
			}

			// No target was found
			if (noiseDirection == WVec.Zero)
				return;

			var moveTo = self.World.Map.CellContaining(self.CenterPosition + noiseDirection);

			while (!self.World.Map.Contains(moveTo) || !mobile.Value.CanEnterCell(moveTo, null, false))
			{
				noiseDirection /= 2;
				moveTo = self.World.Map.CellContaining(self.CenterPosition + noiseDirection);
			}

			// Don't get stuck when the noise is distributed evenly! This will make the worm wander instead of trying to move to where it already is
			if (moveTo == self.Location)
			{
				self.CancelActivity();
				return;
			}

			self.QueueActivity(false, mobile.Value.MoveTo(moveTo, 3));
			IsMovingTowardTarget = true;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			manager.DecreaseWormCount();
		}
	}
}