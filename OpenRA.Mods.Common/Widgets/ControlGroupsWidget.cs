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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Lint;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ControlGroupsWidget : Widget
	{
		readonly ModData modData;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly int hotkeyCount;

		HotkeyReference[] selectGroupHotkeys;
		HotkeyReference[] createGroupHotkeys;
		HotkeyReference[] addToGroupHotkeys;
		HotkeyReference[] combineWithGroupHotkeys;
		HotkeyReference[] jumpToGroupHotkeys;

		// Note: LinterHotkeyNames assumes that these are disabled by default
		public readonly string SelectGroupKeyPrefix = null;
		public readonly string CreateGroupKeyPrefix = null;
		public readonly string AddToGroupKeyPrefix = null;
		public readonly string CombineWithGroupKeyPrefix = null;
		public readonly string JumpToGroupKeyPrefix = null;

		[CustomLintableHotkeyNames]
		public static IEnumerable<string> LinterHotkeyNames(MiniYamlNode widgetNode, Action<string> emitError)
		{
			var count = Game.ModData.DefaultRules.Actors[SystemActors.World].TraitInfo<IControlGroupsInfo>().Groups.Length;
			if (count == 0)
				yield break;

			var selectPrefix = "";
			var selectPrefixNode = widgetNode.Value.Nodes.FirstOrDefault(n => n.Key == "SelectGroupKeyPrefix");
			if (selectPrefixNode != null)
				selectPrefix = selectPrefixNode.Value.Value;

			var createPrefix = "";
			var createPrefixNode = widgetNode.Value.Nodes.FirstOrDefault(n => n.Key == "CreateGroupKeyPrefix");
			if (createPrefixNode != null)
				createPrefix = createPrefixNode.Value.Value;

			var addToPrefix = "";
			var addToPrefixNode = widgetNode.Value.Nodes.FirstOrDefault(n => n.Key == "AddToGroupKeyPrefix");
			if (addToPrefixNode != null)
				addToPrefix = addToPrefixNode.Value.Value;

			var combineWithPrefix = "";
			var combineWithPrefixNode = widgetNode.Value.Nodes.FirstOrDefault(n => n.Key == "CombineWithGroupKeyPrefix");
			if (combineWithPrefixNode != null)
				combineWithPrefix = combineWithPrefixNode.Value.Value;

			var jumpToPrefix = "";
			var jumpToPrefixNode = widgetNode.Value.Nodes.FirstOrDefault(n => n.Key == "JumpToGroupKeyPrefix");
			if (jumpToPrefixNode != null)
				jumpToPrefix = jumpToPrefixNode.Value.Value;

			if (string.IsNullOrEmpty(selectPrefix))
				emitError($"{widgetNode.Location} must define SelectGroupKeyPrefix if control groups count is greater than 0.");

			if (string.IsNullOrEmpty(createPrefix))
				emitError($"{widgetNode.Location} must define CreateGroupKeyPrefix if control groups count is greater than 0.");

			if (string.IsNullOrEmpty(addToPrefix))
				emitError($"{widgetNode.Location} must define AddToGroupKeyPrefix if control groups count is greater than 0.");

			if (string.IsNullOrEmpty(combineWithPrefix))
				emitError($"{widgetNode.Location} must define CombineWithGroupKeyPrefix if control groups count is greater than 0.");

			if (string.IsNullOrEmpty(jumpToPrefix))
				emitError($"{widgetNode.Location} must define JumpToGroupKeyPrefix if control groups count is greater than 0.");

			for (var i = 0; i < count; i++)
			{
				var suffix = (i + 1).ToString("D2");
				yield return selectPrefix + suffix;
				yield return createPrefix + suffix;
				yield return addToPrefix + suffix;
				yield return combineWithPrefix + suffix;
				yield return jumpToPrefix + suffix;
			}
		}

		[ObjectCreator.UseCtor]
		public ControlGroupsWidget(ModData modData, World world, WorldRenderer worldRenderer)
		{
			this.modData = modData;
			this.world = world;
			this.worldRenderer = worldRenderer;
			hotkeyCount = world.ControlGroups.Groups.Length;
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			selectGroupHotkeys = Exts.MakeArray(hotkeyCount,
				i => modData.Hotkeys[SelectGroupKeyPrefix + (i + 1).ToString("D2")]);

			createGroupHotkeys = Exts.MakeArray(hotkeyCount,
				i => modData.Hotkeys[CreateGroupKeyPrefix + (i + 1).ToString("D2")]);

			addToGroupHotkeys = Exts.MakeArray(hotkeyCount,
				i => modData.Hotkeys[AddToGroupKeyPrefix + (i + 1).ToString("D2")]);

			combineWithGroupHotkeys = Exts.MakeArray(hotkeyCount,
				i => modData.Hotkeys[CombineWithGroupKeyPrefix + (i + 1).ToString("D2")]);

			jumpToGroupHotkeys = Exts.MakeArray(hotkeyCount,
				i => modData.Hotkeys[JumpToGroupKeyPrefix + (i + 1).ToString("D2")]);
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event != KeyInputEvent.Down)
				return false;

			for (var i = 0; i < hotkeyCount; i++)
			{
				if (selectGroupHotkeys[i].IsActivatedBy(e))
				{
					world.ControlGroups.SelectControlGroup(i);

					if (e.MultiTapCount >= 2)
						worldRenderer.Viewport.Center(world.ControlGroups.GetActorsInControlGroup(i));

					return true;
				}

				if (createGroupHotkeys[i].IsActivatedBy(e))
				{
					world.ControlGroups.CreateControlGroup(i);
					return true;
				}

				if (addToGroupHotkeys[i].IsActivatedBy(e))
				{
					world.ControlGroups.AddSelectionToControlGroup(i);
					return true;
				}

				if (combineWithGroupHotkeys[i].IsActivatedBy(e))
				{
					world.ControlGroups.CombineSelectionWithControlGroup(i);
					return true;
				}

				if (jumpToGroupHotkeys[i].IsActivatedBy(e))
				{
					worldRenderer.Viewport.Center(world.ControlGroups.GetActorsInControlGroup(i));
					return true;
				}
			}

			return false;
		}
	}
}
