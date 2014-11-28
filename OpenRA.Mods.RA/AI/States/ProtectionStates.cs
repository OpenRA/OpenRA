#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.RA.AI
{
	class UnitsForProtectionIdleState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }
		public void Tick(Squad owner) { owner.fsm.ChangeState(owner, new UnitsForProtectionAttackState(), true); }
		public void Deactivate(Squad owner) { }
	}

	class UnitsForProtectionAttackState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.TargetIsValid)
			{
				owner.Target = owner.bot.FindClosestEnemy(owner.CenterPosition, WRange.FromCells(8));

				if (owner.Target == null)
				{
					owner.fsm.ChangeState(owner, new UnitsForProtectionFleeState(), true);
					return;
				}
			}

			foreach (var a in owner.units)
				owner.world.IssueOrder(new Order(OrderCode.AttackMove, a, false) { TargetLocation = owner.Target.Location });
		}

		public void Deactivate(Squad owner) { }
	}

	class UnitsForProtectionFleeState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			GoToRandomOwnBuilding(owner);
			owner.fsm.ChangeState(owner, new UnitsForProtectionIdleState(), true);
		}

		public void Deactivate(Squad owner) { owner.units.Clear(); }
	}
}
