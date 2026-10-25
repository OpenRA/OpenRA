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

using NUnit.Framework;
using OpenRA.Platforms.Default;
using OpenRA.Primitives;

namespace OpenRA.Test
{
	[TestFixture(GLProfile.Modern)]
	[TestFixture(GLProfile.Embedded)]
	[Explicit("Requires SDL and an OpenGL display.")]
	[NonParallelizable]
	sealed class TextureReadbackTest(GLProfile profile)
	{
		IPlatformWindow window;
		Settings previousSettings;

		[SetUp]
		public void SetUp()
		{
			previousSettings = Game.Settings;
			Game.Settings = new Settings(null, new Arguments([]));
			Game.Settings.Graphics.VSync = false;
			window = new DefaultPlatform().CreateWindow(new Size(64, 64), WindowMode.Windowed, 1, 1024, 1024, 0, profile);
			if (window.GLProfile != profile)
				Assert.Ignore($"Requested {profile} profile is not available.");

			TestContext.WriteLine($"Profile: {window.GLProfile}; version: {window.Context.GLVersion}; context: {window.Context.GetType().Name}");
		}

		[TearDown]
		public void TearDown()
		{
			try
			{
				window?.Dispose();
			}
			finally
			{
				Game.Settings = previousSettings;
			}
		}

		static void AssertPixels(ITexture texture, Size expectedSize, byte blue, byte green, byte red)
		{
			// Check the size before native readback to avoid an undersized destination buffer.
			Assert.That(texture.Size, Is.EqualTo(expectedSize));
			var data = texture.GetData();
			Assert.That(data, Has.Length.EqualTo(4 * expectedSize.Width * expectedSize.Height));
			for (var i = 0; i < data.Length; i += 4)
				Assert.That(new[] { data[i], data[i + 1], data[i + 2], data[i + 3] }, Is.EqualTo(new byte[] { blue, green, red, 255 }));
		}

		[Test]
		public void UploadedTextureReadsPixels()
		{
			using var texture = window.Context.CreateTexture();
			texture.SetData([0, 0, 255, 255], 1, 1);
			AssertPixels(texture, new Size(1, 1), 0, 0, 255);
		}

		[Test]
		public void FirstCopyReportsSizeAndReadsPixels()
		{
			using var frame = window.Context.CreateFrameBuffer(new Size(16, 16), Color.FromArgb(255, 0, 0));
			using var texture = window.Context.CreateTexture();
			frame.Bind();
			texture.SetDataFromReadBuffer(new Rectangle(2, 3, 8, 4));
			AssertPixels(texture, new Size(8, 4), 0, 0, 255);
			frame.Unbind();
		}

		[Test]
		public void CopyReplacesPreviousUploadSize()
		{
			using var frame = window.Context.CreateFrameBuffer(new Size(16, 16), Color.FromArgb(255, 0, 0));
			using var texture = window.Context.CreateTexture();
			texture.SetData(new byte[4], 1, 1);
			frame.Bind();
			texture.SetDataFromReadBuffer(new Rectangle(0, 0, 8, 4));
			AssertPixels(texture, new Size(8, 4), 0, 0, 255);
			frame.Unbind();
		}

		[Test]
		public void RepeatedCopiesFollowChangingDimensions()
		{
			using var frame = window.Context.CreateFrameBuffer(new Size(32, 32), Color.FromArgb(255, 0, 0));
			using var texture = window.Context.CreateTexture();
			frame.Bind();
			foreach (var size in new[] { new Size(16, 8), new Size(4, 4), new Size(8, 16) })
			{
				texture.SetDataFromReadBuffer(new Rectangle(int2.Zero, size));
				AssertPixels(texture, size, 0, 0, 255);
			}

			frame.Unbind();
		}

		[Test]
		public void RepeatedCopyReadsTheCurrentFramebuffer()
		{
			using var red = window.Context.CreateFrameBuffer(new Size(8, 8), Color.FromArgb(255, 0, 0));
			using var blue = window.Context.CreateFrameBuffer(new Size(8, 8), Color.FromArgb(0, 0, 255));
			using var texture = window.Context.CreateTexture();
			red.Bind();
			texture.SetDataFromReadBuffer(new Rectangle(0, 0, 8, 8));
			AssertPixels(texture, new Size(8, 8), 0, 0, 255);
			red.Unbind();
			blue.Bind();
			texture.SetDataFromReadBuffer(new Rectangle(0, 0, 8, 8));
			AssertPixels(texture, new Size(8, 8), 255, 0, 0);
			blue.Unbind();
		}
	}
}
