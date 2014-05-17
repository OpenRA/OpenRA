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
using OpenRA.Graphics;

namespace OpenRA
{
	public class Ruleset
	{
		public readonly IReadOnlyDictionary<string, ActorInfo> Actors;
		public readonly IReadOnlyDictionary<string, WeaponInfo> Weapons;
		public readonly IReadOnlyDictionary<string, SoundInfo> Voices;
		public readonly IReadOnlyDictionary<string, SoundInfo> Notifications;
		public readonly IReadOnlyDictionary<string, MusicInfo> Music;
		public readonly IReadOnlyDictionary<string, string> Movies;
		public readonly IReadOnlyDictionary<string, TileSet> TileSets;
		public readonly IReadOnlyDictionary<string, SequenceProvider> Sequences;

		public Ruleset(
			IDictionary<string, ActorInfo> actors,
			IDictionary<string, WeaponInfo> weapons,
			IDictionary<string, SoundInfo> voices,
			IDictionary<string, SoundInfo> notifications,
			IDictionary<string, MusicInfo> music,
			IDictionary<string, string> movies,
			IDictionary<string, TileSet> tileSets,
			IDictionary<string, SequenceProvider> sequences)
		{
			Actors = new ReadOnlyDictionary<string, ActorInfo>(actors);
			Weapons = new ReadOnlyDictionary<string, WeaponInfo>(weapons);
			Voices = new ReadOnlyDictionary<string, SoundInfo>(voices);
			Notifications = new ReadOnlyDictionary<string, SoundInfo>(notifications);
			Music = new ReadOnlyDictionary<string, MusicInfo>(music);
			Movies = new ReadOnlyDictionary<string, string>(movies);
			TileSets = new ReadOnlyDictionary<string, TileSet>(tileSets);
			Sequences = new ReadOnlyDictionary<string, SequenceProvider>(sequences);
		}

		public IEnumerable<KeyValuePair<string, MusicInfo>> InstalledMusic { get { return Music.Where(m => m.Value.Exists); } }
	}
}
