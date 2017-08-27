#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Used to render LAT resources.", "Attach this to the world actor")]
	public class VeinRendererInfo : ResourceRendererInfo
	{
		public override object Create(ActorInitializer init) { return new VeinRenderer(init.Self, this); }
	}

	public class VeinRenderer : ResourceRenderer
	{
		readonly VeinRendererInfo info;

		[Flags] public enum ClearSides : byte
		{
			None = 0x0,
			Left = 0x1,
			Top = 0x2,
			Right = 0x4,
			Bottom = 0x8,

			All = 0xFF
		}

		public static readonly Dictionary<ClearSides, int[]> SpriteMap = new Dictionary<ClearSides, int[]>()
		{
			{ ClearSides.All, new int[] { 0, 1, 2 } },
			{ ClearSides.Left | ClearSides.Bottom | ClearSides.Right, new int[] { 3, 4, 5 } },
			{ ClearSides.Top | ClearSides.Left | ClearSides.Bottom, new int[] { 6, 7, 8 } },
			{ ClearSides.Left | ClearSides.Bottom, new int[] { 9, 10, 11 } },
			{ ClearSides.Left | ClearSides.Top | ClearSides.Right, new int[] { 12, 13, 14 } },
			{ ClearSides.Left | ClearSides.Right, new int[] { 15, 16, 17 } },
			{ ClearSides.Left | ClearSides.Top, new int[] { 18, 19, 20 } },
			{ ClearSides.Left, new int[] { 21, 22, 23 } },
			{ ClearSides.Top | ClearSides.Right | ClearSides.Bottom, new int[] { 24, 25, 26 } },
			{ ClearSides.Right | ClearSides.Bottom, new int[] { 27, 28, 29 } },
			{ ClearSides.Top | ClearSides.Bottom, new int[] { 30, 31, 32 } },
			{ ClearSides.Bottom, new int[] { 33, 34, 35 } },
			{ ClearSides.Top | ClearSides.Right, new int[] { 36, 37, 38 } },
			{ ClearSides.Right, new int[] { 39, 40, 41 } },
			{ ClearSides.Top, new int[] { 42, 43, 44 } },
			{ ClearSides.None, new int[] { 45, 46, 47 } }
		};

		public VeinRenderer(Actor self, VeinRendererInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		bool CellContains(CPos c, ResourceType t)
		{
			var type = ResourceLayer.GetResourceType(c);
			return RenderContent.Contains(c) && type == t;
		}

		ClearSides FindClearSides(ResourceType t, CPos p)
		{
			var clearSides = ClearSides.None;
			if (!CellContains(p + new CVec(0, -1), t))
				clearSides |= ClearSides.Top;

			if (!CellContains(p + new CVec(-1, 0), t))
				clearSides |= ClearSides.Left;

			if (!CellContains(p + new CVec(0, 1), t))
				clearSides |= ClearSides.Right;

			if (!CellContains(p + new CVec(0, 1), t))
				clearSides |= ClearSides.Bottom;

			return clearSides;
		}

		void UpdateRenderedTileInner(CPos p)
		{
			if (!RenderContent.Contains(p))
				return;

			var type = ResourceLayer.GetResourceType(p);
			if (type != null && !info.RenderTypes.Contains(type.Info.Type))
				return;

			var renderContent = RenderContent[p];
			var density = ResourceLayer.GetResourceDensity(p);
			if (density > 0)
			{
				var clear = FindClearSides(type, p);
				int[] indices;
				if (SpriteMap.TryGetValue(clear, out indices))
				{
					renderContent.Sprite = type.Variants.First().Value[indices.Random(Game.CosmeticRandom)];
					renderContent.Variant = ChooseRandomVariant(type);
				}
				else
					renderContent.Sprite = null;
			}
			else
				renderContent.Sprite = null;

			RenderContent[p] = renderContent;
		}

		protected override void UpdateRenderedSprite(CPos p)
		{
			UpdateRenderedTileInner(p);

			// update neighbouring tiles
			foreach (var direction in CVec.Directions)
				UpdateRenderedTileInner(p + direction);
		}

		protected override string ChooseRandomVariant(ResourceType t)
		{
			return t.Info.Sequences.First();
		}
	}
}
