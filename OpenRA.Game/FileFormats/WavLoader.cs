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

namespace OpenRA.FileFormats
{
	public class WavLoader
	{
		public readonly int FileSize;
		public readonly string Format;

		public readonly int FmtChunkSize;
		public readonly int AudioFormat;
		public readonly int Channels;
		public readonly int SampleRate;
		public readonly int ByteRate;
		public readonly int BlockAlign;
		public readonly int BitsPerSample;

		public readonly int DataSize;
		public readonly byte[] RawOutput;

		public WavLoader(Stream s)
		{
			while (s.Position < s.Length)
			{
				if ((s.Position & 1) == 1)
					s.ReadByte(); // Alignment

				var type = s.ReadASCII(4);
				switch (type)
				{
					case "RIFF":
						FileSize = s.ReadInt32();
						Format = s.ReadASCII(4);
						if (Format != "WAVE")
							throw new NotSupportedException("Not a canonical WAVE file.");
						break;
					case "fmt ":
						FmtChunkSize = s.ReadInt32();
						if (FmtChunkSize != 16)
							throw new NotSupportedException("{0} fmt chunk size is not a supported encoding scheme.".F(FmtChunkSize));
						AudioFormat = s.ReadInt16();
						if (AudioFormat != 1)
							throw new NotSupportedException("Non-PCM compression is not supported.");
						Channels = s.ReadInt16();
						SampleRate = s.ReadInt32();
						ByteRate = s.ReadInt32();
						BlockAlign = s.ReadInt16();
						BitsPerSample = s.ReadInt16();
						break;
					case "data":
						DataSize = s.ReadInt32();
						RawOutput = s.ReadBytes(DataSize);
						break;
					default:
						// Ignore unknown chunks
						var chunkSize = s.ReadInt32();
						s.ReadBytes(chunkSize);
						break;
				}
			}
		}
	}
}