using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Traits;
using OpenRa.FileFormats;

namespace OpenRa
{
	public class Selection
	{
		List<Actor> actors = new List<Actor>();

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

			var voicedUnit = actors.FirstOrDefault(a => a.traits.Contains<Unit>() && a.Owner == world.LocalPlayer);
			Sound.PlayVoice("Select", voicedUnit);

			foreach (var ns in world.WorldActor.traits.WithInterface<INotifySelection>())
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
