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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Commands;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Renders a debug overlay showing the script triggers. Attach this to the world actor.")]
	public class CellTriggerOverlayInfo : TraitInfo
	{
		public readonly string Font = "BigBold";

		public readonly Color Color = Color.Red;

		public override object Create(ActorInitializer init) { return new CellTriggerOverlay(this); }
	}

	public class CellTriggerOverlay : IRenderAnnotations, IWorldLoaded, IChatCommand
	{
		const string CommandName = "triggers";

		[TranslationReference]
		const string CommandDescription = "description-cell-triggers-overlay";

		bool enabled;

		readonly SpriteFont font;
		readonly Color color;

		public CellTriggerOverlay(CellTriggerOverlayInfo info)
		{
			font = Game.Renderer.Fonts[info.Font];
			color = info.Color;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			var console = w.WorldActor.TraitOrDefault<ChatCommands>();
			var help = w.WorldActor.TraitOrDefault<HelpCommand>();

			if (console == null || help == null)
				return;

			console.RegisterCommand(CommandName, this);
			help.RegisterHelp(CommandName, CommandDescription);
		}

		void IChatCommand.InvokeCommand(string name, string arg)
		{
			if (name == CommandName)
				enabled ^= true;
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!enabled)
				yield break;

			var triggerPositions = wr.World.ActorMap.TriggerPositions().ToHashSet();

			foreach (var uv in wr.Viewport.VisibleCellsInsideBounds.CandidateMapCoords)
			{
				if (self.World.ShroudObscures(uv))
					continue;

				var cell = uv.ToCPos(wr.World.Map);
				if (!triggerPositions.Contains(cell))
					continue;

				var center = wr.World.Map.CenterOfCell(cell);
				yield return new TextAnnotationRenderable(font, center, 1024, color, "T");
			}
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
