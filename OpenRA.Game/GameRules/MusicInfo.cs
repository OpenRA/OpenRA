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

using OpenRA.FileSystem;

namespace OpenRA.GameRules
{
	public class MusicInfo
	{
		public readonly string Filename;
		public readonly string Title;
		public readonly bool Hidden;
		public readonly float VolumeModifier = 1f;

		public int Length { get; private set; } // seconds
		public bool Exists { get; private set; }

		public MusicInfo(string key, MiniYaml value)
		{
			Title = value.Value;

			var nd = value.ToDictionary();
			if (nd.ContainsKey("Hidden"))
				bool.TryParse(nd["Hidden"].Value, out Hidden);

			if (nd.ContainsKey("VolumeModifier"))
				VolumeModifier = FieldLoader.GetValue<float>("VolumeModifier", nd["VolumeModifier"].Value);

			var ext = nd.ContainsKey("Extension") ? nd["Extension"].Value : "aud";
			Filename = (nd.ContainsKey("Filename") ? nd["Filename"].Value : key) + "." + ext;
		}

		public void Load(IReadOnlyFileSystem fileSystem)
		{
			if (!fileSystem.TryOpen(Filename, out var stream))
				return;

			try
			{
				Exists = true;
				foreach (var loader in Game.ModData.SoundLoaders)
				{
					if (loader.TryParseSound(stream, out var soundFormat))
					{
						Length = (int)soundFormat.LengthInSeconds;
						soundFormat.Dispose();
						break;
					}
				}
			}
			finally
			{
				stream.Dispose();
			}
		}
	}
}
