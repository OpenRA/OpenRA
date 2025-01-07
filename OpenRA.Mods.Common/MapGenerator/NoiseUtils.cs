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
using OpenRA.Support;

namespace OpenRA.Mods.Common.MapGenerator
{
	public static class NoiseUtils
	{
		/// <summary>Amplitude proportional to wavelength.</summary>
		public static float PinkAmplitude(float wavelength) => wavelength;

		/// <summary>
		/// <para>
		/// Create noise by combining multiple layers of Perlin noise of halving wavelengths.
		/// </para>
		/// <para>
		/// wavelengthScale defines the largest wavelength as a fraction of the largest dimension of
		/// the output.
		/// </para>
		/// <para>
		/// ampFunc specifies the amplitude of each wavelength. PinkAmplitude is often a suitable
		/// choice.
		/// </para>
		/// </summary>
		public static Matrix<float> FractalNoise(
			MersenneTwister random,
			int2 size,
			float featureSize,
			Func<float, float> ampFunc)
		{
			var span = Math.Max(size.X, size.Y);
			var wavelengths = new float[(int)Math.Log2(span)];
			for (var i = 0; i < wavelengths.Length; i++)
				wavelengths[i] = featureSize / (1 << i);

			var noise = new Matrix<float>(size);
			foreach (var wavelength in wavelengths)
			{
				if (wavelength <= 0.5)
					break;

				var amps = ampFunc(wavelength);
				var subSpan = (int)(span / wavelength) + 2;
				var subNoise = PerlinNoise(random, subSpan);

				// Offsets should align to grid.
				// (The wavelength is divided back out later.)
				var offsetX = (int)(random.NextFloat() * wavelength);
				var offsetY = (int)(random.NextFloat() * wavelength);
				for (var y = 0; y < size.Y; y++)
					for (var x = 0; x < size.X; x++)
						noise[y * size.X + x] +=
							amps * MatrixUtils.Interpolate(
								subNoise,
								(offsetX + x) / wavelength,
								(offsetY + y) / wavelength);
			}

			return noise;
		}

		/// <summary>
		/// 2D Perlin Noise generator without interpolation, producing a span-by-span sized matrix.
		/// </summary>
		public static Matrix<float> PerlinNoise(MersenneTwister random, int span)
		{
			var noise = new Matrix<float>(span, span);
			const float D = 0.25f;
			for (var y = 0; y <= span; y++)
				for (var x = 0; x <= span; x++)
				{
					var phase = MathF.Tau * random.NextFloatExclusive();
					var vx = MathF.Cos(phase);
					var vy = MathF.Sin(phase);
					if (x > 0 && y > 0)
						noise[x - 1, y - 1] += vx * -D + vy * -D;
					if (x < span && y > 0)
						noise[x, y - 1] += vx * D + vy * -D;
					if (x > 0 && y < span)
						noise[x - 1, y] += vx * -D + vy * D;
					if (x < span && y < span)
						noise[x, y] += vx * D + vy * D;
				}

			return noise;
		}

		/// <summary>
		/// <para>
		/// Produce symmetric 2D noise by repeatedly applying some generated Perlin noise under
		/// rotation and mirroring.
		/// </para>
		/// <para>
		/// Note that the combination of multiple noise values with varying correlations creates a
		/// noise with different properties to simple Perlin noise.
		/// </para>
		/// </summary>
		public static Matrix<float> SymmetricFractalNoise(
			MersenneTwister random,
			int2 size,
			int rotations,
			Symmetry.Mirror mirror,
			float featureSize,
			Func<float, float> ampFunc)
		{
			if (rotations < 1)
				throw new ArgumentException("rotations must be >= 1");

			// Need higher resolution due to cropping and rotation artifacts
			var templateSpan = Math.Max(size.X, size.Y) * 2 + 2;
			var templateSize = new int2(templateSpan, templateSpan);
			var templateCenter = new float2(templateSpan - 1, templateSpan - 1) / 2.0f;
			var template = FractalNoise(random, templateSize, featureSize, ampFunc);

			var output = new Matrix<float>(size);

			var inclusiveOutputSize = size - new int2(1, 1);
			var outputMid = new float2(inclusiveOutputSize) / 2.0f;

			for (var y = 0; y < size.Y; y++)
				for (var x = 0; x < size.X; x++)
				{
					const float Sqrt2 = 1.4142135623730951f;
					var outputXy = new float2(x, y);
					var outputXyFromCenter = outputXy - outputMid;
					var templateXyFromCenter = outputXyFromCenter * Sqrt2;
					var templateXy = templateXyFromCenter + templateCenter;

					var projections = Symmetry.RotateAndMirrorPointAround(
						templateXy, templateCenter, rotations, mirror);

					foreach (var projection in projections)
						output[x, y] +=
							MatrixUtils.Interpolate(
								template,
								projection.X,
								projection.Y);
				}

			return output;
		}

		/// <summary>
		/// Use SymmetricFractalNoise to fill a CellLayer. The noise is aligned to the CPos
		/// coordinate system.
		/// </summary>
		public static void SymmetricFractalNoiseIntoCellLayer(
			MersenneTwister random,
			CellLayer<float> cellLayer,
			int rotations,
			Symmetry.Mirror mirror,
			float featureSize,
			Func<float, float> ampFunc)
		{
			var cellBounds = CellLayerUtils.CellBounds(cellLayer);
			var size = new int2(cellBounds.Size.Width, cellBounds.Size.Height);
			var noise = SymmetricFractalNoise(
				random,
				size,
				rotations,
				mirror,
				featureSize,
				ampFunc);
			CellLayerUtils.FromMatrix(cellLayer, noise);
		}
	}
}
