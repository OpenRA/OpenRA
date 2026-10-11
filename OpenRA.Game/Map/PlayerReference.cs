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

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA
{
	public class PlayerReference
	{
		static readonly PlayerReference Empty = new();
		static readonly FrozenSet<string> ReservedFields = FieldLoader.GetTypeLoadInfo(typeof(PlayerReference)).Select(l => l.Field.Name).ToFrozenSet();

		[FieldLoader.Ignore]
		readonly ActorReference actorReference = new(SystemActors.Player.ToString(), new MiniYaml(""));

		public string Name;
		public string Palette;
		public string Bot = null;
		public bool AllowBots = true;
		public bool Playable = false;
		public bool Required = false;
		public bool OwnsWorld = false;
		public bool Spectating = false;
		public bool NonCombatant = false;

		public bool LockFaction = false;
		public string Faction;

		public bool LockColor = false;
		public Color Color = Game.ModData.GetOrCreate<DefaultPlayer>().Color;

		public bool LockSpawn = false;

		/// <summary>
		/// Sets the initial spawn point index that is used to override the "Home" location for client (lobby slot) players.
		/// Map players always ignore this and use HomeLocation directly.
		/// </summary>
		public int Spawn = 0;

		public bool LockTeam = false;
		public int Team = 0;

		public bool LockHandicap = false;
		public int Handicap = 0;

		public ImmutableArray<string> Allies = [];
		public ImmutableArray<string> Enemies = [];

		public TypeDictionary Inits => actorReference.InitDict;

		public PlayerReference() { }
		public PlayerReference(MiniYaml my)
		{
			FieldLoader.Load(this, my);

			var initsYaml = new MiniYaml("", my.Nodes.Where(n => !ReservedFields.Contains(n.Key)));
			actorReference = new ActorReference(SystemActors.Player.ToString(), initsYaml);
		}

		public override string ToString() { return Name; }

		public MiniYaml ToMiniYaml()
		{
			var yaml = FieldSaver.SaveDifferences(this, Empty);
			if (actorReference.InitDict.Any())
			{
				var inits = actorReference.Save();
				yaml = yaml.WithNodesAppended(inits.Nodes);
			}

			return yaml;
		}
	}
}
