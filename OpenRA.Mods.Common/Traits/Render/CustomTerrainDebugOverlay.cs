#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Commands;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Displays custom terrain types.")]
	class CustomTerrainDebugOverlayInfo : ITraitInfo
	{
		public readonly string Font = "TinyBold";

		public object Create(ActorInitializer init) { return new CustomTerrainDebugOverlay(init.Self, this); }
	}

	class CustomTerrainDebugOverlay : IWorldLoaded, IChatCommand, IRender
	{
		const string CommandName = "debugcustomterrain";
		const string CommandDesc = "Toggles the custom terrain debug overlay. Optional parameter: 'allcells'";

		readonly SpriteFont font;

		DeveloperMode devMode;
		bool allCells;

		public CustomTerrainDebugOverlay(Actor self, CustomTerrainDebugOverlayInfo info)
		{
			font = Game.Renderer.Fonts[info.Font];
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			devMode = w.LocalPlayer != null ? w.LocalPlayer.PlayerActor.Trait<DeveloperMode>() : null;

			var console = w.WorldActor.TraitOrDefault<ChatCommands>();
			var help = w.WorldActor.TraitOrDefault<HelpCommand>();

			if (console == null || help == null)
				return;

			console.RegisterCommand(CommandName, this);
			help.RegisterHelp(CommandName, CommandDesc);
		}

		public void InvokeCommand(string name, string arg)
		{
			if (name == CommandName && devMode != null)
				devMode.ShowCustomTerrain ^= true;

			if (arg.Contains("allcells"))
				allCells ^= true;
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			if (devMode != null && !devMode.ShowCustomTerrain)
				yield break;

			foreach (var uv in wr.Viewport.VisibleCellsInsideBounds.CandidateMapCoords)
			{
				var cell = uv.ToCPos(wr.World.Map);
				var center = wr.World.Map.CenterOfCell(cell);
				var terrainType = self.World.Map.CustomTerrain[cell];
				if (!allCells && terrainType == byte.MaxValue)
					continue;

				var color = wr.World.Map.GetTerrainInfo(cell).Color;
				yield return new TextRenderable(font, center, 0, color, terrainType.ToString());
			}
		}
	}
}
