#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class DockableInfo : ConditionalTraitInfo, Requires<DockManagerInfo>
	{
		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which the regular (non-force) enter cursor is disabled.")]
		public readonly BooleanExpression RequireForceMoveCondition = null;
	}

	public abstract class Dockable<InfoType> : ConditionalTrait<InfoType>, IDockable, IObservesVariables
		where InfoType : DockableInfo
	{
		public readonly Actor Self;
		protected readonly Mobile Mobile;

		bool requireForceMove;

		public BitSet<DockType> MyDockType => DockType();
		public bool IsAliveAndInWorld => !IsTraitDisabled && !Self.IsDead && Self.IsInWorld && !Self.Disposed;
		Actor IDockable.Self => Self;

		readonly DockManager dockManager;
		public DockManager DockManager => dockManager;

		protected abstract BitSet<DockType> DockType();

		public Dockable(Actor self, InfoType info)
			: base(info)
		{
			Self = self;
			Mobile = self.TraitOrDefault<Mobile>();
			dockManager = self.Trait<DockManager>();
		}

		public virtual bool CanDock()
		{
			return true;
		}

		public bool DockingPossible(BitSet<DockType> type)
		{
			return CanDock() && DockType().Overlaps(type);
		}

		public bool DockingPossible(BitSet<DockType> type, TargetModifiers modifiers)
		{
			if (requireForceMove && !modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			return DockingPossible(type);
		}

		public bool CanDockAt(Dock target, bool allowedToForceEnter)
		{
			if (DockingPossible(target.MyDockType) && target.CanDock(this, allowedToForceEnter))
				return allowedToForceEnter || CanDock();

			return false;
		}

		public virtual void DockStarted(Dock dock) { }

		public virtual bool TickDock(Dock dock) { return false; }

		IEnumerable<VariableObserver> IObservesVariables.GetVariableObservers()
		{
			if (Info.RequireForceMoveCondition != null)
				yield return new VariableObserver(RequireForceMoveConditionChanged, Info.RequireForceMoveCondition.Variables);
		}

		void RequireForceMoveConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			requireForceMove = Info.RequireForceMoveCondition.Evaluate(conditions);
		}
	}
}
