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

using OpenRA.Traits;

namespace OpenRA.Mods.Tcd.Traits
{
	public enum FormationRoleType
	{
		Armor,
		Line,
		Skirmish,
		Fire,
		Support,
	}

	[Desc("Marks where this actor belongs in a tactical formation.",
		"Actors without this trait are treated as Line, rank 1.")]
	public sealed class FormationRoleInfo : TraitInfo<FormationRole>
	{
		[Desc("Tactical role. Descriptive only for now; Rank decides placement.")]
		public readonly FormationRoleType Role = FormationRoleType.Line;

		[Desc("Rank within the formation. 0 is the front rank, higher numbers stand further back.")]
		public readonly int Rank = 1;
	}

	public sealed class FormationRole { }
}
