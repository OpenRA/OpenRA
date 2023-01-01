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
	public class ControlGroupsInfo : TraitInfo, IControlGroupsInfo
	{
		public readonly string[] Groups = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

		public override object Create(ActorInitializer init) { return new ControlGroups(init.World, this); }

		string[] IControlGroupsInfo.Groups => Groups;
	}

	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class ControlGroups : IControlGroups, ITick, IGameSaveTraitData
	{
		readonly World world;
		public string[] Groups { get; }

		readonly List<Actor>[] controlGroups;

		public ControlGroups(World world, ControlGroupsInfo info)
		{
			this.world = world;
			Groups = info.Groups;
			controlGroups = Enumerable.Range(0, Groups.Length).Select(_ => new List<Actor>()).ToArray();
		}

		public void SelectControlGroup(int group)
		{
			world.Selection.Combine(world, GetActorsInControlGroup(group), false, false);
		}

		public void CreateControlGroup(int group)
		{
			if (!world.Selection.Actors.Any())
				return;

			controlGroups[group].Clear();

			RemoveActorsFromAllControlGroups(world.Selection.Actors);

			controlGroups[group].AddRange(world.Selection.Actors.Where(a => a.Owner == world.LocalPlayer));
		}

		public void AddSelectionToControlGroup(int group)
		{
			if (!world.Selection.Actors.Any())
				return;

			RemoveActorsFromAllControlGroups(world.Selection.Actors);

			controlGroups[group].AddRange(world.Selection.Actors.Where(a => a.Owner == world.LocalPlayer));
		}

		public void CombineSelectionWithControlGroup(int group)
		{
			world.Selection.Combine(world, GetActorsInControlGroup(group), true, false);
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
			for (var i = 0; i < controlGroups.Length; i++)
				if (controlGroups[i].Contains(a))
					return i;

			return null;
		}

		void RemoveActorsFromAllControlGroups(IEnumerable<Actor> actors)
		{
			for (var i = 0; i < Groups.Length; i++)
				controlGroups[i].RemoveAll(a => actors.Contains(a));
		}

		public IEnumerable<Actor> GetActorsInControlGroup(int group)
		{
			return controlGroups[group].Where(a => a.IsInWorld);
		}

		void ITick.Tick(Actor self)
		{
			foreach (var cg in controlGroups)
			{
				// note: NOT `!a.IsInWorld`, since that would remove things that are in transports.
				cg.RemoveAll(a => a.Disposed || a.Owner != world.LocalPlayer);
			}
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			var groups = new List<MiniYamlNode>();
			for (var i = 0; i < controlGroups.Length; i++)
			{
				var cg = controlGroups[i];
				if (cg.Count > 0)
				{
					var actorIds = cg.Select(a => a.ActorID).ToArray();
					groups.Add(new MiniYamlNode(i.ToString(), FieldSaver.FormatValue(actorIds)));
				}
			}

			return new List<MiniYamlNode>()
			{
				new MiniYamlNode("Groups", new MiniYaml("", groups))
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, List<MiniYamlNode> data)
		{
			var groupsNode = data.FirstOrDefault(n => n.Key == "Groups");
			if (groupsNode != null)
			{
				foreach (var n in groupsNode.Value.Nodes)
				{
					var group = FieldLoader.GetValue<uint[]>(n.Key, n.Value.Value)
						.Select(a => self.World.GetActorById(a)).Where(a => a != null);
					controlGroups[int.Parse(n.Key)].AddRange(group);
				}
			}
		}
	}
}
