#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.FileSystem;

namespace OpenRA.GameRules
{
	public class MusicInfo
	{
		public readonly string Filename = null;
		public readonly string Title = null;
		public int Length { get; private set; } // seconds
		public bool Exists { get; private set; }

		public MusicInfo( string key, MiniYaml value )
		{
			Title = value.Value;

			var nd = value.ToDictionary();
			var ext = nd.ContainsKey("Extension") ? nd["Extension"].Value : "aud";
			Filename = (nd.ContainsKey("Filename") ? nd["Filename"].Value : key)+"."+ext;
			if (!GlobalFileSystem.Exists(Filename))
				return;

			Exists = true;
			using (var s = GlobalFileSystem.Open(Filename))
				if (Filename.ToLowerInvariant().EndsWith("wav"))
					Length = (int)WavLoader.WaveLength(s);
				else
					Length = (int)AudLoader.SoundLength(s);
		}

		public void Reload()
		{
			if (!GlobalFileSystem.Exists(Filename))
				return;

			Exists = true;
			using (var s = GlobalFileSystem.Open(Filename))
				if (Filename.ToLowerInvariant().EndsWith("wav"))
					Length = (int)WavLoader.WaveLength(s);
				else
					Length = (int)AudLoader.SoundLength(s);
		}
	}
}
