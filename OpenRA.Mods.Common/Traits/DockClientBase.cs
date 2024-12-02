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

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class DockClientBaseInfo : ConditionalTraitInfo, IDockClientInfo, Requires<IDockClientManagerInfo> { }

	public abstract class DockClientBase<InfoType> : ConditionalTrait<InfoType>, IDockClient, INotifyCreated where InfoType : DockClientBaseInfo
	{
		readonly Actor self;

		public abstract BitSet<DockType> GetDockType { get; }
		public DockClientManager DockClientManager { get; }

		protected DockClientBase(Actor self, InfoType info)
			: base(info)
		{
			this.self = self;
			DockClientManager = self.TraitOrDefault<DockClientManager>();
		}

		public virtual bool CanDock(BitSet<DockType> type, bool forceEnter = false)
		{
			return !IsTraitDisabled && GetDockType.Overlaps(type);
		}

		public virtual bool CanDockAt(Actor hostActor, IDockHost host, bool forceEnter = false, bool ignoreOccupancy = false)
		{
			return CanDock(host.GetDockType, forceEnter)
				&& host.IsDockingPossible(self, this, ignoreOccupancy);
		}

		public virtual bool CanQueueDockAt(Actor hostActor, IDockHost host, bool forceEnter, bool isQueued)
		{
			return CanDock(host.GetDockType, true)
				&& host.IsDockingPossible(self, this, true);
		}

		public virtual void OnDockStarted(Actor self, Actor hostActor, IDockHost host) { }

		public virtual bool OnDockTick(Actor self, Actor hostActor, IDockHost host) { return false; }

		public virtual void OnDockCompleted(Actor self, Actor hostActor, IDockHost host) { }
	}
}
