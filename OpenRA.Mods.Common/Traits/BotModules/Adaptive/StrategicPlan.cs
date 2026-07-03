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

namespace OpenRA.Mods.Common.Traits
{
	public enum AdaptiveGoal
	{
		None,
		Scout,
		CounterAir,
		CounterArmor,
		Defend,
		Expand,
		Attack,
		Tech
	}

	public sealed class StrategicPlan
	{
		public AdaptiveGoal ActiveGoal = AdaptiveGoal.None;
		public float AttackReadiness;
		public bool ExpansionAllowed = true;
		public bool ScoutActive;
		public string DefensePriority;
		public Dictionary<string, int> UnitMixOverrides = [];
		public Dictionary<string, int> BuildingBoosts = [];
	}
}
