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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[IncludeStaticFluentReferences(typeof(CustomTerrainDebugOverlay))]
	[Desc("Displays custom terrain types.")]
	sealed class CustomTerrainDebugOverlayInfo : TraitInfo
	{
		public readonly string Font = "TinyBold";

		public override object Create(ActorInitializer init) { return new CustomTerrainDebugOverlay(this); }
	}

	sealed class CustomTerrainDebugOverlay : IWorldLoaded, IChatCommand, IRenderAnnotations
	{
		public const string CommandName = "custom-terrain";

		[FluentReference]
		const string CheatsDisabled = "notification-cheats-disabled";

		[FluentReference]
		const string CommandDescription = "description-custom-terrain-debug-overlay";

		public bool Enabled;

		readonly SpriteFont font;

		DeveloperMode devMode;

		public CustomTerrainDebugOverlay(CustomTerrainDebugOverlayInfo info)
		{
			font = Game.Renderer.Fonts[info.Font];
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			var console = w.WorldActor.TraitOrDefault<ChatCommands>();
			var help = w.WorldActor.TraitOrDefault<HelpCommand>();
			devMode = world.LocalPlayer?.PlayerActor.Trait<DeveloperMode>();

			if (console == null || help == null || devMode == null)
				return;

			console.RegisterCommand(CommandName, this);
			help.RegisterHelp(CommandName, CommandDescription);
		}

		public void InvokeCommand(string name, string arg)
		{
			if (name != CommandName)
				return;

			if (devMode == null || !devMode.Enabled)
			{
				TextNotificationsManager.Debug(FluentProvider.GetMessage(CheatsDisabled));
				return;
			}

			Enabled ^= true;
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!Enabled)
				yield break;

			foreach (var uv in wr.Viewport.VisibleCellsInsideBounds.CandidateMapCoords)
			{
				if (self.World.ShroudObscures(uv))
					continue;

				var cell = uv.ToCPos(wr.World.Map);
				var center = wr.World.Map.CenterOfCell(cell);
				var terrainType = self.World.Map.CustomTerrain[cell];
				if (terrainType == byte.MaxValue)
					continue;

				var info = wr.World.Map.GetTerrainInfo(cell);
				yield return new TextAnnotationRenderable(font, center, 0, info.Color, info.Type);
			}
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
