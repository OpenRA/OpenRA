#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA
{
	public class Selection
	{
		List<Actor> actors = new List<Actor>();
		public void Add(World w, Actor a)
		{
			actors.Add(a);
			foreach (var sel in a.TraitsImplementing<INotifySelected>())
				sel.Selected(a);
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
				var adjNewSelection = newSelection.Take(1);	/* TODO: select BEST, not FIRST */
				actors = (isCombine ? oldSelection.SymmetricDifference(adjNewSelection) : adjNewSelection).ToList();
			}
			else
				actors = (isCombine ? oldSelection.Union(newSelection) : newSelection).ToList();

			var voicedUnit = actors.FirstOrDefault(a => a.Owner == world.LocalPlayer && a.IsInWorld && a.HasVoices());
			if (voicedUnit != null)
				Sound.PlayVoice("Select", voicedUnit, voicedUnit.Owner.Country.Race);

			foreach (var a in newSelection)
				foreach (var sel in a.TraitsImplementing<INotifySelected>())
					sel.Selected(a);
			foreach (var ns in world.WorldActor.TraitsImplementing<INotifySelection>())
				ns.SelectionChanged();
		}

		public IEnumerable<Actor> Actors { get { return actors; } }
		public void Clear() { actors = new List<Actor>(); }

		public void Tick(World world)
		{
			actors.RemoveAll(a => !a.IsInWorld || world.FogObscures(a));

			foreach (var cg in controlGroups.Values)
				// note: NOT `!a.IsInWorld`, since that would remove things that are in transports.
				cg.RemoveAll(a => a.Destroyed);
		}

		Cache<int, List<Actor>> controlGroups = new Cache<int, List<Actor>>(_ => new List<Actor>());

		public void DoControlGroup(World world, WorldRenderer worldRenderer, int group, Modifiers mods, int MultiTapCount)
		{
			var addModifier = Platform.CurrentPlatform == PlatformType.OSX ? Modifiers.Meta : Modifiers.Ctrl;
			if (mods.HasModifier(addModifier))
			{
				if (actors.Count == 0)
					return;

				if (!mods.HasModifier(Modifiers.Shift))
					controlGroups[group].Clear();

				for (var i = 0; i < 10; i++)	/* all control groups */
					controlGroups[i].RemoveAll(a => actors.Contains(a));

				controlGroups[group].AddRange(actors.Where(a => a.Owner == world.LocalPlayer));
				return;
			}

			var groupActors = controlGroups[group].Where(a => !a.IsDead() && !world.FogObscures(a));

			if (mods.HasModifier(Modifiers.Alt) || MultiTapCount >= 2)
			{
				worldRenderer.Viewport.Center(groupActors);
				return;
			}

			Combine(world, groupActors, mods.HasModifier(Modifiers.Shift), false);
		}

		public int? GetControlGroupForActor(Actor a)
		{
			return controlGroups.Where(g => g.Value.Contains(a))
				.Select(g => (int?)g.Key)
				.FirstOrDefault();
		}
	}
}
