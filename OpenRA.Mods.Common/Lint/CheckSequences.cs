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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckSequences : ILintRulesPass
	{
		void ILintRulesPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			// Custom maps define rules.Sequences, default mod rules leave it null.
			if (rules.Sequences == null)
			{
				foreach (var kv in modData.DefaultSequences)
				{
					Console.WriteLine("Testing default sequences for {0}", kv.Key);
					Run(emitError, emitWarning, rules, kv.Value);
				}
			}
			else if (!modData.DefaultSequences.Values.Contains(rules.Sequences))
				Run(emitError, emitWarning, rules, rules.Sequences);
		}

		void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules, SequenceProvider sequences)
		{
			var factions = rules.Actors[SystemActors.World].TraitInfos<FactionInfo>().Select(f => f.InternalName).ToArray();
			foreach (var actorInfo in rules.Actors)
			{
				// Catch TypeDictionary errors
				try
				{
					var images = new HashSet<string>();

					// Actors may have 0 or 1 RenderSprites traits
					var renderInfo = actorInfo.Value.TraitInfoOrDefault<RenderSpritesInfo>();
					if (renderInfo != null)
					{
						images.Add(renderInfo.GetImage(actorInfo.Value, null).ToLowerInvariant());

						// Some actors define faction-specific artwork
						foreach (var faction in factions)
							images.Add(renderInfo.GetImage(actorInfo.Value, faction).ToLowerInvariant());
					}

					foreach (var traitInfo in actorInfo.Value.TraitInfos<TraitInfo>())
					{
						// Remove the "Info" suffix
						var traitName = traitInfo.GetType().Name;
						traitName = traitName.Remove(traitName.Length - 4);

						var fields = traitInfo.GetType().GetFields();
						foreach (var field in fields)
						{
							var sequenceReference = field.GetCustomAttributes<SequenceReferenceAttribute>(true).FirstOrDefault();
							if (sequenceReference == null)
								continue;

							// Some sequences may specify their own Image override
							IEnumerable<string> sequenceImages = images;
							if (!string.IsNullOrEmpty(sequenceReference.ImageReference))
							{
								var imageField = fields.First(f => f.Name == sequenceReference.ImageReference);
								var imageOverride = (string)imageField.GetValue(traitInfo);
								if (string.IsNullOrEmpty(imageOverride))
								{
									if (!sequenceReference.AllowNullImage)
										emitError($"Actor type `{actorInfo.Value.Name}` trait `{traitName}` must define a value for `{sequenceReference.ImageReference}`");

									continue;
								}

								sequenceImages = new[] { imageOverride.ToLowerInvariant() };
							}

							foreach (var sequence in LintExts.GetFieldValues(traitInfo, field, sequenceReference.DictionaryReference))
							{
								if (string.IsNullOrEmpty(sequence))
									continue;

								foreach (var i in sequenceImages)
								{
									if (sequenceReference.Prefix)
									{
										// TODO: Remove prefixed sequence references and instead use explicit lists of lintable references
										if (!sequences.Sequences(i).Any(s => s.StartsWith(sequence)))
											emitWarning($"Actor type `{actorInfo.Value.Name}` trait `{traitName}` field `{field.Name}` defines a prefix `{sequence}` that does not match any sequences on image `{i}`.");
									}
									else if (!sequences.HasSequence(i, sequence))
										emitError($"Actor type `{actorInfo.Value.Name}` trait `{traitName}` field `{field.Name}` references an undefined sequence `{sequence}` on image `{i}`.");
								}
							}
						}
					}
				}
				catch (InvalidOperationException e)
				{
					emitError($"{e.Message} (Actor type `{actorInfo.Key}`)");
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
					var sequenceReference = field.GetCustomAttributes<SequenceReferenceAttribute>(true).FirstOrDefault();
					if (sequenceReference == null)
						continue;

					// All weapon sequences must specify their corresponding image
					var image = ((string)fields.First(f => f.Name == sequenceReference.ImageReference).GetValue(projectileInfo));
					if (string.IsNullOrEmpty(image))
					{
						if (!sequenceReference.AllowNullImage)
							emitError($"Weapon type `{weaponInfo.Key}` projectile field `{sequenceReference.ImageReference}` must define a value");

						continue;
					}

					image = image.ToLowerInvariant();
					foreach (var sequence in LintExts.GetFieldValues(projectileInfo, field, sequenceReference.DictionaryReference))
					{
						if (string.IsNullOrEmpty(sequence))
							continue;

						if (sequenceReference.Prefix)
						{
							// TODO: Remove prefixed sequence references and instead use explicit lists of lintable references
							if (!sequences.Sequences(image).Any(s => s.StartsWith(sequence)))
								emitWarning($"Weapon type `{weaponInfo.Key}` projectile field `{field.Name}` defines a prefix `{sequence}` that does not match any sequences on image `{image}`.");
						}
						else if (!sequences.HasSequence(image, sequence))
							emitError($"Weapon type `{weaponInfo.Key}` projectile field `{field.Name}` references an undefined sequence `{sequence}` on image `{image}`.");
					}
				}
			}
		}
	}
}
