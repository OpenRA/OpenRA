#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public sealed class CaptureType { CaptureType() { } }

	[Desc("Manages Captures and Capturable traits on an actor.")]
	public class CaptureManagerInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("Condition granted when capturing an actor.")]
		public readonly string CapturingCondition = null;

		[GrantedConditionReference]
		[Desc("Condition granted when being captured by another actor.")]
		public readonly string BeingCapturedCondition = null;

		public virtual object Create(ActorInitializer init) { return new CaptureManager(this); }
	}

	public class CaptureManager : INotifyCreated, INotifyCapture
	{
		readonly CaptureManagerInfo info;
		ConditionManager conditionManager;

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
		int capturingToken = ConditionManager.InvalidConditionToken;
		int beingCapturedToken = ConditionManager.InvalidConditionToken;

		public bool BeingCaptured { get; private set; }

		public CaptureManager(CaptureManagerInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();

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
				if (c.Info.ValidStances.HasStance(Stance.Ally))
					allyCapturableTypes = allyCapturableTypes.Union(c.Info.Types);

				if (c.Info.ValidStances.HasStance(Stance.Neutral))
					neutralCapturableTypes = neutralCapturableTypes.Union(c.Info.Types);

				if (c.Info.ValidStances.HasStance(Stance.Enemy))
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
			var stance = self.Owner.Stances[captor.Owner];
			if (stance.HasStance(Stance.Enemy))
				return captorManager.capturesTypes.Overlaps(enemyCapturableTypes);

			if (stance.HasStance(Stance.Neutral))
				return captorManager.capturesTypes.Overlaps(neutralCapturableTypes);

			if (stance.HasStance(Stance.Ally))
				return captorManager.capturesTypes.Overlaps(allyCapturableTypes);

			return false;
		}

		public bool CanBeTargetedBy(Actor self, Actor captor, Captures captures)
		{
			if (captures.IsTraitDisabled)
				return false;

			var stance = self.Owner.Stances[captor.Owner];
			if (stance.HasStance(Stance.Enemy))
				return captures.Info.CaptureTypes.Overlaps(enemyCapturableTypes);

			if (stance.HasStance(Stance.Neutral))
				return captures.Info.CaptureTypes.Overlaps(neutralCapturableTypes);

			if (stance.HasStance(Stance.Ally))
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

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			BeingCaptured = true;
			self.World.AddFrameEndTask(w => BeingCaptured = false);
		}

		/// <summary>
		/// Called by CaptureActor when the activity is ready to enter and capture the target.
		/// This method grants the capturing conditions on the captor and target and returns
		/// true if the captor is able to start entering or false if it needs to wait.
		/// </summary>
		public bool StartCapture(Actor self, Actor target, CaptureManager targetManager)
		{
			if (target != currentTarget)
			{
				if (currentTarget != null)
					CancelCapture(self, currentTarget, currentTargetManager);

				currentTarget = target;
				currentTargetManager = targetManager;
				currentTargetDelay = 0;
			}
			else
				currentTargetDelay += 1;

			if (conditionManager != null && !string.IsNullOrEmpty(info.CapturingCondition) &&
					capturingToken == ConditionManager.InvalidConditionToken)
				capturingToken = conditionManager.GrantCondition(self, info.CapturingCondition);

			if (targetManager.conditionManager != null && !string.IsNullOrEmpty(targetManager.info.BeingCapturedCondition) &&
					targetManager.beingCapturedToken == ConditionManager.InvalidConditionToken)
				targetManager.beingCapturedToken = targetManager.conditionManager.GrantCondition(target, targetManager.info.BeingCapturedCondition);

			foreach (var c in enabledCaptures.OrderBy(c => c.Info.CaptureDelay))
				if (targetManager.CanBeTargetedBy(target, self, c) && c.Info.CaptureDelay <= currentTargetDelay)
					return true;

			return false;
		}

		/// <summary>
		/// Called by CaptureActor when the activity finishes or is cancelled
		/// This method revokes the capturing conditions on the captor and target
		/// and resets any capturing progress.
		/// </summary>
		public void CancelCapture(Actor self, Actor target, CaptureManager targetManager)
		{
			if (capturingToken != ConditionManager.InvalidConditionToken)
				capturingToken = conditionManager.RevokeCondition(self, capturingToken);

			if (targetManager.beingCapturedToken != ConditionManager.InvalidConditionToken)
				targetManager.beingCapturedToken = targetManager.conditionManager.RevokeCondition(self, targetManager.beingCapturedToken);

			currentTarget = null;
			currentTargetManager = null;
			currentTargetDelay = 0;
		}
	}
}
