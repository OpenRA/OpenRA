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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class GameSaveViewportManagerInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new GameSaveViewportManager(); }
	}

	public class GameSaveViewportManager : IWorldLoaded, IGameSaveTraitData
	{
		WorldRenderer worldRenderer;

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr) { worldRenderer = wr; }

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (worldRenderer.World.LocalPlayer == null || self != worldRenderer.World.LocalPlayer.PlayerActor)
				return null;

			return new List<MiniYamlNode>()
			{
				new MiniYamlNode("Viewport", FieldSaver.FormatValue(worldRenderer.Viewport.CenterPosition)),
				new MiniYamlNode("Selection", "", self.World.Selection.Serialize())
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, List<MiniYamlNode> data)
		{
			var viewportNode = data.FirstOrDefault(n => n.Key == "Viewport");
			if (viewportNode != null)
				worldRenderer.Viewport.Center(FieldLoader.GetValue<WPos>("data", viewportNode.Value.Value));

			var selectionNode = data.FirstOrDefault(n => n.Key == "Selection");
			if (selectionNode != null)
				self.World.Selection.Deserialize(self.World, selectionNode.Value.Nodes);
		}
	}
}
