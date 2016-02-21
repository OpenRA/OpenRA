#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class LogicKeyListenerWidget : Widget
	{
		public Func<KeyInput, bool> OnKeyPress = _ => false;

		public override bool HandleKeyPress(KeyInput e)
		{
			return OnKeyPress(e);
		}
	}
}
