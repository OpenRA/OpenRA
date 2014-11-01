#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CheckMapCordon : ILintPass
	{
		const int RecommendedCordonSize = 12;

		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			if (map.Bounds.Left == 0 || map.Bounds.Top == 0
				|| map.Bounds.Right == map.MapSize.X || map.Bounds.Bottom == map.MapSize.Y)
					emitError("This map does not define a valid cordon.\n"
						+"A one cell (or greater) border is required on all four sides "
						+"between the playable bounds and the map edges");

			if (map.Bounds.Left < RecommendedCordonSize || map.Bounds.Top < RecommendedCordonSize
				|| map.MapSize.X - map.Bounds.Right < RecommendedCordonSize
				|| map.MapSize.Y - map.Bounds.Bottom < RecommendedCordonSize)
					emitWarning("This map defines a map cordon < {0} cells which may lead to problems.".F(RecommendedCordonSize));
		}
	}
}

