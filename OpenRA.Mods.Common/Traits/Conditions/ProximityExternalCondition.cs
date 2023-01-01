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
	[Desc("Applies a condition to actors within a specified range.")]
	public class ProximityExternalConditionInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		[Desc("The range to search for actors.")]
		public readonly WDist Range = WDist.FromCells(3);

		[Desc("The maximum vertical range above terrain to search for actors.",
		"Ignored if 0 (actors are selected regardless of vertical distance).")]
		public readonly WDist MaximumVerticalOffset = WDist.Zero;

		[Desc("What player relationships are affected.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[Desc("Condition is applied permanently to this actor.")]
		public readonly bool AffectsParent = false;

		public readonly string EnableSound = null;
		public readonly string DisableSound = null;

		public override object Create(ActorInitializer init) { return new ProximityExternalCondition(init.Self, this); }
	}

	public class ProximityExternalCondition : ConditionalTrait<ProximityExternalConditionInfo>, ITick, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOtherProduction
	{
		readonly Actor self;

		readonly Dictionary<Actor, int> tokens = new Dictionary<Actor, int>();

		int proximityTrigger;
		WPos cachedPosition;
		WDist cachedRange;
		WDist desiredRange;
		WDist cachedVRange;
		WDist desiredVRange;

		public ProximityExternalCondition(Actor self, ProximityExternalConditionInfo info)
			: base(info)
		{
			this.self = self;
			cachedRange = WDist.Zero;
			cachedVRange = WDist.Zero;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			cachedPosition = self.CenterPosition;
			proximityTrigger = self.World.ActorMap.AddProximityTrigger(cachedPosition, cachedRange, cachedVRange, ActorEntered, ActorExited);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveProximityTrigger(proximityTrigger);
		}

		protected override void TraitEnabled(Actor self)
		{
			Game.Sound.Play(SoundType.World, Info.EnableSound, self.CenterPosition);
			desiredRange = Info.Range;
			desiredVRange = Info.MaximumVerticalOffset;
		}

		protected override void TraitDisabled(Actor self)
		{
			Game.Sound.Play(SoundType.World, Info.DisableSound, self.CenterPosition);
			desiredRange = WDist.Zero;
			desiredVRange = WDist.Zero;
		}

		void ITick.Tick(Actor self)
		{
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

			if (a == self && !Info.AffectsParent)
				return;

			if (tokens.ContainsKey(a))
				return;

			var relationship = self.Owner.RelationshipWith(a.Owner);
			if (!Info.ValidRelationships.HasRelationship(relationship))
				return;

			var external = a.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == Info.Condition && t.CanGrantCondition(self));

			if (external != null)
				tokens[a] = external.GrantCondition(a, self);
		}

		public void UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)
		{
			// If the produced Actor doesn't occupy space, it can't be in range
			if (produced.OccupiesSpace == null)
				return;

			// We don't grant conditions when disabled
			if (IsTraitDisabled)
				return;

			// Work around for actors produced within the region not triggering until the second tick
			if ((produced.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= Info.Range.LengthSquared)
			{
				var stance = self.Owner.RelationshipWith(produced.Owner);
				if (!Info.ValidRelationships.HasRelationship(stance))
					return;

				var external = produced.TraitsImplementing<ExternalCondition>()
					.FirstOrDefault(t => t.Info.Condition == Info.Condition && t.CanGrantCondition(self));

				if (external != null)
					tokens[produced] = external.GrantCondition(produced, self);
			}
		}

		void ActorExited(Actor a)
		{
			if (a.Disposed)
				return;

			if (!tokens.TryGetValue(a, out var token))
				return;

			tokens.Remove(a);
			foreach (var external in a.TraitsImplementing<ExternalCondition>())
				if (external.TryRevokeCondition(a, self, token))
					break;
		}
	}
}
