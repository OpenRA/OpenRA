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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Configuration options for the lobby player color picker. Attach this to the world actor.")]
	public class ColorPickerManagerInfo : TraitInfo<ColorPickerManager>, IColorPickerManagerInfo
	{
		[TranslationReference]
		const string PlayerColorTerrain = "notification-player-color-terrain";

		[TranslationReference]
		const string PlayerColorPlayer = "notification-player-color-player";

		[TranslationReference]
		const string InvalidPlayerColor = "notification-invalid-player-color";

		[Desc("Minimum and maximum saturation levels that are valid for use.")]
		public readonly float[] HsvSaturationRange = { 0.3f, 0.95f };

		[Desc("Minimum and maximum value levels that are valid for use.")]
		public readonly float[] HsvValueRange = { 0.3f, 0.95f };

		[Desc("Perceptual color threshold for determining whether two colors are too similar.")]
		public readonly int SimilarityThreshold = 0x50;

		[Desc("List of colors to be displayed in the palette tab.")]
		public readonly Color[] PresetColors = Array.Empty<Color>();

		[ActorReference]
		[Desc("Actor type to show in the color picker. This can be overridden for specific factions with FactionPreviewActors.")]
		public readonly string PreviewActor = null;

		[SequenceReference(dictionaryReference: LintDictionaryReference.Values)]
		[Desc("Actor type to show in the color picker for specific factions. Overrides PreviewActor.",
			"A dictionary of [faction name]: [actor name].")]
		public readonly Dictionary<string, string> FactionPreviewActors = new();

		public bool IsInvalidColor(Color color, IEnumerable<Color> candidateBlockers)
		{
			foreach (var candidate in candidateBlockers)
			{
				// Uses the perceptually based color metric explained by https://www.compuphase.com/cmetric.htm
				// HACK: We provide gamma space colors to a linear space metric. This is not ideal, but
				// it works much better in gamma space. In linear space the metric is too small for dark colors
				// and too large for bright colors. By using gamma we shift the metric to be more uniform.
				// This hack doesn't fully fix bright colors, i.e. it still allows for very similar pinks,
				// greens and reports colors with slightly different saturations as significantly different.
				// TODO: Replace with a model which has image hue remapping in mind.
				var rmean = (color.R + candidate.R) >> 1;

				var rdelta = color.R - candidate.R;
				var gdelta = color.G - candidate.G;
				var bdelta = color.B - candidate.B;

				var weightR = ((512 + rmean) * rdelta * rdelta) >> 8;
				var weightG = 4 * gdelta * gdelta;
				var weightB = ((767 - rmean) * bdelta * bdelta) >> 8;

				if (Math.Sqrt(weightR + weightG + weightB) < SimilarityThreshold)
					return true;
			}

			return false;
		}

		Color MakeValid(float hue, float sat, float val, MersenneTwister random, IEnumerable<Color> terrainColors, IEnumerable<Color> playerColors, Action<string> onError)
		{
			// Clamp saturation without triggering a warning
			// This can only happen due to rounding errors (common) or modified clients (rare)
			sat = sat.Clamp(HsvSaturationRange[0], HsvSaturationRange[1]);
			val = val.Clamp(HsvValueRange[0], HsvValueRange[1]);

			string errorMessage;
			var color = Color.FromAhsv(hue, sat, val);
			if (IsInvalidColor(color, terrainColors))
				errorMessage = PlayerColorTerrain;
			else if (IsInvalidColor(color, playerColors))
				errorMessage = PlayerColorPlayer;
			else
				return color;

			// Move by expanding from the selected color in both directions by a limited amount circling
			// around the hue a bunch of times. This method usually returns a color similar to the selected
			// color and controls the randomness.
			// Exit after 400 iterations to avoid infinite loops.
			for (var i = 2; i < 402; i++)
			{
				color = Color.FromAhsv(
					hue + (i % 2 == 0 ? -1 : 1) * (i / 2) * 0.1f * (0.2f + random.NextFloat()),
					float2.Lerp(HsvSaturationRange[0], HsvSaturationRange[1], random.NextFloat()),
					float2.Lerp(HsvValueRange[0], HsvValueRange[1], random.NextFloat()));

				if (!IsInvalidColor(color, terrainColors) && !IsInvalidColor(color, playerColors))
				{
					onError?.Invoke(errorMessage);
					return color;
				}
			}

			// Failed to find a solution within a reasonable time: return a random color without any validation
			onError?.Invoke(InvalidPlayerColor);
			var randomSat = float2.Lerp(HsvSaturationRange[0], HsvSaturationRange[1], random.NextFloat());
			var randomVal = float2.Lerp(HsvValueRange[0], HsvValueRange[1], random.NextFloat());
			return Color.FromAhsv(random.NextFloat(), randomSat, randomVal);
		}

		#region IColorPickerManagerInfo

		public event Action<Color> OnColorPickerColorUpdate;

		(float SMin, float SMax) IColorPickerManagerInfo.SaturationRange => (HsvSaturationRange[0], HsvSaturationRange[1]);
		(float VMin, float VMax) IColorPickerManagerInfo.ValueRange => (HsvValueRange[0], HsvValueRange[1]);

		Color[] IColorPickerManagerInfo.PresetColors => PresetColors;

		Color IColorPickerManagerInfo.RandomPresetColor(MersenneTwister random, IEnumerable<Color> terrainColors, IEnumerable<Color> playerColors)
		{
			foreach (var color in PresetColors.Shuffle(random))
			{
				// Color may already be taken
				if (!IsInvalidColor(color, terrainColors) && !IsInvalidColor(color, playerColors))
					return color;
			}

			// Fall back to a random non-preset color
			var randomHue = random.NextFloat();
			var randomSat = float2.Lerp(HsvSaturationRange[0], HsvSaturationRange[1], random.NextFloat());
			var randomVal = float2.Lerp(HsvValueRange[0], HsvValueRange[1], random.NextFloat());
			return MakeValid(randomHue, randomSat, randomVal, random, terrainColors, playerColors, null);
		}

		Color IColorPickerManagerInfo.MakeValid(Color color, MersenneTwister random, IEnumerable<Color> terrainColors, IEnumerable<Color> playerColors, Action<string> onError)
		{
			var (_, h, s, v) = color.ToAhsv();
			return MakeValid(h, s, v, random, terrainColors, playerColors, onError);
		}

		Color IColorPickerManagerInfo.RandomValidColor(MersenneTwister random, IEnumerable<Color> terrainColors, IEnumerable<Color> playerColors)
		{
			var h = random.NextFloat();
			var s = float2.Lerp(HsvSaturationRange[0], HsvSaturationRange[1], random.NextFloat());
			var v = float2.Lerp(HsvValueRange[0], HsvValueRange[1], random.NextFloat());
			return MakeValid(h, s, v, random, terrainColors, playerColors, null);
		}

		void IColorPickerManagerInfo.ShowColorDropDown(DropDownButtonWidget dropdownButton, Color initialColor, string initialFaction, WorldRenderer worldRenderer, Action<Color> onExit)
		{
			dropdownButton.RemovePanel();

			// We do not want to force other ColorPickerManager implementations to have an Actor preview.
			// We achieve this by fully encapsulating its initialisation.
			void AddActorPreview(Widget parent)
			{
				var preview = parent.GetOrNull<ActorPreviewWidget>("PREVIEW");
				if (preview == null)
					return;

				if (initialFaction == null || !FactionPreviewActors.TryGetValue(initialFaction, out var actorType))
				{
					if (PreviewActor == null)
						throw new YamlException($"{nameof(ColorPickerManager)} does not define a preview actor" + (initialFaction == null ? "." : $"for faction {initialFaction}."));

					actorType = PreviewActor;
				}

				var actor = worldRenderer.World.Map.Rules.Actors[actorType];

				var td = new TypeDictionary
				{
					new OwnerInit(worldRenderer.World.WorldActor.Owner),
					new FactionInit(worldRenderer.World.WorldActor.Owner.PlayerReference.Faction)
				};

				foreach (var api in actor.TraitInfos<IActorPreviewInitInfo>())
					foreach (var o in api.ActorPreviewInits(actor, ActorPreviewType.ColorPicker))
						td.Add(o);

				preview.SetPreview(actor, td);
			}

			var finalColor = initialColor;
			var colorChooser = Game.LoadWidget(worldRenderer.World, "COLOR_CHOOSER", null, new WidgetArgs()
			{
				{ "onChange", (Action<Color>)(c => { finalColor = c; OnColorPickerColorUpdate(c); }) },
				{ "initialColor", initialColor },
				{ "extraLogic", (Action<Widget>)AddActorPreview },
			});

			dropdownButton.AttachPanel(colorChooser, () => onExit(finalColor));
		}

		#endregion
	}

	public class ColorPickerManager { }
}
