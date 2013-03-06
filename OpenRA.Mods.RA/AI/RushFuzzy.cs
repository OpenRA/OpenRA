#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AI.Fuzzy.Library;

namespace OpenRA.Mods.RA.AI
{
	class RushFuzzy : AttackOrFleeFuzzy
	{
		public RushFuzzy() : base() { }

		protected override void AddingRulesForNormalOwnHealth()
		{
			//1 OwnHealth is Normal
			fuzzyEngine.Rules.Add(fuzzyEngine.ParseRule("if ((OwnHealth is Normal) " +
							"and ((EnemyHealth is NearDead) or (EnemyHealth is Injured) or (EnemyHealth is Normal)) " +
							"and (RelativeAttackPower is Strong) " +
							"and ((RelativeSpeed is Slow) or (RelativeSpeed is Equal) or (RelativeSpeed is Fast))) " +
							"then AttackOrFlee is Attack"));

			fuzzyEngine.Rules.Add(fuzzyEngine.ParseRule("if ((OwnHealth is Normal) " +
							"and ((EnemyHealth is NearDead) or (EnemyHealth is Injured) or (EnemyHealth is Normal)) " +
							"and ((RelativeAttackPower is Weak) or (RelativeAttackPower is Equal)) " +
							"and ((RelativeSpeed is Slow) or (RelativeSpeed is Equal) or (RelativeSpeed is Fast))) " +
							"then AttackOrFlee is Flee"));
		}
	}
}
