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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Used to render spice with round borders.", "Attach this to the world actor")]
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class D2kResourceRendererInfo : ResourceRendererInfo
	{
		public override object Create(ActorInitializer init) { return new D2kResourceRenderer(init.Self, this); }
	}

	public class D2kResourceRenderer : ResourceRenderer
	{
		[Flags]
		public enum ClearSides : byte
		{
			None = 0x0,
			Left = 0x1,
			Top = 0x2,
			Right = 0x4,
			Bottom = 0x8,

			TopLeft = 0x10,
			TopRight = 0x20,
			BottomLeft = 0x40,
			BottomRight = 0x80,

			All = 0xFF
		}

		public static readonly Dictionary<ClearSides, int> SpriteMap = new Dictionary<ClearSides, int>()
		{
			{ ClearSides.Left | ClearSides.Top | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 2 },
			{ ClearSides.Top | ClearSides.Right | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 3 },
			{ ClearSides.Left | ClearSides.Bottom | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 4 },
			{ ClearSides.Right | ClearSides.Bottom | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 5 },
			{ ClearSides.Left | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 6 },
			{ ClearSides.Right | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 7 },
			{ ClearSides.Top | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 8 },
			{ ClearSides.Bottom | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 9 },
			{ ClearSides.Left | ClearSides.Top | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft, 10 },
			{ ClearSides.Top | ClearSides.Right | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomRight, 11 },
			{ ClearSides.Left | ClearSides.Bottom | ClearSides.TopLeft | ClearSides.BottomLeft | ClearSides.BottomRight, 12 },
			{ ClearSides.Right | ClearSides.Bottom | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 13 },
			{ ClearSides.Left | ClearSides.Top | ClearSides.Right | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 14 },
			{ ClearSides.Left | ClearSides.Right | ClearSides.Bottom | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 15 },
			{ ClearSides.Left | ClearSides.Top | ClearSides.Bottom | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 16 },
			{ ClearSides.Top | ClearSides.Right | ClearSides.Bottom | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 17 },
			{ ClearSides.Top | ClearSides.TopLeft | ClearSides.TopRight, 18 },
			{ ClearSides.Right | ClearSides.TopRight | ClearSides.BottomRight, 19 },
			{ ClearSides.Left | ClearSides.TopLeft | ClearSides.BottomLeft, 20 },
			{ ClearSides.Bottom | ClearSides.BottomLeft | ClearSides.BottomRight, 21 },
			{ ClearSides.TopLeft, 22 },
			{ ClearSides.TopRight, 23 },
			{ ClearSides.BottomLeft, 24 },
			{ ClearSides.BottomRight, 25 },
			{ ClearSides.Left | ClearSides.TopLeft | ClearSides.BottomLeft | ClearSides.BottomRight, 26 },
			{ ClearSides.Right | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 27 },
			{ ClearSides.Top | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomRight, 28 },
			{ ClearSides.Top | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft, 29 },
			{ ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 30 },
			{ ClearSides.TopLeft | ClearSides.BottomLeft | ClearSides.BottomRight, 31 },
			{ ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomRight, 32 },
			{ ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft, 33 },
			{ ClearSides.TopRight | ClearSides.BottomRight, 34 },
			{ ClearSides.TopLeft | ClearSides.TopRight, 35 },
			{ ClearSides.TopRight | ClearSides.BottomLeft, 36 },
			{ ClearSides.TopLeft | ClearSides.BottomLeft, 37 },
			{ ClearSides.BottomLeft | ClearSides.BottomRight, 38 },
			{ ClearSides.TopLeft | ClearSides.BottomRight, 39 },
			{ ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 40 },
			{ ClearSides.Left | ClearSides.Right | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 41 },
			{ ClearSides.Top | ClearSides.Bottom | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 42 },
			{ ClearSides.All, 44 },
			{ ClearSides.Left | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomLeft, 46 },
			{ ClearSides.Right | ClearSides.TopLeft | ClearSides.TopRight | ClearSides.BottomRight, 47 },
			{ ClearSides.Bottom | ClearSides.TopRight | ClearSides.BottomLeft | ClearSides.BottomRight, 48 },
			{ ClearSides.Bottom | ClearSides.TopLeft | ClearSides.BottomLeft | ClearSides.BottomRight, 49 },
		};

		public D2kResourceRenderer(Actor self, D2kResourceRendererInfo info)
			: base(self, info) { }

		bool CellContains(CPos cell, string resourceType)
		{
			return RenderContents.Contains(cell) && RenderContents[cell].Type == resourceType;
		}

		ClearSides FindClearSides(CPos cell, string resourceType)
		{
			var ret = ClearSides.None;
			if (!CellContains(cell + new CVec(0, -1), resourceType))
				ret |= ClearSides.Top | ClearSides.TopLeft | ClearSides.TopRight;

			if (!CellContains(cell + new CVec(-1, 0), resourceType))
				ret |= ClearSides.Left | ClearSides.TopLeft | ClearSides.BottomLeft;

			if (!CellContains(cell + new CVec(1, 0), resourceType))
				ret |= ClearSides.Right | ClearSides.TopRight | ClearSides.BottomRight;

			if (!CellContains(cell + new CVec(0, 1), resourceType))
				ret |= ClearSides.Bottom | ClearSides.BottomLeft | ClearSides.BottomRight;

			if (!CellContains(cell + new CVec(-1, -1), resourceType))
				ret |= ClearSides.TopLeft;

			if (!CellContains(cell + new CVec(1, -1), resourceType))
				ret |= ClearSides.TopRight;

			if (!CellContains(cell + new CVec(-1, 1), resourceType))
				ret |= ClearSides.BottomLeft;

			if (!CellContains(cell + new CVec(1, 1), resourceType))
				ret |= ClearSides.BottomRight;

			return ret;
		}

		protected override void UpdateRenderedSprite(CPos cell, RendererCellContents content)
		{
			UpdateRenderedSpriteInner(cell, content);

			var directions = CVec.Directions;
			for (var i = 0; i < directions.Length; i++)
			{
				var neighbour = cell + directions[i];
				if (RenderContents.Contains(neighbour))
					UpdateRenderedSpriteInner(neighbour, RenderContents[neighbour]);
			}
		}

		void UpdateRenderedSpriteInner(CPos cell, RendererCellContents content)
		{
			if (content.Density > 0)
			{
				var clear = FindClearSides(cell, content.Type);

				if (clear == ClearSides.None)
				{
					var maxDensity = ResourceLayer.GetMaxDensity(content.Type);
					var index = content.Density > maxDensity / 2 ? 1 : 0;
					UpdateSpriteLayers(cell, content.Sequence, index, content.Palette);
				}
				else if (SpriteMap.TryGetValue(clear, out var index))
				{
					UpdateSpriteLayers(cell, content.Sequence, index, content.Palette);
				}
				else
					throw new InvalidOperationException($"SpriteMap does not contain an index for ClearSides type '{clear}'");
			}
			else
				UpdateSpriteLayers(cell, null, 0, null);
		}
	}
}
