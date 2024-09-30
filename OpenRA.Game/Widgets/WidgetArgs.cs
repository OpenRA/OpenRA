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

namespace OpenRA.Widgets
{
	public class WidgetArgs : Dictionary<string, object>
	{
		public WidgetArgs() { }
		public WidgetArgs(Dictionary<string, object> args)
			: base(args) { }
		public void Add(string key, Action val) { base.Add(key, val); }
	}
}
