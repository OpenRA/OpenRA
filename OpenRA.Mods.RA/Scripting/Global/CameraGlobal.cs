#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Eluant;
using OpenRA.Effects;
using OpenRA.Scripting;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptGlobal("Camera")]
	public class CameraGlobal : ScriptGlobal
	{
		public CameraGlobal(ScriptContext context)
			: base(context) { }

		[Desc("The center of the visible viewport.")]
		public WPos Position
		{
			get { return context.WorldRenderer.Viewport.CenterPosition; }
			set { context.WorldRenderer.Viewport.Center(value); }
		}
	}
}
