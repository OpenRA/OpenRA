#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Effect")]
	public class EffectGlobal : ScriptGlobal
	{
		readonly IEnumerable<FlashPaletteEffect> fpes;
		readonly GlobalLightingPaletteEffect lighting;

		public EffectGlobal(ScriptContext context)
			: base(context)
		{
			fpes = context.World.WorldActor.TraitsImplementing<FlashPaletteEffect>();
			lighting = context.World.WorldActor.TraitOrDefault<GlobalLightingPaletteEffect>();
		}

		[Desc("Controls the `FlashPaletteEffect` trait.")]
		public void Flash(string type = null, int ticks = -1)
		{
			foreach (var fpe in fpes)
				if (fpe.Info.Type == type)
					fpe.Enable(ticks);
		}

		[Desc("Dynamically adjusts the `GlobalLightingPaletteEffect` trait.")]
		public void ChangeLighting(double red, double green, double blue, double ambient)
		{
			if (lighting == null)
				throw new LuaException("GlobalLightingPaletteEffect needs to be added to the World actor first.");

			lighting.Red = (float)red;
			lighting.Green = (float)green;
			lighting.Blue = (float)blue;
			lighting.Ambient = (float)ambient;
		}
	}
}
