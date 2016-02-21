#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace OpenRA.FileSystem
{
	public sealed class InstallShieldCABExtractor : IReadOnlyPackage
	{
		const uint FileSplit = 0x1;
		const uint FileObfuscated = 0x2;
		const uint FileCompressed = 0x4;
		const uint FileInvalid = 0x8;

		const uint LinkPrev = 0x1;
		const uint LinkNext = 0x2;
		const uint MaxFileGroupCount = 71;

#region Nested Structs

		struct FileGroup
		{
			public readonly string Name;
			public readonly uint FirstFile;
			public readonly uint LastFile;

			public FileGroup(Stream reader, long offset)
			{
				var nameOffset = reader.ReadUInt32();
				/*   unknown  */ reader.ReadBytes(18);
				FirstFile = reader.ReadUInt32();
				LastFile = reader.ReadUInt32();

				reader.Seek(offset + nameOffset, SeekOrigin.Begin);
				Name = reader.ReadASCIIZ();
			}
		}

		struct VolumeHeader
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

			public VolumeHeader(Stream reader)
			{
				DataOffset = reader.ReadUInt32();
				DataOffsetHigh = reader.ReadUInt32();

				FirstFileIndex = reader.ReadUInt32();
				LastFileIndex = reader.ReadUInt32();
				FirstFileOffset = reader.ReadUInt32();
				FirstFileOffsetHigh = reader.ReadUInt32();

				FirstFileSizeExpanded = reader.ReadUInt32();
				FirstFileSizeExpandedHigh = reader.ReadUInt32();
				FirstFileSizeCompressed = reader.ReadUInt32();
				FirstFileSizeCompressedHigh = reader.ReadUInt32();

				LastFileOffset = reader.ReadUInt32();
				LastFileOffsetHigh = reader.ReadUInt32();
				LastFileSizeExpanded = reader.ReadUInt32();
				LastFileSizeExpandedHigh = reader.ReadUInt32();

				LastFileSizeCompressed = reader.ReadUInt32();
				LastFileSizeCompressedHigh = reader.ReadUInt32();
			}
		}

		struct CommonHeader
		{
			public const long Size = 16;
			public readonly uint Version;
			public readonly uint VolumeInfo;
			public readonly long CabDescriptorOffset;
			public readonly uint CabDescriptorSize;

			public CommonHeader(Stream reader)
			{
				Version = reader.ReadUInt32();
				VolumeInfo = reader.ReadUInt32();
				CabDescriptorOffset = reader.ReadUInt32();
				CabDescriptorSize = reader.ReadUInt32();
			}
		}

		struct CabDescriptor
		{
			public readonly long FileTableOffset;
			public readonly uint FileTableSize;
			public readonly uint FileTableSize2;
			public readonly uint DirectoryCount;

			public readonly uint FileCount;
			public readonly long FileTableOffset2;

			public CabDescriptor(Stream reader, CommonHeader commonHeader)
			{
				reader.Seek(commonHeader.CabDescriptorOffset + 12, SeekOrigin.Begin);
				FileTableOffset = reader.ReadUInt32();
				/*    unknown  */ reader.ReadUInt32();
				FileTableSize = reader.ReadUInt32();

				FileTableSize2 = reader.ReadUInt32();
				DirectoryCount = reader.ReadUInt32();
				/*   unknown  */ reader.ReadBytes(8);
				FileCount = reader.ReadUInt32();

				FileTableOffset2 = reader.ReadUInt32();
			}
		}

		struct FileDescriptor
		{
			public readonly ushort	Flags;
			public readonly uint	ExpandedSize;
			public readonly uint	CompressedSize;
			public readonly uint	DataOffset;

			public readonly byte[]	MD5;
			public readonly uint	NameOffset;
			public readonly ushort	DirectoryIndex;
			public readonly uint	LinkToPrevious;

			public readonly uint	LinkToNext;
			public readonly byte	LinkFlags;
			public readonly ushort	Volume;
			public readonly string	Filename;

			public FileDescriptor(Stream reader, long tableOffset)
			{
				Flags = reader.ReadUInt16();
				ExpandedSize = reader.ReadUInt32();
				/*    unknown   */ reader.ReadUInt32();
				CompressedSize = reader.ReadUInt32();

				/*  unknown */ reader.ReadUInt32();
				DataOffset   = reader.ReadUInt32();
				/*  unknown */ reader.ReadUInt32();
				MD5          = reader.ReadBytes(16);

				/*   unknown  */ reader.ReadBytes(16);
				NameOffset     = reader.ReadUInt32();
				DirectoryIndex = reader.ReadUInt16();
				/*   unknown  */ reader.ReadBytes(12);
				LinkToPrevious = reader.ReadUInt32();
				LinkToNext = reader.ReadUInt32();

				LinkFlags = reader.ReadBytes(1)[0];
				Volume = reader.ReadUInt16();
				var posSave = reader.Position;

				reader.Seek(tableOffset + NameOffset, SeekOrigin.Begin);
				Filename = reader.ReadASCIIZ();
				reader.Seek(posSave, SeekOrigin.Begin);
			}
		}

		class CabReader : IDisposable
		{
			readonly FileSystem context;
			readonly FileDescriptor fileDes;
			public uint RemainingArchiveStream;
			public uint RemainingFileStream;
			readonly uint index;
			readonly string commonName;
			ushort volumeNumber;
			Stream cabFile;

			public CabReader(FileSystem context, FileDescriptor fileDes, uint index, string commonName)
			{
				this.fileDes = fileDes;
				this.index = index;
				this.commonName = commonName;
				this.context = context;
				volumeNumber = (ushort)(fileDes.Volume - 1u);
				RemainingArchiveStream = 0;
				if ((fileDes.Flags & FileCompressed) > 0)
					RemainingFileStream = fileDes.CompressedSize;
				else
					RemainingFileStream = fileDes.ExpandedSize;

				cabFile = null;
				NextFile(context);
			}

			public void CopyTo(Stream dest)
			{
				if ((fileDes.Flags & FileCompressed) != 0)
				{
					var inf = new Inflater(true);
					var buffer = new byte[165535];
					do
					{
						var bytesToExtract = cabFile.ReadUInt16();
						RemainingArchiveStream -= 2u;
						RemainingFileStream -= 2u;
						inf.SetInput(GetBytes(bytesToExtract));
						RemainingFileStream -= bytesToExtract;
						while (!inf.IsNeedingInput)
						{
							var inflated = inf.Inflate(buffer);
							dest.Write(buffer, 0, inflated);
						}

						inf.Reset();
					}
					while (RemainingFileStream > 0);
				}
				else
				{
					do
					{
						RemainingFileStream -= RemainingArchiveStream;
						dest.Write(GetBytes(RemainingArchiveStream), 0, (int)RemainingArchiveStream);
					}
					while (RemainingFileStream > 0);
				}
			}

			public byte[] GetBytes(uint count)
			{
				if (count < RemainingArchiveStream)
				{
					RemainingArchiveStream -= count;
					return cabFile.ReadBytes((int)count);
				}
				else
				{
					var outArray = new byte[count];
					var read = cabFile.Read(outArray, 0, (int)RemainingArchiveStream);
					if (RemainingFileStream > RemainingArchiveStream)
					{
						NextFile(context);
						RemainingArchiveStream -= (uint)cabFile.Read(outArray, read, (int)count - read);
					}

					return outArray;
				}
			}

			public void Dispose()
			{
				cabFile.Dispose();
			}

			void NextFile(FileSystem context)
			{
				if (cabFile != null)
					cabFile.Dispose();

				++volumeNumber;
				cabFile = context.Open("{0}{1}.cab".F(commonName, volumeNumber));
				if (cabFile.ReadUInt32() != 0x28635349)
					throw new InvalidDataException("Not an Installshield CAB package");

				uint fileOffset;
				if ((fileDes.Flags & FileSplit) != 0)
				{
					cabFile.Seek(CommonHeader.Size, SeekOrigin.Current);
					var head = new VolumeHeader(cabFile);
					if (index == head.LastFileIndex)
					{
						if ((fileDes.Flags & FileCompressed) != 0)
							RemainingArchiveStream = head.LastFileSizeCompressed;
						else
							RemainingArchiveStream = head.LastFileSizeExpanded;

						fileOffset = head.LastFileOffset;
					}
					else if (index == head.FirstFileIndex)
					{
						if ((fileDes.Flags & FileCompressed) != 0)
							RemainingArchiveStream = head.FirstFileSizeCompressed;
						else
							RemainingArchiveStream = head.FirstFileSizeExpanded;

						fileOffset = head.FirstFileOffset;
					}
					else
						throw new Exception("Cannot Resolve Remaining Stream");
				}
				else
				{
					if ((fileDes.Flags & FileCompressed) != 0)
						RemainingArchiveStream = fileDes.CompressedSize;
					else
						RemainingArchiveStream = fileDes.ExpandedSize;

					fileOffset = fileDes.DataOffset;
				}

				cabFile.Seek(fileOffset, SeekOrigin.Begin);
			}
		}
#endregion

		readonly Stream hdrFile;
		readonly CommonHeader commonHeader;
		readonly CabDescriptor cabDescriptor;
		readonly List<uint> directoryTable;
		readonly Dictionary<uint, string> directoryNames = new Dictionary<uint, string>();
		readonly Dictionary<uint, FileDescriptor> fileDescriptors = new Dictionary<uint, FileDescriptor>();
		readonly Dictionary<string, uint> index = new Dictionary<string, uint>();
		readonly FileSystem context;

		public string Name { get; private set; }
		public IEnumerable<string> Contents { get { return index.Keys; } }

		public InstallShieldCABExtractor(FileSystem context, string hdrFilename)
		{
			var fileGroups = new List<FileGroup>();
			var fileGroupOffsets = new List<uint>();

			hdrFile = context.Open(hdrFilename);
			this.context = context;

			// Strips archive number AND file extension
			Name = Regex.Replace(hdrFilename, @"\d*\.[^\.]*$", "");
			var signature = hdrFile.ReadUInt32();

			if (signature != 0x28635349)
				throw new InvalidDataException("Not an Installshield CAB package");

			commonHeader = new CommonHeader(hdrFile);
			cabDescriptor = new CabDescriptor(hdrFile, commonHeader);
			/*    unknown   */ hdrFile.ReadBytes(14);

			for (var i = 0U; i < MaxFileGroupCount; ++i)
				fileGroupOffsets.Add(hdrFile.ReadUInt32());

			hdrFile.Seek(commonHeader.CabDescriptorOffset + cabDescriptor.FileTableOffset, SeekOrigin.Begin);
			directoryTable = new List<uint>();

			for (var i = 0U; i < cabDescriptor.DirectoryCount; ++i)
				directoryTable.Add(hdrFile.ReadUInt32());

			foreach (var offset in fileGroupOffsets)
			{
				var nextOffset = offset;
				while (nextOffset != 0)
				{
					hdrFile.Seek((long)nextOffset + 4 + commonHeader.CabDescriptorOffset, SeekOrigin.Begin);
					var descriptorOffset = hdrFile.ReadUInt32();
					nextOffset = hdrFile.ReadUInt32();
					hdrFile.Seek(descriptorOffset + commonHeader.CabDescriptorOffset, SeekOrigin.Begin);

					fileGroups.Add(new FileGroup(hdrFile, commonHeader.CabDescriptorOffset));
				}
			}

			hdrFile.Seek(commonHeader.CabDescriptorOffset + cabDescriptor.FileTableOffset + cabDescriptor.FileTableOffset2, SeekOrigin.Begin);
			foreach (var fileGroup in fileGroups)
			{
				for (var i = fileGroup.FirstFile; i <= fileGroup.LastFile; ++i)
				{
					AddFileDescriptorToList(i);
					var fileDescriptor = fileDescriptors[i];
					var fullFilePath   = "{0}\\{1}\\{2}".F(fileGroup.Name, DirectoryName(fileDescriptor.DirectoryIndex), fileDescriptor.Filename);
					index.Add(fullFilePath, i);
				}
			}
		}

		public string DirectoryName(uint index)
		{
			if (directoryNames.ContainsKey(index))
				return directoryNames[index];

			hdrFile.Seek(commonHeader.CabDescriptorOffset +
					cabDescriptor.FileTableOffset +
					directoryTable[(int)index],
					SeekOrigin.Begin);

			var test = hdrFile.ReadASCIIZ();
			return test;
		}

		public bool Contains(string filename)
		{
			return index.ContainsKey(filename);
		}

		public uint DirectoryCount()
		{
			return cabDescriptor.DirectoryCount;
		}

		public string FileName(uint index)
		{
			if (!fileDescriptors.ContainsKey(index))
				AddFileDescriptorToList(index);

			return fileDescriptors[index].Filename;
		}

		void AddFileDescriptorToList(uint index)
		{
			hdrFile.Seek(commonHeader.CabDescriptorOffset +
					cabDescriptor.FileTableOffset +
					cabDescriptor.FileTableOffset2 +
					index * 0x57,
					SeekOrigin.Begin);

			var fd = new FileDescriptor(hdrFile,
				commonHeader.CabDescriptorOffset + cabDescriptor.FileTableOffset);

			fileDescriptors.Add(index, fd);
		}

		public uint FileCount()
		{
			return cabDescriptor.FileCount;
		}

		public void ExtractFile(uint index, string fileName)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(fileName));
			using (var destfile = File.Open(fileName, FileMode.Create))
				GetContentById(index, destfile);
		}

		public Stream GetContentById(uint index)
		{
			var fileDes = fileDescriptors[index];
			if ((fileDes.Flags & FileInvalid) != 0)
				throw new Exception("File Invalid");

			if ((fileDes.LinkFlags & LinkPrev) != 0)
				return GetContentById(fileDes.LinkToPrevious);

			if ((fileDes.Flags & FileObfuscated) != 0)
				throw new NotImplementedException("Haven't implemented obfuscated files");

			var output = new MemoryStream((int)fileDes.ExpandedSize);

			using (var reader = new CabReader(context, fileDes, index, Name))
				reader.CopyTo(output);

			if (output.Length != fileDes.ExpandedSize)
				throw new Exception("Did not fully extract Expected = {0}, Got = {1}".F(fileDes.ExpandedSize, output.Length));

			output.Position = 0;
			return output;
		}

		public void GetContentById(uint index, Stream output)
		{
			var fileDes = fileDescriptors[index];
			if ((fileDes.Flags & FileInvalid) != 0)
				throw new Exception("File Invalid");

			if ((fileDes.LinkFlags & LinkPrev) != 0)
			{
				GetContentById(fileDes.LinkToPrevious, output);
				return;
			}

			if ((fileDes.Flags & FileObfuscated) != 0)
				throw new NotImplementedException("Haven't implemented obfuscated files");

			using (var reader = new CabReader(context, fileDes, index, Name))
				reader.CopyTo(output);

			if (output.Length != fileDes.ExpandedSize)
				throw new Exception("Did not fully extract Expected = {0}, Got = {1}".F(fileDes.ExpandedSize, output.Length));
		}

		public Stream GetStream(string fileName)
		{
			return GetContentById(index[fileName]);
		}

		public void Dispose()
		{
			hdrFile.Dispose();
		}
	}
}
