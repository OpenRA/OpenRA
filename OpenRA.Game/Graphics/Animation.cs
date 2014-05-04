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

		Func<int> facingFunc;

		int frame = 0;
		bool backwards = false;
		bool tickAlways;
		string name;

		public string Name { get { return name; } }

		readonly SequenceProvider sequenceProvider;
		static SequenceProvider lastSequenceProvider;

		public Animation(string name)
			: this(name, () => 0)	{}

		public Animation(string name, Func<int> facingFunc)
		{
			this.name = name.ToLowerInvariant();
			this.tickFunc = () => {};
			this.facingFunc = facingFunc;

			// TODO: This is wrong, don't use the static
			if (Game.orderManager != null && Game.orderManager.world != null && Game.orderManager.world.Map != null)
				sequenceProvider = Game.orderManager.world.Map.SequenceProvider;
			// HACK: This just makes sure we have a sequence provider in between map changes for delayed actions
			// It sucks but it can only be removed when we don't use the statics above but replace them with
			// a possible parameter on this constructor.
			if (sequenceProvider == null)
				sequenceProvider = lastSequenceProvider;
			else
				lastSequenceProvider = sequenceProvider;
		}

		int CurrentFrame { get { return backwards ? CurrentSequence.Start + CurrentSequence.Length - frame - 1 : frame; } }
		public Sprite Image { get { return CurrentSequence.GetSprite(CurrentFrame, facingFunc()); } }

		public IEnumerable<IRenderable> Render(WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale)
		{
			if (CurrentSequence.ShadowStart >= 0)
			{
				var shadow = CurrentSequence.GetShadow(CurrentFrame, facingFunc());
				yield return new SpriteRenderable(shadow, pos, offset, CurrentSequence.ShadowZOffset + zOffset, palette, scale, true);
			}

			yield return new SpriteRenderable(Image, pos, offset, CurrentSequence.ZOffset + zOffset, palette, scale, IsDecoration);
		}

		public IEnumerable<IRenderable> Render(WPos pos, PaletteReference palette)
		{
			return Render(pos, WVec.Zero, 0, palette, 1f);
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
					timeUntilNextFrame += CurrentSequence != null ? CurrentSequence.Tick : 40; // 25 fps == 40 ms
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
