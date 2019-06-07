#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.Common.Traits
{
	public class SelectionInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new Selection(this); }
	}

	public class Selection : ISelection, INotifyCreated, INotifyOwnerChanged, ITick, IGameSaveTraitData
	{
		public int Hash { get; private set; }
		public IEnumerable<Actor> Actors { get { return actors; } }

		readonly HashSet<Actor> actors = new HashSet<Actor>();
		INotifySelection[] worldNotifySelection;

		public Selection(SelectionInfo info) { }

		void INotifyCreated.Created(Actor self)
		{
			worldNotifySelection = self.TraitsImplementing<INotifySelection>().ToArray();
		}

		void UpdateHash()
		{
			// Not a real hash, but things checking this only care about checking when the selection has changed
			// For this purpose, having a false positive (forcing a refresh when nothing changed) is much better
			// than a false negative (selection state mismatch)
			Hash += 1;
		}

		public virtual void Add(Actor a)
		{
			actors.Add(a);
			UpdateHash();

			foreach (var sel in a.TraitsImplementing<INotifySelected>())
				sel.Selected(a);

			foreach (var ns in worldNotifySelection)
				ns.SelectionChanged();
		}

		public virtual void Remove(Actor a)
		{
			if (actors.Remove(a))
			{
				UpdateHash();
				foreach (var ns in worldNotifySelection)
					ns.SelectionChanged();
			}
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor a, Player oldOwner, Player newOwner)
		{
			if (!actors.Contains(a))
				return;

			// Remove the actor from the original owners selection
			// Call UpdateHash directly for everyone else so watchers can account for the owner change if needed
			if (oldOwner == a.World.LocalPlayer)
				Remove(a);
			else
				UpdateHash();
		}

		public bool Contains(Actor a)
		{
			return actors.Contains(a);
		}

		public virtual void Combine(World world, IEnumerable<Actor> newSelection, bool isCombine, bool isClick)
		{
			if (isClick)
			{
				var adjNewSelection = newSelection.Take(1); // TODO: select BEST, not FIRST
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

			UpdateHash();

			foreach (var a in newSelection)
				foreach (var sel in a.TraitsImplementing<INotifySelected>())
					sel.Selected(a);

			foreach (var ns in worldNotifySelection)
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

		public void Clear()
		{
			actors.Clear();
			UpdateHash();
		}

		void ITick.Tick(Actor self)
		{
			var removed = actors.RemoveWhere(a => !a.IsInWorld || (!a.Owner.IsAlliedWith(self.World.RenderPlayer) && self.World.FogObscures(a)));
			if (removed > 0)
				UpdateHash();

			foreach (var cg in controlGroups.Values)
			{
				// note: NOT `!a.IsInWorld`, since that would remove things that are in transports.
				cg.RemoveAll(a => a.Disposed || a.Owner != self.World.LocalPlayer);
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

				for (var i = 0; i < 10; i++) // all control groups
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

		public void RemoveFromControlGroup(Actor a)
		{
			var group = GetControlGroupForActor(a);
			if (group.HasValue)
				controlGroups[group.Value].Remove(a);
		}

		public int? GetControlGroupForActor(Actor a)
		{
			return controlGroups.Where(g => g.Value.Contains(a))
				.Select(g => (int?)g.Key)
				.FirstOrDefault();
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			var groups = controlGroups
				.Where(cg => cg.Value.Any())
				.Select(cg => new MiniYamlNode(cg.Key.ToString(),
					FieldSaver.FormatValue(cg.Value.Select(a => a.ActorID).ToArray())))
				.ToList();

			return new List<MiniYamlNode>()
			{
				new MiniYamlNode("Selection", FieldSaver.FormatValue(Actors.Select(a => a.ActorID).ToArray())),
				new MiniYamlNode("Groups", new MiniYaml("", groups))
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, List<MiniYamlNode> data)
		{
			var selectionNode = data.FirstOrDefault(n => n.Key == "Selection");
			if (selectionNode != null)
			{
				var selected = FieldLoader.GetValue<uint[]>("Selection", selectionNode.Value.Value)
					.Select(a => self.World.GetActorById(a));
				Combine(self.World, selected, false, false);
			}

			var groupsNode = data.FirstOrDefault(n => n.Key == "Groups");
			if (groupsNode != null)
			{
				foreach (var n in groupsNode.Value.Nodes)
				{
					var group = FieldLoader.GetValue<uint[]>(n.Key, n.Value.Value)
						.Select(a => self.World.GetActorById(a));
					controlGroups[int.Parse(n.Key)].AddRange(group);
				}
			}
		}
	}
}
