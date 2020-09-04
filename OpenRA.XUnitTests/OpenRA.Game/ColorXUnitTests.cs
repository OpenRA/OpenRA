using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
using OpenRA.Primitives;
using Xunit;

namespace OpenRA.XUnitTests.OpenRA.Game
{
	public class ColorXUnitTests
	{
		[Fact]
		public void CanGetMaxBrightnessFromWhite()
		{
			Color color = Color.FromArgb(255, 255, 255);

			var returnFromMethod = color.GetBrightness();

			Assert.Equal(1, returnFromMethod);
		}

		[Fact]
		public void CanGetZeroBrightnessFromBlack()
		{
			Color color = Color.FromArgb(0, 0, 0);

			var returnFromMethod = color.GetBrightness();

			Assert.Equal(0, returnFromMethod);
		}

		[Fact]
		public void CanGetMidBrightnessFromChocolateColor()
		{
			Color color = Color.FromArgb(215, 105, 40);

			var returnFromMethod = color.GetBrightness();

			Assert.Equal(.5, returnFromMethod);
		}

		[Fact]
		public void CanGetColorByString()
		{
			Color color;

			Color.TryParse("F0F8FF", out color);

			var expected = Color.FromArgb(0xFFF0F8FF);

			Assert.Equal(expected, color);
		}
	}
}
