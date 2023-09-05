#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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

		public override object Create(ActorInitializer init) { return new CaptureManager(init.Self, this); }
	}

	public class CaptureManager : INotifyCreated, INotifyCapture, ITick, IDisableEnemyAutoTarget
	{
		readonly Actor self;
		readonly CaptureManagerInfo info;

		IMove move;
		ICaptureProgressWatcher[] progressWatchers;

		BitSet<CaptureType> allyCapturesTypes;
		BitSet<CaptureType> neutralCapturesTypes;
		BitSet<CaptureType> enemyCapturesTypes;
		BitSet<CaptureType> capturableTypes;

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

		readonly HashSet<Actor> currentCaptors = new();

		public bool BeingCaptured { get; private set; }

		public CaptureManager(Actor self, CaptureManagerInfo info)
		{
			this.self = self;
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			move = self.TraitOrDefault<IMove>();
			progressWatchers = self.TraitsImplementing<ICaptureProgressWatcher>().ToArray();

			enabledCapturable = self.TraitsImplementing<Capturable>()
				.ToArray()
				.Where(t => !t.IsTraitDisabled);

			enabledCaptures = self.TraitsImplementing<Captures>()
				.ToArray()
				.Where(t => !t.IsTraitDisabled);

			RefreshCaptures();
			RefreshCapturable();
		}

		public void RefreshCaptures()
		{
			allyCapturesTypes = neutralCapturesTypes = enemyCapturesTypes = default;
			foreach (var c in enabledCaptures)
			{
				if (c.Info.ValidRelationships.HasRelationship(PlayerRelationship.Ally))
					allyCapturesTypes = allyCapturesTypes.Union(c.Info.CaptureTypes);

				if (c.Info.ValidRelationships.HasRelationship(PlayerRelationship.Neutral))
					neutralCapturesTypes = neutralCapturesTypes.Union(c.Info.CaptureTypes);

				if (c.Info.ValidRelationships.HasRelationship(PlayerRelationship.Enemy))
					enemyCapturesTypes = enemyCapturesTypes.Union(c.Info.CaptureTypes);
			}
		}

		public void RefreshCapturable()
		{
			capturableTypes = enabledCapturable.Aggregate(
				default(BitSet<CaptureType>),
				(a, b) => a.Union(b.Info.Types));
		}

		/// <summary>Should only be called from the captor's CaptureManager.</summary>
		public bool CanTarget(CaptureManager target)
		{
			return CanTarget(target.self.Owner, target.capturableTypes);
		}

		/// <summary>Should only be called from the captor CaptureManager.</summary>
		public bool CanTarget(FrozenActor target)
		{
			if (!target.Info.HasTraitInfo<CaptureManagerInfo>())
				return false;

			// TODO: FrozenActors don't yet have a way of caching conditions, so we can't filter disabled traits
			// This therefore assumes that all Capturable traits are enabled, which is probably wrong.
			// Actors with FrozenUnderFog should therefore not disable the Capturable trait.
			var targetTypes = target.Info.TraitInfos<CapturableInfo>().Aggregate(
				default(BitSet<CaptureType>),
				(a, b) => a.Union(b.Types));

			return CanTarget(target.Owner, targetTypes);
		}

		bool CanTarget(Player target, BitSet<CaptureType> captureTypes)
		{
			var relationship = self.Owner.RelationshipWith(target);
			if (relationship.HasRelationship(PlayerRelationship.Enemy))
				return captureTypes.Overlaps(enemyCapturesTypes);

			if (relationship.HasRelationship(PlayerRelationship.Neutral))
				return captureTypes.Overlaps(neutralCapturesTypes);

			if (relationship.HasRelationship(PlayerRelationship.Ally))
				return captureTypes.Overlaps(allyCapturesTypes);

			return false;
		}

		public Captures ValidCapturesWithLowestSabotageThreshold(CaptureManager target)
		{
			if (target.self.IsDead)
				return null;

			var relationship = self.Owner.RelationshipWith(target.self.Owner);
			foreach (var c in enabledCaptures.OrderBy(c => c.Info.SabotageThreshold).ThenBy(c => c.Info.CaptureDelay))
				if (c.Info.ValidRelationships.HasRelationship(relationship) && target.capturableTypes.Overlaps(c.Info.CaptureTypes))
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
		public bool StartCapture(CaptureManager targetManager, out Captures captures)
		{
			captures = null;

			// Prevent a capture being restarted after it has been canceled during disposal
			if (self.WillDispose)
				return false;

			var target = targetManager.self;
			if (target != currentTarget)
			{
				if (currentTargetManager != null)
					CancelCapture(currentTargetManager);

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

			captures = ValidCapturesWithLowestSabotageThreshold(targetManager);
			if (captures == null)
				return false;

			// HACK: Make sure the target is not moving and at its normal position with respect to the cell grid
			var enterMobile = target.TraitOrDefault<Mobile>();
			if (enterMobile != null && enterMobile.IsMovingBetweenCells)
				return false;

			if (progressWatchers.Length > 0 || targetManager.progressWatchers.Length > 0)
			{
				currentTargetTotal = captures.Info.CaptureDelay;
				if (move != null && captures.Info.ConsumedByCapture)
				{
					var pos = target.GetTargetablePositions().ClosestToIgnoringPath(self.CenterPosition);
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
		public void CancelCapture(CaptureManager targetManager)
		{
			if (currentTarget == null)
				return;

			var target = targetManager.self;
			foreach (var w in progressWatchers)
				w.Update(self, self, target, 0, 0);

			if (targetManager != null)
				foreach (var w in targetManager.progressWatchers)
					w.Update(target, self, target, 0, 0);

			if (capturingToken != Actor.InvalidConditionToken)
				capturingToken = self.RevokeCondition(capturingToken);

			if (targetManager != null)
			{
				if (targetManager.beingCapturedToken != Actor.InvalidConditionToken)
					targetManager.beingCapturedToken = target.RevokeCondition(targetManager.beingCapturedToken);

				targetManager.currentCaptors.Remove(self);
			}

			currentTarget = null;
			currentTargetManager = null;
			currentTargetDelay = 0;
			enteringCurrentTarget = false;
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
