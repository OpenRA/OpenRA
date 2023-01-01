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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Adds a localized circular light centered on the actor to the world's TerrainLightSource trait.")]
	public class TerrainLightSourceInfo : TraitInfo, INotifyEditorPlacementInfo, IRulesetLoaded, ILobbyCustomRulesIgnore
	{
		public readonly WDist Range = WDist.FromCells(10);
		public readonly float Intensity = 0;
		public readonly float RedTint = 0;
		public readonly float GreenTint = 0;
		public readonly float BlueTint = 0;

		object INotifyEditorPlacementInfo.AddedToEditor(EditorActorPreview preview, World editorWorld)
		{
			var tint = new float3(RedTint, GreenTint, BlueTint);
			return editorWorld.WorldActor.Trait<TerrainLighting>().AddLightSource(preview.CenterPosition, Range, Intensity, tint);
		}

		void INotifyEditorPlacementInfo.RemovedFromEditor(EditorActorPreview preview, World editorWorld, object data)
		{
			editorWorld.WorldActor.Trait<TerrainLighting>().RemoveLightSource((int)data);
		}

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!rules.Actors[SystemActors.World].HasTraitInfo<TerrainLightingInfo>())
				throw new YamlException("TerrainLightSource can only be used with the world TerrainLighting trait.");
		}

		public override object Create(ActorInitializer init) { return new TerrainLightSource(init.Self, this); }
	}

	public sealed class TerrainLightSource : INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly TerrainLightSourceInfo info;
		readonly TerrainLighting terrainLighting;
		int lightingToken = -1;

		public TerrainLightSource(Actor self, TerrainLightSourceInfo info)
		{
			this.info = info;
			terrainLighting = self.World.WorldActor.Trait<TerrainLighting>();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			lightingToken = terrainLighting.AddLightSource(self.CenterPosition, info.Range, info.Intensity, new float3(info.RedTint, info.GreenTint, info.BlueTint));
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			terrainLighting.RemoveLightSource(lightingToken);
		}
	}
}
