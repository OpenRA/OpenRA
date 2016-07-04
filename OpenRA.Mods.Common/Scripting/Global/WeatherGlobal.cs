#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting.Global
{
	[ScriptGlobal("Weather")]
	public class WeatherGlobal : ScriptGlobal
	{
		readonly WeatherOverlay overlay;
		readonly bool hasAnOverlay;

		public WeatherGlobal(ScriptContext context)
			: base(context)
		{
			overlay = context.World.WorldActor.TraitOrDefault<WeatherOverlay>();
			hasAnOverlay = overlay != null;
		}

		public bool Active
		{
			get { return hasAnOverlay && overlay.Active; }
			set { if (hasAnOverlay) overlay.Active = (bool)value; }
		}

		[Desc("Average number of particles per 1000x1000 px square.")]
		public int ParticleDensityFactor
		{
			get
			{
				return hasAnOverlay ? (int)overlay.ParticleDensityFactor : 0;
			}

			set
			{
				if (!hasAnOverlay)
					return;

				overlay.ParticleDensityFactor = (int)value;
				overlay.CalculateMaxParticleCount();
			}
		}

		[Desc("Should the level of the wind change over time, or just stick to the first value of WindLevels?")]
		public bool ChangingWindLevel
		{
			get { return hasAnOverlay && overlay.ChangingWindLevel; }
			set { if (hasAnOverlay) overlay.ChangingWindLevel = (bool)value; }
		}

		[Desc("The levels of wind intensity (particles x-axis movement in px/tick).")]
		public int[] WindLevels
		{
			get { return hasAnOverlay ? overlay.WindLevels : null; }
			set { if (hasAnOverlay) overlay.WindLevels = (int[])value; }
		}

		[Desc("Works only if ChangingWindLevel is enabled. Min. and max. ticks needed to change the WindLevel.")]
		public int[] WindTick
		{
			get { return hasAnOverlay ? overlay.WindTick : null; }
			set { if (hasAnOverlay) overlay.WindTick = (int[])value; }
		}

		[Desc("Hard or soft fading between the WindLevels.")]
		public bool InstantWindChanges
		{
			get { return hasAnOverlay && overlay.InstantWindChanges; }
			set { if (hasAnOverlay) overlay.InstantWindChanges = (bool)value; }
		}

		[Desc("Particles are drawn in squares when enabled, otherwise with lines.")]
		public bool UseSquares
		{
			get { return hasAnOverlay && overlay.UseSquares; }
			set { if (hasAnOverlay) overlay.UseSquares = (bool)value; }
		}

		[Desc("Size / width of the particle in px.")]
		public int[] ParticleSize
		{
			get { return hasAnOverlay ? overlay.ParticleSize : null; }
			set { if (hasAnOverlay) overlay.ParticleSize = (int[])value; }
		}

		[Desc("Scatters falling direction on the x-axis. Scatter min. and max. value in px/tick.")]
		public int[] ScatterDirection
		{
			get { return hasAnOverlay ? overlay.ScatterDirection : null; }
			set { if (hasAnOverlay) overlay.ScatterDirection = (int[])value; }
		}

		[Desc("Min. and max. speed at which particles fall in px/tick.")]
		public int[] Gravity
		{
			get { return hasAnOverlay ? Array.ConvertAll(overlay.GravityAmplitude, e => (int)e) : null; }
			set { if (hasAnOverlay) overlay.GravityAmplitude = Array.ConvertAll(value, e => (float)e); }
		}

		[Desc("The current offset value for the swing movement. SwingOffset min. and max. value in px/100 ticks.")]
		public int[] SwingOffset
		{
			get { return hasAnOverlay ? Array.ConvertAll(overlay.SwingOffset, e => (int)e * 100) : null; }
			set { if (hasAnOverlay) overlay.SwingOffset = Array.ConvertAll(value, e => (float)e / 100); }
		}

		[Desc("The value that particles swing to the side each update. SwingSpeed min. and max. value in px/1000 ticks.")]
		public int[] SwingSpeed
		{
			get { return hasAnOverlay ? Array.ConvertAll(overlay.SwingSpeed, e => (int)e * 1000) : null; }
			set { if (hasAnOverlay) overlay.SwingSpeed = Array.ConvertAll(value, e => (float)e / 1000); }
		}

		[Desc("The value range that can be swung to the left or right. SwingAmplitude min. and max. value in px/100 ticks.")]
		public int[] SwingAmplitude
		{
			get { return hasAnOverlay ? Array.ConvertAll(overlay.SwingAmplitude, e => (int)e * 100) : null; }
			set { if (hasAnOverlay) overlay.SwingAmplitude = Array.ConvertAll(value, e => (float)e / 100); }
		}

		[Desc("The randomly selected rgb(a) hex colors for the particles. Use this order: rrggbb[aa], rrggbb[aa], ...")]
		public HSLColor[] ParticleColors
		{
			get
			{
				if (!hasAnOverlay)
					return null;

				var colors = overlay.ParticleColors;
				var hslColors = new HSLColor[colors.Length];
				for (var i = 0; i < hslColors.Length; i++)
					hslColors[i] = HSLColor.FromRGB(colors[i].R, colors[i].G, colors[i].B);

				return hslColors;
			}

			set
			{
				if (!hasAnOverlay)
					return;

				var hslColors = (HSLColor[])value;
				var colors = new Color[hslColors.Length];
				for (var i = 0; i < hslColors.Length; i++)
					colors[i] = hslColors[i].RGB;

				overlay.ParticleColors = colors;
			}
		}

		[Desc("Works only with line enabled and can be used to fade out the tail of the line like a contrail.")]
		public int LineTailAlphaValue
		{
			get { return hasAnOverlay ? (int)overlay.LineTailAlphaValue : 0; }
			set { if (hasAnOverlay) overlay.LineTailAlphaValue = (byte)value; }
		}
	}
}
