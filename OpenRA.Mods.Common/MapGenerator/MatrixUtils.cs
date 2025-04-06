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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Mods.Common.MapGenerator
{
	public static class MatrixUtils
	{
		public const int MaxBinomialKernelRadius = 10;

		/// <summary>
		/// Debugging method that prints a matrix to stderr.
		/// </summary>
		public static void Dump2d(string label, Matrix<bool> matrix)
		{
			Console.Error.WriteLine($"{label}:");
			for (var y = 0; y < matrix.Size.Y; y++)
			{
				for (var x = 0; x < matrix.Size.X; x++)
					Console.Error.Write(matrix[x, y] ? "\u001b[0;42m .\u001b[m" : "\u001b[m .");
				Console.Error.Write("\n");
			}

			Console.Error.WriteLine("");
			Console.Error.Flush();
		}

		/// <summary>
		/// Debugging method that prints a matrix to stderr.
		/// </summary>
		public static void Dump2d(string label, Matrix<int> matrix)
		{
			Console.Error.WriteLine($"{label}: {matrix.Size.X} by {matrix.Size.Y}, {matrix.Data.Min()} to {matrix.Data.Max()}");
			for (var y = 0; y < matrix.Size.Y; y++)
			{
				for (var x = 0; x < matrix.Size.X; x++)
				{
					var v = matrix[x, y];
					string formatted;
					if (v > 0)
						formatted = string.Format(NumberFormatInfo.InvariantInfo, "\u001b[1;42m{0:X8}\u001b[m ", v);
					else if (v < 0)
						formatted = string.Format(NumberFormatInfo.InvariantInfo, "\u001b[1;41m{0:X8}\u001b[m ", v);
					else
						formatted = "\u001b[m       0 ";
					Console.Error.Write(formatted);
				}

				Console.Error.Write("\n");
			}

			Console.Error.WriteLine("");
			Console.Error.Flush();
		}

		/// <summary>
		/// Debugging method that prints a matrix to stderr.
		/// </summary>
		public static void Dump2d(string label, Matrix<byte> matrix)
		{
			Console.Error.WriteLine($"{label}: {matrix.Size.X} by {matrix.Size.Y}, {matrix.Data.Min()} to {matrix.Data.Max()}");
			for (var y = 0; y < matrix.Size.Y; y++)
			{
				for (var x = 0; x < matrix.Size.X; x++)
				{
					var v = matrix[x, y];
					string formatted;
					if (v > 0 && v < 0x80)
						formatted = string.Format(NumberFormatInfo.InvariantInfo, "\u001b[1;42m{0:X2}\u001b[m ", v);
					else if (v >= 0x80)
						formatted = string.Format(NumberFormatInfo.InvariantInfo, "\u001b[1;41m{0:X2}\u001b[m ", v);
					else
						formatted = "\u001b[m 0 ";
					Console.Error.Write(formatted);
				}

				Console.Error.Write("\n");
			}

			Console.Error.WriteLine("");
			Console.Error.Flush();
		}

		/// <summary>
		/// <para>
		/// Perform a generic flood fill starting at seeds <c>[(xy, prop), ...]</c>.
		/// </para>
		/// <para>
		/// For each point being considered for fill, <c>filler(xy, prop)</c> is
		/// called with the current position (xy) and propagation value (prop).
		/// filler should return the value to be propagated or null if not to be
		/// propagated. Propagation happens to all neighbours (offsets) defined
		/// by spread, regardless of whether they have previously been visited,
		/// so filler is responsible for terminating propagation by returning
		/// nulls. Usually, <c>Direction.SPREAD4</c> or <c>Direction.SPREAD8</c>
		/// is appropriate as a spread pattern.
		/// </para>
		/// <para>
		/// filler should capture and manipulate any necessary input and output
		/// arrays.
		/// </para>
		/// <para>
		/// Each call to filler will have either an equal or greater
		/// growth/propagation distance from their seed value than all calls
		/// before it. (You can think of this as them being called in ordered
		/// growth layers.)
		/// </para>
		/// <para>
		/// Note that filler may be called multiple times for the same spot,
		/// perhaps with different propagation values. Within the same
		/// growth/propagation distance, filler will be called from values
		/// propagated from earlier seeds before values propagated from later
		/// seeds.
		/// </para>
		/// <para>
		/// filler is not called for positions outside of the bounds defined by
		/// size EXCEPT for points being processed as seed values.
		/// </para>
		/// </summary>
		public static void FloodFill<P>(
			int2 size,
			IEnumerable<(int2 XY, P Prop)> seeds,
			Func<int2, P, P?> filler,
			ImmutableArray<int2> spread) where P : struct
		{
			var next = seeds.ToList();
			while (next.Count != 0)
			{
				var current = next;
				next = [];
				foreach (var (source, prop) in current)
				{
					var newProp = filler(source, prop);
					if (newProp != null)
						foreach (var offset in spread)
						{
							var destination = source + offset;
							if (destination.X >= 0 && destination.X < size.X && destination.Y >= 0 && destination.Y < size.Y)
								next.Add((destination, (P)newProp));
						}
				}
			}
		}

		/// <summary>
		/// <para>
		/// Compute the in-game walking distances (in 1024ths) from a set of seeds.
		/// </para>
		/// <para>
		/// The output matrix cells will contain either the distance (if reachable) or
		/// int.MaxValue.
		/// </para>
		/// </summary>
		public static Matrix<int> WalkingDistances(Matrix<bool> passable, IEnumerable<int2> seeds, int maxDistance)
		{
			const int Diagonal = 1448;
			const int Straight = 1024;

			var output = new Matrix<int>(passable.Size).Fill(int.MaxValue);
			var unprocessed = new PriorityArray<int>(passable.Size.X * passable.Size.Y, int.MaxValue);
			foreach (var seed in seeds)
				unprocessed[passable.Index(seed)] = 0;

			while (true)
			{
				var i = unprocessed.GetMinIndex();
				var distance = unprocessed[i];
				var xy = passable.XY(i);

				if (distance > maxDistance)
					break;

				if (distance <= maxDistance && output.ContainsXY(xy))
					output[xy] = distance;
				unprocessed[i] = int.MaxValue;

				foreach (var (offset, direction) in Direction.Spread8D)
				{
					var nextXY = xy + offset;
					if (!passable.ContainsXY(nextXY))
						continue;
					if (!passable[nextXY])
						continue;
					if (output[nextXY] != int.MaxValue)
						continue;
					int nextDistance;
					if (Direction.IsDiagonal(direction))
						nextDistance = distance + Diagonal;
					else
						nextDistance = distance + Straight;

					var nextI = passable.Index(nextXY);
					if (nextDistance < unprocessed[nextI])
						unprocessed[nextI] = nextDistance;
				}
			}

			return output;
		}

		/// <summary>
		/// <para>
		/// Shrinkwraps true space to be as far away from false space as possible, preserving
		/// topology. The result is a kind of rough Voronoi diagram.
		/// </para>
		/// <para>
		/// If the space matrix has width (w, h), the returned matrix will have width (w + 1, h + 1).
		/// Each value in the returned matrix is a Direction bitmask describing the border structure
		/// between the cells of the original space matrix.
		/// </para>
		/// outsideSpace specified the space values for cells which are outside the space matrix.
		/// <para>
		/// </para>
		/// </summary>
		public static Matrix<byte> DeflateSpace(Matrix<bool> space, bool outsideSpace)
		{
			var size = space.Size;
			var holes = new Matrix<int>(size);
			var holeCount = 0;
			for (var y = 0; y < space.Size.Y; y++)
				for (var x = 0; x < space.Size.X; x++)
					if (!space[x, y] && holes[x, y] == 0)
					{
						holeCount++;
						int? Filler(int2 xy, int holeId)
						{
							if (!space[xy] && holes[xy] == 0)
							{
								holes[xy] = holeId;
								return holeId;
							}
							else
							{
								return null;
							}
						}

						FloodFill(space.Size, [(new int2(x, y), holeCount)], Filler, Direction.Spread4);
					}

			const int UNASSIGNED = int.MaxValue;
			var voronoi = new Matrix<int>(size);
			var distances = new Matrix<int>(size).Fill(UNASSIGNED);
			var closestN = new Matrix<int>(size).Fill(UNASSIGNED);
			var midN = (size.X * size.Y + 1) / 2;
			var seeds = new List<(int2, (int, int2, int))>();
			for (var y = 0; y < size.Y; y++)
				for (var x = 0; x < size.X; x++)
				{
					var xy = new int2(x, y);
					if (holes[xy] != 0)
						seeds.Add((xy, (holes[xy], xy, closestN.Index(x, y))));
				}

			if (!outsideSpace)
			{
				holeCount++;
				for (var x = 0; x < size.X; x++)
				{
					// Hack: closestN is actually inside, but starting x, y are outside.
					seeds.Add((new int2(x, 0), (holeCount, new int2(x, -1), closestN.Index(x, 0))));
					seeds.Add((new int2(x, size.Y - 1), (holeCount, new int2(x, size.Y), closestN.Index(x, size.Y - 1))));
				}

				for (var y = 0; y < size.Y; y++)
				{
					// Hack: closestN is actually inside, but starting x, y are outside.
					seeds.Add((new int2(0, y), (holeCount, new int2(-1, y), closestN.Index(0, y))));
					seeds.Add((new int2(size.X - 1, y), (holeCount, new int2(size.X, y), closestN.Index(size.X - 1, y))));
				}
			}

			{
				(int HoleId, int2 StartXY, int StartN)? Filler(int2 xy, (int HoleId, int2 StartXY, int StartN) prop)
				{
					var n = closestN.Index(xy);
					var distance = (xy - prop.StartXY).LengthSquared;
					if (distance < distances[n])
					{
						voronoi[n] = prop.HoleId;
						distances[n] = distance;
						closestN[n] = prop.StartN;
						return (prop.HoleId, prop.StartXY, prop.StartN);
					}
					else if (distance == distances[n])
					{
						if (closestN[n] == prop.StartN)
						{
							return null;
						}
						else if (n <= midN == prop.StartN < closestN[n])
						{
							// For the first half of the map, lower seed indexes are preferred.
							// For the second half of the map, higher seed indexes are preferred.
							voronoi[n] = prop.HoleId;
							closestN[n] = prop.StartN;
							return (prop.HoleId, prop.StartXY, prop.StartN);
						}
						else
						{
							return null;
						}
					}
					else
					{
						return null;
					}
				}

				FloodFill(size, seeds, Filler, Direction.Spread4);
			}

			var deflatedSize = size + new int2(1, 1);
			var deflated = new Matrix<byte>(deflatedSize);
			var neighborhood = new int[4];
			var scan = new int2[]
			{
				new(-1, -1),
				new(0, -1),
				new(-1, 0),
				new(0, 0)
			};
			for (var cy = 0; cy < deflatedSize.Y; cy++)
				for (var cx = 0; cx < deflatedSize.X; cx++)
				{
					for (var neighbor = 0; neighbor < 4; neighbor++)
					{
						var x = Math.Clamp(cx + scan[neighbor].X, 0, size.X - 1);
						var y = Math.Clamp(cy + scan[neighbor].Y, 0, size.Y - 1);
						neighborhood[neighbor] = voronoi[x, y];
					}

					deflated[cx, cy] = (byte)(
						(neighborhood[0] != neighborhood[1] ? Direction.MU : 0) |
						(neighborhood[1] != neighborhood[3] ? Direction.MR : 0) |
						(neighborhood[3] != neighborhood[2] ? Direction.MD : 0) |
						(neighborhood[2] != neighborhood[0] ? Direction.ML : 0));
				}

			return deflated;
		}

		/// <summary>
		/// Convolute a kernel over a boolean input matrix.
		/// If dilating, the values specified by the kernel are logically OR-ed.
		/// If eroding, the values specified by the kernel are logically AND-ed.
		/// </summary>
		public static Matrix<bool> KernelDilateOrErode(Matrix<bool> input, Matrix<bool> kernel, int2 kernelCenter, bool dilate)
		{
			var output = new Matrix<bool>(input.Size).Fill(!dilate);
			for (var cy = 0; cy < input.Size.Y; cy++)
				for (var cx = 0; cx < input.Size.X; cx++)
				{
					void InnerLoop()
					{
						for (var ky = 0; ky < kernel.Size.Y; ky++)
							for (var kx = 0; kx < kernel.Size.X; kx++)
							{
								var x = cx + kx - kernelCenter.X;
								var y = cy + ky - kernelCenter.Y;
								if (!input.ContainsXY(x, y))
									continue;
								if (kernel[kx, ky] && input[x, y] == dilate)
								{
									output[cx, cy] = dilate;
									return;
								}
							}
					}

					InnerLoop();
				}

			return output;
		}

		/// <summary>
		/// <para>
		/// Create a one-dimensional binomial kernel of size (2 * radius + 1, 1).
		/// The total of all kernel cells is 1 &lt;&lt; (radius * 2).
		/// </para>
		/// <para>
		/// This can be applied once, transposed, then applied again to perform a full binomial blur.
		/// See <see cref="BinomialBlur"/>. Maximum supported radius is MaxBinomialKernelRadius.
		/// </para>
		/// </summary>
		static Matrix<long> BinomialKernel1D(int radius)
		{
			if (radius < 0 || radius > 10)
				throw new ArgumentException($"Binomial kernel radius was not in supported range (0 to {MaxBinomialKernelRadius} inclusive).");

			var span = radius * 2 + 1;
			var kernel = new Matrix<long>(new int2(span, 1));
			var factorials = new long[span];
			factorials[0] = 1;
			for (var i = 1; i < span; i++)
				factorials[i] = factorials[i - 1] * i;

			var n = span - 1;
			for (var k = 0; k < span; k++)
				kernel[k] = factorials[n] / (factorials[k] * factorials[n - k]);

			return kernel;
		}

		/// <summary>
		/// Apply an arithmetic convolution of a kernel over an input matrix.
		/// Cells outside the input matrix take the value of the nearest edge/corner cell.
		/// </summary>
		public static Matrix<long> KernelFilter(Matrix<long> input, Matrix<long> kernel, int2 kernelCenter)
		{
			var output = new Matrix<long>(input.Size);
			for (var cy = 0; cy < input.Size.Y; cy++)
				for (var cx = 0; cx < input.Size.X; cx++)
				{
					long total = 0;
					var samples = 0;
					for (var ky = 0; ky < kernel.Size.Y; ky++)
						for (var kx = 0; kx < kernel.Size.X; kx++)
						{
							var x = cx + kx - kernelCenter.X;
							var y = cy + ky - kernelCenter.Y;
							total += input[input.ClampXY(new int2(x, y))] * kernel[kx, ky];
							samples++;
						}

					output[cx, cy] = total;
				}

			return output;
		}

		/// <summary>
		/// Apply a binomial filter-based blur to a matrix, returning a new matrix. The result is
		/// somewhat similar to a Gaussian blur. Maximum supported radius is MaxBinomialKernelRadius.
		/// </summary>
		public static Matrix<int> BinomialBlur(Matrix<int> input, int radius)
		{
			var kernel = BinomialKernel1D(radius);
			var downscale = 2 * radius;
			var stage1 = KernelFilter(input.Map(v => (long)v), kernel, new int2(radius, 0));
			for (var i = 0; i < stage1.Data.Length; i++)
				stage1[i] >>= downscale;
			var stage2 = KernelFilter(stage1, kernel.Transpose(), new int2(0, radius));
			for (var i = 0; i < stage2.Data.Length; i++)
				stage2[i] >>= downscale;

			return stage2.Map(v => (int)v);
		}

		/// <summary>
		/// Finds the local variance of points in a grid (using a square sample area).
		/// Sample areas are centered on data point corners, so output is (size + 1) * (size + 1).
		/// </summary>
		public static Matrix<int> GridVariance(Matrix<int> input, int radius)
		{
			var output = new Matrix<int>(input.Size + new int2(1, 1));
			for (var cy = 0; cy < output.Size.Y; cy++)
				for (var cx = 0; cx < output.Size.X; cx++)
				{
					var total = 0;
					var samples = 0;
					for (var ry = -radius; ry < radius; ry++)
						for (var rx = -radius; rx < radius; rx++)
						{
							var y = cy + ry;
							var x = cx + rx;
							if (!input.ContainsXY(x, y))
								continue;
							total += input[x, y];
							samples++;
						}

					var mean = total / samples;
					long sumOfSquares = 0;
					for (var ry = -radius; ry < radius; ry++)
						for (var rx = -radius; rx < radius; rx++)
						{
							var y = cy + ry;
							var x = cx + rx;
							if (!input.ContainsXY(x, y))
								continue;
							long difference = mean - input[x, y];
							sumOfSquares += difference * difference;
						}

					output[cx, cy] = (int)(sumOfSquares / samples);
				}

			return output;
		}

		/// <summary>
		/// <para>
		/// Blur a boolean matrix using a square kernel, only changing the value
		/// if the neighborhood is significantly different based on a threshold.
		/// </para>
		/// <para>
		/// The threshold / thresholdOutOf is the size of a majority needed to
		/// change a value. For example, a threshold of 20 / 25 means, 80% of
		/// cells must agree to change a cell's value.
		/// </para>
		/// <para>
		/// The space outside of the matrix is treated as if the border was
		/// extended out.
		/// </para>
		/// <para>
		/// Along with the blurred matrix, the number of changes compared to the
		/// original is returned.
		/// </para>
		/// <para>
		/// Runtime complexity is approximately O(input.Size) for small radii.
		/// A more precise complexity would be
		///   O((input.Size.X + radius) * input.Size.Y +
		///     input.Size.X            * (input.Size.Y + radius)).
		/// </para>
		/// </summary>
		public static (Matrix<bool> Output, int Changes) BooleanBlur(
			Matrix<bool> input, int radius, int threshold, int thresholdOutOf)
		{
			// Sum radius-by-1 kernels first in O((size.X + radius) * size.Y) time using a diffing sliding
			// window, then sum 1-by-radius kernels in O(size.X * (size.Y + radius)) time.
			var hTrueCounts = new Matrix<int>(input.Size);
			var kernelArea = (2 * radius + 1) * (2 * radius + 1);

			if (threshold < 1 || thresholdOutOf < 1 || threshold * 2 < thresholdOutOf)
				throw new ArgumentException("invalid threshold");

			var trueThreshold = (kernelArea * threshold + thresholdOutOf - 1) / thresholdOutOf;
			var falseThreshold = kernelArea - trueThreshold;

			var output = new Matrix<bool>(input.Size);
			var changes = 0;

			for (var cy = 0; cy < input.Size.Y; cy++)
			{
				var trueCount = 0;
				for (var ox = -radius; ox <= radius; ox++)
					if (input[input.ClampXY(new int2(ox, cy))])
						trueCount++;
				hTrueCounts[0, cy] = trueCount;

				for (var cx = 1; cx < input.Size.X; cx++)
				{
					if (input[input.ClampXY(new int2(cx - radius - 1, cy))])
						trueCount--;
					if (input[input.ClampXY(new int2(cx + radius, cy))])
						trueCount++;

					hTrueCounts[cx, cy] = trueCount;
				}
			}

			void OutputForXY(int x, int y, int trueCount)
			{
				var thisInput = input[x, y];
				bool thisOutput;
				if (trueCount <= falseThreshold)
					thisOutput = false;
				else if (trueCount >= trueThreshold)
					thisOutput = true;
				else
					thisOutput = thisInput;
				output[x, y] = thisOutput;
				if (thisOutput != thisInput)
					changes++;
			}

			for (var cx = 0; cx < input.Size.X; cx++)
			{
				var trueCount = 0;
				for (var oy = -radius; oy <= radius; oy++)
					trueCount += hTrueCounts[hTrueCounts.ClampXY(new int2(cx, oy))];
				OutputForXY(cx, 0, trueCount);

				for (var cy = 1; cy < input.Size.Y; cy++)
				{
					trueCount -= hTrueCounts[hTrueCounts.ClampXY(new int2(cx, cy - radius - 1))];
					trueCount += hTrueCounts[hTrueCounts.ClampXY(new int2(cx, cy + radius))];

					OutputForXY(cx, cy, trueCount);
				}
			}

			return (output, changes);
		}

		/// <summary>
		/// Preserves foreground cells that can be safely covered by a (possibly
		/// out-of-bound) span-by-span square that doesn't touch any !foreground
		/// cells, and sets any remaining cells to !foreground.
		/// </summary>
		public static (Matrix<bool> Output, int Changes) RetainThickRegions(
			Matrix<bool> input, bool foreground, int span)
		{
			// The time complexity could be improved to O(input.Size) by using
			// a technique similar to BooleanBlur, but, in practice, this
			// hasn't needed optimizing yet.
			var output = new Matrix<bool>(input.Size).Fill(!foreground);
			for (var cy = 1 - span; cy < input.Size.Y; cy++)
				for (var cx = 1 - span; cx < input.Size.X; cx++)
				{
					bool IsRetained()
					{
						for (var ry = 0; ry < span; ry++)
							for (var rx = 0; rx < span; rx++)
							{
								var x = cx + rx;
								var y = cy + ry;
								if (!input.ContainsXY(x, y))
									continue;
								if (input[x, y] != foreground)
									return false;
							}

						return true;
					}

					if (!IsRetained()) continue;

					for (var ry = 0; ry < span; ry++)
						for (var rx = 0; rx < span; rx++)
						{
							var x = cx + rx;
							var y = cy + ry;
							if (!input.ContainsXY(x, y))
								continue;
							output[x, y] = foreground;
						}
				}

			var changes = 0;
			for (var i = 0; i < input.Data.Length; i++)
				if (input[i] != output[i])
					changes++;

			return (output, changes);
		}

		/// <summary>
		/// Read a linearly interpolated value between the cells of a matrix. xWeight and yWeight
		/// must be between 0 and scale inclusive and define the interpolation position
		/// between x and x+1, and y and y+1.
		/// </summary>
		public static int IntegerInterpolate(
			Matrix<int> matrix,
			int x,
			int y,
			int xWeight,
			int yWeight,
			int scale)
		{
			var xa = x;
			var xb = x + 1;
			var ya = y;
			var yb = y + 1;

			if (scale <= 0)
				throw new ArgumentException("Interpolation scale was not > 0");

			if (xWeight < 0 || yWeight < 0 || xWeight > scale || yWeight > scale)
				throw new ArgumentException("Interpolation weights were not between 0 and scale inclusive.");

			// "w" for "weight"
			var xbw = xWeight;
			var ybw = yWeight;
			var xaw = scale - xWeight;
			var yaw = scale - yWeight;

			if (xa < 0)
			{
				xa = 0;
				xb = 0;
			}
			else if (xb > matrix.Size.X - 1)
			{
				xa = matrix.Size.X - 1;
				xb = matrix.Size.X - 1;
			}

			if (ya < 0)
			{
				ya = 0;
				yb = 0;
			}
			else if (yb > matrix.Size.Y - 1)
			{
				ya = matrix.Size.Y - 1;
				yb = matrix.Size.Y - 1;
			}

			long naa = matrix[xa, ya];
			long nba = matrix[xb, ya];
			long nab = matrix[xa, yb];
			long nbb = matrix[xb, yb];
			return (int)(((naa * xaw + nba * xbw) * yaw + (nab * xaw + nbb * xbw) * ybw) / scale / scale);
		}

		/// <summary>
		/// Uniformally add to or subtract from all cells such that count out of every outOf cells,
		/// are no greater than the given target value.
		/// </summary>
		public static void CalibrateQuantileInPlace(Matrix<int> matrix, int target, int count, int outOf)
		{
			var sorted = (int[])matrix.Data.Clone();
			Array.Sort(sorted);
			var adjustment = target - sorted[(long)(sorted.Length - 1) * count / outOf];
			for (var i = 0; i < matrix.Data.Length; i++)
				matrix[i] += adjustment;
		}

		/// <summary>
		/// For true cells, gives the Chebyshev distance to the closest false cell.
		/// For false cells, gives the Chebyshev distance to the closest true cell as a negative.
		/// outsideValue specifies whether cells outside of the matrix are true or false.
		/// </summary>
		public static Matrix<int> ChebyshevRoom(Matrix<bool> input, bool outsideValue)
		{
			var roominess = new Matrix<int>(input.Size);

			var seeds = new List<(int2, int)>();

			// Find true/false boundaries and map boundary
			for (var cy = 0; cy < input.Size.Y; cy++)
				for (var cx = 0; cx < input.Size.X; cx++)
				{
					var pCount = 0;
					var nCount = 0;
					for (var oy = -1; oy <= 1; oy++)
						for (var ox = -1; ox <= 1; ox++)
						{
							var x = cx + ox;
							var y = cy + oy;
							if (input.ContainsXY(x, y) ? input[x, y] : outsideValue)
								pCount++;
							else
								nCount++;
						}

					if (pCount != 9 && nCount != 9)
						seeds.Add((new int2(cx, cy), 1));
				}

			if (seeds.Count == 0)
			{
				// There were no shores. Use minSpan or -minSpan as appropriate.
				var minSpan = Math.Min(input.Size.X, input.Size.Y);
				roominess.Fill(input[0] ? minSpan : -minSpan);
				return roominess;
			}

			int? Filler(int2 xy, int room)
			{
				if (!roominess.ContainsXY(xy) || roominess[xy] != 0)
					return null;
				roominess[xy] = input[xy] ? room : -room;
				return room + 1;
			}

			FloodFill(
				roominess.Size,
				seeds,
				Filler,
				Direction.Spread8);

			return roominess;
		}

		/// <summary>
		/// <para>
		/// Given a set of grid-intersection point arrays, creates a matrix where each cell
		/// identifies whether the closest points are wrapping around it clockwise or
		/// counter-clockwise (as defined in MapUtils.Direction).
		/// </para>
		/// <para>
		/// Positive output values indicate the points are wrapping around it clockwise.
		/// Negative output values indicate the points are wrapping around it counter-clockwise.
		/// Outputs can be zero or non-unit magnitude if there are fighting point arrays.
		/// </para>
		/// </summary>
		public static Matrix<int> PointsChirality(int2 size, IEnumerable<int2[]> pointArrayArray)
		{
			const int FirstPassSentinel = int.MinValue;

			var chirality = new Matrix<int>(size);
			var seeds = new List<(int2, int)>();

			void SeedChirality(int2 point, int value)
			{
				if (!chirality.ContainsXY(point))
					return;
				chirality[point] += value;
				seeds.Add((point, FirstPassSentinel));
			}

			foreach (var pointArray in pointArrayArray)
				for (var i = 1; i < pointArray.Length; i++)
				{
					var from = pointArray[i - 1];
					var to = pointArray[i];
					var direction = Direction.FromInt2(to - from);
					var fx = from.X;
					var fy = from.Y;
					switch (direction)
					{
						case Direction.R:
							SeedChirality(new int2(fx, fy), 1);
							SeedChirality(new int2(fx, fy - 1), -1);
							break;
						case Direction.D:
							SeedChirality(new int2(fx - 1, fy), 1);
							SeedChirality(new int2(fx, fy), -1);
							break;
						case Direction.L:
							SeedChirality(new int2(fx - 1, fy - 1), 1);
							SeedChirality(new int2(fx - 1, fy), -1);
							break;
						case Direction.U:
							SeedChirality(new int2(fx, fy - 1), 1);
							SeedChirality(new int2(fx - 1, fy - 1), -1);
							break;
						default:
							throw new ArgumentException("Unsupported direction for chirality");
					}
				}

			int? FillChirality(int2 point, int prop)
			{
				if (prop == FirstPassSentinel)
					return chirality[point];

				if (chirality[point] != 0)
					return null;
				chirality[point] = prop;
				return prop;
			}

			FloodFill(size, seeds, FillChirality, Direction.Spread4);

			return chirality;
		}

		/// <summary>
		/// <para>
		/// Trace the borders between true and false regions of an input matrix, returning an array
		/// of point sequences.
		/// </para>
		/// <para>
		/// Point sequences follow the borders keeping the true region on the right-hand side as it
		/// traces forward. Loops have a matching start and end point.
		/// </para>
		/// <para>
		/// If a mask is supplied, only borders between matrix cells in the mask are considered.
		/// </para>
		/// </summary>
		public static int2[][] BordersToPoints(Matrix<bool> matrix, Matrix<bool> mask = null)
		{
			if (mask != null && matrix.Size != mask.Size)
				throw new ArgumentException("matrix and mask did not have same size");

			// There is redundant memory/iteration, but I don't care enough.

			// These are really only the signs of the gradients.
			var gradientH = new Matrix<sbyte>(matrix.Size);
			var gradientV = new Matrix<sbyte>(matrix.Size);
			for (var y = 0; y < matrix.Size.Y; y++)
				for (var x = 1; x < matrix.Size.X; x++)
					if (mask == null || (mask[x - 1, y] && mask[x, y]))
					{
						var l = matrix[x - 1, y] ? 1 : 0;
						var r = matrix[x, y] ? 1 : 0;
						gradientV[x, y] = (sbyte)(r - l);
					}

			for (var y = 1; y < matrix.Size.Y; y++)
				for (var x = 0; x < matrix.Size.X; x++)
					if (mask == null || (mask[x, y - 1] && mask[x, y]))
					{
						var u = matrix[x, y - 1] ? 1 : 0;
						var d = matrix[x, y] ? 1 : 0;
						gradientH[x, y] = (sbyte)(d - u);
					}

			// Looping paths contain the start/end point twice.
			var paths = new List<int2[]>();
			void TracePath(int sx, int sy, int direction)
			{
				var points = new List<int2>();
				var x = sx;
				var y = sy;
				points.Add(new int2(x, y));
				do
				{
					switch (direction)
					{
						case Direction.R:
							gradientH[x, y] = 0;
							x++;
							break;
						case Direction.D:
							gradientV[x, y] = 0;
							y++;
							break;
						case Direction.L:
							x--;
							gradientH[x, y] = 0;
							break;
						case Direction.U:
							y--;
							gradientV[x, y] = 0;
							break;
						default:
							throw new ArgumentException("direction assertion failed");
					}

					points.Add(new int2(x, y));
					var r = gradientH.ContainsXY(x, y) && gradientH[x, y] > 0;
					var d = gradientV.ContainsXY(x, y) && gradientV[x, y] < 0;
					var l = gradientH.ContainsXY(x - 1, y) && gradientH[x - 1, y] < 0;
					var u = gradientV.ContainsXY(x, y - 1) && gradientV[x, y - 1] > 0;
					if (direction == Direction.R && u)
						direction = Direction.U;
					else if (direction == Direction.D && r)
						direction = Direction.R;
					else if (direction == Direction.L && d)
						direction = Direction.D;
					else if (direction == Direction.U && l)
						direction = Direction.L;
					else if (r)
						direction = Direction.R;
					else if (d)
						direction = Direction.D;
					else if (l)
						direction = Direction.L;
					else if (u)
						direction = Direction.U;
					else
						break; // Dead end (not a loop)
				}
				while (x != sx || y != sy);

				paths.Add(points.ToArray());
			}

			// Trace non-loops (from edge of map)
			for (var x = 1; x < matrix.Size.X; x++)
			{
				if (gradientV[x, 0] < 0)
					TracePath(x, 0, Direction.D);
				if (gradientV[x, matrix.Size.Y - 1] > 0)
					TracePath(x, matrix.Size.Y, Direction.U);
			}

			for (var y = 1; y < matrix.Size.Y; y++)
			{
				if (gradientH[0, y] > 0)
					TracePath(0, y, Direction.R);
				if (gradientH[matrix.Size.X - 1, y] < 0)
					TracePath(matrix.Size.X, y, Direction.L);
			}

			// Trace loops
			for (var y = 0; y < matrix.Size.Y; y++)
				for (var x = 0; x < matrix.Size.X; x++)
				{
					if (gradientH[x, y] > 0)
						TracePath(x, y, Direction.R);
					else if (gradientH[x, y] < 0)
						TracePath(x + 1, y, Direction.L);

					if (gradientV[x, y] < 0)
						TracePath(x, y, Direction.D);
					else if (gradientV[x, y] > 0)
						TracePath(x, y + 1, Direction.U);
				}

			return paths.ToArray();
		}

		/// <summary>
		/// <para>
		/// Takes an input boolean matrix and performs adjustments to improve the local consistency
		/// of the true and false regions, making them "blotchy":
		/// </para>
		/// <para>
		/// - Smoothing via thresholded median blurs.
		/// </para>
		/// <para>
		/// - A minimum thickness is enforced for all true/false regions. More formally, eroding
		///   and then dilating the true or false regions by minimumThickness results in no change.
		/// </para>
		/// <para>
		/// - No grid points connect diagonally-crossing true and false regions. In other words,
		///   these 2x2 patterns never appear in the output matrix:
		/// <code>
		///     10      01
		///     01  or  10
		/// </code>
		/// </para>
		/// <para>
		/// A new matrix is returned. The input is unmodified.
		/// </para>
		/// </summary>
		public static Matrix<bool> BooleanBlotch(
			Matrix<bool> input,
			int terrainSmoothing,
			int smoothingThreshold,
			int smoothingThresholdOutOf,
			int minimumThickness,
			bool bias)
		{
			var maxSpan = Math.Max(input.Size.X, input.Size.Y);
			var matrix = input;

			(matrix, _) = BooleanBlur(matrix, terrainSmoothing, 1, 2);
			for (var i1 = 0; i1 < /*max passes*/16; i1++)
			{
				for (var i2 = 0; i2 < maxSpan; i2++)
				{
					int changes;
					var changesAcc = 0;
					for (var r = 1; r <= terrainSmoothing; r++)
					{
						(matrix, changes) = BooleanBlur(matrix, r, smoothingThreshold, smoothingThresholdOutOf);
						changesAcc += changes;
					}

					if (changesAcc == 0)
						break;
				}

				{
					var changesAcc = 0;
					int changes;
					(matrix, changes) = RetainThickRegions(matrix, true, minimumThickness);
					changesAcc += changes;
					changes = DilateThinRegionsInPlaceFull(matrix, true, minimumThickness);
					changesAcc += changes;

					var midFixLandmass = matrix.Clone();

					(matrix, changes) = RetainThickRegions(matrix, false, minimumThickness);
					changesAcc += changes;
					changes = DilateThinRegionsInPlaceFull(matrix, false, minimumThickness);
					changesAcc += changes;
					if (changesAcc == 0)
						break;

					if (i1 >= 8 && i1 % 4 == 0)
					{
						var diff = Matrix<bool>.Zip(midFixLandmass, matrix, (a, b) => a != b);
						for (var y = 0; y < matrix.Size.Y; y++)
							for (var x = 0; x < matrix.Size.X; x++)
							{
								if (diff[x, y])
									OverCircle(
										matrix: matrix,
										centerIn1024ths: new int2(x * 1024 + 512, y * 1024 + 512),
										radiusIn1024ths: minimumThickness * 2048,
										outside: false,
										action: (xy, _) => matrix[xy] = bias);
							}
					}
				}
			}

			return matrix;
		}

		/// <summary>
		/// Repeatedly calls DilateThinRegionsInPlace until no changes are made.
		/// </summary>
		static int DilateThinRegionsInPlaceFull(Matrix<bool> input, bool foreground, int width)
		{
			int changes;
			var changesAcc = 0;

			do
			{
				changes = DilateThinRegionsInPlace(input, foreground, width);
				changesAcc += changes;
			}
			while (changes > 0);

			return changesAcc;
		}

		/// <summary>
		/// <para>
		/// If foreground true, finds the thinnest true regions and dilates them.
		/// If foreground false, finds the thinnest false regions and dilates them.
		/// Each call only dilates thin regions by one cell's thickness on each border.
		/// </para>
		/// <para>
		/// Only regions with a thickness less than width (in Chebyshev distance) are considered.
		/// </para>
		/// <para>
		/// Returns the number of changes made.
		/// </para>
		/// </summary>
		static int DilateThinRegionsInPlace(Matrix<bool> input, bool foreground, int width)
		{
			var sizeMinus1 = input.Size - new int2(1, 1);
			var cornerMaskSpan = width + 1;

			// Zero means ignore.
			var cornerMask = new Matrix<int>(cornerMaskSpan, cornerMaskSpan);

			for (var y = 0; y < cornerMaskSpan; y++)
				for (var x = 0; x < cornerMaskSpan; x++)
					cornerMask[x, y] = 1 + width + width - x - y;

			cornerMask[0] = 0;

			// Higher number indicates a thinner area.
			var thinness = new Matrix<int>(input.Size);
			void SetThinness(int x, int y, int v)
			{
				if (!input.ContainsXY(x, y))
					return;
				if (input[x, y] == foreground)
					return;
				thinness[x, y] = Math.Max(v, thinness[x, y]);
			}

			for (var cy = 0; cy < input.Size.Y; cy++)
				for (var cx = 0; cx < input.Size.X; cx++)
				{
					if (input[cx, cy] == foreground)
						continue;

					// _L_eft _R_ight _U_p _D_own
					var l = input[Math.Max(cx - 1, 0), cy] == foreground;
					var r = input[Math.Min(cx + 1, sizeMinus1.X), cy] == foreground;
					var u = input[cx, Math.Max(cy - 1, 0)] == foreground;
					var d = input[cx, Math.Min(cy + 1, sizeMinus1.Y)] == foreground;
					var lu = l && u;
					var ru = r && u;
					var ld = l && d;
					var rd = r && d;
					for (var ry = 0; ry < cornerMaskSpan; ry++)
						for (var rx = 0; rx < cornerMaskSpan; rx++)
						{
							if (rd)
							{
								var x = cx + rx;
								var y = cy + ry;
								SetThinness(x, y, cornerMask[rx, ry]);
							}

							if (ru)
							{
								var x = cx + rx;
								var y = cy - ry;
								SetThinness(x, y, cornerMask[rx, ry]);
							}

							if (ld)
							{
								var x = cx - rx;
								var y = cy + ry;
								SetThinness(x, y, cornerMask[rx, ry]);
							}

							if (lu)
							{
								var x = cx - rx;
								var y = cy - ry;
								SetThinness(x, y, cornerMask[rx, ry]);
							}
						}
				}

			var thinnest = thinness.Data.Max();
			if (thinnest == 0)
			{
				// No fixes
				return 0;
			}

			var changes = 0;
			for (var y = 0; y < input.Size.Y; y++)
				for (var x = 0; x < input.Size.X; x++)
					if (thinness[x, y] == thinnest)
					{
						input[x, y] = foreground;
						changes++;
					}

			// Fixes made, with potentially more that can be done in another pass.
			return changes;
		}

		/// <summary>Remove links from a direction map that are not reciprocated.</summary>
		public static void RemoveStubsFromDirectionMapInPlace(Matrix<byte> matrix)
		{
			var output = matrix.Clone();
			for (var cy = 0; cy < matrix.Size.Y; cy++)
				for (var cx = 0; cx < matrix.Size.X; cx++)
				{
					var fromPos = new int2(cx, cy);
					var fromDm = matrix[fromPos];
					foreach (var (offset, d) in Direction.Spread8D)
					{
						if ((fromDm & (1 << d)) == 0)
							continue;

						var dr = Direction.Reverse(d);
						var toPos = new int2(cx + offset.X, cy + offset.Y);
						if (matrix.ContainsXY(toPos) && (matrix[toPos] & (1 << dr)) != 0)
							continue;

						matrix[fromPos] = (byte)(output[fromPos] & ~(1 << d));
					}
				}
		}

		static Matrix<byte> RemoveJunctionsFromDirectionMap(Matrix<byte> input)
		{
			var output = input.Clone();
			for (var cy = 0; cy < input.Size.Y; cy++)
				for (var cx = 0; cx < input.Size.X; cx++)
				{
					var dm = input[cx, cy];
					if (Direction.Count(dm) > 2)
					{
						output[cx, cy] = 0;
						foreach (var (offset, d) in Direction.Spread8D)
						{
							var xy = new int2(cx + offset.X, cy + offset.Y);
							if (!input.ContainsXY(xy))
								continue;
							var dr = Direction.Reverse(d);
							output[xy] = (byte)(output[xy] & ~(1 << dr));
						}
					}
				}

			return output;
		}

		/// <summary>
		/// Traces a matrix of directions into a set of point sequences. Each point sequence is
		/// traced up to but excluding junction points. Paths are traced in both directions. The
		/// paths in the direction map must be bidirectional and contain no stubs.
		/// </summary>
		public static int2[][] DirectionMapToPaths(Matrix<byte> input)
		{
			var links = RemoveJunctionsFromDirectionMap(input);

			// Find non-loops, starting at terminals.
			var pointArrays = new List<int2[]>();

			void TracePoints(int2 xy, int reverseDm)
			{
				var points = new List<int2>();

				bool AddPoint()
				{
					points.Add(xy);
					var dm = links[xy] & ~reverseDm;
					links[xy] = 0;
					foreach (var (offset, d) in Direction.Spread8D)
						if ((dm & (1 << d)) != 0)
						{
							xy += offset;
							reverseDm = 1 << Direction.Reverse(d);
							return true;
						}

					return false;
				}

				while (AddPoint()) { }

				pointArrays.Add(points.ToArray());
				pointArrays.Add(points.Reverse<int2>().ToArray());
			}

			for (var sy = 0; sy < links.Size.Y; sy++)
				for (var sx = 0; sx < links.Size.X; sx++)
					if (Direction.FromMask(links[sx, sy]) != Direction.None)
						TracePoints(new int2(sx, sy), 0);

			// All non-loops have been removed, leaving only loops left.
			for (var sy = 0; sy < links.Size.Y; sy++)
				for (var sx = 0; sx < links.Size.X; sx++)
					if (links[sx, sy] != 0)
					{
						// Choose direction with most-significant bit
						var reverseDm = links[sx, sy] & (links[sx, sy] - 1);
						TracePoints(new int2(sx, sy), reverseDm);
					}

			return pointArrays.ToArray();
		}

		/// <summary>
		/// Wrapper around DirectionMapToPaths which iteratively prunes stubs and short paths until
		/// all paths are at least a minimumLength. Paths shorter than mimimumJunctionSeparation
		/// sever their neighboring junctions instead of fusing them together.
		/// </summary>
		public static int2[][] DirectionMapToPathsWithPruning(
			Matrix<byte> input,
			int minimumLength,
			int minimumJunctionSeparation,
			bool preserveEdgePaths)
		{
			var links = input.Clone();
			int2[][] pointArrays;

			// Iteratively remove paths which are too short and merge remaining ones.
			while (true)
			{
				RemoveStubsFromDirectionMapInPlace(links);
				pointArrays = DirectionMapToPaths(links);

				var removeablePointArrays = pointArrays;
				if (preserveEdgePaths)
					removeablePointArrays = pointArrays
					.Where(a => !(links.IsEdge(a[0]) || links.IsEdge(a[^1])))
					.ToArray();

				if (removeablePointArrays.Length == 0)
					return pointArrays;

				var shortest = removeablePointArrays.Min(a => a.Length);

				if (shortest >= minimumLength)
					return pointArrays;

				var toDelete = new List<int2>();
				foreach (var pointArray in removeablePointArrays)
					if (pointArray.Length == shortest)
						foreach (var point in pointArray)
						{
							toDelete.Add(point);
							if (pointArray.Length < minimumJunctionSeparation)
								foreach (var (offset, d) in Direction.Spread8D)
									if ((links[point] & (1 << d)) != 0)
										toDelete.Add(point + offset);
						}

				foreach (var point in toDelete)
					links[point] = 0;
			}
		}

		/// <summary>
		/// <para>
		/// Given a set of point sequences and a stencil mask that defines permitted point positions,
		/// remove points that are disallowed, splitting or dropping point sequences as needed.
		/// </para>
		/// <para>
		/// The outside of the matrix is considered false (points disallowed).
		/// </para>
		/// <para>
		/// Sequences with fewer than 2 points are dropped.
		/// </para>
		/// </summary>
		public static int2[][] MaskPathPoints(IEnumerable<int2[]> pointArrayArray, Matrix<bool> mask)
		{
			var newPointArrayArray = new List<int2[]>();

			foreach (var pointArray in pointArrayArray)
			{
				if (pointArray == null || pointArray.Length < 2)
					continue;

				var isLoop = pointArray[0] == pointArray[^1];
				int firstBad;
				for (firstBad = 0; firstBad < pointArray.Length; firstBad++)
					if (!(mask.ContainsXY(pointArray[firstBad]) && mask[pointArray[firstBad]]))
						break;

				if (firstBad == pointArray.Length)
				{
					// The path is entirely within the mask already.
					newPointArrayArray.Add(pointArray);
					continue;
				}

				var startAt = isLoop ? firstBad : 0;
				var wrapAt = isLoop ? pointArray.Length - 1 : pointArray.Length;
				var i = startAt;
				List<int2> currentPointArray = null;
				do
				{
					if (mask.ContainsXY(pointArray[i]) && mask[pointArray[i]])
					{
						currentPointArray ??= [];
						currentPointArray.Add(pointArray[i]);
					}
					else
					{
						if (currentPointArray != null && currentPointArray.Count > 1)
							newPointArrayArray.Add(currentPointArray.ToArray());
						currentPointArray = null;
					}

					i++;
					if (i == wrapAt)
						i = 0;
				}
				while (i != startAt);

				if (currentPointArray != null && currentPointArray.Count > 1)
					newPointArrayArray.Add(currentPointArray.ToArray());
			}

			return newPointArrayArray.ToArray();
		}

		/// <summary>
		/// <para>
		/// Run an action over the inside or outside of a circle of given center and radius,
		/// measured in 1024ths of a cell. The action is called with the int2 cell position (NOT in
		/// 1024ths), and the square of the distance-in-1024ths from the cell's center to the
		/// circle's center. (Square root and divide by 1024 to get the distance in whole cells.)
		/// (0, 0) is a corner of the matrix, and (512, 512) is the center of the first cell.
		/// If outside is true, the action is run for cells outside of the circle instead
		/// of the inside.
		/// </para>
		/// <para>
		/// A matrix cell is inside the circle if its center is &lt;= radius from center.
		/// Coordinates outside of the Matrix are ignored.
		/// </para>
		/// </summary>
		public static void OverCircle<T>(
			Matrix<T> matrix,
			int2 centerIn1024ths,
			int radiusIn1024ths,
			bool outside,
			Action<int2, long> action)
		{
			var size = matrix.Size;
			int minX;
			int minY;
			int maxX;
			int maxY;
			if (outside)
			{
				minX = 0;
				minY = 0;
				maxX = size.X - 1;
				maxY = size.Y - 1;
			}
			else
			{
				minX = (centerIn1024ths.X - radiusIn1024ths) / 1024;
				minY = (centerIn1024ths.Y - radiusIn1024ths) / 1024;
				maxX = (centerIn1024ths.X + radiusIn1024ths + 1023) / 1024;
				maxY = (centerIn1024ths.Y + radiusIn1024ths + 1023) / 1024;
				if (minX < 0)
					minX = 0;
				if (minY < 0)
					minY = 0;
				if (maxX >= size.X)
					maxX = size.X - 1;
				if (maxY >= size.Y)
					maxY = size.Y - 1;
			}

			var radiusSquared = (long)radiusIn1024ths * radiusIn1024ths;
			for (var y = minY; y <= maxY; y++)
				for (var x = minX; x <= maxX; x++)
				{
					var rx = x * 1024 + 512 - centerIn1024ths.X;
					var ry = y * 1024 + 512 - centerIn1024ths.Y;
					var thisRadiusSquared = (long)rx * rx + (long)ry * ry;
					if (thisRadiusSquared <= radiusSquared != outside)
						action(new int2(x, y), thisRadiusSquared);
				}
		}

		/// <summary>
		/// Linearly scales the range of values in a matrix to the given target amplitude.
		/// Returns the modified input. If the input matrix is all zeros, it is left unmodified.
		/// </summary>
		public static Matrix<int> NormalizeRangeInPlace(Matrix<int> matrix, int targetAmplitude)
		{
			long inputAmplitude = matrix.Data.Max(Math.Abs);
			if (inputAmplitude == 0)
				return matrix;

			for (var i = 0; i < matrix.Data.Length; i++)
				matrix[i] = (int)((long)matrix[i] * targetAmplitude / inputAmplitude);

			return matrix;
		}

		/// <summary>
		/// Rank all cell values and select the best (greatest compared) value.
		/// If there are equally good best candidates, choose one at random.
		/// </summary>
		public static (int2 MPos, T Value) FindRandomBest<T>(
			Matrix<T> matrix,
			MersenneTwister random,
			Comparison<T> comparison)
		{
			var candidates = new List<int2>();
			var best = matrix[new int2(0, 0)];
			for (var y = 0; y < matrix.Size.Y; y++)
				for (var x = 0; x < matrix.Size.X; x++)
				{
					var rank = comparison(matrix[x, y], best);
					if (rank > 0)
					{
						best = matrix[x, y];
						candidates.Clear();
					}

					if (rank >= 0)
						candidates.Add(new int2(x, y));
				}

			var choice = candidates[random.Next(candidates.Count)];
			return (choice, best);
		}
	}
}
