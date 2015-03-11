using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.LZW;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.SpriteLoaders
{
	public class GifLoader : ISpriteLoader
	{
		public class GifFrame : ISpriteFrame
		{
			public Size Size { get; private set; }
			public Size FrameSize { get; private set; }
			public float2 Offset { get { return offset; } }

			public byte[] Data { get; set; }
			public bool DisableExportPadding { get { return false; } }

			float2 offset;

			public GifFrame(Stream s, Size size)
			{
				var imageLeft = s.ReadUInt16();
				var imageTop = s.ReadUInt16();
				var imageWidth = s.ReadUInt16();
				var imageHeight = s.ReadUInt16();

				var unpacked = s.ReadUInt8();

				// Local palette, skip it if it exists
				if (((unpacked & 0x01) != 0))
					s.Position += 3 * (1 << ((unpacked * 0x07) + 1));

				var interlaced = false;

				// Check if the data is interlaced
				if ((unpacked & 0x02) != 0)
					interlaced = true;

				s.Position--;
				var lzwMinCodeSize = s.ReadUInt8();

				var count = imageWidth * imageHeight;
				var data = new byte[count];
				var offset = 0;

				using (Stream inStream = new LzwInputStream(s))
				{
					while (true)
					{
						var length = s.ReadUInt8();

						if (length == 0)
							break;

						inStream.Read(data, offset, length);
						offset += length;
					}
				}

				if (interlaced)
				{
					var originData = data;

					var offsets = new int[]{0, 4, 2, 1};
					var steps = new int[]{8, 8, 4, 2};

					var j = 0;
					for (var pass = 0; pass < 4; pass++)
					{
						for (var i = offsets[pass]; i < originData.Length; i += steps[pass])
						{
							data[i] = originData[j];
							j++;
						}
					}
				}

				Size = size;
				FrameSize = new Size(imageWidth, imageHeight);
				this.offset = new float2(imageLeft, imageTop);

				Data = data;
			}
		}

		bool IsGif(Stream s)
		{
			var start = s.Position;

			var a = s.ReadASCII(6);

			s.Position = start;
			return a.StartsWith("GIF");
		}

		GifFrame[] ParseFrames(Stream s)
		{
			var start = s.Position;
			s.Position += 3;
			var version = s.ReadASCII(3);
			if (version != "87a" && version != "89a")
				throw new Exception("Gif: Not a valid version");

			var width = s.ReadUInt16();
			var height = s.ReadUInt16();
			var size = new Size(width, height);

			var GCT = s.ReadUInt8();
			s.Position += 2;

			if ((GCT & 0x01) != 0)
			{
				var GCEntries = 1 << ((GCT & 0x7) + 1);

				// Skip over palette.
				s.Position += GCEntries * 3;
			}

			var frames = new List<GifFrame>();

			while (true)
			{
				var block = s.ReadUInt8();

				switch ((BlockTypes)block)
				{
					case BlockTypes.ExtensionBlock:
						var type = (BlockTypes)s.ReadUInt8();

						// We dont care about any of these, so we skip them.
						switch (type)
						{
							case BlockTypes.GraphicExtensionBlock:
								var lengthG = s.ReadUInt8();
								if (lengthG == 4)
									s.Position += 5;
								else
									s.Position -= 2;
								break;
							case BlockTypes.CommentExtensionBlock:
								SkipSubblocks(s);
								break;
							case BlockTypes.PlainTextExtension:
								var lengthP = s.ReadUInt8();
								s.Position += lengthP;
								SkipSubblocks(s);
								break;
							case BlockTypes.ApllicationExtensionBlock:
								var lengthA = s.ReadUInt8();
								if (lengthA != 11)
									throw new Exception("Bad app extension");

								var identifier = s.ReadASCII(8);
								s.Position += 3;
								if (identifier == "NETSCAPE")
									s.Position += 5;
								else
									SkipSubblocks(s);
								break;
							default:
								SkipSubblocks(s);
								break;
						}
						break;
					case BlockTypes.ImageDescriptor:
						frames.Add(new GifFrame(s, size));
						break;
					case BlockTypes.Trailer:
						return frames.ToArray();
					default:
						Console.WriteLine("Bad data");
						break;
				}

				// Shouldn't happen but if Trailer is missing at EOF we should bail.
				if (s.Position >= s.Length)
					break;
			}

			return frames.ToArray();
		}

		public bool TryParseSprite(Stream s, out ISpriteFrame[] frames)
		{
			if (!IsGif(s))
			{
				frames = null;
				return false;
			}

			frames = ParseFrames(s);
			return true;
		}

		#region Helper functions

		static void SkipSubblocks(Stream s)
		{
			while (true)
			{
				var ssize = s.ReadUInt8();
				if (ssize == 0)
					break;

				s.Position += ssize;
			}
		}

		static byte[] Decode(int minCodeSize, byte[] data)
		{
			var pos = 0;
			var code = 0;
			var last = 0;

			var decrypt = new Action<int>((size) =>
			{
				code = 0;
				for (var i = 0; i < size; i++)
				{
					if ((data[pos >> 3] & (1 << (pos & 7))) != 0)
						code |= 1 << i;
					pos++;
				}
			});

			var output = new List<byte>();

			var clearCode = 1 << minCodeSize;
			var eoiCode = clearCode + 1;

			var codeSize = minCodeSize + 1;

			var dict = new List<byte[]>();

			while (true)
			{
				last = code;
				decrypt(codeSize);

				if (code == clearCode)
				{
					dict.Clear();
					codeSize = minCodeSize + 1;
					for (var i = 0; i < clearCode; i++)
						dict.Add(new byte[]{(byte)i});
					dict.Add(new byte[]{});
					dict.Add(null);
					continue;
				}
				
				if (code == eoiCode)
					break;

				if (code < dict.Count)
				{
					if (last != clearCode)
						dict.Add(dict.ElementAt(last).Concat(new byte[] { dict.ElementAt(code)[0] }).ToArray());
				}
				else
				{
					if (code != dict.Count)
						throw new Exception("Invalid LZW code");

					var element = dict.ElementAt(last);
					dict.Add(element.Concat(new byte[]{element[0]}).ToArray());
				}

				output = output.Concat(dict.ElementAt(code)).ToList();

				if (dict.Count == (1 << codeSize) && codeSize < 12)
					codeSize++;
			}

			return output.ToArray();
		}

		#endregion

		enum BlockTypes
		{
			ImageDescriptor = 0x2C,
			ExtensionBlock = 0x21,
			GraphicExtensionBlock = 0xF9,
			ApllicationExtensionBlock = 0xFF,
			CommentExtensionBlock = 0xFE,
			PlainTextExtension = 0x01,
			Trailer = 0x3B
		}
	}
}
