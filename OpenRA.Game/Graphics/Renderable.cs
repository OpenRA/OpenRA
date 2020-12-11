#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public interface IRenderable
	{
		WPos Pos { get; }
		int ZOffset { get; }
		bool IsDecoration { get; }

		IRenderable WithZOffset(int newOffset);
		IRenderable OffsetBy(WVec offset);
		IRenderable AsDecoration();

		IFinalizedRenderable PrepareRender(WorldRenderer wr);
	}

	public interface IPalettedRenderable : IRenderable
	{
		PaletteReference Palette { get; }
		IPalettedRenderable WithPalette(PaletteReference newPalette);
	}

	[Flags]
	public enum TintModifiers
	{
		None = 0,
		IgnoreWorldTint = 1,
		ReplaceColor = 2
	}

	public interface IModifyableRenderable : IRenderable
	{
		float Alpha { get; }
		float3 Tint { get; }
		TintModifiers TintModifiers { get; }

		IModifyableRenderable WithAlpha(float newAlpha);
		IModifyableRenderable WithTint(in float3 newTint, TintModifiers newTintModifiers);
	}

	public interface IFinalizedRenderable
	{
		void Render(WorldRenderer wr);
		void RenderDebugGeometry(WorldRenderer wr);
		Rectangle ScreenBounds(WorldRenderer wr);
	}
}
