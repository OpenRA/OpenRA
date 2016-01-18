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
	class CheckSequences : ILintMapPass
	{
		Action<string> emitError;

		List<MiniYamlNode> sequenceDefinitions;

		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			if (map != null && !map.SequenceDefinitions.Any())
				return;

			this.emitError = emitError;

			var sequenceSource = map != null ? map.SequenceDefinitions : new List<MiniYamlNode>();
			var partial = Game.ModData.Manifest.Sequences
				.Select(MiniYaml.FromFile)
				.Aggregate(MiniYaml.MergePartial);
			sequenceDefinitions = MiniYaml.Merge(sequenceSource, partial);

			var rules = map == null ? Game.ModData.DefaultRules : map.Rules;
			var factions = rules.Actors["world"].TraitInfos<FactionInfo>().Select(f => f.InternalName).ToArray();
			var sequenceProviders = map == null ? rules.Sequences.Values : new[] { rules.Sequences[map.Tileset] };

			foreach (var actorInfo in rules.Actors)
			{
				foreach (var renderInfo in actorInfo.Value.TraitInfos<RenderSpritesInfo>())
				{
					foreach (var faction in factions)
					{
						foreach (var sequenceProvider in sequenceProviders)
						{
							var image = renderInfo.GetImage(actorInfo.Value, sequenceProvider, faction);
							if (sequenceDefinitions.All(s => s.Key != image.ToLowerInvariant()) && !actorInfo.Value.Name.Contains("^"))
								emitError("Sprite image {0} from actor {1} using faction {2} has no sequence definition."
									.F(image, actorInfo.Value.Name, faction));
						}
					}
				}

				foreach (var traitInfo in actorInfo.Value.TraitInfos<ITraitInfo>())
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

								var renderInfo = actorInfo.Value.TraitInfos<RenderSpritesInfo>().FirstOrDefault();
								if (renderInfo == null)
									continue;

								foreach (var faction in factions)
								{
									var sequenceReference = field.GetCustomAttributes<SequenceReferenceAttribute>(true).FirstOrDefault();
									if (sequenceReference != null && !string.IsNullOrEmpty(sequenceReference.ImageReference))
									{
										var imageField = fields.FirstOrDefault(f => f.Name == sequenceReference.ImageReference);
										if (imageField != null)
										{
											foreach (var imageOverride in LintExts.GetFieldValues(traitInfo, imageField, emitError))
											{
												if (!string.IsNullOrEmpty(imageOverride) && sequenceDefinitions.All(s => s.Key != imageOverride.ToLowerInvariant()))
													emitError("Custom sprite image {0} from actor {1} has no sequence definition.".F(imageOverride, actorInfo.Value.Name));
												else
													CheckDefintions(imageOverride, sequenceReference, actorInfo, sequence, faction, field, traitInfo);
											}
										}
									}
									else
									{
										foreach (var sequenceProvider in sequenceProviders)
										{
											var image = renderInfo.GetImage(actorInfo.Value, sequenceProvider, faction);
											CheckDefintions(image, sequenceReference, actorInfo, sequence, faction, field, traitInfo);
										}
									}
								}
							}
						}
					}
				}

				foreach (var weaponInfo in rules.Weapons)
				{
					var projectileInfo = weaponInfo.Value.Projectile;
					if (projectileInfo == null)
						continue;

					var fields = projectileInfo.GetType().GetFields();
					foreach (var field in fields)
					{
						if (field.HasAttribute<SequenceReferenceAttribute>())
						{
							var sequences = LintExts.GetFieldValues(projectileInfo, field, emitError);
							foreach (var sequence in sequences)
							{
								if (string.IsNullOrEmpty(sequence))
									continue;

								var sequenceReference = field.GetCustomAttributes<SequenceReferenceAttribute>(true).FirstOrDefault();
								if (sequenceReference != null && !string.IsNullOrEmpty(sequenceReference.ImageReference))
								{
									var imageField = fields.FirstOrDefault(f => f.Name == sequenceReference.ImageReference);
									if (imageField != null)
									{
										foreach (var imageOverride in LintExts.GetFieldValues(projectileInfo, imageField, emitError))
										{
											if (!string.IsNullOrEmpty(imageOverride))
											{
												var definitions = sequenceDefinitions.FirstOrDefault(n => n.Key == imageOverride.ToLowerInvariant());
												if (definitions == null)
													emitError("Can't find sequence definition for projectile image {0} at weapon {1}.".F(imageOverride, weaponInfo.Key));
												else if (!definitions.Value.Nodes.Any(n => n.Key == sequence))
													emitError("Projectile sprite image {0} from weapon {1} does not define sequence {2} from field {3} of {4}"
														.F(imageOverride, weaponInfo.Key, sequence, field.Name, projectileInfo));
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		void CheckDefintions(string image, SequenceReferenceAttribute sequenceReference,
			KeyValuePair<string, ActorInfo> actorInfo, string sequence, string faction, FieldInfo field, ITraitInfo traitInfo)
		{
			var definitions = sequenceDefinitions.FirstOrDefault(n => n.Key == image.ToLowerInvariant());
			if (definitions != null)
			{
				if (sequenceReference != null && sequenceReference.Prefix)
				{
					if (!definitions.Value.Nodes.Any(n => n.Key.StartsWith(sequence)))
						emitError("Sprite image {0} from actor {1} of faction {2} does not define sequence prefix {3} from field {4} of {5}"
							.F(image, actorInfo.Value.Name, faction, sequence, field.Name, traitInfo));
				}
				else if (definitions.Value.Nodes.All(n => n.Key != sequence))
				{
					emitError("Sprite image {0} from actor {1} of faction {2} does not define sequence {3} from field {4} of {5}"
						.F(image, actorInfo.Value.Name, faction, sequence, field.Name, traitInfo));
				}
			}
		}
	}
}
