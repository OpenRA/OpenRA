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

namespace OpenRA.Mods.Cnc.Traits
{
	[Flags]
	public enum AttackRequires
	{
		None = 0,
		CargoLoaded = 1,
		Disguised = 2,
	}

	[Desc("Adds metadata for the " + nameof(SendUnitToAttackBotModule) + ".")]
	public sealed class UnitAttackOptions
	{
		[Desc("Base desire provided for attack desire for each of this unit.",
			"When desire reach 100, AI will send them to attack")]
		public readonly int AttackDesireOfEach = 20;

		[Desc("Order used for closing in the target before attack. Left empty for attack directly.")]
		public readonly string MoveToOrderName = null;

		[Desc("Attack order name. Used for actor to attack target.")]
		public readonly string AttackOrderName = "Attack";

		[Desc("Order used for moving the unit back to where it was after attack. Left empty for no return.")]
		public readonly string MoveBackOrderName = null;

		[Desc("Disguise before attack, if possible.")]
		public readonly bool TryDisguise = false;

		[Desc("Repaired before attack, if possible.")]
		public readonly bool TryGetHealed = true;

		[Desc("Filters units don't meet the requirements. Possible values are None, CargoLoaded, MustDisguised.")]
		public readonly AttackRequires AttackRequires = AttackRequires.None;

		public UnitAttackOptions(MiniYaml yaml)
		{
			FieldLoader.Load(this, yaml);
		}
	}
}
