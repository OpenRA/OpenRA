#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Graphics
{
	public class Animation
	{
		public ISpriteSequence CurrentSequence { get; private set; }
		public string Name { get; private set; }
		public bool IsDecoration { get; set; }

		readonly SequenceProvider sequenceProvider;
		readonly Func<WAngle> facingFunc;
		readonly Func<bool> paused;

		int frame;
		bool backwards;
		bool tickAlways;
		int timeUntilNextFrame;
		Action tickFunc = () => { };

		public Animation(World world, string name)
			: this(world, name, () => WAngle.Zero) { }

		public Animation(World world, string name, Func<WAngle> facingFunc)
			: this(world, name, facingFunc, null) { }

		public Animation(World world, string name, Func<bool> paused)
			: this(world, name, () => WAngle.Zero, paused) { }

		public Animation(World world, string name, Func<WAngle> facingFunc, Func<bool> paused)
		{
			sequenceProvider = world.Map.Rules.Sequences;
			Name = name.ToLowerInvariant();
			this.facingFunc = facingFunc;
			this.paused = paused;
		}

		public int CurrentFrame { get { return backwards ? CurrentSequence.Length - frame - 1 : frame; } }
		public Sprite Image { get { return CurrentSequence.GetSprite(CurrentFrame, facingFunc()); } }

		public IRenderable[] Render(WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale)
		{
			var tintModifiers = CurrentSequence.IgnoreWorldTint ? TintModifiers.IgnoreWorldTint : TintModifiers.None;
			var imageRenderable = new SpriteRenderable(Image, pos, offset, CurrentSequence.ZOffset + zOffset, palette, scale, IsDecoration, tintModifiers);

			if (CurrentSequence.ShadowStart >= 0)
			{
				var shadow = CurrentSequence.GetShadow(CurrentFrame, facingFunc());
				var shadowRenderable = new SpriteRenderable(shadow, pos, offset, CurrentSequence.ShadowZOffset + zOffset, palette, scale, true, tintModifiers);
				return new IRenderable[] { shadowRenderable, imageRenderable };
			}

			return new IRenderable[] { imageRenderable };
		}

		public IRenderable[] RenderUI(WorldRenderer wr, int2 pos, WVec offset, int zOffset, PaletteReference palette, float scale)
		{
			var screenOffset = (scale * wr.ScreenVectorComponents(offset)).XY.ToInt2();
			var imagePos = pos + screenOffset - new int2((int)(scale * Image.Size.X / 2), (int)(scale * Image.Size.Y / 2));
			var imageRenderable = new UISpriteRenderable(Image, WPos.Zero + offset, imagePos, CurrentSequence.ZOffset + zOffset, palette, scale);

			if (CurrentSequence.ShadowStart >= 0)
			{
				var shadow = CurrentSequence.GetShadow(CurrentFrame, facingFunc());
				var shadowPos = pos - new int2((int)(scale * shadow.Size.X / 2), (int)(scale * shadow.Size.Y / 2));
				var shadowRenderable = new UISpriteRenderable(shadow, WPos.Zero + offset, shadowPos, CurrentSequence.ShadowZOffset + zOffset, palette, scale);
				return new IRenderable[] { shadowRenderable, imageRenderable };
			}

			return new IRenderable[] { imageRenderable };
		}

		public Rectangle ScreenBounds(WorldRenderer wr, WPos pos, WVec offset, float scale)
		{
			var xy = wr.ScreenPxPosition(pos) + wr.ScreenPxOffset(offset);
			var cb = CurrentSequence.Bounds;
			return Rectangle.FromLTRB(
				xy.X + (int)(cb.Left * scale),
				xy.Y + (int)(cb.Top * scale),
				xy.X + (int)(cb.Right * scale),
				xy.Y + (int)(cb.Bottom * scale));
		}

		public IRenderable[] Render(WPos pos, PaletteReference palette)
		{
			return Render(pos, WVec.Zero, 0, palette, 1f);
		}

		public void Play(string sequenceName)
		{
			PlayThen(sequenceName, null);
		}

		int CurrentSequenceTickOrDefault()
		{
			const int DefaultTick = 40; // 25 fps == 40 ms
			return CurrentSequence != null ? CurrentSequence.Tick : DefaultTick;
		}

		void PlaySequence(string sequenceName)
		{
			CurrentSequence = GetSequence(sequenceName);
			timeUntilNextFrame = CurrentSequenceTickOrDefault();
		}

		public void PlayRepeating(string sequenceName)
		{
			backwards = false;
			tickAlways = false;
			PlaySequence(sequenceName);

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

			CurrentSequence = GetSequence(sequenceName);
			timeUntilNextFrame = Math.Min(CurrentSequenceTickOrDefault(), timeUntilNextFrame);
			frame %= CurrentSequence.Length;
			return true;
		}

		public void PlayThen(string sequenceName, Action after)
		{
			backwards = false;
			tickAlways = false;
			PlaySequence(sequenceName);

			frame = 0;
			tickFunc = () =>
			{
				++frame;
				if (frame >= CurrentSequence.Length)
				{
					frame = CurrentSequence.Length - 1;
					tickFunc = () => { };
					after?.Invoke();
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
			PlaySequence(sequenceName);

			frame = func();
			tickFunc = () => frame = func();
		}

		public void PlayFetchDirection(string sequenceName, Func<int> direction)
		{
			tickAlways = false;
			PlaySequence(sequenceName);

			frame = 0;
			tickFunc = () =>
			{
				var d = direction();
				if (d > 0 && ++frame >= CurrentSequence.Length)
					frame = 0;

				if (d < 0 && --frame < 0)
					frame = CurrentSequence.Length - 1;
			};
		}

		public void Tick()
		{
			if (paused == null || !paused())
				Tick(40); // tick one frame
		}

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
					timeUntilNextFrame += CurrentSequenceTickOrDefault();
				}
			}
		}

		public void ChangeImage(string newImage, string newAnimIfMissing)
		{
			newImage = newImage.ToLowerInvariant();

			if (Name != newImage)
			{
				Name = newImage;
				if (!ReplaceAnim(CurrentSequence.Name))
					ReplaceAnim(newAnimIfMissing);
			}
		}

		public bool HasSequence(string seq) { return sequenceProvider.HasSequence(Name, seq); }

		public ISpriteSequence GetSequence(string sequenceName)
		{
			return sequenceProvider.GetSequence(Name, sequenceName);
		}

		public string GetRandomExistingSequence(string[] sequences, MersenneTwister random)
		{
			return sequences.Where(s => HasSequence(s)).RandomOrDefault(random);
		}
	}
}
