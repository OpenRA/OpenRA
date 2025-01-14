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

namespace OpenRA.Mods.Common.MapGenerator
{
	/// <summary>
	/// A fixed-size 2D array that can be indexed either linearly or by coordinates.
	/// </summary>
	public sealed class Matrix<T>
	{
		/// <summary>Underlying matrix data.</summary>
		public readonly T[] Data;

		/// <summary>Matrix dimensions.</summary>
		public readonly int2 Size;

		/// <summary>
		/// Create a new matrix with the given size and adopt a given array as its backing data.
		/// </summary>
		Matrix(int2 size, T[] data)
		{
			if (data.Length != size.X * size.Y)
				throw new ArgumentException("Matrix data length does not match size");
			Data = data;
			Size = size;
		}

		/// <summary>Create a new matrix with the given size.</summary>
		public Matrix(int2 size)
			: this(size, new T[size.X * size.Y])
		{ }

		/// <summary>Create a new matrix with the given size.</summary>
		public Matrix(int x, int y)
			: this(new int2(x, y))
		{ }

		/// <summary>
		/// Convert a pair of coordinates into an index into Data.
		/// </summary>
		public int Index(int2 xy)
			=> Index(xy.X, xy.Y);

		/// <summary>
		/// Convert a pair of coordinates into an index into Data.
		/// </summary>
		public int Index(int x, int y)
		{
			if (!ContainsXY(x, y))
				throw new IndexOutOfRangeException(
					$"({x}, {y}) is out of bounds for a matrix of size ({Size.X}, {Size.Y})");
			return y * Size.X + x;
		}

		/// <summary>
		/// Convert a Data index into a pair of coordinates.
		/// </summary>
		public int2 XY(int index)
		{
			if (index < 0 || index >= Data.Length)
				throw new IndexOutOfRangeException(
					$"Index {index} is out of range for a matrix of size ({Size.X}, {Size.Y})");
			var y = Math.DivRem(index, Size.X, out var x);
			return new int2(x, y);
		}

		public T this[int x, int y]
		{
			get => Data[Index(x, y)];
			set => Data[Index(x, y)] = value;
		}

		public T this[int2 xy]
		{
			get => Data[Index(xy.X, xy.Y)];
			set => Data[Index(xy.X, xy.Y)] = value;
		}

		/// <summary>Shorthand for Data[i].</summary>
		public T this[int i]
		{
			get => Data[i];
			set => Data[i] = value;
		}

		/// <summary>True iff xy is a valid index within the matrix.</summary>
		public bool ContainsXY(int2 xy)
		{
			return xy.X >= 0 && xy.X < Size.X && xy.Y >= 0 && xy.Y < Size.Y;
		}

		/// <summary>True iff (x, y) is a valid index within the matrix.</summary>
		public bool ContainsXY(int x, int y)
		{
			return x >= 0 && x < Size.X && y >= 0 && y < Size.Y;
		}

		public bool IsEdge(int x, int y)
		{
			return x == 0 || x == Size.X - 1 || y == 0 || y == Size.Y - 1;
		}

		public bool IsEdge(int2 xy)
		{
			return IsEdge(xy.X, xy.Y);
		}

		/// <summary>Clamp xy to be the closest index within the matrix.</summary>
		public int2 ClampXY(int2 xy)
		{
			var (nx, ny) = ClampXY(xy.X, xy.Y);
			return new int2(nx, ny);
		}

		/// <summary>Clamp (x, y) to be the closest index within the matrix.</summary>
		public (int Nx, int Ny) ClampXY(int x, int y)
		{
			if (x >= Size.X)
				x = Size.X - 1;
			if (x < 0)
				x = 0;
			if (y >= Size.Y)
				y = Size.Y - 1;
			if (y < 0)
				y = 0;
			return (x, y);
		}

		/// <summary>
		/// Creates a transposed (shallow) copy of the matrix.
		/// </summary>
		public Matrix<T> Transpose()
		{
			var transposed = new Matrix<T>(new int2(Size.Y, Size.X));
			for (var y = 0; y < Size.Y; y++)
				for (var x = 0; x < Size.X; x++)
					transposed[y, x] = this[x, y];

			return transposed;
		}

		/// <summary>
		/// Return a new matrix with the same shape as this one containing the values after being
		/// transformed by a mapping func.
		/// </summary>
		public Matrix<R> Map<R>(Func<T, R> func)
		{
			var mapped = new Matrix<R>(Size);
			for (var i = 0; i < Data.Length; i++)
				mapped.Data[i] = func(Data[i]);

			return mapped;
		}

		/// <summary>
		/// Replace all values in the matrix with a given value. Returns this.
		/// </summary>
		public Matrix<T> Fill(T value)
		{
			Array.Fill(Data, value);
			return this;
		}

		/// <summary>
		/// Return a shallow clone of this matrix.
		/// </summary>
		public Matrix<T> Clone()
		{
			return new Matrix<T>(Size, (T[])Data.Clone());
		}

		/// <summary>
		/// Combine two same-shape matrices into a new output matrix.
		/// The zipping function specifies how values are combined.
		/// </summary>
		public static Matrix<T> Zip<T1, T2>(Matrix<T1> a, Matrix<T2> b, Func<T1, T2, T> func)
		{
			if (a.Size != b.Size)
				throw new ArgumentException("Input matrices to Zip must match in shape and size");
			var matrix = new Matrix<T>(a.Size);
			for (var i = 0; i < a.Data.Length; i++)
				matrix.Data[i] = func(a.Data[i], b.Data[i]);
			return matrix;
		}
	}
}
