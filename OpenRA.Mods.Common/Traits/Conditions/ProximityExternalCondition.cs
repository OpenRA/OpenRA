#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Applies a condition to actors within a specified range.")]
	public class ProximityExternalConditionInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		[Desc("The range to search for actors.")]
		public readonly WDist Range = WDist.FromCells(3);

		[Desc("The maximum vertical range above terrain to search for actors.",
		"Ignored if 0 (actors are selected regardless of vertical distance).")]
		public readonly WDist MaximumVerticalOffset = WDist.Zero;

		[Desc("What diplomatic stances are affected.")]
		public readonly Stance ValidStances = Stance.Ally;

		[Desc("Condition is applied permanently to this actor.")]
		public readonly bool AffectsParent = false;

		public readonly string EnableSound = null;
		public readonly string DisableSound = null;

		public object Create(ActorInitializer init) { return new ProximityExternalCondition(init.Self, this); }
	}

	public class ProximityExternalCondition : ITick, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOtherProduction
	{
		readonly ProximityExternalConditionInfo info;
		readonly Actor self;

		readonly Dictionary<Actor, int> tokens = new Dictionary<Actor, int>();

		int proximityTrigger;
		WPos cachedPosition;
		WDist cachedRange;
		WDist desiredRange;
		WDist cachedVRange;
		WDist desiredVRange;

		bool cachedDisabled = true;

		public ProximityExternalCondition(Actor self, ProximityExternalConditionInfo info)
		{
			this.info = info;
			this.self = self;
			cachedRange = info.Range;
			cachedVRange = info.MaximumVerticalOffset;
		}

		public void AddedToWorld(Actor self)
		{
			cachedPosition = self.CenterPosition;
			proximityTrigger = self.World.ActorMap.AddProximityTrigger(cachedPosition, cachedRange, cachedVRange, ActorEntered, ActorExited);
		}

		public void RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveProximityTrigger(proximityTrigger);
		}

		public void Tick(Actor self)
		{
			var disabled = self.IsDisabled();

			if (cachedDisabled != disabled)
			{
				Game.Sound.Play(SoundType.World, disabled ? info.DisableSound : info.EnableSound, self.CenterPosition);
				desiredRange = disabled ? WDist.Zero : info.Range;
				desiredVRange = disabled ? WDist.Zero : info.MaximumVerticalOffset;
				cachedDisabled = disabled;
			}

			if (self.CenterPosition != cachedPosition || desiredRange != cachedRange || desiredVRange != cachedVRange)
			{
				cachedPosition = self.CenterPosition;
				cachedRange = desiredRange;
				cachedVRange = desiredVRange;
				self.World.ActorMap.UpdateProximityTrigger(proximityTrigger, cachedPosition, cachedRange, cachedVRange);
			}
		}

		void ActorEntered(Actor a)
		{
			if (a.Disposed || self.Disposed)
				return;

			if (a == self && !info.AffectsParent)
				return;

			var stance = self.Owner.Stances[a.Owner];
			if (!info.ValidStances.HasStance(stance))
				return;

			var cm = a.TraitOrDefault<ConditionManager>();
			if (cm != null && !tokens.ContainsKey(a) && cm.AcceptsExternalCondition(a, info.Condition))
				tokens[a] = cm.GrantCondition(a, info.Condition, true);
		}

		public void UnitProducedByOther(Actor self, Actor producer, Actor produced)
		{
			// If the produced Actor doesn't occupy space, it can't be in range
			if (produced.OccupiesSpace == null)
				return;

			// We don't grant conditions when disabled
			if (self.IsDisabled())
				return;

			// Work around for actors produced within the region not triggering until the second tick
			if ((produced.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= info.Range.LengthSquared)
			{
				var stance = self.Owner.Stances[produced.Owner];
				if (!info.ValidStances.HasStance(stance))
					return;

				var cm = produced.TraitOrDefault<ConditionManager>();
				if (cm != null && cm.AcceptsExternalCondition(produced, info.Condition))
					tokens[produced] = cm.GrantCondition(produced, info.Condition, true);
			}
		}

		void ActorExited(Actor a)
		{
			if (a.Disposed)
				return;

			int token;
			if (!tokens.TryGetValue(a, out token))
				return;

			tokens.Remove(a);
			var cm = a.TraitOrDefault<ConditionManager>();
			if (cm != null)
				cm.RevokeCondition(a, token);
		}
	}
}
