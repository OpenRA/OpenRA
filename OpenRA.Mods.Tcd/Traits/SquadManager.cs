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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Tcd.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Tracks persistent squads. Client-side only: squad membership never reaches",
		"the simulation, so it cannot desync. Attach this to the world actor.")]
	public sealed class SquadManagerInfo : TraitInfo
	{
		[Desc("Maximum number of actors allowed in a single squad. Use 0 for no limit.")]
		public readonly int MaxSquadSize = 24;

		[Desc("Gap between neighbouring units in a formation, in whole cells.",
			"Whole cells only: anything else lands between cells and leaves uneven gaps.")]
		public readonly int FormationSpacingCells = 1;

		[Desc("Widest row a formation will build before wrapping onto another row.")]
		public readonly int FormationMaxRowWidth = 8;

		public override object Create(ActorInitializer init) { return new SquadManager(init.World, this); }
	}

	public sealed class Squad
	{
		public readonly int Id;

		readonly List<Actor> members;

		public IReadOnlyList<Actor> Members => members;
		public bool IsEmpty => members.Count == 0;

		public Squad(int id, List<Actor> members)
		{
			Id = id;
			this.members = members;
		}

		public bool Contains(Actor a) { return members.Contains(a); }

		public int RemoveAll(Predicate<Actor> match) { return members.RemoveAll(match); }
	}

	public sealed class SquadManager : ITick
	{
		readonly World world;
		readonly SquadManagerInfo info;
		readonly List<Squad> squads = [];
		readonly Predicate<Actor> isStale;

		int nextId = 1;

		public SquadManager(World world, SquadManagerInfo info)
		{
			this.world = world;
			this.info = info;

			// Built once rather than per tick: Tick runs on every frame of every game.
			isStale = a => a.IsDead || !a.IsInWorld || a.Owner != world.LocalPlayer;
		}

		public IReadOnlyList<Squad> Squads => squads;

		public int FormationSpacingCells => info.FormationSpacingCells;

		public int FormationMaxRowWidth => info.FormationMaxRowWidth;

		public bool TryGetSquad(Actor a, out Squad squad)
		{
			foreach (var s in squads)
			{
				if (s.Contains(a))
				{
					squad = s;
					return true;
				}
			}

			squad = null;
			return false;
		}

		// Pulls the given actors out of any squad they already belong to, then groups them.
		public Squad Form(IEnumerable<Actor> actors)
		{
			var members = actors
				.Where(a => a.Owner == world.LocalPlayer && a.IsInWorld && !a.IsDead)
				.Distinct()
				.ToList();

			if (info.MaxSquadSize > 0 && members.Count > info.MaxSquadSize)
				members.RemoveRange(info.MaxSquadSize, members.Count - info.MaxSquadSize);

			if (members.Count == 0)
				return null;

			// An actor belongs to at most one squad.
			foreach (var s in squads)
				s.RemoveAll(members.Contains);

			squads.RemoveAll(s => s.IsEmpty);

			var squad = new Squad(nextId++, members);
			squads.Add(squad);
			return squad;
		}

		public int DisbandContaining(IEnumerable<Actor> actors)
		{
			var doomed = new HashSet<Squad>();
			foreach (var a in actors)
				if (TryGetSquad(a, out var squad))
					doomed.Add(squad);

			return squads.RemoveAll(doomed.Contains);
		}

		void ITick.Tick(Actor self)
		{
			foreach (var s in squads)
				s.RemoveAll(isStale);

			squads.RemoveAll(s => s.IsEmpty);
		}
	}
}
