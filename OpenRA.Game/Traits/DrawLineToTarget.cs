#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;

namespace OpenRA.Traits
{
	public class DrawLineToTargetInfo : ITraitInfo
	{
		public readonly int Ticks = 60;

		public virtual object Create(ActorInitializer init) { return new DrawLineToTarget(this); }
	}

	public class DrawLineToTarget :IRenderSelection
	{
		DrawLineToTargetInfo Info;
		public DrawLineToTarget(DrawLineToTargetInfo info)
		{
			this.Info = info;
		}
		
		Actor target;
		float2 pos;
		int lifetime;
		Color c;
		public void SetTarget(Actor self, int2 cell, Color c)
		{
			pos = Game.CellSize * (cell + new float2(0.5f, 0.5f));
			lifetime = Info.Ticks;
			target = null;
			this.c = c;
		}
		
		public void SetTarget(Actor self, Actor target, Color c)
		{
			this.target = target;
			lifetime = Info.Ticks;
			this.c = c;
		}
		
		public void Render (Actor self)
		{
			var force = Game.controller.GetModifiers().HasModifier(Modifiers.Alt);
			if ((lifetime <= 0 || --lifetime <= 0) && !force)
				return;
			
			var p = (target != null) ? target.CenterLocation : pos;
			
			Game.Renderer.LineRenderer.DrawLine(self.CenterLocation, p, c, c);
			for (bool b = false; !b; p = self.CenterLocation, b = true) 
			{
				Game.Renderer.LineRenderer.DrawLine(p + new float2(-1, -1), p + new float2(-1, 1), c, c);
				Game.Renderer.LineRenderer.DrawLine(p + new float2(-1, 1), p + new float2(1, 1), c, c);
				Game.Renderer.LineRenderer.DrawLine(p + new float2(1, 1), p + new float2(1, -1), c, c);
				Game.Renderer.LineRenderer.DrawLine(p + new float2(1, -1), p + new float2(-1, -1), c, c);
			}
			Game.Renderer.LineRenderer.Flush();
		}
	}
}

