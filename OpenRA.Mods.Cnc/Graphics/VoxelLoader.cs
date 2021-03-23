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
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.Graphics
{
	public sealed class VoxelLoader : IDisposable
	{
		static readonly float[] ChannelSelect = { 0.75f, 0.25f, -0.25f, -0.75f };

		readonly List<Vertex[]> vertices = new List<Vertex[]>();
		readonly Cache<(string, string), Voxel> voxels;
		readonly IReadOnlyFileSystem fileSystem;
		IVertexBuffer<Vertex> vertexBuffer;
		int totalVertexCount;
		int cachedVertexCount;

		SheetBuilder sheetBuilder;

		static SheetBuilder CreateSheetBuilder()
		{
			var allocated = false;
			Func<Sheet> allocate = () =>
			{
				if (allocated)
					throw new SheetOverflowException("");
				allocated = true;
				return SheetBuilder.AllocateSheet(SheetType.Indexed, Game.Settings.Graphics.SheetSize);
			};

			return new SheetBuilder(SheetType.Indexed, allocate);
		}

		public VoxelLoader(IReadOnlyFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
			voxels = new Cache<(string, string), Voxel>(LoadFile);
			vertices = new List<Vertex[]>();
			totalVertexCount = 0;
			cachedVertexCount = 0;

			sheetBuilder = CreateSheetBuilder();
		}

		Vertex[] GenerateSlicePlane(int su, int sv, Func<int, int, VxlElement> first, Func<int, int, VxlElement> second, Func<int, int, float3> coord)
		{
			var colors = new byte[su * sv];
			var normals = new byte[su * sv];

			var c = 0;
			for (var v = 0; v < sv; v++)
			{
				for (var u = 0; u < su; u++)
				{
					var voxel = first(u, v) ?? second(u, v);
					colors[c] = voxel == null ? (byte)0 : voxel.Color;
					normals[c] = voxel == null ? (byte)0 : voxel.Normal;
					c++;
				}
			}

			var size = new Size(su, sv);
			var s = sheetBuilder.Allocate(size);
			var t = sheetBuilder.Allocate(size);
			OpenRA.Graphics.Util.FastCopyIntoChannel(s, colors, SpriteFrameType.Indexed8);
			OpenRA.Graphics.Util.FastCopyIntoChannel(t, normals, SpriteFrameType.Indexed8);

			// s and t are guaranteed to use the same sheet because
			// of the custom voxel sheet allocation implementation
			s.Sheet.CommitBufferedData();

			var channelP = ChannelSelect[(int)s.Channel];
			var channelC = ChannelSelect[(int)t.Channel];
			return new Vertex[6]
			{
				new Vertex(coord(0, 0), s.Left, s.Top, t.Left, t.Top, channelP, channelC),
				new Vertex(coord(su, 0), s.Right, s.Top, t.Right, t.Top, channelP, channelC),
				new Vertex(coord(su, sv), s.Right, s.Bottom, t.Right, t.Bottom, channelP, channelC),
				new Vertex(coord(su, sv), s.Right, s.Bottom, t.Right, t.Bottom, channelP, channelC),
				new Vertex(coord(0, sv), s.Left, s.Bottom, t.Left, t.Bottom, channelP, channelC),
				new Vertex(coord(0, 0), s.Left, s.Top, t.Left, t.Top, channelP, channelC)
			};
		}

		IEnumerable<Vertex[]> GenerateSlicePlanes(VxlLimb l)
		{
			Func<int, int, int, VxlElement> get = (x, y, z) =>
			{
				if (x < 0 || y < 0 || z < 0)
					return null;

				if (x >= l.Size[0] || y >= l.Size[1] || z >= l.Size[2])
					return null;

				var v = l.VoxelMap[(byte)x, (byte)y];
				if (v == null || !v.ContainsKey((byte)z))
					return null;

				return l.VoxelMap[(byte)x, (byte)y][(byte)z];
			};

			// Cull slices without any visible faces
			var xPlanes = new bool[l.Size[0] + 1];
			var yPlanes = new bool[l.Size[1] + 1];
			var zPlanes = new bool[l.Size[2] + 1];
			for (var x = 0; x < l.Size[0]; x++)
			{
				for (var y = 0; y < l.Size[1]; y++)
				{
					for (var z = 0; z < l.Size[2]; z++)
					{
						if (get(x, y, z) == null)
							continue;

						// Only generate a plane if it is actually visible
						if (!xPlanes[x] && get(x - 1, y, z) == null)
							xPlanes[x] = true;
						if (!xPlanes[x + 1] && get(x + 1, y, z) == null)
							xPlanes[x + 1] = true;

						if (!yPlanes[y] && get(x, y - 1, z) == null)
							yPlanes[y] = true;
						if (!yPlanes[y + 1] && get(x, y + 1, z) == null)
							yPlanes[y + 1] = true;

						if (!zPlanes[z] && get(x, y, z - 1) == null)
							zPlanes[z] = true;
						if (!zPlanes[z + 1] && get(x, y, z + 1) == null)
							zPlanes[z + 1] = true;
					}
				}
			}

			for (var x = 0; x <= l.Size[0]; x++)
				if (xPlanes[x])
					yield return GenerateSlicePlane(l.Size[1], l.Size[2],
						(u, v) => get(x, u, v),
						(u, v) => get(x - 1, u, v),
						(u, v) => new float3(x, u, v));

			for (var y = 0; y <= l.Size[1]; y++)
				if (yPlanes[y])
					yield return GenerateSlicePlane(l.Size[0], l.Size[2],
						(u, v) => get(u, y, v),
						(u, v) => get(u, y - 1, v),
						(u, v) => new float3(u, y, v));

			for (var z = 0; z <= l.Size[2]; z++)
				if (zPlanes[z])
					yield return GenerateSlicePlane(l.Size[0], l.Size[1],
						(u, v) => get(u, v, z),
						(u, v) => get(u, v, z - 1),
						(u, v) => new float3(u, v, z));
		}

		public ModelRenderData GenerateRenderData(VxlLimb l)
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
			return new ModelRenderData(start, count, sheetBuilder.Current);
		}

		public void RefreshBuffer()
		{
			vertexBuffer?.Dispose();
			vertexBuffer = Game.Renderer.CreateVertexBuffer(totalVertexCount);
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

		Voxel LoadFile((string Vxl, string Hva) files)
		{
			VxlReader vxl;
			HvaReader hva;
			using (var s = fileSystem.Open(files.Vxl + ".vxl"))
				vxl = new VxlReader(s);

			using (var s = fileSystem.Open(files.Hva + ".hva"))
				hva = new HvaReader(s, files.Hva + ".hva");
			return new Voxel(this, vxl, hva, files);
		}

		public Voxel Load(string vxl, string hva)
		{
			return voxels[(vxl, hva)];
		}

		public void Finish()
		{
			sheetBuilder.Current.ReleaseBuffer();
		}

		public void Dispose()
		{
			vertexBuffer?.Dispose();
			sheetBuilder.Dispose();
		}
	}
}
