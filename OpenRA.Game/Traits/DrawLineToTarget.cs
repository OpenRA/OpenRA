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

	public class DrawLineToTarget :IPostRenderSelection
	{
		DrawLineToTargetInfo Info;
		public DrawLineToTarget(DrawLineToTargetInfo info)
		{
			this.Info = info;
		}
		
		Target target;
		int lifetime;
		Color c;
		
		public void SetTarget(Actor self, Target target, Color c)
		{
			this.target = target;
			lifetime = Info.Ticks;
			this.c = c;
		}
		
		public void SetTargetSilently(Actor self, Target target, Color c)
		{
			this.target = target;
			this.c = c;
		}	
		
		public void RenderAfterWorld (Actor self)
		{
			var force = Game.GetModifierKeys().HasModifier(Modifiers.Alt);
			if ((lifetime <= 0 || --lifetime <= 0) && !force)
				return;
			
			var p = target.CenterLocation;
			
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

