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
using OpenRA.Support;

namespace OpenRA.Widgets
{
	public class VqaPlayerWidget : Widget
	{		
		public string Video = "";

		float timestep;
		Sprite videoSprite;
		VqaReader video = null;
		
		float invLength;
		public void LoadVideo(string filename)
		{
			video = new VqaReader(FileSystem.Open(filename));
			timestep = 1f/video.Framerate;
			invLength = video.Framerate*1f/video.Frames;

			var size = OpenRA.Graphics.Util.NextPowerOf2(Math.Max(video.Width, video.Height));
			videoSprite = new Sprite(new Sheet(new Size(size,size)), new Rectangle( 0, 0, video.Width, video.Height ), TextureChannel.Alpha);
		}

		bool first = true;
		bool advanceNext = false;
		Stopwatch sw = new Stopwatch();
		public override void DrawInner(World world)
		{
			if (video == null)
				LoadVideo(Video);
						
			var nextFrame = (int)float2.Lerp(0, video.Frames, (float)(sw.ElapsedTime()*invLength));
			if (first || nextFrame > video.Frames)
			{
				video.Reset();
				sw.Reset();
				Sound.PlayRaw(video.AudioData);
				
				nextFrame = 0;
				videoSprite.sheet.Texture.SetData(video.FrameData);
				first = false;
			}
			
			while (nextFrame > video.CurrentFrame)
			{
				video.AdvanceFrame();
				if (nextFrame == video.CurrentFrame)
					videoSprite.sheet.Texture.SetData(video.FrameData);
			}
			
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(videoSprite, new int2(RenderBounds.X,RenderBounds.Y), "chrome");
		}
	}
}
