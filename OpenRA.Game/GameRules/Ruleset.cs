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

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA
{
	public class Ruleset
	{
		public readonly IReadOnlyDictionary<string, ActorInfo> Actors;
		public readonly IReadOnlyDictionary<string, WeaponInfo> Weapons;
		public readonly IReadOnlyDictionary<string, SoundInfo> Voices;
		public readonly IReadOnlyDictionary<string, SoundInfo> Notifications;
		public readonly IReadOnlyDictionary<string, MusicInfo> Music;
		public readonly IReadOnlyDictionary<string, TileSet> TileSets;
		public readonly IReadOnlyDictionary<string, SequenceProvider> Sequences;

		public Ruleset(
			IDictionary<string, ActorInfo> actors,
			IDictionary<string, WeaponInfo> weapons,
			IDictionary<string, SoundInfo> voices,
			IDictionary<string, SoundInfo> notifications,
			IDictionary<string, MusicInfo> music,
			IDictionary<string, TileSet> tileSets,
			IDictionary<string, SequenceProvider> sequences)
		{
			Actors = new ReadOnlyDictionary<string, ActorInfo>(actors);
			Weapons = new ReadOnlyDictionary<string, WeaponInfo>(weapons);
			Voices = new ReadOnlyDictionary<string, SoundInfo>(voices);
			Notifications = new ReadOnlyDictionary<string, SoundInfo>(notifications);
			Music = new ReadOnlyDictionary<string, MusicInfo>(music);
			TileSets = new ReadOnlyDictionary<string, TileSet>(tileSets);
			Sequences = new ReadOnlyDictionary<string, SequenceProvider>(sequences);

			foreach (var a in Actors.Values)
			{
				foreach (var t in a.TraitInfos<IRulesetLoaded>())
				{
					try
					{
						t.RulesetLoaded(this, a);
					}
					catch (YamlException e)
					{
						throw new YamlException("Actor type {0}: {1}".F(a.Name, e.Message));
					}
				}
			}

			foreach (var weapon in Weapons)
			{
				foreach (var warhead in weapon.Value.Warheads)
				{
					var cacher = warhead as IRulesetLoaded<WeaponInfo>;
					if (cacher != null)
					{
						try
						{
							cacher.RulesetLoaded(this, weapon.Value);
						}
						catch (YamlException e)
						{
							throw new YamlException("Weapon type {0}: {1}".F(weapon.Key, e.Message));
						}
					}
				}
			}
		}

		public IEnumerable<KeyValuePair<string, MusicInfo>> InstalledMusic { get { return Music.Where(m => m.Value.Exists); } }
	}
}
