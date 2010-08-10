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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.FileFormats;

namespace OpenRA.Widgets
{
	public class VqaPlayerWidget : Widget
	{		
		public string Video = "";

		float timestep;
		Sprite videoSprite;
		Bitmap videoFrame;
		VqaReader video = null;
		
		public void LoadVideo(string filename)
		{
			video = new VqaReader(FileSystem.Open(filename));
			timestep = 1e3f/video.framerate;
			
			var size = OpenRA.Graphics.Util.NextPowerOf2(Math.Max(video.width, video.height));
			videoFrame = new Bitmap(size,size);
			video.FrameData(ref videoFrame);
			
			videoSprite = new Sprite(new Sheet(new Size(size,size)), new Rectangle( 0, 0, video.width, video.height ), TextureChannel.Alpha);
			videoSprite.sheet.Texture.SetData(videoFrame);	
		}

		int lastTime;
		public override void DrawInner(World world)
		{
			if (video == null)
				LoadVideo(Video);
			
			int t = Environment.TickCount;
			int dt = t - lastTime;
			
			if (dt > timestep)
			{
				lastTime = t;
				video.AdvanceFrame();
				video.FrameData(ref videoFrame);
				videoSprite.sheet.Texture.SetData(videoFrame);
			}
			
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(videoSprite, new int2(RenderBounds.X,RenderBounds.Y), "chrome");
		}
	}
}
