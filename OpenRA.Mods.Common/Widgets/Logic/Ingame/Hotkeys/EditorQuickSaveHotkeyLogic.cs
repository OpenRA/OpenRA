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
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	[ChromeLogicArgsHotkeys("EditorQuickSaveKey")]
	public class EditorQuickSaveHotkeyLogic : SingleHotkeyBaseLogic
	{
		readonly World world;
		readonly ModData modData;

		[ObjectCreator.UseCtor]
		public EditorQuickSaveHotkeyLogic(Widget widget, ModData modData, World world, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "QuickSaveKey", "GLOBAL_KEYHANDLER", logicArgs)
		{
			this.world = world;
			this.modData = modData;
		}

		protected override bool OnHotkeyActivated(KeyInput keyInput)
		{
			var actionManager = world.WorldActor.TraitOrDefault<EditorActionManager>();
			if (actionManager != null && (!actionManager.Modified || actionManager.SaveFailed))
				return false;

			var map = world.Map;
			Action<string> saveMap = (string combinedPath) =>
			{
				var editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();

				var actorDefinitions = editorActorLayer.Save();
				if (actorDefinitions != null)
					map.ActorDefinitions = actorDefinitions;

				var playerDefinitions = editorActorLayer.Players.ToMiniYaml();
				if (playerDefinitions != null)
					map.PlayerDefinitions = playerDefinitions;

				var package = (IReadWritePackage)map.Package;
				SaveMapLogic.SaveMapInner(map, package, world, modData);
			};

			SaveMapLogic.SaveMap(modData, world, map, map.Package?.Name, saveMap);
			return true;
		}
	}
}
