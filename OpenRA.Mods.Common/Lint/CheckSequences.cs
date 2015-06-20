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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckSequences : ILintPass
	{
		Action<string> emitWarning;

		List<MiniYamlNode> sequenceDefinitions;

		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			this.emitWarning = emitWarning;

			sequenceDefinitions = MiniYaml.MergeLiberal(map.SequenceDefinitions,
				Game.ModData.Manifest.Sequences.Select(s => MiniYaml.FromFile(s)).Aggregate(MiniYaml.MergeLiberal));

			var races = map.Rules.Actors["world"].Traits.WithInterface<CountryInfo>().Select(c => c.Race);

			foreach (var actorInfo in map.Rules.Actors)
			{
				foreach (var renderInfo in actorInfo.Value.Traits.WithInterface<RenderSpritesInfo>())
				{
					foreach (var race in races)
					{
						var image = renderInfo.GetImage(actorInfo.Value, map.Rules.Sequences[map.Tileset], race);
						if (!sequenceDefinitions.Any(s => s.Key == image.ToLowerInvariant()) && !actorInfo.Value.Name.Contains("^"))
							emitWarning("Sprite image {0} from actor {1} on tileset {2} using race {3} has no sequence definition."
								.F(image, actorInfo.Value.Name, map.Tileset, race));
					}
				}

				foreach (var traitInfo in actorInfo.Value.Traits.WithInterface<ITraitInfo>())
				{
					var fields = traitInfo.GetType().GetFields();
					foreach (var field in fields)
					{
						if (field.HasAttribute<SequenceReferenceAttribute>())
						{
							var sequences = LintExts.GetFieldValues(traitInfo, field, emitError);
							foreach (var sequence in sequences)
							{
								if (string.IsNullOrEmpty(sequence))
									continue;

								var renderInfo = actorInfo.Value.Traits.WithInterface<RenderSpritesInfo>().FirstOrDefault();
								if (renderInfo == null)
									continue;

								foreach (var race in races)
								{
									var sequenceReference = field.GetCustomAttributes<SequenceReferenceAttribute>(true).FirstOrDefault();
									if (sequenceReference != null && !string.IsNullOrEmpty(sequenceReference.ImageReference))
									{
										var imageField = fields.FirstOrDefault(f => f.Name == sequenceReference.ImageReference);
										if (imageField != null)
										{
											foreach (var imageOverride in LintExts.GetFieldValues(traitInfo, imageField, emitError))
											{
												if (!string.IsNullOrEmpty(imageOverride) && !sequenceDefinitions.Any(s => s.Key == imageOverride.ToLowerInvariant()))
													emitWarning("Custom sprite image {0} from actor {1} has no sequence definition.".F(imageOverride, actorInfo.Value.Name));
												else
													CheckDefintions(imageOverride, sequenceReference, actorInfo, sequence, race, field, traitInfo);
											}
										}
									}
									else
									{
										var image = renderInfo.GetImage(actorInfo.Value, map.SequenceProvider, race);
										CheckDefintions(image, sequenceReference, actorInfo, sequence, race, field, traitInfo);
									}
								}
							}
						}
					}
				}
			}
		}

		void CheckDefintions(string image, SequenceReferenceAttribute sequenceReference,
			KeyValuePair<string, ActorInfo> actorInfo, string sequence, string race, FieldInfo field, ITraitInfo traitInfo)
		{
			var definitions = sequenceDefinitions.FirstOrDefault(n => n.Key == image.ToLowerInvariant());
			if (definitions != null)
			{
				if (sequenceReference != null && sequenceReference.Prefix)
				{
					if (!definitions.Value.Nodes.Any(n => n.Key.StartsWith(sequence)))
						emitWarning("Sprite image {0} from actor {1} of faction {2} does not define sequence prefix {3} from field {4} of {5}"
							.F(image, actorInfo.Value.Name, race, sequence, field.Name, traitInfo));
				}
				else if (!definitions.Value.Nodes.Any(n => n.Key == sequence))
				{
					emitWarning("Sprite image {0} from actor {1} of faction {2} does not define sequence {3} from field {4} of {5}"
						.F(image, actorInfo.Value.Name, race, sequence, field.Name, traitInfo));
				}
			}
		}
	}
}
