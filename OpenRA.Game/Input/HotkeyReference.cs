#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA
{
	/// <summary>
	/// A reference to either a named hotkey (defined in the game settings) or a statically assigned hotkey
	/// </summary>
	public class HotkeyReference
	{
		static readonly Func<Hotkey> Invalid = () => Hotkey.Invalid;

		readonly Func<Hotkey> getValue;

		public HotkeyReference()
		{
			getValue = Invalid;
		}

		public HotkeyReference(string name, KeySettings settings)
		{
			// Try parsing the value as a reference to a named hotkey
			getValue = settings.GetHotkeyReference(name);

			if (getValue == null)
			{
				// Try parsing the value as a normal (static) hotkey
				var staticKey = Hotkey.Invalid;
				Hotkey.TryParse(name, out staticKey);
				getValue = () => staticKey;
			}
		}

		public Hotkey GetValue()
		{
			return getValue();
		}
	}
}
