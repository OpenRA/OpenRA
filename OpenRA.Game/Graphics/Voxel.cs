#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;

namespace OpenRA.Graphics
{
	struct Limb
	{
		public float Scale;
		public float[] Bounds;
		public byte[] Size;
		public VoxelRenderData RenderData;
	}

	public class Voxel
	{
		Limb[] limbs;
		HvaReader hva;
		VoxelLoader loader;

		float[][] transform, lightDirection, groundNormal;
		float[] groundZ;

		public Voxel(VoxelLoader loader, VxlReader vxl, HvaReader hva)
		{
			this.hva = hva;
			this.loader = loader;

			limbs = new Limb[vxl.LimbCount];
			for (var i = 0; i < vxl.LimbCount; i++)
			{
				var vl = vxl.Limbs[i];
				var l = new Limb();
				l.Scale = vl.Scale;
				l.Bounds = (float[])vl.Bounds.Clone();
				l.Size = (byte[])vl.Size.Clone();
				l.RenderData = loader.GenerateRenderData(vxl.Limbs[i]);
				limbs[i] = l;
			}

			transform = new float[vxl.LimbCount][];
			lightDirection = new float[vxl.LimbCount][];
			groundNormal = new float[vxl.LimbCount][];
			groundZ = new float[vxl.LimbCount];
		}

		// Extract the rotation components from a matrix and apply them to a vector
		static float[] ExtractRotationVector(float[] mtx, WVec vec)
		{
			var tVec = Util.MatrixVectorMultiply(mtx, new float[] {vec.X, vec.Y, vec.Z, 1});
			var tOrigin = Util.MatrixVectorMultiply(mtx, new float[] {0,0,0,1});
			tVec[0] -= tOrigin[0]*tVec[3]/tOrigin[3];
			tVec[1] -= tOrigin[1]*tVec[3]/tOrigin[3];
			tVec[2] -= tOrigin[2]*tVec[3]/tOrigin[3];

			// Renormalize
			var w = (float)Math.Sqrt(tVec[0]*tVec[0] + tVec[1]*tVec[1] + tVec[2]*tVec[2]);
			tVec[0] /= w;
			tVec[1] /= w;
			tVec[2] /= w;
			tVec[3] = 1f;

			return tVec;
		}

		static float[] MakeFloatMatrix(int[] imtx)
		{
			var fmtx = new float[16];
			for (var i = 0; i < 16; i++)
				fmtx[i] = imtx[i]*1f / imtx[15];
			return fmtx;
		}

		public void Draw(VoxelRenderer r, float[] lightAmbientColor, float[] lightDiffuseColor,
		                 int colorPalette, int normalsPalette)
		{
			for (var i = 0; i < limbs.Length; i++)
				r.Render(loader, limbs[i].RenderData, transform[i], lightDirection[i],
				         lightAmbientColor, lightDiffuseColor, colorPalette, normalsPalette);
		}

		public void DrawShadow(VoxelRenderer r, int shadowPalette)
		{
			for (var i = 0; i < limbs.Length; i++)
				r.RenderShadow(loader, limbs[i].RenderData, transform[i], lightDirection[i],
				               groundNormal[i], groundZ[i], shadowPalette);
		}

		static readonly WVec forward = new WVec(1024,0,0);
		static readonly WVec up = new WVec(0,0,1024);
		public void PrepareForDraw(WorldRenderer wr, WPos pos, IEnumerable<WRot> rotations,
		                           WRot camera, uint frame, float scale, WRot lightSource)
		{
			// Calculate the shared view matrix components
			var pxPos = wr.ScreenPosition(pos);

			// Rotations
			var rot = rotations.Reverse().Aggregate(MakeFloatMatrix(camera.AsMatrix()),
				(a,b) => Util.MatrixMultiply(a, MakeFloatMatrix(b.AsMatrix())));

			// Each limb has its own transformation matrix
			for (uint i = 0; i < limbs.Length; i++)
			{
				Limb l = limbs[i];
				var t = Util.MatrixMultiply(Util.ScaleMatrix(1,-1,1),
				                            hva.TransformationMatrix(i, frame));
				// Fix limb position
				t[12] *= l.Scale*(l.Bounds[3] - l.Bounds[0]) / l.Size[0];
				t[13] *= l.Scale*(l.Bounds[4] - l.Bounds[1]) / l.Size[1];
				t[14] *= l.Scale*(l.Bounds[5] - l.Bounds[2]) / l.Size[2];
				t = Util.MatrixMultiply(t, Util.TranslationMatrix(l.Bounds[0], l.Bounds[1], l.Bounds[2]));

				// Apply view matrix
				transform[i] = Util.MatrixMultiply(Util.TranslationMatrix(pxPos.X, pxPos.Y, pxPos.Y),
					Util.ScaleMatrix(scale*l.Scale, scale*l.Scale, scale*l.Scale));
				transform[i] = Util.MatrixMultiply(transform[i], rot);
				transform[i] = Util.MatrixMultiply(transform[i], t);

				// Transform light direction into limb-space
				var undoPitch = MakeFloatMatrix(new WRot(camera.Pitch, WAngle.Zero, WAngle.Zero).AsMatrix());
				var lightTransform = Util.MatrixMultiply(Util.MatrixInverse(transform[i]), undoPitch);

				lightDirection[i] = ExtractRotationVector(lightTransform, forward.Rotate(lightSource));
				groundNormal[i] = ExtractRotationVector(Util.MatrixInverse(t), up);

				// Hack: Extract the ground z position independently of y.
				groundZ[i] = (wr.ScreenPosition(pos).Y - wr.ScreenZPosition(pos, 0)) / 2;
			}
		}

		public uint Frames { get { return hva.FrameCount; }}
		public uint LimbCount { get { return (uint)limbs.Length; }}

		public float[] Size
		{
			get
			{
				return limbs.Select(a => a.Size.Select(b => a.Scale*b).ToArray())
					.Aggregate((a,b) => new float[]
					{
						Math.Max(a[0], b[0]),
						Math.Max(a[1], b[1]),
						Math.Max(a[2], b[2])
					});
			}
		}
	}
}
