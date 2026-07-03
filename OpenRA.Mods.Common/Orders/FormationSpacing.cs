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

namespace OpenRA.Mods.Common.Orders
{
	public enum FormationSpacing
	{
		Tight,
		Normal,
		Medium,
		Far,
	}

	public static class FormationSpacingExtensions
	{
		public static int Apply(this FormationSpacing spacing, int unitFootprint)
		{
			var fp = Math.Max(1, unitFootprint);

			return spacing switch
			{
				FormationSpacing.Tight => fp,
				FormationSpacing.Normal => fp + 1,
				FormationSpacing.Medium => fp + 2,
				FormationSpacing.Far => fp + 3,
				_ => fp + 1,
			};
		}
	}
}
