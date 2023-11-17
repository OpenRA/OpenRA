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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	public class GameSaveViewportManagerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new GameSaveViewportManager(); }
	}

	public class GameSaveViewportManager : IWorldLoaded, IGameSaveTraitData
	{
		WorldRenderer worldRenderer;

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr) { worldRenderer = wr; }

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			// HACK: Store the viewport state for the skirmish observer on the first bot's trait
			// TODO: This won't make sense for MP saves
			var localPlayer = worldRenderer.World.LocalPlayer;
			if ((localPlayer != null && localPlayer.PlayerActor != self) ||
				(localPlayer == null && self.Owner != self.World.Players.FirstOrDefault(p => p.IsBot)))
				return null;

			var nodes = new List<MiniYamlNode>()
			{
				new("Viewport", FieldSaver.FormatValue(worldRenderer.Viewport.CenterPosition))
			};

			var renderPlayer = worldRenderer.World.RenderPlayer;
			if (localPlayer == null && renderPlayer != null)
				nodes.Add(new MiniYamlNode("RenderPlayer", FieldSaver.FormatValue(renderPlayer.PlayerActor.ActorID)));

			return nodes;
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			var viewportNode = data.NodeWithKeyOrDefault("Viewport");
			if (viewportNode != null)
				worldRenderer.Viewport.Center(FieldLoader.GetValue<WPos>("Viewport", viewportNode.Value.Value));

			var renderPlayerNode = data.NodeWithKeyOrDefault("RenderPlayer");
			if (renderPlayerNode != null)
			{
				var renderPlayerActorID = FieldLoader.GetValue<uint>("RenderPlayer", renderPlayerNode.Value.Value);
				worldRenderer.World.RenderPlayer = worldRenderer.World.GetActorById(renderPlayerActorID).Owner;
			}
		}
	}
}
