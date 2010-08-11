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
		float timestep;
		Sprite videoSprite;
		VqaReader video = null;
		string cachedVideo;
		float invLength;
		public void Load(string filename)
		{
			if (filename == cachedVideo)
				return;
			
			cachedVideo = filename;
			video = new VqaReader(FileSystem.Open(filename));
			timestep = 1f/video.Framerate;
			invLength = video.Framerate*1f/video.Frames;

			var size = OpenRA.Graphics.Util.NextPowerOf2(Math.Max(video.Width, video.Height));
			videoSprite = new Sprite(new Sheet(new Size(size,size)), new Rectangle( 0, 0, video.Width, video.Height ), TextureChannel.Alpha);
		}
		
		bool playing = false;	
		Stopwatch sw = new Stopwatch();
		bool first;
		public override void DrawInner(World world)
		{
			if (!playing)
				return;
						
			var nextFrame = (int)float2.Lerp(0, video.Frames, (float)(sw.ElapsedTime()*invLength));
			if (nextFrame > video.Frames)
			{
				Stop();
				return;
			}
			
			while (nextFrame > video.CurrentFrame)
			{
				video.AdvanceFrame();
				if (nextFrame == video.CurrentFrame)
					videoSprite.sheet.Texture.SetData(video.FrameData);
			}
			
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(videoSprite, new int2(RenderBounds.X,RenderBounds.Y), "chrome");
		}
		
		public void Play()
		{
			playing = true;
			video.Reset();
			videoSprite.sheet.Texture.SetData(video.FrameData);
			sw.Reset();
			Sound.PlayRaw(video.AudioData);
		}
		
		public void Stop()
		{
			playing = false;
			// TODO: Stop audio
		}
	}
}
