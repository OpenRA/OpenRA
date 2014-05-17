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
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class CheckSequences : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			var sequences = MiniYaml.MergeLiberal(map.SequenceDefinitions,
				Game.modData.Manifest.Sequences.Select(s => MiniYaml.FromFile(s))
				.Aggregate(MiniYaml.MergeLiberal));

			foreach (var actorInfo in map.Rules.Actors)
				foreach (var renderInfo in actorInfo.Value.Traits.WithInterface<RenderSimpleInfo>())
				{
					var image = renderInfo.Image ?? actorInfo.Value.Name;
					if (!sequences.Any(s => s.Key == image.ToLower()) && !actorInfo.Value.Name.Contains("^"))
						emitWarning("Sprite image {0} from actor {1} has no sequence definition.".F(image, actorInfo.Value.Name));
				}
		}
	}
}
