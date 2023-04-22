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
using OpenRA.Mods.Common.MapFormats;
using OpenRA.Server;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckMapMetadata : ILintMapPass, ILintServerMapPass
	{
		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, IMap imap)
		{
			var map = (Map)imap;
			Run(emitError, map.Author, map.Title, map.Categories, (imap as DefaultMap).MapFormat);
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview mapPreview, Ruleset mapRules)
		{
			Run(emitError, mapPreview.Author, mapPreview.Title, mapPreview.Categories, mapPreview.MapFormat);
		}

		void Run(Action<string> emitError, string author, string title, string[] categories, int mapFormat)
		{
			if (mapFormat < DefaultMap.SupportedMapFormat)
				emitError($"Map format {mapFormat} does not match the supported version {DefaultMap.CurrentMapFormat}.");

			if (author == null)
				emitError("Map does not define a valid author.");

			if (title == null)
				emitError("Map does not define a valid title.");

			if (categories.Length == 0)
				emitError("Map does not define any categories.");
		}
	}
}
