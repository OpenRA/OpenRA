#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Text;
using OpenRA.Network;

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
				throw new ArgumentNullException("info");

			GameInfo = info;
		}

		ReplayMetadata(BinaryReader reader, string path)
		{
			FilePath = path;

			// Read start marker
			if (reader.ReadInt32() != MetaStartMarker)
				throw new InvalidOperationException("Expected MetaStartMarker but found an invalid value.");

			// Read version
			var version = reader.ReadInt32();
			if (version != MetaVersion)
				throw new NotSupportedException("Metadata version {0} is not supported".F(version));

			// Read game info (max 100K limit as a safeguard against corrupted files)
			string data = ReadUtf8String(reader, 1024 * 100);
			GameInfo = GameInformation.Deserialize(data);
		}

		public void Write(BinaryWriter writer)
		{
			// Write start marker & version
			writer.Write(MetaStartMarker);
			writer.Write(MetaVersion);

			// Write data
			int dataLength = 0;
			{
				// Write lobby info data
				dataLength += WriteUtf8String(writer, GameInfo.Serialize());
			}

			// Write total length & end marker
			writer.Write(dataLength);
			writer.Write(MetaEndMarker);
		}

		public void RenameFile(string newFilenameWithoutExtension)
		{
			var newPath = Path.Combine(Path.GetDirectoryName(FilePath), newFilenameWithoutExtension) + ".rep";
			File.Move(FilePath, newPath);
			FilePath = newPath;
		}

		public static ReplayMetadata Read(string path)
		{
			using (var fs = new FileStream(path, FileMode.Open))
				return Read(fs, path);
		}

		static ReplayMetadata Read(FileStream fs, string path)
		{
			if (!fs.CanSeek)
				return null;

			if (fs.Length < 20)
				return null;

			try
			{
				fs.Seek(-(4 + 4), SeekOrigin.End);
				using (var reader = new BinaryReader(fs))
				{
					var dataLength = reader.ReadInt32();
					if (reader.ReadInt32() == MetaEndMarker)
					{
						// go back by (end marker + length storage + data + version + start marker) bytes
						fs.Seek(-(4 + 4 + dataLength + 4 + 4), SeekOrigin.Current);
						try
						{
							return new ReplayMetadata(reader, path);
						}
						catch (InvalidOperationException ex)
						{
							Log.Write("debug", ex.ToString());
						}
						catch (NotSupportedException ex)
						{
							Log.Write("debug", ex.ToString());
						}
					}
				}
			}
			catch (IOException ex)
			{
				Log.Write("debug", ex.ToString());
			}

			return null;
		}

		static int WriteUtf8String(BinaryWriter writer, string text)
		{
			byte[] bytes;

			if (!string.IsNullOrEmpty(text))
				bytes = Encoding.UTF8.GetBytes(text);
			else
				bytes = new byte[0];

			writer.Write(bytes.Length);
			writer.Write(bytes);

			return 4 + bytes.Length;
		}

		static string ReadUtf8String(BinaryReader reader, int maxLength)
		{
			var length = reader.ReadInt32();
			if (length > maxLength)
				throw new InvalidOperationException("The length of the string ({0}) is longer than the maximum allowed ({1}).".F(length, maxLength));

			return Encoding.UTF8.GetString(reader.ReadBytes(length));
		}
	}
}
