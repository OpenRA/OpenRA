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
	public readonly struct Hotkey : IEquatable<Hotkey>
	{
		public static Hotkey Invalid = new Hotkey(Keycode.UNKNOWN, Modifiers.None);
		public bool IsValid()
		{
			return Key != Keycode.UNKNOWN;
		}

		public readonly Keycode Key;
		public readonly Modifiers Modifiers;

		public static bool TryParse(string s, out Hotkey result)
		{
			result = Invalid;
			if (string.IsNullOrWhiteSpace(s))
				return false;

			var parts = s.Split(' ');

			if (!Enum<Keycode>.TryParse(parts[0], true, out var key))
			{
				if (!int.TryParse(parts[0], out var c))
					return false;
				key = (Keycode)c;
			}

			var mods = Modifiers.None;
			if (parts.Length >= 2)
			{
				var modString = s.Substring(s.IndexOf(' '));
				if (!Enum<Modifiers>.TryParse(modString, true, out mods))
					return false;
			}

			result = new Hotkey(key, mods);
			return true;
		}

		public static Hotkey FromKeyInput(KeyInput ki)
		{
			return new Hotkey(ki.Key, ki.Modifiers);
		}

		public Hotkey(Keycode virtKey, Modifiers mod)
		{
			Key = virtKey;
			Modifiers = mod;
		}

		public static bool operator !=(Hotkey a, Hotkey b) { return !(a == b); }
		public static bool operator ==(Hotkey a, Hotkey b)
		{
			// Unknown keys are never equal
			if (a.Key == Keycode.UNKNOWN)
				return false;

			return a.Key == b.Key && a.Modifiers == b.Modifiers;
		}

		public override int GetHashCode() { return Key.GetHashCode() ^ Modifiers.GetHashCode(); }

		public bool Equals(Hotkey other)
		{
			return other == this;
		}

		public override bool Equals(object obj)
		{
			return obj is Hotkey o && (Hotkey?)o == this;
		}

		public override string ToString() { return $"{Key} {Modifiers.ToString("F")}"; }

		public string DisplayString()
		{
			var ret = KeycodeExts.DisplayString(Key);

			if (Modifiers.HasModifier(Modifiers.Shift))
				ret = "Shift + " + ret;

			if (Modifiers.HasModifier(Modifiers.Alt))
				ret = "Alt + " + ret;

			if (Modifiers.HasModifier(Modifiers.Ctrl))
				ret = "Ctrl + " + ret;

			if (Modifiers.HasModifier(Modifiers.Meta))
				ret = (Platform.CurrentPlatform == PlatformType.OSX ? "Cmd + " : "Meta + ") + ret;

			return ret;
		}
	}
}
