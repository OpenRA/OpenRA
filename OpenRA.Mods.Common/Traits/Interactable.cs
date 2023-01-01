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

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Used to enable mouse interaction on actors that are not Selectable.")]
	public class InteractableInfo : TraitInfo, IMouseBoundsInfo
	{
		[Desc("Defines a custom rectangle for mouse interaction with the actor.",
			"If null, the engine will guess an appropriate size based on the With*Body trait.",
			"The first two numbers define the width and height of the rectangle as a world distance.",
			"The (optional) second two numbers define an x and y offset from the actor center.")]
		public readonly WDist[] Bounds = null;

		[Desc("Defines a custom rectangle for Decorations (e.g. the selection box).",
			"If null, Bounds will be used instead")]
		public readonly WDist[] DecorationBounds = null;

		public override object Create(ActorInitializer init) { return new Interactable(this); }
	}

	public class Interactable : INotifyCreated, IMouseBounds
	{
		readonly InteractableInfo info;
		IAutoMouseBounds[] autoBounds;

		public Interactable(InteractableInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			autoBounds = self.TraitsImplementing<IAutoMouseBounds>().ToArray();
		}

		Rectangle AutoBounds(Actor self, WorldRenderer wr)
		{
			return autoBounds.Select(s => s.AutoMouseoverBounds(self, wr)).FirstOrDefault(r => !r.IsEmpty);
		}

		Polygon Bounds(Actor self, WorldRenderer wr, WDist[] bounds)
		{
			if (bounds == null)
				return new Polygon(AutoBounds(self, wr));

			// Convert from WDist to pixels
			var size = new int2(bounds[0].Length * wr.TileSize.Width / wr.TileScale, bounds[1].Length * wr.TileSize.Height / wr.TileScale);

			var offset = -size / 2;
			if (bounds.Length > 2)
				offset += new int2(bounds[2].Length * wr.TileSize.Width / wr.TileScale, bounds[3].Length * wr.TileSize.Height / wr.TileScale);

			var xy = wr.ScreenPxPosition(self.CenterPosition) + offset;
			return new Polygon(new Rectangle(xy.X, xy.Y, size.X, size.Y));
		}

		Polygon IMouseBounds.MouseoverBounds(Actor self, WorldRenderer wr)
		{
			return Bounds(self, wr, info.Bounds);
		}

		public Rectangle DecorationBounds(Actor self, WorldRenderer wr)
		{
			return Bounds(self, wr, info.DecorationBounds ?? info.Bounds).BoundingRect;
		}
	}
}
