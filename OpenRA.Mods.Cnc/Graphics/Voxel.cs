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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.Graphics
{
	struct Limb
	{
		public float Scale;
		public float[] Bounds;
		public byte[] Size;
		public ModelRenderData RenderData;
	}

	public class Voxel : IModel
	{
		readonly Limb[] limbData;
		readonly float[] transforms;
		readonly uint frames;
		readonly uint limbs;

		uint IModel.Frames { get { return frames; } }
		uint IModel.Sections { get { return limbs; } }

		public Voxel(VoxelLoader loader, VxlReader vxl, HvaReader hva, (string Vxl, string Hva) files)
		{
			if (vxl.LimbCount != hva.LimbCount)
				throw new InvalidOperationException("{0}.vxl and {1}.hva limb counts don't match.".F(files.Vxl, files.Hva));

			transforms = hva.Transforms;
			frames = hva.FrameCount;
			limbs = hva.LimbCount;

			limbData = new Limb[vxl.LimbCount];
			for (var i = 0; i < vxl.LimbCount; i++)
			{
				var vl = vxl.Limbs[i];
				var l = default(Limb);
				l.Scale = vl.Scale;
				l.Bounds = (float[])vl.Bounds.Clone();
				l.Size = (byte[])vl.Size.Clone();
				l.RenderData = loader.GenerateRenderData(vxl.Limbs[i]);
				limbData[i] = l;
			}
		}

		public float[] TransformationMatrix(uint limb, uint frame)
		{
			if (frame >= frames)
				throw new ArgumentOutOfRangeException("frame", "Only {0} frames exist.".F(frames));
			if (limb >= limbs)
				throw new ArgumentOutOfRangeException("limb", "Only {1} limbs exist.".F(limbs));

			var l = limbData[limb];
			var t = new float[16];
			Array.Copy(transforms, 16 * (limbs * frame + limb), t, 0, 16);

			// Fix limb position
			t[12] *= l.Scale * (l.Bounds[3] - l.Bounds[0]) / l.Size[0];
			t[13] *= l.Scale * (l.Bounds[4] - l.Bounds[1]) / l.Size[1];
			t[14] *= l.Scale * (l.Bounds[5] - l.Bounds[2]) / l.Size[2];

			// Center, flip and scale
			t = OpenRA.Graphics.Util.MatrixMultiply(t, OpenRA.Graphics.Util.TranslationMatrix(l.Bounds[0], l.Bounds[1], l.Bounds[2]));
			t = OpenRA.Graphics.Util.MatrixMultiply(OpenRA.Graphics.Util.ScaleMatrix(l.Scale, -l.Scale, l.Scale), t);

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
					.Aggregate((a, b) => new float[]
					{
						Math.Max(a[0], b[0]),
						Math.Max(a[1], b[1]),
						Math.Max(a[2], b[2])
					});
			}
		}

		public float[] Bounds(uint frame)
		{
			var ret = new[]
			{
				float.MaxValue, float.MaxValue, float.MaxValue,
				float.MinValue, float.MinValue, float.MinValue
			};

			for (uint j = 0; j < limbs; j++)
			{
				var l = limbData[j];
				var b = new[]
				{
					0, 0, 0,
					l.Bounds[3] - l.Bounds[0],
					l.Bounds[4] - l.Bounds[1],
					l.Bounds[5] - l.Bounds[2]
				};

				// Calculate limb bounding box
				var bb = OpenRA.Graphics.Util.MatrixAABBMultiply(TransformationMatrix(j, frame), b);
				for (var i = 0; i < 3; i++)
				{
					ret[i] = Math.Min(ret[i], bb[i]);
					ret[i + 3] = Math.Max(ret[i + 3], bb[i + 3]);
				}
			}

			return ret;
		}

		public Rectangle AggregateBounds
		{
			get
			{
				// Corner offsets
				var ix = new uint[] { 0, 0, 0, 0, 3, 3, 3, 3 };
				var iy = new uint[] { 1, 1, 4, 4, 1, 1, 4, 4 };
				var iz = new uint[] { 2, 5, 2, 5, 2, 5, 2, 5 };

				// Calculate the smallest sphere that covers the model limbs
				var rSquared = 0f;
				for (var f = 0U; f < frames; f++)
				{
					var bounds = Bounds(f);
					for (var i = 0; i < 8; i++)
					{
						var x = bounds[ix[i]];
						var y = bounds[iy[i]];
						var z = bounds[iz[i]];
						rSquared = Math.Max(rSquared, x * x + y * y + z * z);
					}
				}

				var r = (int)Math.Sqrt(rSquared) + 1;
				return Rectangle.FromLTRB(-r, -r, r, r);
			}
		}
	}
}
