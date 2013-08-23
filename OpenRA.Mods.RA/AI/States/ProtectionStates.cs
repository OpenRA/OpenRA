#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.AI
{
	class UnitsForProtectionIdleState : GroundStateBase, IState
	{
		public void Enter(Squad owner) { }
		public void Execute(Squad owner) { owner.fsm.ChangeState(new UnitsForProtectionAttackState(), true); }
		public void Exit(Squad owner) { }
	}

	class UnitsForProtectionAttackState : GroundStateBase, IState
	{
		public void Enter(Squad owner) { }

		public void Execute(Squad owner)
		{
			if (owner.IsEmpty) return;
			if (!owner.TargetIsValid)
			{
				var circaPostion = AverageUnitsPosition(owner.units);
				if (circaPostion == null) return;
				owner.Target = owner.bot.FindClosestEnemy(circaPostion.Value.CenterPosition, WRange.FromCells(8));

				if (owner.Target == null)
				{
					owner.fsm.ChangeState(new UnitsForProtectionFleeState(), true);
					return;
				}
			}
			foreach (var a in owner.units)
				owner.world.IssueOrder(new Order("AttackMove", a, false) { TargetLocation = owner.Target.Location });
		}

		public void Exit(Squad owner) { }
	}

	class UnitsForProtectionFleeState : GroundStateBase, IState
	{
		public void Enter(Squad owner) { }

		public void Execute(Squad owner)
		{
			if (owner.IsEmpty) return;

			GoToRandomOwnBuilding(owner);
			owner.fsm.ChangeState(new UnitsForProtectionIdleState(), true);
		}

		public void Exit(Squad owner) { owner.units.Clear(); }
	}
}
