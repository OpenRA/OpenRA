using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.FileFormats.Graphics
{
	// format80-only SHP writer

	public static class ShpWriter
	{
		public static void Write(Stream s, int width, int height, IEnumerable<byte[]> frames)
		{
			var compressedFrames = frames.Select(f => Format80.Encode(f)).ToArray();

			// note: end-of-file and all-zeroes headers
			var dataOffset = 14 + (compressedFrames.Length + 2) * ImageHeader.SizeOnDisk;

			using (var bw = new BinaryWriter(s))
			{
				bw.Write((ushort)compressedFrames.Length);
				bw.Write((ushort)0);	// unused
				bw.Write((ushort)0);	// unused
				bw.Write((ushort)width);
				bw.Write((ushort)height);
				bw.Write((uint)0);		// unused

				foreach (var f in compressedFrames)
				{
					var ih = new ImageHeader { Format = Format.Format80, Offset = (uint)dataOffset };
					dataOffset += f.Length;

					ih.WriteTo(bw);
				}

				var eof = new ImageHeader { Offset = (uint)dataOffset };
				eof.WriteTo(bw);

				var allZeroes = new ImageHeader { };
				allZeroes.WriteTo(bw);

				foreach (var f in compressedFrames)
					bw.Write(f);
			}
		}
	}
}
