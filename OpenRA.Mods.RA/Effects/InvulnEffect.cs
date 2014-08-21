#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA.Effects
{
	class InvulnEffect : IEffect
	{
		Actor a;
		IronCurtainable b;

		public InvulnEffect(Actor a)
		{
			this.a = a;
			this.b = a.Trait<IronCurtainable>();
		}

		public void Tick( World world )
		{
			if (a.IsDead() || b.GetDamageModifier(null, null) > 0)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (a.Destroyed) // Tick will clean up
				yield break;

			foreach (var r in a.Render(wr))
				if (!r.IsDecoration)
				yield return r.WithPalette(wr.Palette("invuln"));
		}
	}
}
