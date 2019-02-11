#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Camera")]
	public class CameraGlobal : ScriptGlobal
	{
		public CameraGlobal(ScriptContext context)
			: base(context) { }

		[Desc("Locks the player's viewport movement to prevent unwanted manipulations. Scripts can still manipulate it even when locked.")]
		public bool IsViewportMovementLocked
		{
			get { return Context.WorldRenderer.Viewport.IsMovementLocked; }
			set { Context.WorldRenderer.Viewport.IsMovementLocked = value; }
		}

		[Desc("Locks the player's viewport zooming to prevent unwanted manipulations. Scripts can still manipulate it even when locked.")]
		public bool IsViewportZoomingLocked
		{
			get { return Context.WorldRenderer.Viewport.IsZoomingLocked; }
			set { Context.WorldRenderer.Viewport.IsZoomingLocked = value; }
		}

		[Desc("The center of the visible viewport.")]
		public WPos Position
		{
			get { return Context.WorldRenderer.Viewport.CenterPosition; }
			set { Context.WorldRenderer.Viewport.Center(value, true); }
		}

		[Desc("The zoom level. Only use allowed values to avoid rendering issues.")]
		public double Zoom
		{
			get { return Context.WorldRenderer.Viewport.Zoom; }
			set { Context.WorldRenderer.Viewport.SetZoom((float)value, true); }
		}
	}
}
