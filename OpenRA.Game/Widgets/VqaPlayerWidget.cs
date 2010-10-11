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
		Sprite videoSprite, overlaySprite;
		VqaReader video = null;
		string cachedVideo;
		float invLength;
		float2 videoOrigin, videoSize;
		uint[,] overlay;
		bool stopped;
		bool paused;
		
		Action OnComplete;
		
		public bool Paused { get { return paused; } }
		
		public bool DrawOverlay = true;
		public void Load(string filename)
		{
			if (filename == cachedVideo)
				return;
			
			stopped = true;
			paused = true;
			Sound.StopVideo();
			OnComplete = () => {};
			
			cachedVideo = filename;
			video = new VqaReader(FileSystem.Open(filename));
			
			invLength = video.Framerate*1f/video.Frames;

			var size = Math.Max(video.Width, video.Height);
			var textureSize = OpenRA.Graphics.Util.NextPowerOf2(size);
			videoSprite = new Sprite(new Sheet(new Size(textureSize,textureSize)), new Rectangle( 0, 0, video.Width, video.Height ), TextureChannel.Alpha);
			videoSprite.sheet.Texture.SetData(video.FrameData);

			var scale = Math.Min(RenderBounds.Width / video.Width, RenderBounds.Height / video.Height);
			videoOrigin = new float2(RenderBounds.X + (RenderBounds.Width - scale*video.Width)/2, RenderBounds.Y +  (RenderBounds.Height - scale*video.Height)/2);
			videoSize = new float2(video.Width * scale, video.Height * scale);
			
			if (!DrawOverlay)
				return;

			overlay = new uint[2*textureSize, 2*textureSize];
			uint black = (uint)255 << 24;
			for (var y = 0; y < video.Height; y++)
				for (var x = 0; x < video.Width; x++)
				overlay[2*y,x] = black;
			
			overlaySprite = new Sprite(new Sheet(new Size(2*textureSize,2*textureSize)), new Rectangle( 0, 0, video.Width, 2*video.Height ), TextureChannel.Alpha);
			overlaySprite.sheet.Texture.SetData(overlay);
		}
		
		public override void DrawInner()
		{
			if (video == null)
				return;
			
			if (!(stopped || paused))
			{
				var nextFrame = (int)float2.Lerp(0, video.Frames, Sound.VideoSeekPosition*invLength);
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
			}
			
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(videoSprite, videoOrigin, videoSize);
			
			if (DrawOverlay)
				Game.Renderer.RgbaSpriteRenderer.DrawSprite(overlaySprite, videoOrigin, videoSize);
		}
		
		public override bool HandleKeyPressInner(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
			{
				if (e.KeyChar == 27) // Escape
				{
					Stop();
					return true;
				}
			}
			return false;
		}
		
		public void Play()
		{
			PlayThen(() => {});	
		}
		
		public void PlayThen(Action after)
		{
			if (video == null)
				return;
			
			OnComplete = after;
			if (stopped)
				Sound.PlayVideo(video.AudioData);
			else
				Sound.PlayVideo();
			
			stopped = paused = false;
		}
		
		public void Pause()
		{
			if (stopped || paused || video == null)
				return;
			
			paused = true;
			Sound.PauseVideo();
		}
		
		public void Stop()
		{
			if (stopped || video == null)
				return;
			
			stopped = true;
			paused = true;
			Sound.StopVideo();
			video.Reset();
			videoSprite.sheet.Texture.SetData(video.FrameData);
			OnComplete();
		}
	}
}
