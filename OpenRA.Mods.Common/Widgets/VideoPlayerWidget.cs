#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	// This is a base/placeholder class
	public class VideoPlayerWidget : Widget
	{
		public Hotkey CancelKey = new Hotkey(Keycode.ESCAPE, Modifiers.None);
		public float AspectRatio = 1.2f;
		public bool DrawOverlay = true;
		public bool Skippable = true;

		public bool Paused { get { return paused; } }
		public virtual int VideoFrameCount { get { return 0; } }
		public virtual int VideoCurrentFrame { get { return 0; } }

		string cachedVideo;
		bool stopped;
		bool paused;

		public virtual void Load(string filename)
		{
			if (filename == cachedVideo)
				return;

			cachedVideo = filename;

			stopped = true;
			paused = true;
			Game.Sound.StopVideo();
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

		public virtual void Play()
		{
			PlayThen(() => { });
		}

		public virtual void PlayThen(Action after) { }

		public virtual void Pause()
		{
			if (stopped || paused)
				return;

			paused = true;
			Game.Sound.PauseVideo();
		}

		public virtual void Stop()
		{
			if (stopped)
				return;

			stopped = true;
			paused = true;
			Game.Sound.StopVideo();
		}

		public virtual void CloseVideo()
		{
			Stop();
		}
	}
}
