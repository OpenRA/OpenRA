#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Adds a particle-based overlay.")]
	public class WeatherOverlayInfo : ITraitInfo, ILobbyCustomRulesIgnore
	{
		[Desc("Factor for particle density. As higher as more particles will get spawned.")]
		public readonly float ParticleDensityFactor = 0.0007625f;

		[Desc("Should the level of the wind change over time, or just stick to the first value of WindLevels?")]
		public readonly bool ChangingWindLevel = true;

		[Desc("The levels of wind intensity (particles x-axis movement in px/tick).")]
		public readonly int[] WindLevels = { -5, -3, -2, 0, 2, 3, 5 };

		[Desc("Works only if ChangingWindLevel is enabled. Min. and max. ticks needed to change the WindLevel.")]
		public readonly int[] WindTick = { 150, 750 };

		[Desc("Hard or soft fading between the WindLevels.")]
		public readonly bool InstantWindChanges = false;

		[Desc("Particles are drawn in squares when enabled, otherwise with lines.")]
		public readonly bool UseSquares = true;

		[Desc("Size / width of the particle in px.")]
		public readonly int[] ParticleSize = { 1, 3 };

		[Desc("Scatters falling direction on the x-axis. Scatter min. and max. value in px/tick.")]
		public readonly int[] ScatterDirection = { -1, 1 };

		[Desc("Min. and max. speed at which particles fall in px/tick.")]
		public readonly float[] Gravity = { 1.00f, 2.00f };

		[Desc("The current offset value for the swing movement. SwingOffset min. and max. value in px/tick.")]
		public readonly float[] SwingOffset = { 1.0f, 1.5f };

		[Desc("The value that particles swing to the side each update. SwingSpeed min. and max. value in px/tick.")]
		public readonly float[] SwingSpeed = { 0.001f, 0.025f };

		[Desc("The value range that can be swung to the left or right. SwingAmplitude min. and max. value in px/tick.")]
		public readonly float[] SwingAmplitude = { 1.0f, 1.5f };

		[Desc("The randomly selected rgb(a) hex colors for the particles. Use this order: rrggbb[aa], rrggbb[aa], ...")]
		public readonly Color[] ParticleColors = {
			Color.FromArgb(236, 236, 236),
			Color.FromArgb(228, 228, 228),
			Color.FromArgb(208, 208, 208),
			Color.FromArgb(188, 188, 188)
		};

		[Desc("Works only with line enabled and can be used to fade out the tail of the line like a contrail.")]
		public readonly byte LineTailAlphaValue = 200;

		public object Create(ActorInitializer init) { return new WeatherOverlay(init.World, this); }
	}

	public class WeatherOverlay : ITick, IPostRender
	{
		readonly WeatherOverlayInfo info;
		readonly World world;
		struct Particle
		{
			public float PosX;
			public float PosY;
			public int Size;
			public float DirectionScatterX;
			public float Gravity;
			public float SwingOffset;
			public float SwingSpeed;
			public int SwingDirection;
			public float SwingAmplitude;
			public Color Color;
			public Color TailColor;
		}

		readonly List<Particle> particleList = new List<Particle>();
		readonly int maxParticleCount;

		enum ParticleCountFaderType { Hold, FadeIn, FadeOut }
		ParticleCountFaderType particleCountFader = ParticleCountFaderType.FadeIn;

		float targetWindXOffset = 0f;
		float currentWindXOffset = 0f;
		int currentWindIndex = 0;
		long windTickCountdown = 1500;
		float2 antiScrollPrevTopLeft;

		public WeatherOverlay(World world, WeatherOverlayInfo info)
		{
			this.info = info;
			this.world = world;
			currentWindIndex = info.WindLevels.Length / 2;
			targetWindXOffset = info.WindLevels[0];
			maxParticleCount = CalculateParticleCount(Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);
		}

		int CalculateParticleCount(int x, int y)
		{
			return (int)(x * y * info.ParticleDensityFactor);
		}

		void SpawnParticles(int count, int rangeY, int spawnChancePercent)
		{
			for (var i = 0; i < count; i++)
			{
				if (Game.CosmeticRandom.Next(100) < spawnChancePercent)
				{
					var tempColor = info.ParticleColors.Random(Game.CosmeticRandom);
					var tempColorTail = Color.FromArgb(info.LineTailAlphaValue, tempColor.R, tempColor.G, tempColor.B);
					var tempSwingDirection = Game.CosmeticRandom.Next(2) == 0 ? 1 : -1;

					particleList.Add(
						new Particle
						{
							PosX = Game.CosmeticRandom.Next(Game.Renderer.Resolution.Width),
							PosY = Game.CosmeticRandom.Next(rangeY),
							Size = Game.CosmeticRandom.Next(info.ParticleSize[0], info.ParticleSize[1] + 1),
							DirectionScatterX = info.ScatterDirection[0] + Game.CosmeticRandom.Next(info.ScatterDirection[1] - info.ScatterDirection[0]),
							Gravity = float2.Lerp(info.Gravity[0], info.Gravity[1], Game.CosmeticRandom.NextFloat()),
							SwingOffset = float2.Lerp(info.SwingOffset[0], info.SwingOffset[1], Game.CosmeticRandom.NextFloat()),
							SwingSpeed = float2.Lerp(info.SwingSpeed[0], info.SwingSpeed[1], Game.CosmeticRandom.NextFloat()),
							SwingDirection = tempSwingDirection,
							SwingAmplitude = float2.Lerp(info.SwingAmplitude[0], info.SwingAmplitude[1], Game.CosmeticRandom.NextFloat()),
							Color = tempColor,
							TailColor = tempColorTail
						});
				}
			}
		}

		void ParticlesCountLogic(WorldRenderer wr)
		{
			// Logic to switch between the states of the particleCountFader
			if (particleCountFader == ParticleCountFaderType.Hold && particleList.Count < maxParticleCount)
				particleCountFader = ParticleCountFaderType.FadeIn;
			else if (particleCountFader == ParticleCountFaderType.FadeIn && particleList.Count >= maxParticleCount)
				particleCountFader = ParticleCountFaderType.Hold;
			else if (particleCountFader == ParticleCountFaderType.FadeOut && particleList.Count == 0)
				particleCountFader = ParticleCountFaderType.Hold;

			// Do the fade functions
			if (particleCountFader == ParticleCountFaderType.FadeIn)
				FadeInParticleCount(wr);
			else if (particleCountFader == ParticleCountFaderType.FadeOut)
				FadeOutParticleCount(wr);
		}

		void FadeInParticleCount(WorldRenderer wr)
		{
			SpawnParticles(1, 0, 100);

			// Remove Particles, which are getting replaced from the top to the bottom by the "EdgeCheckReplace",
			// when scrolling down, as long as the FadeIn is not completed,
			// to avoid having particles at the top and bottom, but not in the middle of the screen.
			for (var i = 0; i < particleList.Count; i++)
				if (particleList[i].PosY < 0)
					particleList.RemoveAt(i);

			// Add Particles when the weather is fading in and scrolling up, to fill areas above
			if (antiScrollPrevTopLeft.Y > wr.Viewport.TopLeft.Y)
			{
				// Get delta Y and limit to the max value
				var tempRangeY = antiScrollPrevTopLeft.Y - wr.Viewport.TopLeft.Y;
				var tempParticleCount = CalculateParticleCount(Game.Renderer.Resolution.Width, (int)tempRangeY);
				if (particleList.Count + tempParticleCount > maxParticleCount)
					tempParticleCount = maxParticleCount - particleList.Count;

				SpawnParticles(tempParticleCount, (int)tempRangeY, 50);
			}
		}

		void FadeOutParticleCount(WorldRenderer wr)
		{
			for (var i = 0; i < particleList.Count; i++)
				if (particleList[i].PosY > (Game.Renderer.Resolution.Height - particleList[i].Gravity))
					particleList.RemoveAt(i);
		}

		void XAxisSwing(ref Particle tempParticle)
		{
			// Direction turn
			if (tempParticle.SwingOffset < -tempParticle.SwingAmplitude || tempParticle.SwingOffset > tempParticle.SwingAmplitude)
				tempParticle.SwingDirection *= -1;

			// Perform the X-Axis-Swing
			tempParticle.SwingOffset += tempParticle.SwingDirection * tempParticle.SwingSpeed;
		}

		public void Tick(Actor self)
		{
			windTickCountdown--;
		}

		void WindLogic(ref Particle tempParticle)
		{
			if (!info.ChangingWindLevel)
				targetWindXOffset = info.WindLevels[0];
			else if (windTickCountdown <= 0)
			{
				windTickCountdown = Game.CosmeticRandom.Next(info.WindTick[0], info.WindTick[1]);
				if (Game.CosmeticRandom.Next(2) == 1 && currentWindIndex > 0)
				{
					currentWindIndex--;
					targetWindXOffset = info.WindLevels[currentWindIndex];
				}
				else if (currentWindIndex < info.WindLevels.Length - 1)
				{
					currentWindIndex++;
					targetWindXOffset = info.WindLevels[currentWindIndex];
				}
			}

			// Fading the wind in little steps towards the TargetWindOffset
			if (info.InstantWindChanges)
				currentWindXOffset = targetWindXOffset;
			else if (currentWindXOffset != targetWindXOffset)
			{
				if (currentWindXOffset > targetWindXOffset)
					currentWindXOffset -= 0.00001f;
				else if (currentWindXOffset < targetWindXOffset)
					currentWindXOffset += 0.00001f;
			}
		}

		void Movement(ref Particle tempParticle)
		{
			tempParticle.PosX += tempParticle.DirectionScatterX + tempParticle.SwingOffset + currentWindXOffset;
			tempParticle.PosY += tempParticle.Gravity;
		}

		// AntiScroll keeps the particles in place when scrolling the viewport
		void AntiScroll(ref Particle tempParticle, WorldRenderer wr)
		{
			tempParticle.PosX += antiScrollPrevTopLeft.X - wr.Viewport.TopLeft.X;
			tempParticle.PosY += antiScrollPrevTopLeft.Y - wr.Viewport.TopLeft.Y;
		}

		void EdgeCheckReplace(ref Particle tempParticle, WorldRenderer wr)
		{
			tempParticle.PosX %= Game.Renderer.Resolution.Width;
			if (tempParticle.PosX < 0)
				tempParticle.PosX += Game.Renderer.Resolution.Width;

			tempParticle.PosY %= Game.Renderer.Resolution.Height;
			if (tempParticle.PosY < 0 && particleCountFader != ParticleCountFaderType.FadeIn)
				tempParticle.PosY += Game.Renderer.Resolution.Height;
		}

		void UpdateWeatherOverlay(WorldRenderer wr)
		{
			ParticlesCountLogic(wr);

			for (var i = 0; i < particleList.Count; i++)
			{
				Particle tempParticle = particleList[i];

				XAxisSwing(ref tempParticle);
				WindLogic(ref tempParticle);
				Movement(ref tempParticle);
				AntiScroll(ref tempParticle, wr);
				EdgeCheckReplace(ref tempParticle, wr);

				particleList[i] = tempParticle;
			}

			antiScrollPrevTopLeft = wr.Viewport.TopLeft;
		}

		void DrawWeatherOverlay(WorldRenderer wr)
		{
			var topLeft = wr.Viewport.TopLeft;
			foreach (var item in particleList)
			{
				var tempPos = new float2(item.PosX + topLeft.X, item.PosY + topLeft.Y);

				if (info.UseSquares)
					Game.Renderer.WorldRgbaColorRenderer.FillRect(tempPos, tempPos + new float2(item.Size, item.Size), item.Color);
				else
				{
					var tempPosTail = new float2(topLeft.X + item.PosX - currentWindXOffset, item.PosY - (item.Gravity * 2 / 3) + topLeft.Y);
					Game.Renderer.WorldRgbaColorRenderer.DrawLine(tempPos, tempPosTail, item.Size, item.TailColor);
				}
			}
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (!world.Paused)
				UpdateWeatherOverlay(wr);

			DrawWeatherOverlay(wr);
		}
	}
}
