#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	using CellContents = D2kResourceLayer.CellContents;
	using ClearSides = D2kResourceLayer.ClearSides;

	[Desc("Used to render spice with round borders.")]
	public class D2kEditorResourceLayerInfo : EditorResourceLayerInfo
	{
		public override object Create(ActorInitializer init) { return new D2kEditorResourceLayer(init.Self); }
	}

	public class D2kEditorResourceLayer : EditorResourceLayer
	{
		public D2kEditorResourceLayer(Actor self)
			: base(self) { }

		public override CellContents UpdateDirtyTile(CPos c)
		{
			var t = Tiles[c];

			// Empty tile
			if (t.Type == null)
			{
				t.Sprite = null;
				return t;
			}

			NetWorth -= t.Density * t.Type.Info.ValuePerUnit;

			t.Density = ResourceDensityAt(c);

			NetWorth += t.Density * t.Type.Info.ValuePerUnit;

			int index;
			var clear = FindClearSides(t.Type, c);
			if (clear == ClearSides.None)
			{
				var sprites = D2kResourceLayer.Variants[t.Variant];
				var frame = t.Density > t.Type.Info.MaxDensity / 2 ? 1 : 0;
				t.Sprite = t.Type.Variants.First().Value[sprites[frame]];
			}
			else if (D2kResourceLayer.SpriteMap.TryGetValue(clear, out index))
				t.Sprite = t.Type.Variants.First().Value[index];
			else
				t.Sprite = null;

			return t;
		}

		protected override string ChooseRandomVariant(ResourceType t)
		{
			return D2kResourceLayer.Variants.Keys.Random(Game.CosmeticRandom);
		}

		bool CellContains(CPos c, ResourceType t)
		{
			return Tiles.Contains(c) && Tiles[c].Type == t;
		}

		ClearSides FindClearSides(ResourceType t, CPos p)
		{
			var ret = ClearSides.None;
			if (!CellContains(p + new CVec(0, -1), t))
				ret |= ClearSides.Top | ClearSides.TopLeft | ClearSides.TopRight;

			if (!CellContains(p + new CVec(-1, 0), t))
				ret |= ClearSides.Left | ClearSides.TopLeft | ClearSides.BottomLeft;

			if (!CellContains(p + new CVec(1, 0), t))
				ret |= ClearSides.Right | ClearSides.TopRight | ClearSides.BottomRight;

			if (!CellContains(p + new CVec(0, 1), t))
				ret |= ClearSides.Bottom | ClearSides.BottomLeft | ClearSides.BottomRight;

			if (!CellContains(p + new CVec(-1, -1), t))
				ret |= ClearSides.TopLeft;

			if (!CellContains(p + new CVec(1, -1), t))
				ret |= ClearSides.TopRight;

			if (!CellContains(p + new CVec(-1, 1), t))
				ret |= ClearSides.BottomLeft;

			if (!CellContains(p + new CVec(1, 1), t))
				ret |= ClearSides.BottomRight;

			return ret;
		}
	}
}
