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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public sealed class CaptureType { CaptureType() { } }

	[RequireExplicitImplementation]
	public interface ICaptureProgressWatcher
	{
		void Update(Actor self, Actor captor, Actor target, int progress, int total);
	}

	[Desc("Manages Captures and Capturable traits on an actor.")]
	public class CaptureManagerInfo : TraitInfo
	{
		[GrantedConditionReference]
		[Desc("Condition granted when capturing an actor.")]
		public readonly string CapturingCondition = null;

		[GrantedConditionReference]
		[Desc("Condition granted when being captured by another actor.")]
		public readonly string BeingCapturedCondition = null;

		[Desc("Should units friendly to the capturing actor auto-target this actor while it is being captured?")]
		public readonly bool PreventsAutoTarget = true;

		public override object Create(ActorInitializer init) { return new CaptureManager(this); }

		public bool CanBeTargetedBy(FrozenActor frozenActor, Actor captor, Captures captures)
		{
			if (captures.IsTraitDisabled)
				return false;

			// TODO: FrozenActors don't yet have a way of caching conditions, so we can't filter disabled traits
			// This therefore assumes that all Capturable traits are enabled, which is probably wrong.
			// Actors with FrozenUnderFog should therefore not disable the Capturable trait.
			var stance = captor.Owner.RelationshipWith(frozenActor.Owner);
			return frozenActor.Info.TraitInfos<CapturableInfo>()
				.Any(c => c.ValidRelationships.HasStance(stance) && captures.Info.CaptureTypes.Overlaps(c.Types));
		}
	}

	public class CaptureManager : INotifyCreated, INotifyCapture, ITick, IDisableEnemyAutoTarget
	{
		readonly CaptureManagerInfo info;
		IMove move;
		ICaptureProgressWatcher[] progressWatchers;

		BitSet<CaptureType> allyCapturableTypes;
		BitSet<CaptureType> neutralCapturableTypes;
		BitSet<CaptureType> enemyCapturableTypes;
		BitSet<CaptureType> capturesTypes;

		IEnumerable<Capturable> enabledCapturable;
		IEnumerable<Captures> enabledCaptures;

		// Related to a specific capture in process
		Actor currentTarget;
		CaptureManager currentTargetManager;
		int currentTargetDelay;
		int currentTargetTotal;
		int capturingToken = Actor.InvalidConditionToken;
		int beingCapturedToken = Actor.InvalidConditionToken;
		bool enteringCurrentTarget;

		HashSet<Actor> currentCaptors = new HashSet<Actor>();

		public bool BeingCaptured { get; private set; }

		public CaptureManager(CaptureManagerInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			move = self.TraitOrDefault<IMove>();
			progressWatchers = self.TraitsImplementing<ICaptureProgressWatcher>().ToArray();

			enabledCapturable = self.TraitsImplementing<Capturable>()
				.ToArray()
				.Where(Exts.IsTraitEnabled);

			enabledCaptures = self.TraitsImplementing<Captures>()
				.ToArray()
				.Where(Exts.IsTraitEnabled);

			RefreshCaptures(self);
			RefreshCapturable(self);
		}

		public void RefreshCapturable(Actor self)
		{
			allyCapturableTypes = neutralCapturableTypes = enemyCapturableTypes = default(BitSet<CaptureType>);
			foreach (var c in enabledCapturable)
			{
				if (c.Info.ValidRelationships.HasStance(PlayerRelationship.Ally))
					allyCapturableTypes = allyCapturableTypes.Union(c.Info.Types);

				if (c.Info.ValidRelationships.HasStance(PlayerRelationship.Neutral))
					neutralCapturableTypes = neutralCapturableTypes.Union(c.Info.Types);

				if (c.Info.ValidRelationships.HasStance(PlayerRelationship.Enemy))
					enemyCapturableTypes = enemyCapturableTypes.Union(c.Info.Types);
			}
		}

		public void RefreshCaptures(Actor self)
		{
			capturesTypes = enabledCaptures.Aggregate(
				default(BitSet<CaptureType>),
				(a, b) => a.Union(b.Info.CaptureTypes));
		}

		public bool CanBeTargetedBy(Actor self, Actor captor, CaptureManager captorManager)
		{
			var stance = captor.Owner.RelationshipWith(self.Owner);
			if (stance.HasStance(PlayerRelationship.Enemy))
				return captorManager.capturesTypes.Overlaps(enemyCapturableTypes);

			if (stance.HasStance(PlayerRelationship.Neutral))
				return captorManager.capturesTypes.Overlaps(neutralCapturableTypes);

			if (stance.HasStance(PlayerRelationship.Ally))
				return captorManager.capturesTypes.Overlaps(allyCapturableTypes);

			return false;
		}

		public bool CanBeTargetedBy(Actor self, Actor captor, Captures captures)
		{
			if (captures.IsTraitDisabled)
				return false;

			var stance = captor.Owner.RelationshipWith(self.Owner);
			if (stance.HasStance(PlayerRelationship.Enemy))
				return captures.Info.CaptureTypes.Overlaps(enemyCapturableTypes);

			if (stance.HasStance(PlayerRelationship.Neutral))
				return captures.Info.CaptureTypes.Overlaps(neutralCapturableTypes);

			if (stance.HasStance(PlayerRelationship.Ally))
				return captures.Info.CaptureTypes.Overlaps(allyCapturableTypes);

			return false;
		}

		public Captures ValidCapturesWithLowestSabotageThreshold(Actor self, Actor captee, CaptureManager capteeManager)
		{
			if (captee.IsDead)
				return null;

			foreach (var c in enabledCaptures.OrderBy(c => c.Info.SabotageThreshold))
				if (capteeManager.CanBeTargetedBy(captee, self, c))
					return c;

			return null;
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			BeingCaptured = true;
			self.World.AddFrameEndTask(w => BeingCaptured = false);
		}

		/// <summary>
		/// Called by CaptureActor when the activity is ready to enter and capture the target.
		/// This method grants the capturing conditions on the captor and target and returns
		/// true if the captor is able to start entering or false if it needs to wait.
		/// </summary>
		public bool StartCapture(Actor self, Actor target, CaptureManager targetManager, out Captures captures)
		{
			captures = null;

			// Prevent a capture being restarted after it has been canceled during disposal
			if (self.WillDispose)
				return false;

			if (target != currentTarget)
			{
				if (currentTarget != null)
					CancelCapture(self, currentTarget, currentTargetManager);

				targetManager.currentCaptors.Add(self);
				currentTarget = target;
				currentTargetManager = targetManager;
				currentTargetDelay = 0;
			}
			else
				currentTargetDelay += 1;

			if (capturingToken == Actor.InvalidConditionToken)
				capturingToken = self.GrantCondition(info.CapturingCondition);

			if (targetManager.beingCapturedToken == Actor.InvalidConditionToken)
				targetManager.beingCapturedToken = target.GrantCondition(targetManager.info.BeingCapturedCondition);

			captures = enabledCaptures
				.OrderBy(c => c.Info.CaptureDelay)
				.FirstOrDefault(c => targetManager.CanBeTargetedBy(target, self, c));

			if (captures == null)
				return false;

			// HACK: Make sure the target is not moving and at its normal position with respect to the cell grid
			var enterMobile = target.TraitOrDefault<Mobile>();
			if (enterMobile != null && enterMobile.IsMovingBetweenCells)
				return false;

			if (progressWatchers.Any() || targetManager.progressWatchers.Any())
			{
				currentTargetTotal = captures.Info.CaptureDelay;
				if (move != null && captures.Info.ConsumedByCapture)
				{
					var pos = target.GetTargetablePositions().PositionClosestTo(self.CenterPosition);
					currentTargetTotal += move.EstimatedMoveDuration(self, self.CenterPosition, pos);
				}

				foreach (var w in progressWatchers)
					w.Update(self, self, target, currentTargetDelay, currentTargetTotal);

				foreach (var w in targetManager.progressWatchers)
					w.Update(target, self, target, currentTargetDelay, currentTargetTotal);
			}

			enteringCurrentTarget = currentTargetDelay >= captures.Info.CaptureDelay;
			return enteringCurrentTarget;
		}

		/// <summary>
		/// Called by CaptureActor when the activity finishes or is cancelled
		/// This method revokes the capturing conditions on the captor and target
		/// and resets any capturing progress.
		/// </summary>
		public void CancelCapture(Actor self, Actor target, CaptureManager targetManager)
		{
			if (currentTarget == null)
				return;

			foreach (var w in progressWatchers)
				w.Update(self, self, target, 0, 0);

			foreach (var w in targetManager.progressWatchers)
				w.Update(target, self, target, 0, 0);

			if (capturingToken != Actor.InvalidConditionToken)
				capturingToken = self.RevokeCondition(capturingToken);

			if (targetManager.beingCapturedToken != Actor.InvalidConditionToken)
				targetManager.beingCapturedToken = target.RevokeCondition(targetManager.beingCapturedToken);

			currentTarget = null;
			currentTargetManager = null;
			currentTargetDelay = 0;
			enteringCurrentTarget = false;
			targetManager.currentCaptors.Remove(self);
		}

		void ITick.Tick(Actor self)
		{
			// TryCapture is not called once the captor starts entering the target
			// so we continue ticking the progress watchers ourself
			if (!enteringCurrentTarget)
				return;

			if (currentTargetDelay < currentTargetTotal)
				currentTargetDelay++;

			foreach (var w in progressWatchers)
				w.Update(self, self, currentTarget, currentTargetDelay, currentTargetTotal);

			foreach (var w in currentTargetManager.progressWatchers)
				w.Update(currentTarget, self, currentTarget, currentTargetDelay, currentTargetTotal);
		}

		bool IDisableEnemyAutoTarget.DisableEnemyAutoTarget(Actor self, Actor attacker)
		{
			return info.PreventsAutoTarget && currentCaptors.Any(c => attacker.AppearsFriendlyTo(c));
		}
	}
}
