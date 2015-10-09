#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;

namespace OpenRA.GameRules
{
	public class MusicInfo
	{
		public readonly string Filename;
		public readonly string Title;
		public readonly bool Hidden;

		public int Length { get; private set; } // seconds
		public bool Exists { get; private set; }

		public MusicInfo(string key, MiniYaml value)
		{
			Title = value.Value;

			var nd = value.ToDictionary();
			if (nd.ContainsKey("Hidden"))
				bool.TryParse(nd["Hidden"].Value, out Hidden);

			var ext = nd.ContainsKey("Extension") ? nd["Extension"].Value : "aud";
			Filename = (nd.ContainsKey("Filename") ? nd["Filename"].Value : key) + "." + ext;
		}

		public void Load()
		{
			if (!Game.ModData.ModFiles.Exists(Filename))
				return;

			Exists = true;
			using (var s = Game.ModData.ModFiles.Open(Filename))
			{
				if (Filename.ToLowerInvariant().EndsWith("wav"))
					Length = (int)WavLoader.WaveLength(s);
				else
					Length = (int)AudLoader.SoundLength(s);
			}
		}
	}
}
