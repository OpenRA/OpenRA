#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA
{
	// List of keycodes, duplicated from SDL 2.0.1
	public enum Keycode
	{
		UNKNOWN = 0,
		RETURN = 40,
		KP_ENTER = 88,
		TAB = 186,
		ESCAPE = 41,
		LALT = 226,
		RIGHT = 79,
		LEFT = 80,
		DOWN = 81,
		UP = 82,
		HOME = 74,
		END = 77,
		DELETE = 76,
		BACKSPACE = 42,

		A = 4,
		B = 5,
		C = 6,
		D = 7,
		E = 8,
		F = 9,
		G = 10,
		H = 11,
		I = 12,
		J = 13,
		K = 14,
		L = 15,
		M = 16,
		N = 17,
		O = 18,
		P = 19,
		Q = 20,
		R = 21,
		S = 22,
		T = 23,
		U = 24,
		V = 25,
		W = 26,
		X = 27,
		Y = 28,
		Z = 29,

		NUMBER_1 = 30,
		NUMBER_2 = 31,
		NUMBER_3 = 32,
		NUMBER_4 = 33,
		NUMBER_5 = 34,
		NUMBER_6 = 35,
		NUMBER_7 = 36,
		NUMBER_8 = 37,
		NUMBER_9 = 38,
		NUMBER_0 = 39,

		RSHIFT = 229,
		LSHIFT = 225,
		RCTRL = 228,
		LCTRL = 224,
		RALT = 230,
		RGUI = 231,
		LGUI = 227,

		SPACE = 44,
		PAUSE = 72,
		PAGEDOWN = 78,
		PAGEUP = 75,
		MINUS = 45,
		EQUALS = 46,
		COMMA = 54,
		PERIOD = 55,

		F1 = 58,
		F2 = 59,
		F3 = 60,
		F4 = 61,
		F5 = 62,
		F6 = 63,
		F7 = 64,
		F8 = 65,
		F9 = 66,
		F10 = 67,
		F11 = 68,
		F12 = 69
	}

	public static class KeycodeExts
	{
		static readonly Dictionary<Keycode, string> KeyNames = new Dictionary<Keycode, string>
		{
			{ Keycode.UNKNOWN, "Unknown" },
			{ Keycode.RETURN, "Return" },
			{ Keycode.ESCAPE, "Escape" },
			{ Keycode.BACKSPACE, "Backspace" },
			{ Keycode.TAB, "Tab" },
			{ Keycode.SPACE, "Space" },
			{ Keycode.COMMA, "," },
			{ Keycode.MINUS, "-" },
			{ Keycode.PERIOD, "." },
			{ Keycode.NUMBER_0, "0" },
			{ Keycode.NUMBER_1, "1" },
			{ Keycode.NUMBER_2, "2" },
			{ Keycode.NUMBER_3, "3" },
			{ Keycode.NUMBER_4, "4" },
			{ Keycode.NUMBER_5, "5" },
			{ Keycode.NUMBER_6, "6" },
			{ Keycode.NUMBER_7, "7" },
			{ Keycode.NUMBER_8, "8" },
			{ Keycode.NUMBER_9, "9" },
			{ Keycode.EQUALS, "=" },
			{ Keycode.A, "A" },
			{ Keycode.B, "B" },
			{ Keycode.C, "C" },
			{ Keycode.D, "D" },
			{ Keycode.E, "E" },
			{ Keycode.F, "F" },
			{ Keycode.G, "G" },
			{ Keycode.H, "H" },
			{ Keycode.I, "I" },
			{ Keycode.J, "J" },
			{ Keycode.K, "K" },
			{ Keycode.L, "L" },
			{ Keycode.M, "M" },
			{ Keycode.N, "N" },
			{ Keycode.O, "O" },
			{ Keycode.P, "P" },
			{ Keycode.Q, "Q" },
			{ Keycode.R, "R" },
			{ Keycode.S, "S" },
			{ Keycode.T, "T" },
			{ Keycode.U, "U" },
			{ Keycode.V, "V" },
			{ Keycode.W, "W" },
			{ Keycode.X, "X" },
			{ Keycode.Y, "Y" },
			{ Keycode.Z, "Z" },
			{ Keycode.F1, "F1" },
			{ Keycode.F2, "F2" },
			{ Keycode.F3, "F3" },
			{ Keycode.F4, "F4" },
			{ Keycode.F5, "F5" },
			{ Keycode.F6, "F6" },
			{ Keycode.F7, "F7" },
			{ Keycode.F8, "F8" },
			{ Keycode.F9, "F9" },
			{ Keycode.F10, "F10" },
			{ Keycode.F11, "F11" },
			{ Keycode.F12, "F12" },
			{ Keycode.PAUSE, "Pause" },
			{ Keycode.HOME, "Home" },
			{ Keycode.PAGEUP, "PageUp" },
			{ Keycode.DELETE, "Delete" },
			{ Keycode.END, "End" },
			{ Keycode.PAGEDOWN, "PageDown" },
			{ Keycode.RIGHT, "Right" },
			{ Keycode.LEFT, "Left" },
			{ Keycode.DOWN, "Down" },
			{ Keycode.UP, "Up" },
			{ Keycode.KP_ENTER, "Keypad Enter" },
			{ Keycode.LCTRL, "Left Ctrl" },
			{ Keycode.LSHIFT, "Left Shift" },
			{ Keycode.LALT, "Left Alt" },
			{ Keycode.LGUI, "Left GUI" },
			{ Keycode.RCTRL, "Right Ctrl" },
			{ Keycode.RSHIFT, "Right Shift" },
			{ Keycode.RALT, "Right Alt" },
			{ Keycode.RGUI, "Right GUI" }
		};

		public static string DisplayString(Keycode k)
		{
			string ret;
			if (!KeyNames.TryGetValue(k, out ret))
				return k.ToString();

			return ret;
		}
	}
}
