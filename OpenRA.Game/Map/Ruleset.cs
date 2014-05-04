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
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;

namespace OpenRA
{
	public class ModRuleset
	{
		public readonly IReadOnlyDictionary<string, MusicInfo> Music;
		public readonly IReadOnlyDictionary<string, string> Movies;
		public readonly IReadOnlyDictionary<string, TileSet> TileSets;

		public ModRuleset(ModRuleset other)
		{
			this.Music = other.Music;
			this.Movies = other.Movies;
			this.TileSets = other.TileSets;
		}

		public ModRuleset(
			IDictionary<string, MusicInfo> music,
			IDictionary<string, string> movies,
			IDictionary<string, TileSet> tileSets)
		{
			this.Music = new ReadOnlyDictionary<string, MusicInfo>(music);
			this.Movies = new ReadOnlyDictionary<string, string>(movies);
			this.TileSets = new ReadOnlyDictionary<string, TileSet>(tileSets);
		}

		public IEnumerable<KeyValuePair<string, MusicInfo>> InstalledMusic { get { return Music.Where(m => m.Value.Exists); } }
	}

	public class MapRuleset : ModRuleset
	{
		public readonly IReadOnlyDictionary<string, ActorInfo> Actors;
		public readonly IReadOnlyDictionary<string, WeaponInfo> Weapons;
		public readonly IReadOnlyDictionary<string, SoundInfo> Voices;
		public readonly IReadOnlyDictionary<string, SoundInfo> Notifications;

		public MapRuleset(
			ModRuleset modRuleset,
			IDictionary<string, ActorInfo> actors,
			IDictionary<string, WeaponInfo> weapons,
			IDictionary<string, SoundInfo> voices,
			IDictionary<string, SoundInfo> notifications)
			: base(modRuleset)
		{
			this.Actors = new ReadOnlyDictionary<string, ActorInfo>(actors);
			this.Weapons = new ReadOnlyDictionary<string, WeaponInfo>(weapons);
			this.Voices = new ReadOnlyDictionary<string, SoundInfo>(voices);
			this.Notifications = new ReadOnlyDictionary<string, SoundInfo>(notifications);
		}
	}
}
