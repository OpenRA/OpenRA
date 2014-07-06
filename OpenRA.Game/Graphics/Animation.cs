#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Graphics
{
	public class Animation
	{
		public Sequence CurrentSequence { get; private set; }
		public bool IsDecoration = false;
		public Func<bool> Paused;

		readonly Func<int> facingFunc;

		int frame = 0;
		bool backwards = false;

		string name;

		readonly int defaultTick = 40; // 25 fps == 40 ms
		bool tickAlways;

		public string Name { get { return name; } }

		readonly SequenceProvider sequenceProvider;

		public Animation(World world, string name)
			: this(world, name, () => 0) { }

		public Animation(World world, string name, Func<int> facingFunc)
			: this(world.Map.SequenceProvider, name, facingFunc) { }

		public Animation(SequenceProvider sequenceProvider, string name, Func<int> facingFunc)
		{
			this.sequenceProvider = sequenceProvider;
			this.name = name.ToLowerInvariant();
			this.tickFunc = () => {};
			this.facingFunc = facingFunc;
		}

		int CurrentFrame { get { return backwards ? CurrentSequence.Start + CurrentSequence.Length - frame - 1 : frame; } }
		public Sprite Image { get { return CurrentSequence.GetSprite(CurrentFrame, facingFunc()); } }

		public IEnumerable<IRenderable> Render(WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale)
		{
			var imageRenderable = new SpriteRenderable(Image, pos, offset, CurrentSequence.ZOffset + zOffset, palette, scale, IsDecoration);

			if (CurrentSequence.ShadowStart >= 0)
			{
				var shadow = CurrentSequence.GetShadow(CurrentFrame, facingFunc());
				var shadowRenderable = new SpriteRenderable(shadow, pos, offset, CurrentSequence.ShadowZOffset + zOffset, palette, scale, true);
				return new IRenderable[] { shadowRenderable, imageRenderable };
			}

			return new IRenderable[] { imageRenderable };
		}

		public IEnumerable<IRenderable> Render(WPos pos, PaletteReference palette)
		{
			return Render(pos, WVec.Zero, 0, palette, 1f);
		}

		public void Initialize(string sequenceName)
		{
			CurrentSequence = sequenceProvider.GetSequence(name, sequenceName);
			tickAlways = true;
		}

		public void Play(string sequenceName)
		{
			PlayThen(sequenceName, null);
		}

		public void PlayRepeating(string sequenceName)
		{
			backwards = false;
			tickAlways = false;
			CurrentSequence = sequenceProvider.GetSequence(name, sequenceName);
			frame = 0;
			tickFunc = () =>
			{
				++frame;
				if (frame >= CurrentSequence.Length)
					frame = 0;
			};
		}

		public bool ReplaceAnim(string sequenceName)
		{
			if (!HasSequence(sequenceName))
				return false;

			CurrentSequence = sequenceProvider.GetSequence(name, sequenceName);
			frame %= CurrentSequence.Length;
			return true;
		}

		public void PlayThen(string sequenceName, Action after)
		{
			backwards = false;
			tickAlways = false;
			CurrentSequence = sequenceProvider.GetSequence(name, sequenceName);
			frame = 0;
			tickFunc = () =>
			{
				++frame;
				if (frame >= CurrentSequence.Length)
				{
					frame = CurrentSequence.Length - 1;
					tickFunc = () => { };
					if (after != null) after();
				}
			};
		}

		public void PlayBackwardsThen(string sequenceName, Action after)
		{
			PlayThen(sequenceName, after);
			backwards = true;
		}

		public void PlayFetchIndex(string sequenceName, Func<int> func)
		{
			backwards = false;
			tickAlways = true;
			CurrentSequence = sequenceProvider.GetSequence(name, sequenceName);
			frame = func();
			tickFunc = () => frame = func();
		}

		int timeUntilNextFrame;
		Action tickFunc;

		public void Tick()
		{
			if (Paused == null || !Paused())
				Tick(40); // tick one frame
		}

		public bool HasSequence(string seq) { return sequenceProvider.HasSequence(name, seq); }

		public void Tick(int t)
		{
			if (tickAlways)
				tickFunc();
			else
			{
				timeUntilNextFrame -= t;
				while (timeUntilNextFrame <= 0)
				{
					tickFunc();
					timeUntilNextFrame += CurrentSequence != null ? CurrentSequence.Tick : defaultTick;
				}
			}
		}

		public void ChangeImage(string newImage, string newAnimIfMissing)
		{
			newImage = newImage.ToLowerInvariant();

			if (name != newImage)
			{
				name = newImage.ToLowerInvariant();
				if (!ReplaceAnim(CurrentSequence.Name))
					ReplaceAnim(newAnimIfMissing);
			}
		}

		public Sequence GetSequence(string sequenceName)
		{
			return sequenceProvider.GetSequence(name, sequenceName);
		}
	}
}
