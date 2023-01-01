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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class SelectionInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new Selection(); }
	}

	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class Selection : ISelection, INotifyCreated, INotifyOwnerChanged, ITick, IGameSaveTraitData
	{
		public int Hash { get; private set; }
		public IEnumerable<Actor> Actors => actors;

		readonly HashSet<Actor> actors = new HashSet<Actor>();
		readonly List<Actor> rolloverActors = new List<Actor>();
		World world;

		INotifySelection[] worldNotifySelection;

		void INotifyCreated.Created(Actor self)
		{
			worldNotifySelection = self.TraitsImplementing<INotifySelection>().ToArray();
			world = self.World;
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

			Sync.RunUnsynced(world, () => world.OrderGenerator.SelectionChanged(world, actors));
			foreach (var ns in worldNotifySelection)
				ns.SelectionChanged();
		}

		public virtual void Remove(Actor a)
		{
			if (actors.Remove(a))
			{
				UpdateHash();
				Sync.RunUnsynced(world, () => world.OrderGenerator.SelectionChanged(world, actors));
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
			if (oldOwner == world.LocalPlayer)
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
				// TODO: select BEST, not FIRST
				var adjNewSelection = newSelection.Take(1);
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

			Sync.RunUnsynced(world, () => world.OrderGenerator.SelectionChanged(world, actors));
			foreach (var ns in worldNotifySelection)
				ns.SelectionChanged();

			if (world.IsGameOver)
				return;

			// Play the selection voice from one of the selected actors
			// TODO: This probably should only be considering the newly selected actors
			foreach (var actor in actors)
			{
				if (actor.Owner != world.LocalPlayer || !actor.IsInWorld)
					continue;

				var selectable = actor.Info.TraitInfoOrDefault<ISelectableInfo>();
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
			Sync.RunUnsynced(world, () => world.OrderGenerator.SelectionChanged(world, actors));
			foreach (var ns in worldNotifySelection)
				ns.SelectionChanged();
		}

		public void SetRollover(IEnumerable<Actor> rollover)
		{
			rolloverActors.Clear();
			rolloverActors.AddRange(rollover);
		}

		public bool RolloverContains(Actor a)
		{
			return rolloverActors.Contains(a);
		}

		void ITick.Tick(Actor self)
		{
			var removed = actors.RemoveWhere(a => !a.IsInWorld || (!a.Owner.IsAlliedWith(world.RenderPlayer) && world.FogObscures(a)));
			if (removed > 0)
			{
				UpdateHash();
				Sync.RunUnsynced(world, () => world.OrderGenerator.SelectionChanged(world, actors));
				foreach (var ns in worldNotifySelection)
					ns.SelectionChanged();
			}
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			return new List<MiniYamlNode>()
			{
				new MiniYamlNode("Selection", FieldSaver.FormatValue(Actors.Select(a => a.ActorID).ToArray()))
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, List<MiniYamlNode> data)
		{
			var selectionNode = data.FirstOrDefault(n => n.Key == "Selection");
			if (selectionNode != null)
			{
				var selected = FieldLoader.GetValue<uint[]>("Selection", selectionNode.Value.Value)
					.Select(a => self.World.GetActorById(a)).Where(a => a != null);
				Combine(self.World, selected, false, false);
			}
		}
	}
}
