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
using System.IO;
using System.Text;

namespace OpenRA.FileFormats
{
	public class ReplayMetadata
	{
		// Must be an invalid replay 'client' value
		public const int MetaStartMarker = -1;
		public const int MetaEndMarker = -2;
		public const int MetaVersion = 0x00000001;

		public readonly GameInformation GameInfo;
		public string FilePath { get; private set; }

		public ReplayMetadata(GameInformation info)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			GameInfo = info;
		}

		ReplayMetadata(FileStream fs, string path)
		{
			FilePath = path;

			// Read start marker
			if (fs.ReadInt32() != MetaStartMarker)
				throw new InvalidOperationException("Expected MetaStartMarker but found an invalid value.");

			// Read version
			var version = fs.ReadInt32();
			if (version != MetaVersion)
				throw new NotSupportedException($"Metadata version {version} is not supported");

			// Read game info (max 100K limit as a safeguard against corrupted files)
			var data = fs.ReadString(Encoding.UTF8, 1024 * 100);
			GameInfo = GameInformation.Deserialize(data);
		}

		public void Write(BinaryWriter writer)
		{
			// Write start marker & version
			writer.Write(MetaStartMarker);
			writer.Write(MetaVersion);

			// Write data
			var dataLength = 0;
			{
				// Write lobby info data
				writer.Flush();
				dataLength += writer.BaseStream.WriteString(Encoding.UTF8, GameInfo.Serialize());
			}

			// Write total length & end marker
			writer.Write(dataLength);
			writer.Write(MetaEndMarker);
		}

		public void RenameFile(string newFilenameWithoutExtension)
		{
			var newPath = Path.Combine(Path.GetDirectoryName(FilePath), newFilenameWithoutExtension) + ".orarep";
			File.Move(FilePath, newPath);
			FilePath = newPath;
		}

		public static ReplayMetadata Read(string path)
		{
			try
			{
				using (var fs = new FileStream(path, FileMode.Open))
				{
					if (!fs.CanSeek)
						return null;

					if (fs.Length < 20)
						return null;

					fs.Seek(-(4 + 4), SeekOrigin.End);
					var dataLength = fs.ReadInt32();
					if (fs.ReadInt32() == MetaEndMarker)
					{
						// Go back by (end marker + length storage + data + version + start marker) bytes
						fs.Seek(-(4 + 4 + dataLength + 4 + 4), SeekOrigin.Current);
						return new ReplayMetadata(fs, path);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Write("debug", ex.ToString());
			}

			return null;
		}
	}
}
