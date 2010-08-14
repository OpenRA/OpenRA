#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Traits;

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
			if (a.IsDead() || b.GetDamageModifier(null) > 0)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			foreach (var r in a.Render())
				yield return r.WithPalette("invuln");
		}
	}
}
