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
using System.Linq;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Common.Installer
{
	public class InstallerUtils
	{
		public static bool IsValidSourcePath(string path, ModContent.ModSource source)
		{
			try
			{
				foreach (var kv in source.IDFiles.Nodes)
				{
					var filePath = FS.ResolveCaseInsensitivePath(Path.Combine(path, kv.Key));
					if (!File.Exists(filePath))
						return false;

					using (var fileStream = File.OpenRead(filePath))
					{
						var offsetNode = kv.Value.Nodes.FirstOrDefault(n => n.Key == "Offset");
						var lengthNode = kv.Value.Nodes.FirstOrDefault(n => n.Key == "Length");
						if (offsetNode != null || lengthNode != null)
						{
							var offset = 0L;
							if (offsetNode != null)
								offset = FieldLoader.GetValue<long>("Offset", offsetNode.Value.Value);

							var length = fileStream.Length - offset;
							if (lengthNode != null)
								length = FieldLoader.GetValue<long>("Length", lengthNode.Value.Value);

							fileStream.Position = offset;
							var data = fileStream.ReadBytes((int)length);
							if (CryptoUtil.SHA1Hash(data) != kv.Value.Value)
								return false;
						}
						else if (CryptoUtil.SHA1Hash(fileStream) != kv.Value.Value)
							return false;
					}
				}
			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}

		public static void CopyStream(Stream input, Stream output, long length, Action<long> onProgress = null)
		{
			var buffer = new byte[4096];
			var copied = 0L;
			while (copied < length)
			{
				var read = (int)Math.Min(buffer.Length, length - copied);
				var write = input.Read(buffer, 0, read);
				output.Write(buffer, 0, write);
				copied += write;

				onProgress?.Invoke(copied);
			}
		}
	}
}
