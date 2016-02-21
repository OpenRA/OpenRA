#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Lighting")]
	public class LightingGlobal : ScriptGlobal
	{
		readonly IEnumerable<FlashPaletteEffect> flashPaletteEffects;
		readonly GlobalLightingPaletteEffect lighting;
		readonly bool hasLighting;

		public LightingGlobal(ScriptContext context)
			: base(context)
		{
			flashPaletteEffects = context.World.WorldActor.TraitsImplementing<FlashPaletteEffect>();
			lighting = context.World.WorldActor.TraitOrDefault<GlobalLightingPaletteEffect>();
			hasLighting = lighting != null;
		}

		[Desc("Controls the `FlashPaletteEffect` trait.")]
		public void Flash(string type = null, int ticks = -1)
		{
			foreach (var effect in flashPaletteEffects)
				if (effect.Info.Type == type)
					effect.Enable(ticks);
		}

		public double Red
		{
			get { return hasLighting ? lighting.Red : 1d; }
			set { if (hasLighting) lighting.Red = (float)value; }
		}

		public double Green
		{
			get { return hasLighting ? lighting.Green : 1d; }
			set { if (hasLighting) lighting.Green = (float)value; }
		}

		public double Blue
		{
			get { return hasLighting ? lighting.Blue : 1d; }
			set { if (hasLighting) lighting.Blue = (float)value; }
		}

		public double Ambient
		{
			get { return hasLighting ? lighting.Ambient : 1d; }
			set { if (hasLighting) lighting.Ambient = (float)value; }
		}
	}
}
