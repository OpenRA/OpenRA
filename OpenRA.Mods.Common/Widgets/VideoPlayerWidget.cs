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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Video;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class VideoPlayerWidget : Widget
	{
		public Hotkey CancelKey = new(Keycode.ESCAPE, Modifiers.None);
		public float AspectRatio = 1.2f;
		public bool DrawOverlay = true;
		public bool Skippable = true;

		public bool Paused => !playTime.IsRunning;
		public IVideo Video { get; private set; } = null;

		Sprite videoSprite, overlaySprite;
		Sheet overlaySheet;
		string cachedVideoFileName;
		float invLength;
		float2 videoOrigin, videoSize;
		float2 overlayOrigin, overlaySize;
		float overlayScale;
		readonly Stopwatch playTime = new();
		int textureWidth;
		int textureHeight;

		Action onComplete;

		/// <summary>
		/// Tries to load a video from the specified file and play it. Does nothing if the file name matches the already loaded video.
		/// </summary>
		/// <param name="filename">Name of the file, including the extension.</param>
		public void LoadAndPlay(string filename)
		{
			if (filename == cachedVideoFileName)
				return;

			cachedVideoFileName = filename;
			var stream = Game.ModData.DefaultFileSystem.Open(filename);
			var video = VideoLoader.GetVideo(stream, true, Game.ModData.VideoLoaders);
			Play(video);
		}

		/// <summary>
		/// Tries to load a video from the specified file and play it. Does nothing if the file name matches the already loaded video.
		/// </summary>
		/// <param name="filename">Name of the file, including the extension.</param>
		/// <param name="after">Action to perform after the video ends.</param>
		public void LoadAndPlayAsync(string filename, Action after)
		{
			if (filename == cachedVideoFileName)
				return;

			cachedVideoFileName = filename;

			if (Video != null)
				CloseVideo();

			Task.Run(() =>
			{
				try
				{
					var stream = Game.ModData.DefaultFileSystem.Open(filename);
					var video = VideoLoader.GetVideo(stream, true, Game.ModData.VideoLoaders);

					// Safeguard against race conditions with two videos being loaded at the same time - prefer to play only the last one.
					if (filename != cachedVideoFileName)
					{
						after();
						return;
					}

					Game.RunAfterTick(() =>
					{
						Play(video);
						PlayThen(() =>
						{
							after();
							CloseVideo();
						});
					});
				}
				catch (FileNotFoundException)
				{
					after();
				}
			});
		}

		/// <summary>
		/// Plays the given <see cref="IVideo"/>.
		/// </summary>
		/// <param name="video">An <see cref="IVideo"/> instance.</param>
		public void Play(IVideo video)
		{
			Video = video;

			if (video == null)
				return;

			playTime.Reset();
			Game.Sound.StopVideo();
			onComplete = () => { };

			invLength = video.Framerate * 1f / video.FrameCount;

			textureWidth = Exts.NextPowerOf2(video.Width);
			textureHeight = Exts.NextPowerOf2(video.Height);
			var videoSheet = new Sheet(SheetType.BGRA, new Size(textureWidth, textureHeight));

			videoSheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;
			videoSheet.GetTexture().SetData(video.CurrentFrameData, textureWidth, textureHeight);

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
			if (Video == null)
				return;

			if (!Paused)
			{
				int nextFrame;
				if (Video.HasAudio && !Game.Sound.DummyEngine)
					nextFrame = (int)float2.Lerp(0, Video.FrameCount, Game.Sound.VideoSeekPosition * invLength);
				else
					nextFrame = (int)float2.Lerp(0, Video.FrameCount, (float)playTime.Elapsed.TotalSeconds * invLength);

				// Without the 2nd check the sound playback sometimes ends before the final frame is displayed which causes the player to be stuck on the first frame
				if (nextFrame > Video.FrameCount || nextFrame < Video.CurrentFrameIndex)
				{
					Stop();
					return;
				}

				var skippedFrames = 0;
				while (nextFrame > Video.CurrentFrameIndex)
				{
					Video.AdvanceFrame();
					skippedFrames++;
				}

				if (skippedFrames > 0)
					videoSprite.Sheet.GetTexture().SetData(Video.CurrentFrameData, textureWidth, textureHeight);

				if (skippedFrames > 1)
					Log.Write("perf", $"{nameof(VideoPlayerWidget)}: {cachedVideoFileName} skipped {skippedFrames} frames at position {Video.CurrentFrameIndex}");
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
					var videoScale = Math.Min((float)RenderBounds.Width / Video.Width, RenderBounds.Height / (Video.Height * AspectRatio));
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
			if (Video == null)
				return;

			onComplete = after;
			if (playTime.ElapsedTicks == 0 && Video.HasAudio)
				Game.Sound.PlayVideo(Video.AudioData, Video.AudioChannels, Video.SampleBits, Video.SampleRate);
			else
				Game.Sound.PlayVideo();

			playTime.Start();
		}

		public void Pause()
		{
			if (Paused || Video == null)
				return;

			playTime.Stop();
			Game.Sound.PauseVideo();
		}

		public void Stop()
		{
			if (Video == null)
				return;

			playTime.Reset();
			Game.Sound.StopVideo();
			Video.Reset();
			videoSprite.Sheet.GetTexture().SetData(Video.CurrentFrameData, textureWidth, textureHeight);
			Game.RunAfterTick(() =>
			{
				if (onComplete != null)
				{
					onComplete();
					onComplete = null;
				}
			});
		}

		public void CloseVideo()
		{
			Stop();
			Video = null;
		}
	}
}
