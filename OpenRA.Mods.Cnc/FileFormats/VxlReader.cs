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

using System.Collections.Generic;
using System.IO;

namespace OpenRA.Mods.Cnc.FileFormats
{
	public enum NormalType { TiberianSun = 2, RedAlert2 = 4 }
	public class VxlElement
	{
		public byte Color;
		public byte Normal;
	}

	public class VxlLimb
	{
		public string Name;
		public float Scale;
		public float[] Bounds;
		public byte[] Size;
		public NormalType Type;

		public uint VoxelCount;
		public Dictionary<byte, VxlElement>[,] VoxelMap;
	}

	public class VxlReader
	{
		public readonly uint LimbCount;
		public VxlLimb[] Limbs;

		readonly uint bodySize;

		static void ReadVoxelData(Stream s, VxlLimb l)
		{
			var baseSize = l.Size[0] * l.Size[1];
			var colStart = new int[baseSize];
			for (var i = 0; i < baseSize; i++)
				colStart[i] = s.ReadInt32();
			s.Seek(4 * baseSize, SeekOrigin.Current);
			var dataStart = s.Position;

			// Count the voxels in this limb
			l.VoxelCount = 0;
			for (var i = 0; i < baseSize; i++)
			{
				// Empty column
				if (colStart[i] == -1)
					continue;

				s.Seek(dataStart + colStart[i], SeekOrigin.Begin);
				var z = 0;
				do
				{
					z += s.ReadUInt8();
					var count = s.ReadUInt8();
					z += count;
					l.VoxelCount += count;
					s.Seek(2 * count + 1, SeekOrigin.Current);
				}
				while (z < l.Size[2]);
			}

			// Read the data
			l.VoxelMap = new Dictionary<byte, VxlElement>[l.Size[0], l.Size[1]];
			for (var i = 0; i < baseSize; i++)
			{
				// Empty column
				if (colStart[i] == -1)
					continue;

				s.Seek(dataStart + colStart[i], SeekOrigin.Begin);

				var x = (byte)(i % l.Size[0]);
				var y = (byte)(i / l.Size[0]);
				byte z = 0;
				l.VoxelMap[x, y] = new Dictionary<byte, VxlElement>();
				do
				{
					z += s.ReadUInt8();
					var count = s.ReadUInt8();
					for (var j = 0; j < count; j++)
					{
						var v = new VxlElement
						{
							Color = s.ReadUInt8(),
							Normal = s.ReadUInt8()
						};

						l.VoxelMap[x, y].Add(z, v);
						z++;
					}

					// Skip duplicate count
					s.ReadUInt8();
				}
				while (z < l.Size[2]);
			}
		}

		public VxlReader(Stream s)
		{
			if (!s.ReadASCII(16).StartsWith("Voxel Animation"))
				throw new InvalidDataException("Invalid vxl header");

			s.ReadUInt32();
			LimbCount = s.ReadUInt32();
			s.ReadUInt32();
			bodySize = s.ReadUInt32();
			s.Seek(770, SeekOrigin.Current);

			// Read Limb headers
			Limbs = new VxlLimb[LimbCount];
			for (var i = 0; i < LimbCount; i++)
			{
				Limbs[i] = new VxlLimb
				{
					Name = s.ReadASCII(16)
				};

				s.Seek(12, SeekOrigin.Current);
			}

			// Skip to the Limb footers
			s.Seek(802 + 28 * LimbCount + bodySize, SeekOrigin.Begin);

			var limbDataOffset = new uint[LimbCount];
			for (var i = 0; i < LimbCount; i++)
			{
				limbDataOffset[i] = s.ReadUInt32();
				s.Seek(8, SeekOrigin.Current);
				Limbs[i].Scale = s.ReadFloat();
				s.Seek(48, SeekOrigin.Current);

				Limbs[i].Bounds = new float[6];
				for (var j = 0; j < 6; j++)
					Limbs[i].Bounds[j] = s.ReadFloat();
				Limbs[i].Size = s.ReadBytes(3);
				Limbs[i].Type = (NormalType)s.ReadByte();
			}

			for (var i = 0; i < LimbCount; i++)
			{
				s.Seek(802 + 28 * LimbCount + limbDataOffset[i], SeekOrigin.Begin);
				ReadVoxelData(s, Limbs[i]);
			}
		}

		public static VxlReader Load(string filename)
		{
			using (var s = File.OpenRead(filename))
				return new VxlReader(s);
		}
	}
}
