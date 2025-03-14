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
		readonly float trailLengthMinusSkip;

		public ContrailRenderable(
			World world, Actor owner, Color startcolor, bool usePlayerStartColor, Color endcolor, bool usePlayerEndColor,
			WDist startWidth, WDist endWidth, int length, int skip, int zOffset)
			: this(world, owner, new WPos[length], startWidth, endWidth, 0, 0, skip,
				  startcolor, usePlayerStartColor, endcolor, usePlayerEndColor, zOffset)
		{ }

		ContrailRenderable(World world, Actor owner, WPos[] trail, WDist startWidth, WDist endWidth,
			int next, int length, int skip, Color startColor, bool usePlayerStartColor, Color endColor, bool usePlayerEndColor, int zOffset)
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
			trailLengthMinusSkip = trail.Length - skip - 1;
			ZOffset = zOffset;
		}

		public WPos Pos => trail[Index(next - 1)];
		public int ZOffset { get; }
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset) =>
			new ContrailRenderable(
				world, owner, (WPos[])trail.Clone(), startWidth, endWidth, next,
				length, skip, startColor, usePlayerStartColor, endColor, usePlayerEndColor, newOffset);
		public IRenderable OffsetBy(in WVec vec)
		{
			// Lambdas can't use 'in' variables, so capture a copy for later
			var offset = vec;
			return new ContrailRenderable(
				world, owner, trail.Select(pos => pos + offset).ToArray(), startWidth, endWidth, next,
				length, skip, startColor, usePlayerStartColor, endColor, usePlayerEndColor, ZOffset);
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

			var startColor = usePlayerStartColor ? Color.FromArgb(this.startColor.A, owner.OwnerColor()) : this.startColor;
			var endColor = usePlayerEndColor ? Color.FromArgb(this.endColor.A, owner.OwnerColor()) : this.endColor;

			var curPos = trail[Index(next - skip - 1)];
			var curColor = startColor;
			var curWidth = startWidth.Length * screenWidth;
			var endr = float3.Zero;
			var endl = float3.Zero;

			for (var i = 1; i < renderLength; i++)
			{
				var pos = curPos;
				var j = next - skip - 1 - i;
				var prepos = trail[Index(j)];
				var trailVec = prepos - pos;
				var trailLength = (float)trailVec.Length;

				WPos nextPos;
				if (trailLength == 0 || renderLength == 2)
					nextPos = curPos;
				else
				{
					// Smooth the line segment by averaging the direction of the previous segments.
					for (var k = 1; k < renderLength - i && k < MaxSmoothLength; k++)
					{
						prepos = trail[Index(j - k)];
						trailVec += prepos - pos;
						pos = prepos;
					}

					// Normalize the vector to the length of the trail.
					var length = (float)trailVec.Length;
					nextPos = length == 0
						? curPos
						: new WPos(
							curPos.X + (int)(trailVec.X * (float)(trailLength / length)),
							curPos.Y + (int)(trailVec.Y * (float)(trailLength / length)),
							curPos.Z + (int)(trailVec.Z * (float)(trailLength / length)));
				}

				var start = wr.Screen3DPosition(curPos);
				var end = wr.Screen3DPosition(nextPos);

				// Use positions from the last segment to draw the first segment, to avoid gaps.
				var delta = (end - start) / (end - start).XY.Length;
				float3 startl, startr;
				if (i == 1)
				{
					var startCorner = curWidth * 0.5f * new float3(-delta.Y, delta.X, delta.Z);
					startl = start - startCorner + RgbaColorRenderer.Offset;
					startr = start + startCorner + RgbaColorRenderer.Offset;
				}
				else
				{
					startl = endl;
					startr = endr;
				}

				var lerp = i / trailLengthMinusSkip;
				var width = (startWidth.Length * (1 - lerp) + endWidth.Length * lerp) * screenWidth;
				if (width < 0)
					width = 0;

				var endCorner = width * 0.5f * new float3(-delta.Y, delta.X, delta.Z);
				endr = end + endCorner + RgbaColorRenderer.Offset;
				endl = end - endCorner + RgbaColorRenderer.Offset;

				var nextColor = Exts.ColorLerp(lerp, startColor, endColor);
				if ((width != 0 || curWidth != 0)
					&& curPos != nextPos
					&& !world.FogObscures(curPos) && !world.FogObscures(nextPos))
					wcr.DrawLine(startl, startr, endr, endl, curColor, nextColor);

				curPos = nextPos;
				curColor = nextColor;
				curWidth = width;
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
