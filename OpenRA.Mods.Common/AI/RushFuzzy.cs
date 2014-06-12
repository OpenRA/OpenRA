#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.AI
{
	class RushFuzzy : AttackOrFleeFuzzy
	{
		protected override void AddingRulesForNormalOwnHealth()
		{
			AddFuzzyRule("if ((OwnHealth is Normal) " +
				"and ((EnemyHealth is NearDead) or (EnemyHealth is Injured) or (EnemyHealth is Normal)) " +
				"and (RelativeAttackPower is Strong) " +
				"and ((RelativeSpeed is Slow) or (RelativeSpeed is Equal) or (RelativeSpeed is Fast))) " +
				"then AttackOrFlee is Attack");

			AddFuzzyRule("if ((OwnHealth is Normal) " +
				"and ((EnemyHealth is NearDead) or (EnemyHealth is Injured) or (EnemyHealth is Normal)) " +
				"and ((RelativeAttackPower is Weak) or (RelativeAttackPower is Equal)) " +
				"and ((RelativeSpeed is Slow) or (RelativeSpeed is Equal) or (RelativeSpeed is Fast))) " +
				"then AttackOrFlee is Flee");
		}
	}
}
