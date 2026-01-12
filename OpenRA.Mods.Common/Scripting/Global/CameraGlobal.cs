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

using OpenRA.Graphics;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Camera")]
	public class CameraGlobal : ScriptGlobal
	{
		public CameraGlobal(ScriptContext context)
			: base(context) { }

		[Desc("The center of the visible viewport.")]
		public WPos Position
		{
			get => Context.WorldRenderer.Viewport.CenterPosition;
			set => Context.WorldRenderer.Viewport.Center(value, true);
		}

		Actor focusedActor;

		float2? GetViewportCenter(WorldRenderer worldRenderer)
		{
			if (focusedActor == null || focusedActor.IsDead || !focusedActor.IsInWorld)
			{
				focusedActor = null;
				Context.WorldRenderer.Viewport.IsMovementLocked = false;

				void RemoveAction(World world)
				{
					if (focusedActor == null || focusedActor.IsDead || !focusedActor.IsInWorld)
						worldRenderer.Viewport.ViewportCenterProvider = null;
				}

				worldRenderer.World.AddFrameEndTask(RemoveAction);
				return null;
			}

			var pos = focusedActor.CenterPosition;
			return new float2(pos.X, pos.Y - pos.Z);
		}

		[Desc("Locks the player's viewport to the specified actor. The viewport will follow the actor as it moves. Set to nil to unlock.")]
		public Actor FocusedActor
		{
			get => focusedActor;
			set
			{
				focusedActor = value;
				if (focusedActor == null)
				{
					Context.WorldRenderer.Viewport.IsMovementLocked = false;
					Context.WorldRenderer.Viewport.ViewportCenterProvider = null;
					return;
				}

				Context.WorldRenderer.Viewport.IsMovementLocked = true;
				Context.WorldRenderer.Viewport.ViewportCenterProvider = () => GetViewportCenter(Context.WorldRenderer);
			}
		}
	}
}
