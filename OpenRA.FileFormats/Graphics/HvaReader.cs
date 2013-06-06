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
using System.IO;
using System.Linq;

namespace OpenRA.FileFormats
{
	public class HvaReader
	{
		public readonly uint FrameCount;
		public readonly uint LimbCount;
		float[] Transforms;

		public HvaReader(Stream s)
		{
			// Index swaps for transposing a matrix
			var ids = new byte[]{0,4,8,12,1,5,9,13,2,6,10,14};

			s.Seek(16, SeekOrigin.Begin);
			FrameCount = s.ReadUInt32();
			LimbCount = s.ReadUInt32();

			// Skip limb names
			s.Seek(16*LimbCount, SeekOrigin.Current);
			Transforms = new float[16*FrameCount*LimbCount];
			for (var j = 0; j < FrameCount; j++)
				for (var i = 0; i < LimbCount; i++)
			{
				// Convert to column-major matrices and add the final matrix row
				var c = 16*(LimbCount*j + i);
				Transforms[c + 3] = 0;
				Transforms[c + 7] = 0;
				Transforms[c + 11] = 0;
				Transforms[c + 15] = 1;

				for (var k = 0; k < 12; k++)
					Transforms[c + ids[k]] = s.ReadFloat();
			}
		}

		public float[] TransformationMatrix(uint limb, uint frame)
		{
			if (frame >= FrameCount)
				throw new ArgumentOutOfRangeException("frame", "Only {0} frames exist.".F(FrameCount));
			if (limb >= LimbCount)
				throw new ArgumentOutOfRangeException("limb", "Only {1} limbs exist.".F(LimbCount));

			var t = new float[16];
			Array.Copy(Transforms, 16*(LimbCount*frame + limb), t, 0, 16);

			return t;
		}

		public static HvaReader Load(string filename)
		{
			using (var s = File.OpenRead(filename))
				return new HvaReader(s);
		}
	}
}
