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

		internal HotkeyReference(Func<Hotkey> getValue)
		{
			this.getValue = getValue;
		}

		public Hotkey GetValue()
		{
			return getValue();
		}

		public bool IsActivatedBy(KeyInput e)
		{
			var currentValue = getValue();
			return currentValue.Key == e.Key && currentValue.Modifiers == e.Modifiers;
		}
	}
}
