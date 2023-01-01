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
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using OpenRA.Graphics;
using OpenRA.Mods.Common.SpriteLoaders;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.SpriteLoaders
{
	public class ShpRemasteredLoader : ISpriteLoader
	{
		public static bool IsShpRemastered(Stream s)
		{
			var start = s.Position;
			var isZipFile = s.ReadUInt32() == 0x04034B50;
			s.Position = start;

			return isZipFile;
		}

		public bool TryParseSprite(Stream s, string filename, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;
			if (!IsShpRemastered(s))
			{
				frames = null;
				return false;
			}

			frames = new ShpRemasteredSprite(s).Frames.ToArray();
			return true;
		}
	}

	public class ShpRemasteredSprite
	{
		static readonly Regex FilenameRegex = new Regex(@"^(?<prefix>.+?[\-_])(?<frame>\d{4})\.tga$");
		static readonly Regex MetaRegex = new Regex(@"^\{""size"":\[(?<width>\d+),(?<height>\d+)\],""crop"":\[(?<left>\d+),(?<top>\d+),(?<right>\d+),(?<bottom>\d+)\]\}$");

		int ParseGroup(Match match, string group)
		{
			return int.Parse(match.Groups[group].Value);
		}

		public IReadOnlyList<ISpriteFrame> Frames { get; }

		public ShpRemasteredSprite(Stream stream)
		{
			var container = new ZipFile(stream);

			string framePrefix = null;
			var frameCount = 0;
			foreach (ZipEntry entry in container)
			{
				var match = FilenameRegex.Match(entry.Name);
				if (!match.Success)
					continue;

				var prefix = match.Groups["prefix"].Value;
				if (framePrefix == null)
					framePrefix = prefix;

				if (prefix != framePrefix)
					throw new InvalidDataException($"Frame prefix mismatch: `{prefix}` != `{framePrefix}`");

				frameCount = Math.Max(frameCount, int.Parse(match.Groups["frame"].Value) + 1);
			}

			var frames = new ISpriteFrame[frameCount];
			for (var i = 0; i < frames.Length; i++)
			{
				var tgaEntry = container.GetEntry($"{framePrefix}{i:D4}.tga");

				// Blank frame
				if (tgaEntry == null)
				{
					frames[i] = new TgaSprite.TgaFrame();
					continue;
				}

				var metaEntry = container.GetEntry($"{framePrefix}{i:D4}.meta");
				using (var tgaStream = container.GetInputStream(tgaEntry))
				{
					var metaStream = metaEntry != null ? container.GetInputStream(metaEntry) : null;
					if (metaStream != null)
					{
						var meta = MetaRegex.Match(metaStream.ReadAllText());
						var crop = Rectangle.FromLTRB(
							ParseGroup(meta, "left"), ParseGroup(meta, "top"),
							ParseGroup(meta, "right"), ParseGroup(meta, "bottom"));

						var frameSize = new Size(ParseGroup(meta, "width"), ParseGroup(meta, "height"));
						frames[i] = new TgaSprite.TgaFrame(tgaStream, frameSize, crop);
						metaStream.Dispose();
					}
					else
						frames[i] = new TgaSprite.TgaFrame(tgaStream);
				}
			}

			Frames = frames;
		}
	}
}
