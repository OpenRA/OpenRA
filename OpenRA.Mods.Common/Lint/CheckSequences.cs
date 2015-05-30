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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckSequences : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			var sequences = MiniYaml.MergeLiberal(map.SequenceDefinitions, Game.ModData.Manifest.Sequences.Select(s => MiniYaml.FromFile(s)).Aggregate(MiniYaml.MergeLiberal));
			var races = map.Rules.Actors["world"].Traits.WithInterface<CountryInfo>().Select(c => c.Race);

			foreach (var actorInfo in map.Rules.Actors)
			{
				foreach (var renderInfo in actorInfo.Value.Traits.WithInterface<RenderSpritesInfo>())
				{
					foreach (var race in races)
					{
						var image = renderInfo.GetImage(actorInfo.Value, map.Rules.Sequences[map.Tileset], race);
						if (!sequences.Any(s => s.Key == image.ToLowerInvariant()) && !actorInfo.Value.Name.Contains("^"))
							emitWarning("Sprite image {0} from actor {1} on tileset {2} using race {3} has no sequence definition."
								.F(image, actorInfo.Value.Name, map.Tileset, race));
					}
				}
			}
		}
	}
}
