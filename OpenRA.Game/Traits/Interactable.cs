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

using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Traits
{
	[Desc("Used to enable mouse interaction on actors that are not Selectable.")]
	public class InteractableInfo : ITraitInfo, IMouseBoundsInfo, IDecorationBoundsInfo
	{
		[Desc("Defines a custom rectangle for mouse interaction with the actor.",
			"If null, the engine will guess an appropriate size based on the With*Body trait.",
			"The first two numbers define the width and height of the rectangle.",
			"The (optional) second two numbers define an x and y offset from the actor center.")]
		public readonly int[] Bounds = null;

		[Desc("Defines a custom rectangle for Decorations (e.g. the selection box).",
			"If null, Bounds will be used instead")]
		public readonly int[] DecorationBounds = null;

		public virtual object Create(ActorInitializer init) { return new Interactable(this); }
	}

	public class Interactable : INotifyCreated, IMouseBounds, IDecorationBounds
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

		Rectangle Bounds(Actor self, WorldRenderer wr, int[] bounds)
		{
			if (bounds == null)
				return AutoBounds(self, wr);

			var size = new int2(bounds[0], bounds[1]);

			var offset = -size / 2;
			if (bounds.Length > 2)
				offset += new int2(bounds[2], bounds[3]);

			var xy = wr.ScreenPxPosition(self.CenterPosition) + offset;
			return new Rectangle(xy.X, xy.Y, size.X, size.Y);
		}

		Rectangle IMouseBounds.MouseoverBounds(Actor self, WorldRenderer wr)
		{
			return Bounds(self, wr, info.Bounds);
		}

		Rectangle IDecorationBounds.DecorationBounds(Actor self, WorldRenderer wr)
		{
			return Bounds(self, wr, info.DecorationBounds ?? info.Bounds);
		}
	}
}
