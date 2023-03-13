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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Server;

namespace OpenRA.Mods.Common.Lint
{
	sealed class CheckInteractable : ILintRulesPass, ILintServerMapPass
	{
		void ILintRulesPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			Run(emitError, rules, modData);
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules)
		{
			Run(emitError, mapRules, modData);
		}

		static void Run(Action<string> emitError, Ruleset rules, ModData modData)
		{
			// As the map has not been created we need to get MapGrid info directly from manifest.
			var grid = modData.Manifest.Get<MapGrid>();
			foreach (var actorInfo in rules.Actors)
			{
				// Catch TypeDictionary errors.
				try
				{
					var interactable = actorInfo.Value.TraitInfoOrDefault<InteractableInfo>();
					if (interactable == null)
						continue;

					if (HasInvalidBounds(interactable.Bounds, grid.TileSize, grid.TileScale))
						emitError($"{actorInfo.Key}.{interactable.GetType().Name}.{nameof(interactable.Bounds)} are empty or negative.");

					if (HasInvalidBounds(interactable.DecorationBounds, grid.TileSize, grid.TileScale))
						emitError($"{actorInfo.Key}.{interactable.GetType().Name}.{nameof(interactable.DecorationBounds)} are empty or negative.");
				}
				catch (InvalidOperationException e)
				{
					emitError($"{e.Message} (Actor type `{actorInfo.Key}`).");
				}
			}
		}

		static bool HasInvalidBounds(WDist[] bounds, Size tileSize, int tileScale)
		{
			if (bounds == null)
				return false;

			if (bounds.Length != 2 && bounds.Length != 4)
				return true;

			var size = new int2(bounds[0].Length * tileSize.Width / tileScale, bounds[1].Length * tileSize.Height / tileScale);
			return size.X <= 0 || size.Y <= 0;
		}
	}
}
