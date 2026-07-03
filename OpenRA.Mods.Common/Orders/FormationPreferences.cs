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

using OpenRA.Mods.Common.Widgets;

namespace OpenRA.Mods.Common.Orders
{
	/// <summary>
	/// Client-side formation preference. Not synced; move orders carry resolved destination cells.
	/// </summary>
	public static class FormationPreferences
	{
		public static FormationType Selected { get; set; } = FormationType.Default;
		public static FormationSpacing SelectedSpacing { get; set; } = FormationSpacing.Normal;
		public static bool OrangePreviewEnabled { get; set; } = true;

		static DropDownButtonWidget formationDropdown;

		public static void SetFormationDropdown(DropDownButtonWidget button)
		{
			formationDropdown = button;
		}

		/// <summary>Close the formation dropdown if open. Returns true when ESC should be consumed.</summary>
		public static bool TryCloseOpenDropdown()
		{
			if (formationDropdown == null || !formationDropdown.IsPanelOpen)
				return false;

			formationDropdown.RemovePanel();
			return true;
		}
	}
}
