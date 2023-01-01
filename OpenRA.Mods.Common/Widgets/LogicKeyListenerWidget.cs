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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class LogicKeyListenerWidget : Widget
	{
		readonly List<Func<KeyInput, bool>> handlers = new List<Func<KeyInput, bool>>();

		public override bool HandleKeyPress(KeyInput e)
		{
			foreach (var handler in handlers)
				if (handler(e))
					return true;

			return false;
		}

		public void AddHandler(Func<KeyInput, bool> func)
		{
			handlers.Add(func);
		}
	}
}
