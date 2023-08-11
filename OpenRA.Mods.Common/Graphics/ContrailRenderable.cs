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

namespace OpenRA.Mods.Common.Graphics
{
	public class ContrailRenderable : IRenderable, IFinalizedRenderable
	{
		const int MaxSmoothLength = 4;

		public int Length => trail.Length;

		readonly Actor owner;
		readonly World world;
		readonly Color startColor;
		readonly bool usePlayerStartColor;
		readonly Color endColor;
		readonly bool usePlayerEndColor;

		// Store trail positions in a circular buffer
		readonly WPos[] trail;
		readonly WDist startWidth;
		readonly WDist endWidth;
		int next;
		int length;
		readonly int skip;

		public ContrailRenderable(World world, Actor owner, Color startcolor, bool usePlayerStartColor, Color endcolor, bool usePlayerEndColor, WDist startWidth, WDist endWidth, int length, int skip, int zOffset)
			: this(world, owner, new WPos[length], startWidth, endWidth, 0, 0, skip, startcolor, usePlayerStartColor, endcolor, usePlayerEndColor, zOffset) { }

		ContrailRenderable(World world, Actor owner, WPos[] trail, WDist startWidth, WDist endWidth, int next, int length, int skip, Color startColor, bool usePlayerStartColor, Color endColor, bool usePlayerEndColor, int zOffset)
		{
			this.world = world;
			this.owner = owner;
			this.trail = trail;
			this.startWidth = startWidth;
			this.endWidth = endWidth;
			this.next = next;
			this.length = length;
			this.skip = skip;
			this.startColor = startColor;
			this.usePlayerStartColor = usePlayerStartColor;
			this.usePlayerEndColor = usePlayerEndColor;
			this.endColor = endColor;
			ZOffset = zOffset;
		}

		public WPos Pos => trail[Index(next - 1)];
		public int ZOffset { get; }
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset) { return new ContrailRenderable(world, owner, (WPos[])trail.Clone(), startWidth, endWidth, next, length, skip, startColor, usePlayerStartColor, endColor, usePlayerEndColor, newOffset); }
		public IRenderable OffsetBy(in WVec vec)
		{
			// Lambdas can't use 'in' variables, so capture a copy for later
			var offset = vec;
			return new ContrailRenderable(world, owner, trail.Select(pos => pos + offset).ToArray(), startWidth, endWidth, next, length, skip, startColor, usePlayerStartColor, endColor, usePlayerEndColor, ZOffset);
		}

		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			// Note: The length of contrail is now actually the number of the points to draw the contrail
			// and we require at least two points to draw a tail.
			var renderLength = length - skip;
			if (renderLength <= 1)
				return;

			var screenWidth = wr.ScreenVector(new WVec(1, 0, 0))[0];
			var wcr = Game.Renderer.WorldRgbaColorRenderer;

			var startColor = this.startColor;
			if (usePlayerStartColor)
				startColor = Color.FromArgb(this.startColor.A, owner.OwnerColor());

			var endColor = this.endColor;
			if (usePlayerEndColor)
				endColor = Color.FromArgb(this.endColor.A, owner.OwnerColor());

			// Start of the first line segment is the tail of the list - don't smooth it.
			var curPos = trail[Index(next - skip - 1)];
			var curColor = startColor;

			for (var i = 1; i < renderLength; i++)
			{
				var j = next - skip - 1 - i;
				var nextColor = Exts.ColorLerp(i / (renderLength - 1f), startColor, endColor);

				var nextX = 0L;
				var nextY = 0L;
				var nextZ = 0L;
				var k = 0;
				for (; k < renderLength - i && k < MaxSmoothLength; k++)
				{
					var prepos = trail[Index(j - k)];
					nextX += prepos.X;
					nextY += prepos.Y;
					nextZ += prepos.Z;
				}

				var nextPos = new WPos((int)(nextX / k), (int)(nextY / k), (int)(nextZ / k));

				// When renderLength = 2 we are rendering only one segment, so it needs to be handled differently to avoid
				// division by 0. For width we choose startWidth instead of the average as this makes the transition between
				// rendering 1 and multiple segments smoother. Above checks make sure that renderLength can never be lower than 2.
				float width;
				if (renderLength == 2)
					width = startWidth.Length;
				else
				{
					var lerp = (i - 1f) / (renderLength - 2);
					width = startWidth.Length * (1 - lerp) + endWidth.Length * lerp;
				}

				if (width > 0 && !world.FogObscures(curPos) && !world.FogObscures(nextPos))
					wcr.DrawLine(wr.Screen3DPosition(curPos), wr.Screen3DPosition(nextPos), screenWidth * width, curColor, nextColor);

				curPos = nextPos;
				curColor = nextColor;
			}
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }

		// Array index modulo length
		int Index(int i)
		{
			var j = i % trail.Length;
			return j < 0 ? j + trail.Length : j;
		}

		public void Update(WPos pos)
		{
			trail[next] = pos;
			next = Index(next + 1);

			if (length < trail.Length)
				length++;
		}
	}
}
