#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class VqaPlayerWidget : Widget
	{		
		public string Video = "";

		float timestep;
		Sprite videoSprite;
		VqaReader video = null;
		
		public void LoadVideo(string filename)
		{
			video = new VqaReader(FileSystem.Open(filename));
			timestep = 1e3f/video.Framerate;
			
			var size = OpenRA.Graphics.Util.NextPowerOf2(Math.Max(video.Width, video.Height));
			videoSprite = new Sprite(new Sheet(new Size(size,size)), new Rectangle( 0, 0, video.Width, video.Height ), TextureChannel.Alpha);
		}

		int lastTime;
		bool advanceNext = false;
		public override void DrawInner(World world)
		{
			if (video == null)
			{
				LoadVideo(Video);
				Sound.PlayRaw(video.AudioData);
			}
			
			int t = Environment.TickCount;
			int dt = t - lastTime;
			
			if (advanceNext)
			{
				if (video.CurrentFrame == 0)
					Sound.PlayRaw(video.AudioData);
				advanceNext = false;
				video.AdvanceFrame();
			}
			
			if (dt > timestep)
			{
				lastTime = t;
				advanceNext = true;
				videoSprite.sheet.Texture.SetData(video.FrameData());
			}
			
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(videoSprite, new int2(RenderBounds.X,RenderBounds.Y), "chrome");
		}
	}
}
