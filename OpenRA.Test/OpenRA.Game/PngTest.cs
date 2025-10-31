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

using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Test
{
	[TestFixture]
	public class PngTests
	{
		[Test]
		public void Save_ShouldProduceValidPngFile()
		{
			// Arrange
			var colors = Enumerable.Range(0, 256).Select(i => Color.FromArgb(i, i, i)).ToArray();
			var png = new Png(new byte[10 * 20], SpriteFrameType.Indexed8, 10, 20, colors);

			// Act
			var result = png.Save();

			// Assert
			Assert.That(Png.Verify(new MemoryStream(result)), Is.True);
		}

		[Test]
		public void Save_Method_Should_Write_Indexed8_Palette_If_256_Colors_Or_Less()
		{
			// Arrange
			var colors = Enumerable.Range(0, 256).Select(i => Color.FromArgb(i, i, i)).ToArray();
			var png = new Png(new byte[10 * 20], SpriteFrameType.Indexed8, 10, 20, colors);

			// Act
			var result = png.Save();

			// Assert
			// Byte at index 25 contains color type information
			// 0x03 represents Indexed8 with a palette
			var colorTypeByte = result[25];
			Assert.That(colorTypeByte, Is.EqualTo(0x03));
		}

		[Test]
		public void Save_Method_Should_Write_Rgba32_If_Alpha_Channel_Required()
		{
			// Arrange
			var png = new Png(new byte[10 * 20 * 4], SpriteFrameType.Rgba32, 10, 20);

			// Act
			var result = png.Save();

			// Assert
			// Byte at index 25 contains color type information
			// 0x06 represents RGBA32 with alpha
			var colorTypeByte = result[25];
			Assert.That(colorTypeByte, Is.EqualTo(0x06));
		}

		[Test]
		public void Save_ShouldThrowException_WhenDataLenghtNotEqualExpectedLenght()
		{
			// Arrange
			var colors = Enumerable.Range(0, 256).Select(i => Color.FromArgb(i, i, i)).ToArray();

			// Act
			void TestDelegate() => new Png(new byte[10 * 20], SpriteFrameType.Indexed8, 100, 20, colors);

			// Assert
			var ex = Assert.Throws<InvalidDataException>(TestDelegate);
			Assert.That(ex.Message, Is.EqualTo("Input data does not match expected length"));
		}

		[Test]
		public void PngConstructor_InvalidSignature_ThrowsInvalidDataException()
		{
			// Arrange
			byte[] invalidSignature = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07];

			// Act & Assert
			var exception = Assert.Throws<InvalidDataException>(() => new Png(new MemoryStream(invalidSignature)));
			Assert.That("PNG Signature is bogus", Does.Contain(exception.Message));
		}

		[Test]
		public void PngConstructor_HeaderNotFirst_ThrowsInvalidDataException()
		{
			// Arrange
			var invalidPngData = new byte[]
			{
				0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a,
				0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4e, 0x44,
				0x00, 0x00, 0x00, 0x00,
			};

			// Act & Assert
			var exception = Assert.Throws<InvalidDataException>(() => new Png(new MemoryStream(invalidPngData)));
			Assert.That("Invalid PNG file - header does not appear first.", Does.Contain(exception.Message));
		}

		[Test]
		public void PngConstructor_DuplicateIhdrHeader_ThrowsInvalidDataException()
		{
			// Arrange
			var invalidPngData = new byte[]
			{
				0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a,
				0x00, 0x00, 0x00, 0x0d, 0x49, 0x48, 0x44, 0x52,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x00,
			};

			using (var stream = new MemoryStream(invalidPngData))
			{
				// Act & Assert
				var exception = Assert.Throws<EndOfStreamException>(() => new Png(stream));
			}
		}

		public void PngConstructor_CompressionMethodNotSupported_ThrowsInvalidDataException()
		{
			// Arrange
			var invalidPngData = new byte[]
			{
				0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a,
				0x00, 0x00, 0x00, 0x0d, 0x49, 0x48, 0x44, 0x52,
				0x00, 0x00, 0x00, 0x00, 0x08, 0x02, 0x00, 0x00,
				0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x00,
			};

			using (var stream = new MemoryStream(invalidPngData))
			{
				// Act & Assert
				var exception = Assert.Throws<InvalidDataException>(() => new Png(stream));
				Assert.That("Compression method not supported", Is.EqualTo(exception.Message));
			}
		}

		[Test]
		public void Constructor_ThrowsExceptionForNullPaletteIfTypeIsIndexed8()
		{
			// Arrange
			const int Width = 100;
			const int Height = 100;
			const SpriteFrameType Type = SpriteFrameType.Indexed8;

			// Act and Assert
			Assert.Throws<InvalidDataException>(() => new Png(new byte[Width * Height], Type, Width, Height, null));
		}
	}
}
