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
using System.Linq;
using System.Numerics;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.Graphics
{
	struct Limb
	{
		public float Scale;
		public AABB Bounds;
		public byte[] Size;
		public ModelRenderData RenderData;
	}

	public class Voxel : IModel
	{
		readonly Limb[] limbData;
		readonly Matrix4x4[] transforms;
		readonly uint frames;
		readonly uint limbs;

		uint IModel.Frames => frames;
		uint IModel.Sections => limbs;

		public Voxel(VoxelLoader loader, VxlReader vxl, HvaReader hva, (string Vxl, string Hva) files)
		{
			if (vxl.LimbCount != hva.LimbCount)
				throw new InvalidOperationException($"{files.Vxl}.vxl and {files.Hva}.hva limb counts don't match.");

			transforms = hva.Transforms;
			frames = hva.FrameCount;
			limbs = hva.LimbCount;

			limbData = new Limb[vxl.LimbCount];
			for (var i = 0; i < vxl.LimbCount; i++)
			{
				var vl = vxl.Limbs[i];
				var l = default(Limb);
				l.Scale = vl.Scale;
				l.Bounds = vl.Bounds;
				l.Size = (byte[])vl.Size.Clone();
				l.RenderData = loader.GenerateRenderData(vxl.Limbs[i]);
				limbData[i] = l;
			}
		}

		public Matrix4x4 TransformationMatrix(uint limb, uint frame)
		{
			if (frame >= frames)
				throw new ArgumentOutOfRangeException(nameof(frame), $"Only {frames} frames exist.");
			if (limb >= limbs)
				throw new ArgumentOutOfRangeException(nameof(limb), $"Only {limbs} limbs exist.");

			var l = limbData[limb];
			var t = transforms[limbs * frame + limb];

			// Fix limb position
			t[3, 0] *= l.Scale * (l.Bounds.MaxX - l.Bounds.MinX) / l.Size[0];
			t[3, 1] *= l.Scale * (l.Bounds.MaxY - l.Bounds.MinY) / l.Size[1];
			t[3, 2] *= l.Scale * (l.Bounds.MaxZ - l.Bounds.MinZ) / l.Size[2];

			// Center, flip and scale
			t = Util.MatrixMultiply(t, Util.TranslationMatrix(l.Bounds.MinX, l.Bounds.MinY, l.Bounds.MinZ));
			t = Util.MatrixMultiply(Util.ScaleMatrix(l.Scale, -l.Scale, l.Scale), t);

			return t;
		}

		public ModelRenderData RenderData(uint limb)
		{
			return limbData[limb].RenderData;
		}

		public float[] Size
		{
			get
			{
				return limbData.Select(a => a.Size.Select(b => a.Scale * b).ToArray())
					.Aggregate((a, b) =>
					[
						Math.Max(a[0], b[0]),
						Math.Max(a[1], b[1]),
						Math.Max(a[2], b[2])
					]);
			}
		}

		public AABB Bounds(uint frame)
		{
			var minX = float.MaxValue;
			var minY = float.MaxValue;
			var minZ = float.MaxValue;
			var maxX = float.MinValue;
			var maxY = float.MinValue;
			var maxZ = float.MinValue;

			for (uint j = 0; j < limbs; j++)
			{
				var l = limbData[j];
				var b = new AABB(
					0, 0, 0,
					l.Bounds.MaxX - l.Bounds.MinX,
					l.Bounds.MaxY - l.Bounds.MinY,
					l.Bounds.MaxZ - l.Bounds.MinZ);

				// Calculate limb bounding box
				var bb = Util.MatrixAABBMultiply(TransformationMatrix(j, frame), b);
				minX = Math.Min(minX, bb.MinX);
				minY = Math.Min(minY, bb.MinY);
				minZ = Math.Min(minZ, bb.MinZ);
				maxX = Math.Max(maxX, bb.MaxX);
				maxY = Math.Max(maxY, bb.MaxY);
				maxZ = Math.Max(maxZ, bb.MaxZ);
			}

			return new AABB(minX, minY, minZ, maxX, maxY, maxZ);
		}

		public Rectangle AggregateBounds
		{
			get
			{
				// Calculate the smallest sphere that covers the model limbs
				var rSquared = 0f;
				for (var f = 0U; f < frames; f++)
				{
					var bounds = Bounds(f);
					for (var i = 0; i < 8; i++)
					{
						var corner = bounds.Corner(i);
						var x = corner.X;
						var y = corner.Y;
						var z = corner.Z;
						rSquared = Math.Max(rSquared, x * x + y * y + z * z);
					}
				}

				var r = (int)Math.Sqrt(rSquared) + 1;
				return Rectangle.FromLTRB(-r, -r, r, r);
			}
		}
	}
}
