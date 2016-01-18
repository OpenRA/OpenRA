#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckMapMetadata : ILintMapPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			if (map.MapFormat < Map.MinimumSupportedMapFormat)
				emitError("Map format {0} is older than the minimum supported version {1}."
					.F(map.MapFormat, Map.MinimumSupportedMapFormat));

			if (map.Author == null)
				emitError("Map does not define a valid author.");

			if (map.Title == null)
				emitError("Map does not define a valid title.");

			if (map.Type == null)
				emitError("Map does not define a valid type.");
		}
	}
}