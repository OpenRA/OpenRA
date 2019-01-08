#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;

namespace OpenRA.Mods.Common.Activities
{
	public class RemoveSelf : Activity
	{
		public override Activity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			self.Dispose();
			return null;
		}
	}
}
