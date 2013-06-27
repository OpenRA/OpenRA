﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class MakeAnimation : Activity
	{
		readonly bool Reversed;
		readonly Action OnComplete;
		RenderBuilding rb;

		public MakeAnimation(Actor self, Action onComplete) : this(self, false, onComplete) {}
		public MakeAnimation(Actor self, bool reversed, Action onComplete)
		{
			Reversed = reversed;
			OnComplete = onComplete;
		}

		bool complete = false;
		bool started = false;

		public override Activity Tick( Actor self )
		{
			if (self.IsDead())
				return NextActivity;

			if (started)
			{
				// Don't break the actor if someone has overriden the animation prematurely
				if (rb.anim.CurrentSequence.Name != "make")
				{
					complete = true;
					OnComplete();
				}
				return complete ? NextActivity : this;
			}

			started = true;
			rb = self.Trait<RenderBuilding>();
			if (Reversed)
			{
				// TODO: These don't belong here
				var bi = self.Info.Traits.GetOrDefault<BuildingInfo>();
				if (bi != null)
					foreach (var s in bi.SellSounds)
						Sound.PlayToPlayer(self.Owner, s, self.CenterLocation);

				rb.PlayCustomAnimBackwards(self, "make", () => { OnComplete(); complete = true;});
			}
			else
				rb.PlayCustomAnimThen(self, "make", () => { OnComplete(); complete = true;});

			return this;
		}

		// Cannot be cancelled
		public override void Cancel( Actor self ) { }
	}
}
