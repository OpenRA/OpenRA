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
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Traits
{
	static class EditorCellStatusOverlay
	{
		public static readonly float3 ValidTint = new float3(0, 255, 0) / 255f;
		public static readonly float3 InvalidTint = new float3(255, 0, 0) / 255f;
		public static readonly TintModifiers TintModifiers = TintModifiers.ReplaceColor | TintModifiers.IgnoreWorldTint;

		public static Sprite GetOverlaySprite(World world, string image, string sequence)
		{
			var tileset = world.Map.Tileset.ToLowerInvariant();
			var sequences = world.Map.Sequences;
			var sequenceName = sequences.HasSequence(image, $"{sequence}-{tileset}")
				? $"{sequence}-{tileset}"
				: sequence;

			return sequences.GetSequence(image, sequenceName).GetSprite(0);
		}

		public static float GetOverlayScale(World world, string image, string sequence)
		{
			var tileset = world.Map.Tileset.ToLowerInvariant();
			var sequences = world.Map.Sequences;
			var sequenceName = sequences.HasSequence(image, $"{sequence}-{tileset}")
				? $"{sequence}-{tileset}"
				: sequence;

			return sequences.GetSequence(image, sequenceName).Scale;
		}

		public static void RenderCells(
			WorldRenderer wr,
			Func<CPos, bool> isValid,
			Sprite sprite,
			float scale,
			float alpha,
			PaletteReference palette)
		{
			var map = wr.World.Map;

			foreach (var uv in wr.Viewport.AllVisibleCells.CandidateMapCoords)
			{
				if (!map.Contains(uv))
					continue;

				var cell = uv.ToCPos(map);
				var ramp = map.Grid.Ramps[map.Ramp[cell]];
				var pos = map.CenterOfCell(cell);
				var offset = new WVec(0, 0, -ramp.CenterHeightOffset);
				var tint = isValid(cell) ? ValidTint : InvalidTint;

				new SpriteRenderable(sprite, pos, offset, -511, palette, scale, alpha, tint, TintModifiers, false)
					.Render(wr);
			}
		}
	}
}
