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

using System;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class DockClientBaseInfo : ConditionalTraitInfo, IDockClientInfo, Requires<IDockClientManagerInfo>
	{
		[Desc("Player relationships the owner of the dock host needs, when not forced enter.",
			"Use None to only allow the same owner.")]
		public readonly PlayerRelationship DockRelationships = PlayerRelationship.None;

		[Desc("Player relationships the owner of the dock host needs, when forced enter.",
			"Use None to only allow the same owner.")]
		public readonly PlayerRelationship ForceDockRelationships = PlayerRelationship.Ally;
	}

	public abstract class DockClientBase<InfoType> : ConditionalTrait<InfoType>, IDockClient, INotifyCreated where InfoType : DockClientBaseInfo
	{
		readonly Actor self;

		public abstract BitSet<DockType> GetDockType { get; }
		public DockClientManager DockClientManager { get; }
		readonly Predicate<Actor> hasDockRelationshipWith;
		readonly Predicate<Actor> hasDockForcedRelationshipWith;

		protected DockClientBase(Actor self, InfoType info)
			: base(info)
		{
			this.self = self;
			DockClientManager = self.TraitOrDefault<DockClientManager>();

			if (info.DockRelationships == PlayerRelationship.None)
				hasDockRelationshipWith = a => a.Owner == self.Owner;
			else
				hasDockRelationshipWith = a => info.DockRelationships.HasRelationship(a.Owner.RelationshipWith(self.Owner));

			if (info.ForceDockRelationships == PlayerRelationship.None)
				hasDockForcedRelationshipWith = a => a.Owner == self.Owner;
			else
				hasDockForcedRelationshipWith = a => info.ForceDockRelationships.HasRelationship(a.Owner.RelationshipWith(self.Owner));
		}

		protected virtual bool CanDock()
		{
			return true;
		}

		public virtual bool IsDockingPossible(BitSet<DockType> type, bool? forceEnter = false)
		{
			return !IsTraitDisabled && GetDockType.Overlaps(type) && (!forceEnter.HasValue || forceEnter.Value || CanDock());
		}

		public virtual bool CanDockAt(Actor hostActor, IDockHost host, bool? forceEnter = false, bool ignoreOccupancy = false)
		{
			return (forceEnter.HasValue ? (forceEnter.Value ? hasDockForcedRelationshipWith(hostActor) : hasDockRelationshipWith(hostActor)) : (hasDockForcedRelationshipWith(hostActor) || hasDockRelationshipWith(hostActor)))
				&& IsDockingPossible(host.GetDockType, forceEnter) && host.IsDockingPossible(self, this, ignoreOccupancy);
		}

		public virtual void OnDockStarted(Actor self, Actor hostActor, IDockHost host) { }

		public virtual bool OnDockTick(Actor self, Actor hostActor, IDockHost host) { return false; }

		public virtual void OnDockCompleted(Actor self, Actor hostActor, IDockHost host) { }
	}
}
