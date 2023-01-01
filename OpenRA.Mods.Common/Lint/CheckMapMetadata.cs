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
using OpenRA.Server;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckMapMetadata : ILintMapPass, ILintServerMapPass
	{
		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			Run(emitError, map.MapFormat, map.Author, map.Title, map.Categories);
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules)
		{
			Run(emitError, map.MapFormat, map.Author, map.Title, map.Categories);
		}

		void Run(Action<string> emitError, int mapFormat, string author, string title, string[] categories)
		{
			if (mapFormat < Map.SupportedMapFormat)
				emitError($"Map format {mapFormat} does not match the supported version {Map.CurrentMapFormat}.");

			if (author == null)
				emitError("Map does not define a valid author.");

			if (title == null)
				emitError("Map does not define a valid title.");

			if (categories.Length == 0)
				emitError("Map does not define any categories.");
		}
	}
}
