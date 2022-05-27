#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Adds a particle-based overlay.")]
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class WeatherOverlayInfo : TraitInfo, ILobbyCustomRulesIgnore
	{
		[Desc("Average number of particles per 100x100 px square.")]
		public readonly int ParticleDensityFactor = 8;

		[Desc("Should the level of the wind change over time, or just stick to the first value of WindLevels?")]
		public readonly bool ChangingWindLevel = true;

		[Desc("The levels of wind intensity (particles x-axis movement in px/tick).")]
		public readonly int[] WindLevels = { -12, -7, -5, 0, 5, 7, 12 };

		[Desc("Works only if ChangingWindLevel is enabled. Min. and max. ticks needed to change the WindLevel.")]
		public readonly int[] WindTick = { 150, 550 };

		[Desc("Hard or soft fading between the WindLevels.")]
		public readonly bool InstantWindChanges = false;

		[Desc("Particles are drawn in squares when enabled, otherwise with lines.")]
		public readonly bool UseSquares = true;

		[Desc("Size / width of the particle in px.")]
		public readonly int[] ParticleSize = { 1, 3 };

		[Desc("Scatters falling direction on the x-axis. Scatter min. and max. value in px/tick.")]
		public readonly int[] ScatterDirection = { -1, 1 };

		[Desc("Min. and max. speed at which particles fall in px/tick.")]
		public readonly float[] Gravity = { 2.5f, 5f };

		[Desc("The current offset value for the swing movement. SwingOffset min. and max. value in px/tick.")]
		public readonly float[] SwingOffset = { 2.5f, 3.5f };

		[Desc("The value that particles swing to the side each update. SwingSpeed min. and max. value in px/tick.")]
		public readonly float[] SwingSpeed = { 0.0025f, 0.06f };

		[Desc("The value range that can be swung to the left or right. SwingAmplitude min. and max. value in px/tick.")]
		public readonly float[] SwingAmplitude = { 1.0f, 1.5f };

		[Desc("The randomly selected rgb(a) hex colors for the particles. Use this order: rrggbb[aa], rrggbb[aa], ...")]
		public readonly Color[] ParticleColors =
		{
			Color.FromArgb(236, 236, 236),
			Color.FromArgb(228, 228, 228),
			Color.FromArgb(208, 208, 208),
			Color.FromArgb(188, 188, 188)
		};

		[Desc("Works only with line enabled and can be used to fade out the tail of the line like a contrail.")]
		public readonly byte LineTailAlphaValue = 200;

		public override object Create(ActorInitializer init) { return new WeatherOverlay(init.World, this); }
	}

	public class WeatherOverlay : ITick, IRenderAboveWorld, INotifyViewportZoomExtentsChanged
	{
		readonly struct Particle
		{
			public readonly float2 Pos;
			public readonly int Size;
			public readonly float DirectionScatterX;
			public readonly float Gravity;
			public readonly float SwingOffset;
			public readonly float SwingSpeed;
			public readonly int SwingDirection;
			public readonly float SwingAmplitude;
			public readonly Color Color;
			public readonly Color TailColor;

			public Particle(WeatherOverlayInfo info, MersenneTwister r, Rectangle viewport)
			{
				var x = r.Next(viewport.Left, viewport.Right);
				var y = r.Next(viewport.Top, viewport.Bottom);

				Pos = new int2(x, y);
				Size = r.Next(info.ParticleSize[0], info.ParticleSize[1] + 1);
				DirectionScatterX = info.ScatterDirection[0] + r.Next(info.ScatterDirection[1] - info.ScatterDirection[0]);
				Gravity = float2.Lerp(info.Gravity[0], info.Gravity[1], r.NextFloat());
				SwingOffset = float2.Lerp(info.SwingOffset[0], info.SwingOffset[1], r.NextFloat());
				SwingSpeed = float2.Lerp(info.SwingSpeed[0], info.SwingSpeed[1], r.NextFloat());
				SwingDirection = r.Next(2) == 0 ? 1 : -1;
				SwingAmplitude = float2.Lerp(info.SwingAmplitude[0], info.SwingAmplitude[1], r.NextFloat());
				Color = info.ParticleColors.Random(r);
				TailColor = Color.FromArgb(info.LineTailAlphaValue, Color.R, Color.G, Color.B);
			}

			Particle(in Particle source)
			{
				Pos = source.Pos;
				Size = source.Size;
				DirectionScatterX = source.DirectionScatterX;
				Gravity = source.Gravity;
				SwingOffset = source.SwingOffset;
				SwingSpeed = source.SwingSpeed;
				SwingDirection = source.SwingDirection;
				SwingAmplitude = source.SwingAmplitude;
				Color = source.Color;
				TailColor = source.TailColor;
			}

			public Particle(in Particle source, float2 pos)
				: this(source)
			{
				Pos = pos;
			}

			public Particle(in Particle source, float2 pos, int swingDirection, float swingOffset)
				: this(source)
			{
				Pos = pos;
				SwingDirection = swingDirection;
				SwingOffset = swingOffset;
			}
		}

		readonly WeatherOverlayInfo info;
		readonly World world;

		float windStrength;
		int targetWindStrengthIndex;
		long windUpdateCountdown;
		Particle[] particles;
		Size viewportSize;
		long lastRender;

		public WeatherOverlay(World world, WeatherOverlayInfo info)
		{
			this.info = info;
			this.world = world;
			targetWindStrengthIndex = info.ChangingWindLevel ? world.LocalRandom.Next(info.WindLevels.Length) : 0;
			windUpdateCountdown = world.LocalRandom.Next(info.WindTick[0], info.WindTick[1]);
			windStrength = info.WindLevels[targetWindStrengthIndex];
		}

		void INotifyViewportZoomExtentsChanged.ViewportZoomExtentsChanged(float minZoom, float maxZoom)
		{
			// Track particles in a viewport fixed to the minimum zoom level
			var s = (1f / minZoom * new float2(Game.Renderer.NativeResolution)).ToInt2();
			viewportSize = new Size(s.X, s.Y);

			// Randomly distribute particles within the initial viewport
			var particleCount = viewportSize.Width * viewportSize.Height * info.ParticleDensityFactor / 10000;
			particles = new Particle[particleCount];
			var rect = new Rectangle(int2.Zero, viewportSize);
			for (var i = 0; i < particles.Length; i++)
				particles[i] = new Particle(info, world.LocalRandom, rect);
		}

		void ITick.Tick(Actor self)
		{
			if (!info.ChangingWindLevel || info.WindLevels.Length == 1)
				return;

			if (--windUpdateCountdown <= 0)
			{
				windUpdateCountdown = self.World.LocalRandom.Next(info.WindTick[0], info.WindTick[1]);
				if (targetWindStrengthIndex > 0 && self.World.LocalRandom.Next(2) == 1)
					targetWindStrengthIndex--;
				else if (targetWindStrengthIndex < info.WindLevels.Length - 1)
					targetWindStrengthIndex++;
			}

			// Fading the wind in little steps towards the TargetWindOffset
			var targetWindLevel = info.WindLevels[targetWindStrengthIndex];
			if (info.InstantWindChanges)
				windStrength = targetWindLevel;
			else if (Math.Abs(windStrength - targetWindLevel) > 0.01f)
			{
				if (windStrength > targetWindLevel)
					windStrength -= 0.01f;
				else if (windStrength < targetWindLevel)
					windStrength += 0.01f;
			}
		}

		void IRenderAboveWorld.RenderAboveWorld(Actor self, WorldRenderer wr)
		{
			var center = wr.Viewport.CenterLocation;
			var viewport = new Rectangle(center - new int2(viewportSize) / 2, viewportSize);
			var wcr = Game.Renderer.WorldRgbaColorRenderer;

			// SwingSpeed is defined in px/tick so we must account for the fraction of a tick that elapsed since the last render.
			// The scale is capped at 1 tick to avoid unexpected behaviour at game start, if RunTime overflows, or if the game stalls.
			var runtime = Game.RunTime;
			var tickFraction = Math.Min((runtime - lastRender) * 1f / world.Timestep, 1);
			lastRender = runtime;

			for (var i = 0; i < particles.Length; i++)
			{
				// Simulate wind and gravity effects on the particle
				var p = particles[i];
				if (!world.Paused)
				{
					var swingDirection = p.SwingDirection;
					if (p.SwingOffset < -p.SwingAmplitude || p.SwingOffset > p.SwingAmplitude)
						swingDirection *= -1;

					var swingOffset = p.SwingOffset + p.SwingDirection * p.SwingSpeed;
					var pos = p.Pos + tickFraction * new float2(p.DirectionScatterX + p.SwingOffset + windStrength, p.Gravity);
					particles[i] = p = new Particle(p, pos, swingDirection, swingOffset);
				}

				// Move the particle back inside the viewport if necessary
				if (!viewport.Contains(p.Pos.ToInt2()))
				{
					var dx = (p.Pos.X - viewport.Left) % viewport.Width;
					var dy = (p.Pos.Y - viewport.Top) % viewport.Height;

					if (dx < 0)
						dx += viewport.Width;

					if (dy < 0)
						dy += viewport.Height;

					particles[i] = p = new Particle(p, new float2(viewport.Left + dx, viewport.Top + dy));
				}

				// Render the particle
				// We must provide a z coordinate to stop the GL near and far Z limits from culling the geometry
				var a = new float3(p.Pos.X, p.Pos.Y, p.Pos.Y);
				if (info.UseSquares)
				{
					var b = a + new float2(p.Size, p.Size);
					wcr.FillRect(a, b, p.Color);
				}
				else
				{
					var tail = p.Pos + new float2(-windStrength, -p.Gravity * 2 / 3);

					var b = new float3(tail.X, tail.Y, tail.Y);
					wcr.DrawLine(a, b, p.Size, p.TailColor);
				}
			}
		}
	}
}
