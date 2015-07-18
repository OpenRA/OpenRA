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
	[Desc("Renders a debug overlay showing cell coordinates. Attach this to the world actor.")]
	public class MapCoordinatesOverlayInfo : ITraitInfo
	{
		public readonly string Font = "TinyBold";

		public readonly Color Color = Color.White;

		public object Create(ActorInitializer init) { return new MapCoordinatesOverlay(this); }
	}

	public class MapCoordinatesOverlay : IRender, IWorldLoaded, IChatCommand
	{
		const string CommandName = "coordinatesoverlay";
		const string CommandDesc = "Toggles the map coordinates overlay. Optional parameter: 'allcells'";

		public bool Enabled;
		public bool AllCells;

		readonly SpriteFont font;
		readonly Color color;
		readonly TileShape shape;

		public MapCoordinatesOverlay(MapCoordinatesOverlayInfo info)
		{
			font = Game.Renderer.Fonts[info.Font];
			color = info.Color;
			shape = Game.ModData.Manifest.TileShape;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			var console = w.WorldActor.TraitOrDefault<ChatCommands>();
			var help = w.WorldActor.TraitOrDefault<HelpCommand>();

			if (console == null || help == null)
				return;

			console.RegisterCommand(CommandName, this);
			help.RegisterHelp(CommandName, CommandDesc);
		}

		public void InvokeCommand(string name, string arg)
		{
			if (name == CommandName)
				Enabled ^= true;

			if (arg.Contains("allcells"))
				AllCells ^= true;
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			if (!Enabled)
				yield break;

			var mouseCell = wr.Viewport.ViewToWorld(Viewport.LastMousePos).ToMPos(wr.World.Map).ToCPos(shape);
			var cells = AllCells ? wr.Viewport.AllVisibleCells : new CellRegion(shape, mouseCell, mouseCell);
			foreach (var cell in cells)
			{
				var center = wr.World.Map.CenterOfCell(cell);
				yield return new TextRenderable(font, center - new WVec(0, 256, 0), 0, color, cell.X.ToString());
				yield return new TextRenderable(font, center + new WVec(0, 256, 0), 0, color, cell.Y.ToString());
			}
		}
	}
}
