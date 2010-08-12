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
		Sprite videoSprite;
		VqaReader video = null;
		string cachedVideo;
		float invLength;
		float2 videoOrigin, videoSize;
		public void Load(string filename)
		{
			if (filename == cachedVideo)
				return;
			
			cachedVideo = filename;
			video = new VqaReader(FileSystem.Open(filename));
			invLength = video.Framerate*1f/video.Frames;

			var size = Math.Max(video.Width, video.Height);
			var textureSize = OpenRA.Graphics.Util.NextPowerOf2(size);
			videoSprite = new Sprite(new Sheet(new Size(textureSize,textureSize)), new Rectangle( 0, 0, video.Width, video.Height ), TextureChannel.Alpha);
			
			var scale = Math.Min(RenderBounds.Width / video.Width, RenderBounds.Height / video.Height);
			videoOrigin = new float2(RenderBounds.X + (RenderBounds.Width - scale*video.Width)/2, RenderBounds.Y +  (RenderBounds.Height - scale*video.Height)/2);
			videoSize = new float2(video.Width * scale, video.Height * scale);
		}
		
		bool playing = false;	
		Stopwatch sw = new Stopwatch();
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
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(videoSprite, videoOrigin, "chrome", videoSize);
		}
		
		public void Play()
		{
			playing = true;
			video.Reset();
			videoSprite.sheet.Texture.SetData(video.FrameData);
			sw.Reset();
			Sound.PlayVideoSoundtrack(video.AudioData);
		}
		
		public void Stop()
		{
			playing = false;
			Sound.StopVideoSoundtrack();
		}
	}
}
