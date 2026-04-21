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

namespace OpenRA.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Enables visualization commands. Attach this to the world actor.")]
	public class DebugVisualizationsInfo : TraitInfo<DebugVisualizations> { }

	public class DebugVisualizations
	{
		public bool CombatGeometry;
		public bool RenderGeometry;
		public bool ScreenMap;
		public bool ActorTags;

		// The depth buffer may have been left enabled by the previous world
		// Initializing this as dirty forces us to reset the default rendering before the first render
		bool depthBufferDirty = true;
		bool depthBuffer;
		public bool DepthBuffer
		{
			get => depthBuffer;
			set
			{
				depthBuffer = value;
				depthBufferDirty = true;
			}
		}

		float depthBufferContrast = 1f;
		public float DepthBufferContrast
		{
			get => depthBufferContrast;
			set
			{
				depthBufferContrast = value;
				depthBufferDirty = true;
			}
		}

		float depthBufferOffset;
		public float DepthBufferOffset
		{
			get => depthBufferOffset;
			set
			{
				depthBufferOffset = value;
				depthBufferDirty = true;
			}
		}

		public void UpdateDepthBuffer()
		{
			if (depthBufferDirty)
			{
				Game.Renderer.WorldSpriteRenderer.SetDepthPreview(DepthBuffer, DepthBufferContrast, DepthBufferOffset);
				depthBufferDirty = false;
			}
		}
	}
}
