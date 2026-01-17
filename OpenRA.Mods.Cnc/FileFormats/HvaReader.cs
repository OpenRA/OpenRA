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

using System.IO;
using System.Numerics;

namespace OpenRA.Mods.Cnc.FileFormats
{
	public class HvaReader
	{
		public readonly uint FrameCount;
		public readonly uint LimbCount;
		public readonly Matrix4x4[] Transforms;

		public HvaReader(Stream s, string fileName)
		{
			s.Seek(16, SeekOrigin.Begin);
			FrameCount = s.ReadUInt32();
			LimbCount = s.ReadUInt32();

			// Skip limb names
			s.Seek(16 * LimbCount, SeekOrigin.Current);
			Transforms = new Matrix4x4[FrameCount * LimbCount];

			for (var j = 0; j < FrameCount; j++)
				for (var i = 0; i < LimbCount; i++)
				{
					// Convert to column-major matrices and add the final matrix row
					var c = LimbCount * j + i;
					Transforms[c][3, 0] = 0;
					Transforms[c][3, 1] = 0;
					Transforms[c][3, 2] = 0;
					Transforms[c][3, 3] = 1;

					for (var k = 0; k < 12; k++)
						Transforms[c][k / 4, k % 4] = s.ReadSingle();

					if (!Matrix4x4.Invert(Transforms[c], out var _))
						throw new InvalidDataException(
							$"The transformation matrix for HVA file `{fileName}` section {i} frame {j} is invalid because it is not invertible!");
				}
		}

		public static HvaReader Load(string filename)
		{
			using (var s = File.OpenRead(filename))
				return new HvaReader(s, Path.GetFileName(filename));
		}
	}
}
