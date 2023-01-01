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

namespace OpenRA.Mods.Common.Lint
{
	public class CheckMapCordon : ILintMapPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			if (map.Bounds.Left == 0 || map.Bounds.Top == 0
				|| map.Bounds.Right == map.MapSize.X || map.Bounds.Bottom == map.MapSize.Y)
				emitError("This map does not define a valid cordon.\n"
					+ "A one cell (or greater) border is required on all four sides "
					+ "between the playable bounds and the map edges");
		}
	}
}
