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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor can be captured by units in a specified proximity.")]
	public abstract class ProximityCapturableBaseInfo : TraitInfo, IRulesetLoaded
	{
		[Desc("Allowed " + nameof(ProximityCaptor) + " actors to capture this actor.")]
		public readonly BitSet<CaptureType> CaptorTypes = new("Player", "Vehicle", "Tank", "Infantry");

		[Desc("If set, the capturing process stops immediately after another player comes into range.")]
		public readonly bool MustBeClear = false;

		[Desc("If set, the ownership will not revert back when the captor leaves the area.")]
		public readonly bool Sticky = false;

		[Desc("If set, the actor can only be captured via this logic once.",
			"This option implies the `" + nameof(Sticky) + "` behaviour as well.")]
		public readonly bool Permanent = false;

		[Desc("If set, will draw a border in the owner's color around the capturable area.")]
		public readonly bool DrawDecoration = true;

		public void RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			var pci = rules.Actors[SystemActors.Player].TraitInfoOrDefault<ProximityCaptorInfo>();
			if (pci == null)
				throw new YamlException(
					nameof(ProximityCapturableBase) + " requires the `" + nameof(Player) + "` actor to have the " + nameof(ProximityCaptor) + " trait.");
		}

		public abstract override object Create(ActorInitializer init);
	}

	public abstract class ProximityCapturableBase : ITick, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOwnerChanged, IRenderAnnotations
	{
		public readonly Player OriginalOwner;
		public bool Captured => Self.Owner != OriginalOwner;

		public ProximityCapturableBaseInfo Info;
		public Actor Self;

		readonly List<Actor> actorsInRange = [];
		protected int trigger;
		WPos prevPosition;
		bool skipTriggerUpdate;

		protected ProximityCapturableBase(ActorInitializer init, ProximityCapturableBaseInfo info)
		{
			Info = info;
			Self = init.Self;
			OriginalOwner = Self.Owner;
		}

		protected abstract int CreateTrigger(Actor self);
		protected abstract void RemoveTrigger(Actor self, int trigger);
		protected abstract void TickInner(Actor self);
		protected abstract IRenderable GetRenderable(Actor self, WorldRenderer wr);

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			if (skipTriggerUpdate)
				return;

			trigger = CreateTrigger(self);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (skipTriggerUpdate)
				return;

			RemoveTrigger(self, trigger);
			actorsInRange.Clear();
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsInWorld || self.CenterPosition == prevPosition)
				return;

			TickInner(self);
			prevPosition = self.CenterPosition;
		}

		protected void ActorEntered(Actor other)
		{
			if (skipTriggerUpdate || !CanBeCapturedBy(other))
				return;

			actorsInRange.Add(other);
			UpdateOwnership();
		}

		protected void ActorLeft(Actor other)
		{
			if (skipTriggerUpdate || !CanBeCapturedBy(other))
				return;

			actorsInRange.Remove(other);
			UpdateOwnership();
		}

		bool CanBeCapturedBy(Actor a)
		{
			if (a == Self)
				return false;

			var pc = a.Info.TraitInfoOrDefault<ProximityCaptorInfo>();
			return pc != null && pc.Types.Overlaps(Info.CaptorTypes);
		}

		void UpdateOwnership()
		{
			if (Captured && Info.Permanent)
			{
				// This area has been captured and cannot ever be re-captured, so we get rid of the
				// trigger and ensure that it won't be recreated in AddedToWorld.
				skipTriggerUpdate = true;
				RemoveTrigger(Self, trigger);

				return;
			}

			// The actor that has been in the area the longest will be the captor.
			// The previous implementation used the closest one, but that doesn't work with
			// ProximityTriggers since they only generate events when actors enter or leave.
			var captor = actorsInRange.FirstOrDefault();

			// The last unit left the area
			if (captor == null)
			{
				// Unless the Sticky option is set, we revert to the original owner.
				if (Captured && !Info.Sticky)
					ChangeOwnership(Self, OriginalOwner.PlayerActor);
			}
			else
			{
				if (Info.MustBeClear)
				{
					var isClear = actorsInRange.All(a => captor.Owner.RelationshipWith(a.Owner) == PlayerRelationship.Ally);

					// An enemy unit has wandered into the area, so we've lost control of it.
					if (Captured && !isClear)
						ChangeOwnership(Self, OriginalOwner.PlayerActor);

					// We don't own the area yet, but it is clear from enemy units, so we take possession of it.
					else if (Self.Owner != captor.Owner && isClear)
						ChangeOwnership(Self, captor);
				}
				else
				{
					// In all other cases, we just take over.
					if (Self.Owner != captor.Owner)
						ChangeOwnership(Self, captor);
				}
			}
		}

		void ChangeOwnership(Actor self, Actor captor)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (self.Disposed || captor.Disposed)
					return;

				// prevent (Added|Removed)FromWorld from firing during Actor.ChangeOwner
				skipTriggerUpdate = true;
				var previousOwner = self.Owner;
				self.ChangeOwner(captor.Owner);

				if (self.Owner == self.World.LocalPlayer)
					w.Add(new FlashTarget(self, Color.White));

				var pc = captor.Info.TraitInfoOrDefault<ProximityCaptorInfo>();
				foreach (var t in self.TraitsImplementing<INotifyCapture>())
					t.OnCapture(self, captor, previousOwner, captor.Owner, pc.Types);
			});
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			Game.RunAfterTick(() => skipTriggerUpdate = false);
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!self.IsInWorld || !Info.DrawDecoration)
				return [];

			return [GetRenderable(self, wr)];
		}

		bool IRenderAnnotations.SpatiallyPartitionable { get { return false; } }
	}
}
