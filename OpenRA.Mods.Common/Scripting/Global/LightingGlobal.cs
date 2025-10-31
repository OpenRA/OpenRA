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
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Lighting")]
	public class LightingGlobal : ScriptGlobal
	{
		readonly IEnumerable<FlashPostProcessEffect> flashEffects;
		readonly TintPostProcessEffect tintEffect;

		public LightingGlobal(ScriptContext context)
			: base(context)
		{
			flashEffects = context.World.WorldActor.TraitsImplementing<FlashPostProcessEffect>();
			tintEffect = context.World.WorldActor.TraitOrDefault<TintPostProcessEffect>();
		}

		[Desc("Controls the `" + nameof(FlashPostProcessEffect) + "` trait.")]
		public void Flash(string type = null, int ticks = -1)
		{
			foreach (var effect in flashEffects)
				if (effect.Info.Type == type)
					effect.Enable(ticks);
		}

		[Desc("Red component (0-1).")]
		public double Red
		{
			get => tintEffect?.Red ?? 1;
			set { if (tintEffect != null) tintEffect.Red = (float)value; }
		}

		[Desc("Green component (0-1).")]
		public double Green
		{
			get => tintEffect?.Green ?? 1;
			set { if (tintEffect != null) tintEffect.Green = (float)value; }
		}

		[Desc("Blue component (0-1).")]
		public double Blue
		{
			get => tintEffect?.Blue ?? 1;
			set { if (tintEffect != null) tintEffect.Blue = (float)value; }
		}

		[Desc("Strength of the lighting (0-1).")]
		public double Ambient
		{
			get => tintEffect?.Ambient ?? 1;
			set { if (tintEffect != null) tintEffect.Ambient = (float)value; }
		}
	}
}
