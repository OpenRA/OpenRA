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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace OpenRA.Mods.Common.FileFormats
{
	public sealed class InstallShieldCABCompression
	{
		const uint MaxFileGroupCount = 71;

		enum CABFlags : ushort
		{
			FileSplit = 0x1,
			FileObfuscated = 0x2,
			FileCompressed = 0x4,
			FileInvalid = 0x8,
		}

		enum LinkFlags : byte
		{
			Prev = 0x1,
			Next = 0x2
		}

		readonly struct FileGroup
		{
			public readonly string Name;
			public readonly uint FirstFile;
			public readonly uint LastFile;

			public FileGroup(Stream stream, long offset, uint version)
			{
				var nameOffset = stream.ReadUInt32();
				stream.Position += 18;

				if (version <= 5)
					stream.Position += 54;

				FirstFile = stream.ReadUInt32();
				LastFile = stream.ReadUInt32();

				var pos = stream.Position;
				stream.Position = offset + nameOffset;
				Name = stream.ReadASCIIZ();
				stream.Position = pos;
			}
		}

		readonly struct CabDescriptor
		{
			public readonly long FileTableOffset;
			public readonly uint FileTableSize;
			public readonly uint FileTableSize2;
			public readonly uint DirectoryCount;

			public readonly uint FileCount;
			public readonly long FileTableOffset2;

			public CabDescriptor(Stream stream)
			{
				FileTableOffset = stream.ReadUInt32();
				stream.Position += 4;
				FileTableSize = stream.ReadUInt32();
				FileTableSize2 = stream.ReadUInt32();
				DirectoryCount = stream.ReadUInt32();
				stream.Position += 8;
				FileCount = stream.ReadUInt32();
				FileTableOffset2 = stream.ReadUInt32();
			}
		}

		readonly struct DirectoryDescriptor
		{
			public readonly string Name;

			public DirectoryDescriptor(Stream stream, long nameTableOffset)
			{
				var nameOffset = stream.ReadUInt32();
				var pos = stream.Position;

				stream.Position = nameTableOffset + nameOffset;

				Name = stream.ReadASCIIZ();
				stream.Position = pos;
			}
		}

		readonly struct FileDescriptor
		{
			public readonly uint Index;
			public readonly CABFlags Flags;
			public readonly uint ExpandedSize;
			public readonly uint CompressedSize;
			public readonly uint DataOffset;

			public readonly byte[] MD5;
			public readonly uint NameOffset;
			public readonly uint DirectoryIndex;
			public readonly uint LinkToPrevious;

			public readonly uint LinkToNext;
			public readonly LinkFlags LinkFlags;
			public readonly ushort Volume;
			public readonly string Filename;

			public FileDescriptor(Stream stream, uint index, long tableOffset, uint version)
			{
				Index = index;

				if (version <= 5)
				{
					NameOffset = stream.ReadUInt32();
					DirectoryIndex = stream.ReadUInt32();
					Flags = (CABFlags)stream.ReadUInt16();
					ExpandedSize = stream.ReadUInt32();
					CompressedSize = stream.ReadUInt32();
					stream.Position += 20;
					DataOffset = stream.ReadUInt32();

					MD5 = new byte[16];
					LinkToPrevious = 0;
					LinkToNext = 0;
					LinkFlags = 0;
					Volume = 0;

					if ((Flags & CABFlags.FileInvalid) == 0)
					{
						var pos = stream.Position;
						stream.Position = tableOffset + NameOffset;
						Filename = stream.ReadASCIIZ();
						stream.Position = pos;
					}
					else
						Filename = "";
				}
				else
				{
					Flags = (CABFlags)stream.ReadUInt16();
					ExpandedSize = stream.ReadUInt32();
					stream.Position += 4;
					CompressedSize = stream.ReadUInt32();

					stream.Position += 4;
					DataOffset = stream.ReadUInt32();
					stream.Position += 4;
					MD5 = stream.ReadBytes(16);

					stream.Position += 16;
					NameOffset = stream.ReadUInt32();
					DirectoryIndex = stream.ReadUInt16();
					stream.Position += 12;
					LinkToPrevious = stream.ReadUInt32();
					LinkToNext = stream.ReadUInt32();

					LinkFlags = (LinkFlags)stream.ReadUInt8();
					Volume = stream.ReadUInt16();

					var pos = stream.Position;
					stream.Position = tableOffset + NameOffset;
					Filename = stream.ReadASCIIZ();
					stream.Position = pos;
				}
			}
		}

		readonly struct CommonHeader
		{
			public const long Size = 16;
			public readonly uint Version;
			public readonly uint VolumeInfo;
			public readonly long CabDescriptorOffset;
			public readonly uint CabDescriptorSize;

			public CommonHeader(Stream stream)
			{
				Version = stream.ReadUInt32();
				VolumeInfo = stream.ReadUInt32();
				CabDescriptorOffset = stream.ReadUInt32();
				CabDescriptorSize = stream.ReadUInt32();
			}
		}

		readonly struct VolumeHeader
		{
			public readonly uint DataOffset;
			public readonly uint DataOffsetHigh;
			public readonly uint FirstFileIndex;
			public readonly uint LastFileIndex;

			public readonly uint FirstFileOffset;
			public readonly uint FirstFileOffsetHigh;
			public readonly uint FirstFileSizeExpanded;
			public readonly uint FirstFileSizeExpandedHigh;

			public readonly uint FirstFileSizeCompressed;
			public readonly uint FirstFileSizeCompressedHigh;
			public readonly uint LastFileOffset;
			public readonly uint LastFileOffsetHigh;

			public readonly uint LastFileSizeExpanded;
			public readonly uint LastFileSizeExpandedHigh;
			public readonly uint LastFileSizeCompressed;
			public readonly uint LastFileSizeCompressedHigh;

			public VolumeHeader(Stream stream)
			{
				DataOffset = stream.ReadUInt32();
				DataOffsetHigh = stream.ReadUInt32();

				FirstFileIndex = stream.ReadUInt32();
				LastFileIndex = stream.ReadUInt32();
				FirstFileOffset = stream.ReadUInt32();
				FirstFileOffsetHigh = stream.ReadUInt32();

				FirstFileSizeExpanded = stream.ReadUInt32();
				FirstFileSizeExpandedHigh = stream.ReadUInt32();
				FirstFileSizeCompressed = stream.ReadUInt32();
				FirstFileSizeCompressedHigh = stream.ReadUInt32();

				LastFileOffset = stream.ReadUInt32();
				LastFileOffsetHigh = stream.ReadUInt32();
				LastFileSizeExpanded = stream.ReadUInt32();
				LastFileSizeExpandedHigh = stream.ReadUInt32();

				LastFileSizeCompressed = stream.ReadUInt32();
				LastFileSizeCompressedHigh = stream.ReadUInt32();
			}
		}

		class CabExtracter
		{
			readonly FileDescriptor file;
			readonly Dictionary<int, Stream> volumes;

			uint remainingInArchive;
			uint toExtract;

			int currentVolumeID;
			Stream currentVolume;

			public CabExtracter(FileDescriptor file, Dictionary<int, Stream> volumes)
			{
				this.file = file;
				this.volumes = volumes;

				remainingInArchive = 0;
				toExtract = file.Flags.HasFlag(CABFlags.FileCompressed) ? file.CompressedSize : file.ExpandedSize;

				SetVolume(file.Volume);
			}

			public void CopyTo(Stream output, Action<int> onProgress)
			{
				if (file.Flags.HasFlag(CABFlags.FileCompressed))
				{
					var inf = new Inflater(true);
					var buffer = new byte[165535];
					do
					{
						var bytesToExtract = currentVolume.ReadUInt16();
						remainingInArchive -= 2;
						toExtract -= 2;
						inf.SetInput(GetBytes(bytesToExtract));
						toExtract -= bytesToExtract;
						while (!inf.IsNeedingInput)
						{
							onProgress?.Invoke((int)(100 * output.Position / file.ExpandedSize));

							var inflated = inf.Inflate(buffer);
							output.Write(buffer, 0, inflated);
						}

						inf.Reset();
					}
					while (toExtract > 0);
				}
				else
				{
					do
					{
						onProgress?.Invoke((int)(100 * output.Position / file.ExpandedSize));

						toExtract -= remainingInArchive;
						output.Write(GetBytes(remainingInArchive), 0, (int)remainingInArchive);
					}
					while (toExtract > 0);
				}
			}

			public byte[] GetBytes(uint count)
			{
				if (count < remainingInArchive)
				{
					remainingInArchive -= count;
					return currentVolume.ReadBytes((int)count);
				}
				else
				{
					var outArray = new byte[count];
					var read = currentVolume.Read(outArray, 0, (int)remainingInArchive);
					if (toExtract > remainingInArchive)
					{
						SetVolume(currentVolumeID + 1);
						remainingInArchive -= (uint)currentVolume.Read(outArray, read, (int)count - read);
					}

					return outArray;
				}
			}

			void SetVolume(int newVolume)
			{
				currentVolumeID = newVolume;
				if (!volumes.TryGetValue(currentVolumeID, out currentVolume))
					throw new FileNotFoundException($"Volume {currentVolumeID} is not available");

				currentVolume.Position = 0;
				if (currentVolume.ReadUInt32() != 0x28635349)
					throw new InvalidDataException("Not an Installshield CAB package");

				uint fileOffset;
				if (file.Flags.HasFlag(CABFlags.FileSplit))
				{
					currentVolume.Position += CommonHeader.Size;
					var head = new VolumeHeader(currentVolume);
					if (file.Index == head.LastFileIndex)
					{
						if (file.Flags.HasFlag(CABFlags.FileCompressed))
							remainingInArchive = head.LastFileSizeCompressed;
						else
							remainingInArchive = head.LastFileSizeExpanded;

						fileOffset = head.LastFileOffset;
					}
					else if (file.Index == head.FirstFileIndex)
					{
						if (file.Flags.HasFlag(CABFlags.FileCompressed))
							remainingInArchive = head.FirstFileSizeCompressed;
						else
							remainingInArchive = head.FirstFileSizeExpanded;

						fileOffset = head.FirstFileOffset;
					}
					else
						throw new InvalidDataException("Cannot Resolve Remaining Stream");
				}
				else
				{
					if (file.Flags.HasFlag(CABFlags.FileCompressed))
						remainingInArchive = file.CompressedSize;
					else
						remainingInArchive = file.ExpandedSize;

					fileOffset = file.DataOffset;
				}

				currentVolume.Position = fileOffset;
			}
		}

		readonly Dictionary<string, FileDescriptor> index = new Dictionary<string, FileDescriptor>();
		readonly Dictionary<int, Stream> volumes;
		readonly uint version;

		public InstallShieldCABCompression(Stream header, Dictionary<int, Stream> volumes)
		{
			this.volumes = volumes;

			if (header.ReadUInt32() != 0x28635349)
				throw new InvalidDataException("Not an Installshield CAB package");

			var versionTmp = header.ReadUInt32();

			// Logic taken from UnShield
			// https://github.com/twogood/unshield/blob/1.5.1/lib/libunshield.c#L277-L288
			if (versionTmp >> 24 == 1)
				version = (versionTmp >> 12) & 0xf;
			else if (versionTmp >> 24 == 2 || versionTmp >> 24 == 4)
			{
				version = versionTmp & 0xffff;
				if (version != 0)
					version /= 100;
			}

			header.Position += 4;
			var cabDescriptorOffset = header.ReadUInt32();
			header.Position = cabDescriptorOffset + 12;
			var cabDescriptor = new CabDescriptor(header);
			header.Position += 14;

			var fileGroupOffsets = new uint[MaxFileGroupCount];
			for (var i = 0; i < MaxFileGroupCount; i++)
				fileGroupOffsets[i] = header.ReadUInt32();

			header.Position = cabDescriptorOffset + cabDescriptor.FileTableOffset;
			var directories = new DirectoryDescriptor[cabDescriptor.DirectoryCount];
			for (var i = 0; i < directories.Length; i++)
				directories[i] = new DirectoryDescriptor(header, cabDescriptorOffset + cabDescriptor.FileTableOffset);

			var fileGroups = new List<FileGroup>();
			foreach (var offset in fileGroupOffsets)
			{
				var nextOffset = offset;
				while (nextOffset != 0)
				{
					header.Position = cabDescriptorOffset + (long)nextOffset + 4;
					var descriptorOffset = header.ReadUInt32();
					nextOffset = header.ReadUInt32();
					header.Position = cabDescriptorOffset + descriptorOffset;

					fileGroups.Add(new FileGroup(header, cabDescriptorOffset, version));
				}
			}

			header.Position = cabDescriptorOffset + cabDescriptor.FileTableOffset + cabDescriptor.FileTableOffset2;
			foreach (var fileGroup in fileGroups)
			{
				for (var i = fileGroup.FirstFile; i <= fileGroup.LastFile; i++)
				{
					if (version <= 5)
					{
						header.Position = cabDescriptorOffset +	cabDescriptor.FileTableOffset + cabDescriptor.FileTableOffset2 + i * 4;
						header.Position = cabDescriptorOffset +	cabDescriptor.FileTableOffset + header.ReadUInt32();
					}
					else
						header.Position = cabDescriptorOffset +	cabDescriptor.FileTableOffset + cabDescriptor.FileTableOffset2 + i * 0x57;

					var file = new FileDescriptor(header, i, cabDescriptorOffset + cabDescriptor.FileTableOffset, version);
					var path = $"{fileGroup.Name}\\{directories[file.DirectoryIndex].Name}\\{file.Filename}";
					index[path] = file;
				}
			}
		}

		public void ExtractFile(string filename, Stream output, Action<int> onProgress = null)
		{
			if (!index.TryGetValue(filename, out var file))
				throw new FileNotFoundException(filename);

			ExtractFile(file, output, onProgress);
		}

		void ExtractFile(FileDescriptor file, Stream output, Action<int> onProgress = null)
		{
			if (file.Flags.HasFlag(CABFlags.FileInvalid))
				throw new InvalidDataException("File Invalid");

			if (file.LinkFlags.HasFlag(LinkFlags.Prev))
			{
				var prev = index.Values.First(f => f.Index == file.LinkToPrevious);
				ExtractFile(prev, output, onProgress);
				return;
			}

			if (file.Flags.HasFlag(CABFlags.FileObfuscated))
				throw new NotImplementedException("Obfuscated files are not supported");

			var extracter = new CabExtracter(file, volumes);
			extracter.CopyTo(output, onProgress);

			if (output.Length != file.ExpandedSize)
				throw new InvalidDataException($"File expanded to wrong length. Expected = {file.ExpandedSize}, Got = {output.Length}");
		}

		public IReadOnlyDictionary<int, IEnumerable<string>> Contents
		{
			get
			{
				var contents = new Dictionary<int, List<string>>();
				foreach (var kv in index)
					contents.GetOrAdd(kv.Value.Volume).Add(kv.Key);

				return new ReadOnlyDictionary<int, IEnumerable<string>>(contents
					.ToDictionary(x => x.Key, x => x.Value.AsEnumerable()));
			}
		}
	}
}
