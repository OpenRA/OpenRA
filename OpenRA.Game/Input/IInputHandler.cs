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
using System.Collections.Generic;

namespace OpenRA
{
	public interface IInputHandler
	{
		void ModifierKeys(Modifiers mods);
		void OnKeyInput(KeyInput input);
		void OnMouseInput(MouseInput input);
		void OnTextInput(string text);
	}

	public enum MouseInputEvent { Down, Move, Up, Scroll }
	public record struct MouseInput(MouseInputEvent Event, MouseButton Button, int2 Location, int2 Delta, Modifiers Modifiers, int MultiTapCount);

	[Flags]
	public enum MouseButton
	{
		None = 0,
		Left = 1,
		Right = 2,
		Middle = 4
	}

	[Flags]
	public enum Modifiers
	{
		None = 0,
		Shift = 1,
		Alt = 2,
		Ctrl = 4,
		Meta = 8,
	}

	public static class ModifiersExts
	{
		[FluentReference]
		public const string Cmd = "keycode-modifier.cmd";

		[FluentReference(Traits.LintDictionaryReference.Values)]
		public static readonly IReadOnlyDictionary<Modifiers, string> ModifierFluentKeys = new Dictionary<Modifiers, string>()
		{
			{ Modifiers.None, "keycode-modifier.none" },
			{ Modifiers.Shift, "keycode-modifier.shift" },
			{ Modifiers.Alt, "keycode-modifier.alt" },
			{ Modifiers.Ctrl, "keycode-modifier.ctrl" },
			{ Modifiers.Meta, "keycode-modifier.meta" },
		};

		public static string DisplayString(Modifiers m)
		{
			if (m == Modifiers.Meta && Platform.CurrentPlatform == PlatformType.OSX)
				return FluentProvider.GetMessage(Cmd);

			if (!ModifierFluentKeys.TryGetValue(m, out var fluentKey))
				return m.ToString();

			return FluentProvider.GetMessage(fluentKey);
		}
	}

	public enum KeyInputEvent { Down, Up }
	public struct KeyInput
	{
		public KeyInputEvent Event;
		public Keycode Key;
		public Modifiers Modifiers;
		public int MultiTapCount;
		public char UnicodeChar;
		public bool IsRepeat;
	}
}
