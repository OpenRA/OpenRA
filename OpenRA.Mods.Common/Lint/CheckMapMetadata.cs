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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckMapMetadata : ILintMapPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			if (map.MapFormat != Map.SupportedMapFormat)
				emitError("Map format {0} does not match the supported version {1}."
					.F(map.MapFormat, Map.SupportedMapFormat));

			if (map.Author == null)
				emitError("Map does not define a valid author.");

			if (map.Title == null)
				emitError("Map does not define a valid title.");

			if (!map.Categories.Any())
				emitError("Map does not define any categories.");
		}
	}
}