#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Graphics;

namespace OpenRA
{
	public class Cursor
	{
		CursorSequence sequence;
		public Cursor(string cursor)
		{
			sequence = CursorProvider.GetCursorSequence(cursor);
		}
		
		public void Draw(int frame, float2 pos)
		{
			Game.Renderer.SpriteRenderer.DrawSprite(sequence.GetSprite(frame), pos - sequence.Hotspot, sequence.Palette);
		}
	}
}
