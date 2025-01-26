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
using System.Numerics;
using OpenRA.Support;

namespace OpenRA.Mods.Common.MapGenerator
{
	public static class NoiseUtils
	{
		const int Scale = 1024;
		const int ScaledSqrt2 = 1448;

		/// <summary>Amplitude proportional to wavelength.</summary>
		public static int PinkAmplitude(int wavelength) => wavelength;

		/// <summary>
		/// <para>
		/// Create noise by combining multiple layers of Perlin noise of halving wavelengths.
		/// </para>
		/// <para>
		/// featureSize defines the largest wavelength in 1024ths of a matrix cell.
		/// the output.
		/// </para>
		/// <para>
		/// ampFunc specifies the amplitude of each wavelength. PinkAmplitude is often a suitable
		/// choice.
		/// </para>
		/// </summary>
		public static Matrix<int> FractalNoise(
			MersenneTwister random,
			int2 size,
			int featureSize,
			Func<int, int> ampFunc)
		{
			var span = Math.Max(size.X, size.Y);
			var wavelengths = new int[BitOperations.Log2((uint)span)];
			for (var i = 0; i < wavelengths.Length; i++)
				wavelengths[i] = featureSize >> i;

			var noise = new Matrix<int>(size);
			foreach (var wavelength in wavelengths)
			{
				if (wavelength <= Scale / 2)
					break;

				var amps = ampFunc(wavelength);
				var subSpan = span * Scale / wavelength + 2;
				var subNoise = PerlinNoise(random, subSpan);

				// Offsets should align to grid.
				// (The wavelength is divided back out later.)
				var scaledOffsetX = (int)(random.NextUint() % (wavelength + 1));
				var scaledOffsetY = (int)(random.NextUint() % (wavelength + 1));
				for (var y = 0; y < size.Y; y++)
					for (var x = 0; x < size.X; x++)
					{
						var scaledMappedX = x * Scale + scaledOffsetX;
						var scaledMappedY = y * Scale + scaledOffsetY;
						noise[y * size.X + x] +=
							amps * MatrixUtils.IntegerInterpolate(
								subNoise,
								scaledMappedX / wavelength,
								scaledMappedY / wavelength,
								scaledMappedX % wavelength,
								scaledMappedY % wavelength,
								wavelength);
					}
			}

			return noise;
		}

		/// <summary>
		/// 2D Perlin Noise generator without interpolation, producing a span-by-span sized matrix.
		/// Output values range from -5792 to +5792.
		/// </summary>
		public static Matrix<int> PerlinNoise(MersenneTwister random, int span)
		{
			var noise = new Matrix<int>(span, span);
			for (var y = 0; y <= span; y++)
				for (var x = 0; x <= span; x++)
				{
					var phase = new WAngle((int)random.NextUint() % 1024);
					var vx = phase.Cos();
					var vy = phase.Sin();
					if (x > 0 && y > 0)
						noise[x - 1, y - 1] += -vx + -vy;
					if (x < span && y > 0)
						noise[x, y - 1] += vx + -vy;
					if (x > 0 && y < span)
						noise[x - 1, y] += -vx + vy;
					if (x < span && y < span)
						noise[x, y] += vx + vy;
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
		public static Matrix<int> SymmetricFractalNoise(
			MersenneTwister random,
			int2 size,
			int rotations,
			Symmetry.Mirror mirror,
			int featureSize,
			Func<int, int> ampFunc)
		{
			if (rotations < 1)
				throw new ArgumentException("rotations must be >= 1");

			// Need higher resolution due to cropping and rotation artifacts
			var templateSpan = Math.Max(size.X, size.Y) * 2 + 2;
			var templateSize = new int2(templateSpan, templateSpan);
			var scaledTemplateCenter = new int2(templateSpan - 1, templateSpan - 1) * Scale / 2;
			var template = FractalNoise(random, templateSize, featureSize, ampFunc);

			var output = new Matrix<int>(size);

			var inclusiveOutputSize = size - new int2(1, 1);
			var scaledOutputMid = inclusiveOutputSize * Scale / 2;

			for (var y = 0; y < size.Y; y++)
				for (var x = 0; x < size.X; x++)
				{
					var outputXy = new int2(x, y);
					var scaledOutputXy = outputXy * Scale;
					var scaledOutputXyFromCenter = scaledOutputXy - scaledOutputMid;

					// Apply sqrt2 scaling so that diagonal samples don't alias.
					var scaledTemplateXyFromCenter = scaledOutputXyFromCenter * ScaledSqrt2 / Scale;
					var scaledTemplateXy = scaledTemplateXyFromCenter + scaledTemplateCenter;

					var projections = Symmetry.RotateAndMirrorPointAround(
						scaledTemplateXy, scaledTemplateCenter, rotations, mirror);

					foreach (var projection in projections)
						output[x, y] +=
							MatrixUtils.IntegerInterpolate(
								template,
								projection.X / Scale,
								projection.Y / Scale,
								projection.X % Scale,
								projection.Y % Scale,
								Scale);
				}

			return output;
		}

		/// <summary>
		/// Use SymmetricFractalNoise to fill a CellLayer. The noise is aligned to the CPos
		/// coordinate system.
		/// </summary>
		public static void SymmetricFractalNoiseIntoCellLayer(
			MersenneTwister random,
			CellLayer<int> cellLayer,
			int rotations,
			Symmetry.Mirror mirror,
			int featureSize,
			Func<int, int> ampFunc)
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
