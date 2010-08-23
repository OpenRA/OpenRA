#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA
{
	public class Selection
	{
		List<Actor> actors = new List<Actor>();
		public void Add(World w, Actor a)
		{
			actors.Add(a);
			foreach (var ns in w.WorldActor.TraitsImplementing<INotifySelection>())
				ns.SelectionChanged();
		}
		
		public bool Contains(Actor a)
		{
			return actors.AsEnumerable().Contains(a);
		}

		public void Combine(World world, IEnumerable<Actor> newSelection, bool isCombine, bool isClick)
		{
			var oldSelection = actors.AsEnumerable();

			if (isClick)
			{
				var adjNewSelection = newSelection.Take(1);	/* todo: select BEST, not FIRST */
				actors = (isCombine ? oldSelection.SymmetricDifference(adjNewSelection) : adjNewSelection).ToList();
			}
			else
				actors = (isCombine ? oldSelection.Union(newSelection) : newSelection).ToList();

			var voicedUnit = actors.FirstOrDefault(a => a.Owner == world.LocalPlayer && a.HasVoice());
			if (voicedUnit != null)
				Sound.PlayVoice("Select", voicedUnit, voicedUnit.Owner.Country.Race);

			foreach (var ns in world.WorldActor.TraitsImplementing<INotifySelection>())
				ns.SelectionChanged();
		}

		public IEnumerable<Actor> Actors { get { return actors; } }
		public void Clear() { actors = new List<Actor>(); }

		public void Tick(World world)
		{
			actors.RemoveAll(a => !a.IsInWorld);
		}

		Cache<int, List<Actor>> controlGroups = new Cache<int, List<Actor>>(_ => new List<Actor>());

		public void DoControlGroup(World world, int group, Modifiers mods)
		{
			if (mods.HasModifier(Modifiers.Ctrl))
			{
				if (actors.Count == 0)
					return;

				controlGroups[group].Clear();

				for (var i = 0; i < 10; i++)	/* all control groups */
					controlGroups[i].RemoveAll(a => actors.Contains(a));

				controlGroups[group].AddRange(actors);
				return;
			}

			if (mods.HasModifier(Modifiers.Alt))
			{
				Game.viewport.Center(controlGroups[group]);
				return;
			}

			Combine(world, controlGroups[group],
				mods.HasModifier(Modifiers.Shift), false);
		}

		public int? GetControlGroupForActor(Actor a)
		{
			return controlGroups.Where(g => g.Value.Contains(a))
				.Select(g => (int?)g.Key)
				.FirstOrDefault();
		}
	}
}
