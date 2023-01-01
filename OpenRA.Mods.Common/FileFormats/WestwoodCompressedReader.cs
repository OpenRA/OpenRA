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

namespace OpenRA.Mods.Common.FileFormats
{
	public class WestwoodCompressedReader
	{
		static readonly int[] AudWsStepTable2 = { -2, -1, 0, 1 };
		static readonly int[] AudWsStepTable4 = { -9, -8, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 8 };

		public static void DecodeWestwoodCompressedSample(byte[] input, byte[] output)
		{
			if (input.Length == output.Length)
			{
				Array.Copy(input, output, output.Length);

				return;
			}

			var sample = 0x80;
			var r = 0;
			var w = 0;

			while (r < input.Length)
			{
				var count = input[r++] & 0x3f;

				switch (input[r - 1] >> 6)
				{
					case 0:
						for (count++; count > 0; count--)
						{
							var code = input[r++];
							output[w++] = (byte)(sample = (sample + AudWsStepTable2[(code >> 0) & 0x03]).Clamp(byte.MinValue, byte.MaxValue));
							output[w++] = (byte)(sample = (sample + AudWsStepTable2[(code >> 2) & 0x03]).Clamp(byte.MinValue, byte.MaxValue));
							output[w++] = (byte)(sample = (sample + AudWsStepTable2[(code >> 4) & 0x03]).Clamp(byte.MinValue, byte.MaxValue));
							output[w++] = (byte)(sample = (sample + AudWsStepTable2[(code >> 6) & 0x03]).Clamp(byte.MinValue, byte.MaxValue));
						}

						break;

					case 1:
						for (count++; count > 0; count--)
						{
							var code = input[r++];
							output[w++] = (byte)(sample = (sample + AudWsStepTable4[(code >> 0) & 0x0f]).Clamp(byte.MinValue, byte.MaxValue));
							output[w++] = (byte)(sample = (sample + AudWsStepTable4[(code >> 4) & 0xff]).Clamp(byte.MinValue, byte.MaxValue));
						}

						break;

					case 2 when (count & 0x20) != 0:
						output[w++] = (byte)(sample += (sbyte)((sbyte)count << 3) >> 3);

						break;

					case 2:
						for (count++; count > 0; count--)
							output[w++] = input[r++];

						sample = input[r - 1];

						break;

					default:
						for (count++; count > 0; count--)
							output[w++] = (byte)sample;

						break;
				}
			}
		}
	}
}
