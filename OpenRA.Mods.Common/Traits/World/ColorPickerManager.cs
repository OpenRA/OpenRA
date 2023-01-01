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
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Configuration options for the lobby player color picker. Attach this to the world actor.")]
	public class ColorPickerManagerInfo : TraitInfo<ColorPickerManager>, IRulesetLoaded
	{
		[TranslationReference]
		const string PlayerColorTerrain = "notification-player-color-terrain";

		[TranslationReference]
		const string PlayerColorPlayer = "notification-player-color-player";

		[TranslationReference]
		const string InvalidPlayerColor = "notification-invalid-player-color";

		[Desc("Minimum and maximum saturation levels that are valid for use.")]
		public readonly float[] HsvSaturationRange = { 0.3f, 0.95f };

		[Desc("HSV value component for player colors.")]
		public readonly float V = 0.95f;

		[Desc("Perceptual color threshold for determining whether two colors are too similar.")]
		public readonly float SimilarityThreshold = 0.314f;

		[Desc("List of hue components for the preset colors in the palette tab. Each entry must have a corresponding PresetSaturations definition.")]
		public readonly float[] PresetHues = Array.Empty<float>();

		[Desc("List of saturation components for the preset colors in the palette tab. Each entry must have a corresponding PresetHues definition.")]
		public readonly float[] PresetSaturations = Array.Empty<float>();

		[ActorReference]
		[Desc("Actor type to show in the color picker. This can be overridden for specific factions with FactionPreviewActors.")]
		public readonly string PreviewActor = null;

		[SequenceReference(dictionaryReference: LintDictionaryReference.Values)]
		[Desc("Actor type to show in the color picker for specific factions. Overrides PreviewActor.",
			"A dictionary of [faction name]: [actor name].")]
		public readonly Dictionary<string, string> FactionPreviewActors = new Dictionary<string, string>();

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (PresetHues.Length != PresetSaturations.Length)
				throw new YamlException("PresetHues and PresetSaturations must have the same number of elements.");
		}

		public IEnumerable<Color> PresetColors()
		{
			for (var i = 0; i < PresetHues.Length; i++)
				yield return Color.FromAhsv(PresetHues[i], PresetSaturations[i], V);
		}

		public Color Color;

		bool TryGetBlockingColor((float R, float G, float B) color, List<(float R, float G, float B)> candidateBlockers, out (float R, float G, float B) closestBlocker)
		{
			var closestDistance = SimilarityThreshold;
			closestBlocker = default;

			foreach (var candidate in candidateBlockers)
			{
				// Uses the perceptually based color metric explained by https://www.compuphase.com/cmetric.htm
				// Input colors are expected to be in the linear (non-gamma corrected) color space
				var rmean = (color.R + candidate.R) / 2.0;
				var r = color.R - candidate.R;
				var g = color.G - candidate.G;
				var b = color.B - candidate.B;
				var weightR = 2.0 + rmean;
				var weightG = 4.0;
				var weightB = 3.0 - rmean;

				var distance = (float)Math.Sqrt(weightR * r * r + weightG * g * g + weightB * b * b);
				if (distance < closestDistance)
				{
					closestBlocker = candidate;
					closestDistance = distance;
				}
			}

			return closestDistance < SimilarityThreshold;
		}

		public Color RandomPresetColor(MersenneTwister random, IEnumerable<Color> terrainColors, IEnumerable<Color> playerColors)
		{
			var terrainLinear = terrainColors.Select(c => c.ToLinear()).ToList();
			var playerLinear = playerColors.Select(c => c.ToLinear()).ToList();

			if (PresetHues.Length > 0)
			{
				foreach (var i in Exts.MakeArray(PresetHues.Length, x => x).Shuffle(random))
				{
					var h = PresetHues[i];
					var s = PresetSaturations[i];
					var preset = Color.FromAhsv(h, s, V);

					// Color may already be taken
					var linear = preset.ToLinear();
					if (!TryGetBlockingColor(linear, terrainLinear, out _) && !TryGetBlockingColor(linear, playerLinear, out _))
						return preset;
				}
			}

			// Fall back to a random non-preset color
			var randomHue = random.NextFloat();
			var randomSat = float2.Lerp(HsvSaturationRange[0], HsvSaturationRange[1], random.NextFloat());
			return MakeValid(randomHue, randomSat, random, terrainLinear, playerLinear, null);
		}

		public Color RandomValidColor(MersenneTwister random, IEnumerable<Color> terrainColors, IEnumerable<Color> playerColors)
		{
			var h = random.NextFloat();
			var s = float2.Lerp(HsvSaturationRange[0], HsvSaturationRange[1], random.NextFloat());
			return MakeValid(h, s, random, terrainColors, playerColors, null);
		}

		public Color MakeValid(Color color, MersenneTwister random, IEnumerable<Color> terrainColors, IEnumerable<Color> playerColors, Action<string> onError = null)
		{
			var (_, h, s, _) = color.ToAhsv();
			return MakeValid(h, s, random, terrainColors, playerColors, onError);
		}

		Color MakeValid(float hue, float sat, MersenneTwister random, IEnumerable<Color> terrainColors, IEnumerable<Color> playerColors, Action<string> onError)
		{
			var terrainLinear = terrainColors.Select(c => c.ToLinear()).ToList();
			var playerLinear = playerColors.Select(c => c.ToLinear()).ToList();

			return MakeValid(hue, sat, random, terrainLinear, playerLinear, onError);
		}

		Color MakeValid(float hue, float sat, MersenneTwister random, List<(float R, float G, float B)> terrainLinear, List<(float R, float G, float B)> playerLinear, Action<string> onError)
		{
			// Clamp saturation without triggering a warning
			// This can only happen due to rounding errors (common) or modified clients (rare)
			sat = sat.Clamp(HsvSaturationRange[0], HsvSaturationRange[1]);

			// Limit to 100 attempts, which is enough to move all the way around the hue range
			string errorMessage = null;
			var stepSign = 0;
			for (var i = 0; i < 101; i++)
			{
				var linear = Color.FromAhsv(hue, sat, V).ToLinear();
				if (TryGetBlockingColor(linear, terrainLinear, out var blocker))
					errorMessage = PlayerColorTerrain;
				else if (TryGetBlockingColor(linear, playerLinear, out blocker))
					errorMessage = PlayerColorPlayer;
				else
				{
					if (errorMessage != null)
						onError?.Invoke(errorMessage);

					return Color.FromAhsv(hue, sat, V);
				}

				// Pick a direction based on the first blocking color and step in hue
				// until we either find a suitable color or loop back to where we started.
				// This is a simple way to avoid being trapped between two blocking colors.
				if (stepSign == 0)
					stepSign = Color.FromLinear(255, blocker.R, blocker.G, blocker.B).ToAhsv().H > hue ? -1 : 1;

				hue += stepSign * 0.01f;
			}

			// Failed to find a solution within a reasonable time: return a random color without any validation
			onError?.Invoke(InvalidPlayerColor);
			return Color.FromAhsv(random.NextFloat(), float2.Lerp(HsvSaturationRange[0], HsvSaturationRange[1], random.NextFloat()), V);
		}
	}

	public class ColorPickerManager { }
}
