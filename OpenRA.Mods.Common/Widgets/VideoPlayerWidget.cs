#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Video;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class VideoPlayerWidget : Widget
	{
		public Hotkey CancelKey = new Hotkey(Keycode.ESCAPE, Modifiers.None);
		public float AspectRatio = 1.2f;
		public bool DrawOverlay = true;
		public bool Skippable = true;

		public bool Paused => paused;
		public IVideo Video => video;

		Sprite videoSprite, overlaySprite;
		Sheet overlaySheet;
		IVideo video = null;
		string cachedVideoFileName;
		float invLength;
		float2 videoOrigin, videoSize;
		float2 overlayOrigin, overlaySize;
		float overlayScale;
		bool stopped;
		bool paused;
		int textureSize;

		Action onComplete;

		/// <summary>
		/// Tries to load a video from the specified file and play it. Does nothing if the file name matches the already loaded video.
		/// </summary>
		/// <param name="filename">Name of the file, including the extension.</param>
		public void LoadAndPlay(string filename)
		{
			if (filename == cachedVideoFileName)
				return;

			var stream = Game.ModData.DefaultFileSystem.Open(filename);
			var video = VideoLoader.GetVideo(stream, true, Game.ModData.VideoLoaders);
			Play(video);

			cachedVideoFileName = filename;
		}

		/// <summary>
		/// Plays the given <see cref="IVideo"/>.
		/// </summary>
		/// <param name="video">An <see cref="IVideo"/> instance.</param>
		public void Play(IVideo video)
		{
			this.video = video;

			if (video == null)
				return;

			stopped = true;
			paused = true;
			Game.Sound.StopVideo();
			onComplete = () => { };

			invLength = video.Framerate * 1f / video.FrameCount;

			var size = Math.Max(video.Width, video.Height);
			textureSize = Exts.NextPowerOf2(size);
			var videoSheet = new Sheet(SheetType.BGRA, new Size(textureSize, textureSize));

			videoSheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;
			videoSheet.GetTexture().SetData(video.CurrentFrameData, textureSize, textureSize);

			videoSprite = new Sprite(videoSheet,
				new Rectangle(
					0,
					0,
					video.Width,
					video.Height),
				TextureChannel.RGBA);

			var scale = Math.Min((float)RenderBounds.Width / video.Width, RenderBounds.Height / (video.Height * AspectRatio));
			videoOrigin = new float2(
				RenderBounds.X + (RenderBounds.Width - scale * video.Width) / 2,
				RenderBounds.Y + (RenderBounds.Height - scale * video.Height * AspectRatio) / 2);

			// Round size to integer pixels. Round up to be consistent with the scale calculation.
			videoSize = new float2((int)Math.Ceiling(video.Width * scale), (int)Math.Ceiling(video.Height * AspectRatio * scale));
		}

		public override void Draw()
		{
			if (video == null)
				return;

			if (!stopped && !paused)
			{
				int nextFrame;
				if (video.HasAudio && !Game.Sound.DummyEngine)
					nextFrame = (int)float2.Lerp(0, video.FrameCount, Game.Sound.VideoSeekPosition * invLength);
				else
					nextFrame = video.CurrentFrameIndex + 1;

				// Without the 2nd check the sound playback sometimes ends before the final frame is displayed which causes the player to be stuck on the first frame
				if (nextFrame > video.FrameCount || nextFrame < video.CurrentFrameIndex)
				{
					Stop();
					return;
				}

				var skippedFrames = 0;
				while (nextFrame > video.CurrentFrameIndex)
				{
					video.AdvanceFrame();
					videoSprite.Sheet.GetTexture().SetData(video.CurrentFrameData, textureSize, textureSize);
					skippedFrames++;
				}

				if (skippedFrames > 1)
					Log.Write("perf", $"{nameof(VideoPlayerWidget)}: {cachedVideoFileName} skipped {skippedFrames} frames at position {video.CurrentFrameIndex}");
			}

			WidgetUtils.DrawSprite(videoSprite, videoOrigin, videoSize);

			if (DrawOverlay)
			{
				// Create the scan line grid to render over the video
				// To avoid aliasing, this must be an integer number of screen pixels.
				// A few complications to be aware of:
				// - The video may have a different aspect ratio to the widget RenderBounds
				// - The RenderBounds coordinates may be a non-integer scale of the screen pixel size
				// - The screen pixel size may change while the video is playing back
				//   (user moves a window between displays with different DPI on macOS)
				var scale = Game.Renderer.WindowScale;
				if (overlaySheet == null || overlayScale != scale)
				{
					overlaySheet?.Dispose();

					// Calculate the scan line height by converting the video scale (copied from Open()) to screen
					// pixels, halving it (scan lines cover half the pixel height), and rounding to the nearest integer.
					var videoScale = Math.Min((float)RenderBounds.Width / video.Width, RenderBounds.Height / (video.Height * AspectRatio));
					var halfRowHeight = (int)(videoScale * scale / 2 + 0.5f);

					// If the video is "too tightly packed" into the player and there is no room for drawing an overlay disable it.
					if (halfRowHeight == 0)
					{
						DrawOverlay = false;
						return;
					}

					// The overlay can be minimally stored in a 1px column which is stretched to cover the full screen
					var overlayHeight = (int)(RenderBounds.Height * scale / halfRowHeight);
					var overlaySheetSize = new Size(1, Exts.NextPowerOf2(overlayHeight));
					var overlay = new byte[4 * Exts.NextPowerOf2(overlayHeight)];
					overlaySheet = new Sheet(SheetType.BGRA, overlaySheetSize);

					// Every second pixel is the scan line - set alpha to 128 to make the lines less harsh
					for (var i = 3; i < 4 * overlayHeight; i += 8)
						overlay[i] = 128;

					overlaySheet.GetTexture().SetData(overlay, overlaySheetSize.Width, overlaySheetSize.Height);
					overlaySprite = new Sprite(overlaySheet, new Rectangle(0, 0, 1, overlayHeight), TextureChannel.RGBA);

					// Overlay origin must be rounded to the nearest screen pixel to prevent aliasing
					overlayOrigin = new float2((int)(RenderBounds.X * scale + 0.5f), (int)(RenderBounds.Y * scale + 0.5f)) / scale;
					overlaySize = new float2(RenderBounds.Width, overlayHeight * halfRowHeight / scale);
					overlayScale = scale;
				}

				WidgetUtils.DrawSprite(overlaySprite, overlayOrigin, overlaySize);
			}
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (Hotkey.FromKeyInput(e) != CancelKey || e.Event != KeyInputEvent.Down || !Skippable)
				return false;

			Stop();
			return true;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			return RenderBounds.Contains(mi.Location) && Skippable;
		}

		public override string GetCursor(int2 pos)
		{
			return null;
		}

		public void Play()
		{
			PlayThen(() => { });
		}

		public void PlayThen(Action after)
		{
			if (video == null)
				return;

			onComplete = after;
			if (stopped && video.HasAudio)
				Game.Sound.PlayVideo(video.AudioData, video.AudioChannels, video.SampleBits, video.SampleRate);
			else
				Game.Sound.PlayVideo();

			stopped = paused = false;
		}

		public void Pause()
		{
			if (stopped || paused || video == null)
				return;

			paused = true;
			Game.Sound.PauseVideo();
		}

		public void Stop()
		{
			if (stopped || video == null)
				return;

			stopped = true;
			paused = true;
			Game.Sound.StopVideo();
			video.Reset();
			videoSprite.Sheet.GetTexture().SetData(video.CurrentFrameData, textureSize, textureSize);
			Game.RunAfterTick(onComplete);
		}

		public void CloseVideo()
		{
			Stop();
			video = null;
		}
	}
}
