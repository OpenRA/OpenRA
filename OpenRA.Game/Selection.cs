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
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA
{
	public class Selection
	{
		readonly HashSet<Actor> actors = new HashSet<Actor>();
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
			return actors.Contains(a);
		}

		public void Combine(World world, IEnumerable<Actor> newSelection, bool isCombine, bool isClick)
		{
			if (isClick)
			{
				var adjNewSelection = newSelection.Take(1);	/* TODO: select BEST, not FIRST */
				if (isCombine)
					actors.SymmetricExceptWith(adjNewSelection);
				else
				{
					actors.Clear();
					actors.UnionWith(adjNewSelection);
				}
			}
			else
			{
				if (isCombine)
					actors.UnionWith(newSelection);
				else
				{
					actors.Clear();
					actors.UnionWith(newSelection);
				}
			}

			foreach (var a in newSelection)
				foreach (var sel in a.TraitsImplementing<INotifySelected>())
					sel.Selected(a);

			foreach (var ns in world.WorldActor.TraitsImplementing<INotifySelection>())
				ns.SelectionChanged();

			if (world.IsGameOver)
				return;

			// Play the selection voice from one of the selected actors
			// TODO: This probably should only be considering the newly selected actors
			// TODO: Ship this into an INotifySelection trait to remove the engine dependency on Selectable
			foreach (var actor in actors)
			{
				if (actor.Owner != world.LocalPlayer || !actor.IsInWorld)
					continue;

				var selectable = actor.Info.TraitInfoOrDefault<SelectableInfo>();
				if (selectable == null || !actor.HasVoice(selectable.Voice))
					continue;

				actor.PlayVoice(selectable.Voice);
				break;
			}
		}

		public IEnumerable<Actor> Actors { get { return actors; } }
		public void Clear() { actors.Clear(); }

		public void Tick(World world)
		{
			actors.RemoveWhere(a => !a.IsInWorld || (!a.Owner.IsAlliedWith(world.RenderPlayer) && world.FogObscures(a)));

			foreach (var cg in controlGroups.Values)
			{
				// note: NOT `!a.IsInWorld`, since that would remove things that are in transports.
				cg.RemoveAll(a => a.Disposed || a.Owner != world.LocalPlayer);
			}
		}

		Cache<int, List<Actor>> controlGroups = new Cache<int, List<Actor>>(_ => new List<Actor>());

		public void DoControlGroup(World world, WorldRenderer worldRenderer, int group, Modifiers mods, int multiTapCount)
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

			var groupActors = controlGroups[group].Where(a => !a.IsDead);

			if (mods.HasModifier(Modifiers.Alt) || multiTapCount >= 2)
			{
				worldRenderer.Viewport.Center(groupActors);
				return;
			}

			Combine(world, groupActors, mods.HasModifier(Modifiers.Shift), false);
		}

		public void AddToControlGroup(Actor a, int group)
		{
			if (!controlGroups[group].Contains(a))
				controlGroups[group].Add(a);
		}

		public int? GetControlGroupForActor(Actor a)
		{
			return controlGroups.Where(g => g.Value.Contains(a))
				.Select(g => (int?)g.Key)
				.FirstOrDefault();
		}
	}
}
