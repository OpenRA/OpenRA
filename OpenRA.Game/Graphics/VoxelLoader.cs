#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public struct VoxelRenderData
	{
		public readonly int Start;
		public readonly int Count;
		public readonly Sheet Sheet;

		public VoxelRenderData(int start, int count, Sheet sheet)
		{
			Start = start;
			Count = count;
			Sheet = sheet;
		}
	}

	public class VoxelLoader
	{
		SheetBuilder sheetBuilder;

		Cache<Pair<string,string>, Voxel> voxels;
		IVertexBuffer<Vertex> vertexBuffer;
		List<Vertex[]> vertices;
		int totalVertexCount;
		int cachedVertexCount;

		static SheetBuilder CreateSheetBuilder()
		{
			var allocated = false;
			Func<Sheet> allocate = () =>
			{
				if (allocated)
					throw new SheetOverflowException("");
				allocated = true;
				return SheetBuilder.AllocateSheet();
			};

			return new SheetBuilder(SheetType.DualIndexed, allocate);
		}

		public VoxelLoader()
		{
			voxels = new Cache<Pair<string,string>, Voxel>(LoadFile);
			vertices = new List<Vertex[]>();
			totalVertexCount = 0;
			cachedVertexCount = 0;

			sheetBuilder = CreateSheetBuilder();
		}

		static float[] channelSelect = { 0.75f, 0.25f, -0.25f, -0.75f };
		Vertex[] GenerateSlicePlane(int su, int sv, Func<int,int,VxlElement> first, Func<int,int,VxlElement> second, Func<int, int, float[]> coord)
		{
			var colors = new byte[su*sv];
			var normals = new byte[su*sv];

			var c = 0;
			for (var v = 0; v < sv; v++)
				for (var u = 0; u < su; u++)
			{
				var voxel = first(u,v) ?? second(u,v);
				colors[c] = voxel == null ? (byte)0 : voxel.Color;
				normals[c] = voxel == null ? (byte)0 : voxel.Normal;
				c++;
			}

			var s = sheetBuilder.Allocate(new Size(su, sv));
			Util.FastCopyIntoChannel(s, 0, colors);
			Util.FastCopyIntoChannel(s, 1, normals);
			s.sheet.CommitData();

			var channelP =channelSelect[(int)s.channel];
			var channelC = channelSelect[(int)s.channel + 1];
			return new Vertex[4]
			{
				new Vertex(coord(0, 0), s.left, s.top, channelP, channelC),
				new Vertex(coord(su, 0),s.right, s.top, channelP, channelC),
				new Vertex(coord(su, sv), s.right, s.bottom, channelP, channelC),
				new Vertex(coord(0, sv), s.left, s.bottom, channelP, channelC)
			};
		}

		IEnumerable<Vertex[]> GenerateSlicePlanes(VxlLimb l)
		{
			Func<int,int,int,VxlElement> get = (x,y,z) =>
			{
				if (x < 0 || y < 0 || z < 0)
					return null;

				if (x >= l.Size[0] || y >= l.Size[1] || z >= l.Size[2])
					return null;

				var v = l.VoxelMap[(byte)x,(byte)y];
				if (v == null || !v.ContainsKey((byte)z))
					return null;

				return l.VoxelMap[(byte)x,(byte)y][(byte)z];
			};

			// Cull slices without any visible faces
			var xPlanes = new bool[l.Size[0]+1];
			var yPlanes = new bool[l.Size[1]+1];
			var zPlanes = new bool[l.Size[2]+1];
			for (var x = 0; x < l.Size[0]; x++)
			{
				for (var y = 0; y < l.Size[1]; y++)
				{
					for (var z = 0; z < l.Size[2]; z++)
					{
						if (get(x,y,z) == null)
							continue;

						// Only generate a plane if it is actually visible
						if (!xPlanes[x] && get(x-1,y,z) == null)
							xPlanes[x] = true;
						if (!xPlanes[x+1] && get(x+1,y,z) == null)
							xPlanes[x+1] = true;

						if (!yPlanes[y] && get(x,y-1,z) == null)
							yPlanes[y] = true;
						if (!yPlanes[y+1] && get(x,y+1,z) == null)
							yPlanes[y+1] = true;

						if (!zPlanes[z] && get(x,y,z-1) == null)
							zPlanes[z] = true;
						if (!zPlanes[z+1] && get(x,y,z+1) == null)
							zPlanes[z+1] = true;
					}
				}
			}

			for (var x = 0; x <= l.Size[0]; x++)
				if (xPlanes[x])
					yield return GenerateSlicePlane(l.Size[1], l.Size[2],
						(u,v) => get(x, u, v),
						(u,v) => get(x - 1, u, v),
						(u,v) => new float[] {x, u, v});

			for (var y = 0; y <= l.Size[1]; y++)
				if (yPlanes[y])
					yield return GenerateSlicePlane(l.Size[0], l.Size[2],
						(u,v) => get(u, y, v),
						(u,v) => get(u, y - 1, v),
						(u,v) => new float[] {u, y, v});

			for (var z = 0; z <= l.Size[2]; z++)
				if (zPlanes[z])
					yield return GenerateSlicePlane(l.Size[0], l.Size[1],
						(u,v) => get(u, v, z),
						(u,v) => get(u, v, z - 1),
						(u,v) => new float[] {u, v, z});
		}

		public VoxelRenderData GenerateRenderData(VxlLimb l)
		{
			Vertex[] v;
			try
			{
				v = GenerateSlicePlanes(l).SelectMany(x => x).ToArray();
			}
			catch (SheetOverflowException)
			{
				// Sheet overflow - allocate a new sheet and try once more
				Log.Write("debug", "Voxel sheet overflow! Generating new sheet");
				sheetBuilder.Current.ReleaseBuffer();
				sheetBuilder = CreateSheetBuilder();
				v = GenerateSlicePlanes(l).SelectMany(x => x).ToArray();
			}

			vertices.Add(v);

			var start = totalVertexCount;
			var count = v.Length;
			totalVertexCount += count;
			return new VoxelRenderData(start, count, sheetBuilder.Current);
		}

		public void RefreshBuffer()
		{
			vertexBuffer = Game.Renderer.Device.CreateVertexBuffer(totalVertexCount);
			vertexBuffer.SetData(vertices.SelectMany(v => v).ToArray(), totalVertexCount);
			cachedVertexCount = totalVertexCount;
		}

		public IVertexBuffer<Vertex> VertexBuffer
		{
			get
			{
				if (cachedVertexCount != totalVertexCount)
					RefreshBuffer();
				return vertexBuffer;
			}
		}

		Voxel LoadFile(Pair<string,string> files)
		{
			VxlReader vxl;
			HvaReader hva;
			using (var s = GlobalFileSystem.OpenWithExts(files.First, ".vxl"))
				vxl = new VxlReader(s);
			using (var s = GlobalFileSystem.OpenWithExts(files.Second, ".hva"))
				hva = new HvaReader(s);
			return new Voxel(this, vxl, hva);
		}

		public Voxel Load(string vxl, string hva)
		{
			return voxels[Pair.New(vxl, hva)];
		}

		public void Finish()
		{
			sheetBuilder.Current.ReleaseBuffer();
		}
	}
}
