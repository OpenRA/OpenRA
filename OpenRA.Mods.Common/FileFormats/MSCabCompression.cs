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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace OpenRA.Mods.Common.FileFormats
{
	public sealed class MSCabCompression
	{
		class CabFolder
		{
			public readonly uint BlockOffset;
			public readonly ushort BlockCount;
			public readonly ushort CompressionType;

			public CabFolder(Stream stream)
			{
				BlockOffset = stream.ReadUInt32();
				BlockCount = stream.ReadUInt16();
				CompressionType = stream.ReadUInt16();
			}
		}

		class CabFile
		{
			public readonly string FileName;
			public readonly uint DecompressedLength;
			public readonly uint DecompressedOffset;
			public readonly ushort FolderIndex;

			public CabFile(Stream stream)
			{
				DecompressedLength = stream.ReadUInt32();
				DecompressedOffset = stream.ReadUInt32();
				FolderIndex = stream.ReadUInt16();
				stream.Position += 6;
				FileName = stream.ReadASCIIZ();
			}
		}

		readonly CabFolder[] folders;
		readonly CabFile[] files;
		readonly Stream stream;

		public MSCabCompression(Stream stream)
		{
			this.stream = stream;

			var signature = stream.ReadASCII(4);
			if (signature != "MSCF")
				throw new InvalidDataException("Not a Microsoft CAB package!");

			stream.Position += 12;
			var filesOffset = stream.ReadUInt32();
			stream.Position += 6;
			var folderCount = stream.ReadUInt16();
			var fileCount = stream.ReadUInt16();
			if (stream.ReadUInt16() != 0)
				throw new InvalidDataException("Only plain packages (without reserved header space or prev/next archives) are supported!");

			stream.Position += 4;

			folders = new CabFolder[folderCount];
			for (var i = 0; i < folderCount; i++)
			{
				folders[i] = new CabFolder(stream);
				if (folders[i].CompressionType != 1)
					throw new InvalidDataException("Compression type is not supported");
			}

			files = new CabFile[fileCount];
			stream.Seek(filesOffset, SeekOrigin.Begin);
			for (var i = 0; i < fileCount; i++)
				files[i] = new CabFile(stream);
		}

		public void ExtractFile(string filename, Stream output, Action<int> onProgress = null)
		{
			var file = files.FirstOrDefault(f => f.FileName == filename);
			if (file == null)
				throw new FileNotFoundException(filename);

			var folder = folders[file.FolderIndex];
			stream.Seek(folder.BlockOffset, SeekOrigin.Begin);

			var inflater = new Inflater(true);
			var buffer = new byte[4096];
			var decompressedBytes = 0;
			for (var i = 0; i < folder.BlockCount; i++)
			{
				onProgress?.Invoke((int)(100 * output.Position / file.DecompressedLength));

				// Ignore checksums
				stream.Position += 4;
				var blockLength = stream.ReadUInt16();
				stream.Position += 4;

				using (var batch = new MemoryStream(stream.ReadBytes(blockLength - 2)))
				using (var inflaterStream = new InflaterInputStream(batch, inflater))
				{
					int n;
					while ((n = inflaterStream.Read(buffer, 0, buffer.Length)) > 0)
					{
						var offset = Math.Max(0, file.DecompressedOffset - decompressedBytes);
						var count = Math.Min(n - offset, file.DecompressedLength - decompressedBytes);
						if (offset < n)
							output.Write(buffer, (int)offset, (int)count);

						decompressedBytes += n;
					}
				}

				inflater.Reset();
			}
		}

		public IEnumerable<string> Contents { get { return files.Select(f => f.FileName); } }
	}
}
