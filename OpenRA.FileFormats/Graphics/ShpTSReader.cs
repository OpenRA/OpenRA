#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace OpenRA.FileFormats
{

	public struct Header
	{
		public ushort A;
		// Unknown
		// Width and Height of the images
		public ushort Width;
		public ushort Height;
		public ushort NumImages;
	}

	public class HeaderImage
	{
		public ushort x;
		public ushort y;
		public ushort cx;
		public ushort cy;
		// cx and cy are width n height of stored image
		public byte compression;
		public byte[] align;
		public byte[] transparent;
		public int zero;
		public int offset;
		public byte[] Image;
	}

	public struct SHPData
	{
		public HeaderImage HeaderImage;
		public byte[] Databuffer;
		public byte[] FrameImage;
	}

	public struct SHP
	{
		public Header Header;
		public SHPData[] Data;
	}


	public class ShpTSReader : IEnumerable<HeaderImage>
	{
		public readonly int ImageCount;
		public readonly ushort Width;
		public readonly ushort Height;
		public readonly ushort Width2;
		public readonly ushort Height2;
		public int arroff = 0;
		public int erri = 0;
		public int errj = 0;
		public int errk = 0;
		public int errl = 0;

		public static int FindNextOffsetFrom(SHP SHP, int Init, int Last)
		{
			int result;
			result = 0;
			Last++;
			while ((result == 0) && (Init < Last))
			{
				result = SHP.Data[Init].HeaderImage.offset;
				Init++;
			}
			return result;
		}

		private readonly List<HeaderImage> headers = new List<HeaderImage>();

		public ShpTSReader(Stream stream)
		{

			SHP SHP = new SHP();
				int FileSize;
				int x;
				int k = 0;
				int l = 0;

				int ImageSize;
				int NextOffset;

				byte[] FData;
				byte cp;
				byte[] Databuffer;

				BinaryReader sReader = new BinaryReader(stream);
				FileSize = (int)sReader.BaseStream.Length;
				// Get Header
				SHP.Header.A = sReader.ReadUInt16();
				SHP.Header.Width = sReader.ReadUInt16();
				SHP.Header.Height = sReader.ReadUInt16();
				SHP.Header.NumImages = sReader.ReadUInt16();

				SHP.Data = new SHPData[SHP.Header.NumImages + 1];

				ImageCount = SHP.Header.NumImages;
				
				for (x = 1; x <= SHP.Header.NumImages; x++)
				{
					SHP.Data[x].HeaderImage = new HeaderImage();
 
					SHP.Data[x].HeaderImage.x = sReader.ReadUInt16();
					SHP.Data[x].HeaderImage.y = sReader.ReadUInt16();
					SHP.Data[x].HeaderImage.cx = sReader.ReadUInt16();
					SHP.Data[x].HeaderImage.cy = sReader.ReadUInt16();

					SHP.Data[x].HeaderImage.compression = sReader.ReadByte();
					SHP.Data[x].HeaderImage.align = sReader.ReadBytes(3);
					sReader.ReadInt32();
					SHP.Data[x].HeaderImage.zero = sReader.ReadByte();
					SHP.Data[x].HeaderImage.transparent = sReader.ReadBytes(3);

					SHP.Data[x].HeaderImage.offset = sReader.ReadInt32();

				}

				Width = SHP.Header.Width;
				Height = SHP.Header.Height;

				for (int i = 0; i < ImageCount; i++)
				{
					headers.Add(SHP.Data[i+1].HeaderImage);
				}

				// Read and decode each image from the file
				for (x = 1; x <= SHP.Header.NumImages; x++)
				{
					headers[x - 1].Image = new byte[(Width * Height)];
					for (int i = 0; i < headers[x - 1].Image.Length; i++)
						headers[x - 1].Image[i] = 0;

					FData = new byte[(Width * Height)];

					// Does it really reads the frame?
					if (SHP.Data[x].HeaderImage.offset != 0)
					{
						try
						{
						// Now it checks the compression:
						if ((SHP.Data[x].HeaderImage.compression == 3))
						{
							// decode it
							// Compression 3
							NextOffset = FindNextOffsetFrom(SHP, x + 1, SHP.Header.NumImages);
							if (NextOffset != 0)
							{
								
								ImageSize = NextOffset - SHP.Data[x].HeaderImage.offset;
								Databuffer = new byte[ImageSize];
								for (int i = 0; i < ImageSize; i++)
								{
									sReader.BaseStream.Position = SHP.Data[x].HeaderImage.offset + i;
									Databuffer[i] = sReader.ReadByte();
								}
								SHP.Data[x].Databuffer = new byte[(SHP.Data[x].HeaderImage.cx * SHP.Data[x].HeaderImage.cy)];
								Decode3(Databuffer, ref SHP.Data[x].Databuffer, SHP.Data[x].HeaderImage.cx, SHP.Data[x].HeaderImage.cy, ref FileSize);

								k = 0;
								l = 0;
								for (int i = 0; i < Height; i++)
								{
									erri = i;
									for (int j = SHP.Data[x].HeaderImage.x; j < Width; j++)
									{
										errj = j;
										errl = l;
										errk = k;
										arroff = i + j + l;

										if (((j + 1) > (SHP.Data[x].HeaderImage.cx + SHP.Data[x].HeaderImage.x)) || ((i + 1) > (SHP.Data[x].HeaderImage.cy)))
											cp = 0;
										else
											cp = SHP.Data[x].Databuffer[i + (j - SHP.Data[x].HeaderImage.x) + l];

										FData[i + j + k] = cp;

										if (j == (SHP.Data[x].HeaderImage.cx + SHP.Data[x].HeaderImage.x - 1))
											l = l + (SHP.Data[x].HeaderImage.cx - 1);

										if (j == (Width - 1))
											k = k + (Width - 1);
									} 
								}
								//FData = headers[x - 1].Image;
								k = 0;
								for (int i = 0; i < (Height - SHP.Data[x].HeaderImage.y); i++)
								{
									for (int j = 0; j < Width; j++)
									{
										headers[x - 1].Image[i + j + k + (Width * SHP.Data[x].HeaderImage.y)] = FData[i + j + k];
										if (j == (Width - 1))
										{
											k = k + (Width - 1);
										}
									}
								}

							}
							else
							{
								
								ImageSize = 0;
								ImageSize = FileSize - SHP.Data[x].HeaderImage.offset;
								Databuffer = new byte[ImageSize];
								for (int i = 0; i < ImageSize; i++)
								{
									sReader.BaseStream.Position = SHP.Data[x].HeaderImage.offset + i;
									Databuffer[i] = sReader.ReadByte();
								}
								SHP.Data[x].Databuffer = new byte[((SHP.Data[x].HeaderImage.cx * SHP.Data[x].HeaderImage.cy))];
								
								Decode3(Databuffer, ref SHP.Data[x].Databuffer, SHP.Data[x].HeaderImage.cx, SHP.Data[x].HeaderImage.cy, ref ImageSize);

								k = 0;
								l = 0;
								for (int i = 0; i < Height; i++)
								{
									erri = i;
									for (int j = SHP.Data[x].HeaderImage.x; j < Width; j++)
									{
										errj = j;
										errl = l;
										errk = k;
										arroff = i + j + l;

										if (((j + 1) > (SHP.Data[x].HeaderImage.cx + SHP.Data[x].HeaderImage.x)) || ((i + 1) > (SHP.Data[x].HeaderImage.cy)))
											cp = 0;
										else
											cp = SHP.Data[x].Databuffer[i + (j - SHP.Data[x].HeaderImage.x) + l];

										FData[i + j + k] = cp;

										if (j == (SHP.Data[x].HeaderImage.cx + SHP.Data[x].HeaderImage.x - 1))
											l = l + (SHP.Data[x].HeaderImage.cx - 1);

										if (j == (Width - 1))
											k = k + (Width - 1);
									}
								}
								//FData = headers[x - 1].Image;
								k = 0;
								for (int i = 0; i < (Height - SHP.Data[x].HeaderImage.y); i++)
								{
									for (int j = 0; j < Width; j++)
									{
										headers[x - 1].Image[i + j + k + (Width * SHP.Data[x].HeaderImage.y)] = FData[i + j + k];
										if (j == (Width - 1))
										{
											k = k + (Width - 1);
										}
									}
								}
							}
						}
						else if ((SHP.Data[x].HeaderImage.compression == 2))
						{
							NextOffset = FindNextOffsetFrom(SHP, x + 1, SHP.Header.NumImages);
							if (NextOffset != 0)
							{
								ImageSize = NextOffset - SHP.Data[x].HeaderImage.offset;
								SHP.Data[x].Databuffer = new byte[(SHP.Data[x].HeaderImage.cx * SHP.Data[x].HeaderImage.cy)];
								Databuffer = new byte[ImageSize];
								for (int i = 0; i < ImageSize; i++)
								{
									sReader.BaseStream.Position = SHP.Data[x].HeaderImage.offset + i;
									Databuffer[i] = sReader.ReadByte();
								}

								Decode2(Databuffer, ref SHP.Data[x].Databuffer, SHP.Data[x].HeaderImage.cx, SHP.Data[x].HeaderImage.cy, ref ImageSize);

								k = 0;
								l = 0;
								for (int i = 0; i < Height; i++)
								{
									erri = i;
									for (int j = SHP.Data[x].HeaderImage.x; j < Width; j++)
									{
										errj = j;
										errl = l;
										errk = k;
										arroff = i + j + l;

										if (((j + 1) > (SHP.Data[x].HeaderImage.cx + SHP.Data[x].HeaderImage.x)) || ((i + 1) > (SHP.Data[x].HeaderImage.cy)))
											cp = 0;
										else
											cp = SHP.Data[x].Databuffer[i + (j - SHP.Data[x].HeaderImage.x) + l];

										FData[i + j + k] = cp;

										if (j == (SHP.Data[x].HeaderImage.cx + SHP.Data[x].HeaderImage.x - 1))
											l = l + (SHP.Data[x].HeaderImage.cx - 1);

										if (j == (Width - 1))
											k = k + (Width - 1);
									}
								}
								//FData = headers[x - 1].Image;
								k = 0;
								for (int i = 0; i < (Height - SHP.Data[x].HeaderImage.y); i++)
								{
									for (int j = 0; j < Width; j++)
									{
										headers[x - 1].Image[i + j + k + (Width * SHP.Data[x].HeaderImage.y)] = FData[i + j + k];
										if (j == (Width - 1))
										{
											k = k + (Width - 1);
										}
									}
								}

								// Compression 2
							}
							else
							{
								ImageSize = 0;
								ImageSize = FileSize - SHP.Data[x].HeaderImage.offset;
								Databuffer = new byte[ImageSize];
								for (int i = 0; i < ImageSize; i++)
								{
									sReader.BaseStream.Position = SHP.Data[x].HeaderImage.offset + i;
									Databuffer[i] = sReader.ReadByte();
								}
								SHP.Data[x].Databuffer = new byte[((SHP.Data[x].HeaderImage.cx * SHP.Data[x].HeaderImage.cy))];
								Decode2(Databuffer, ref SHP.Data[x].Databuffer, SHP.Data[x].HeaderImage.cx, SHP.Data[x].HeaderImage.cy, ref ImageSize);

								k = 0;
								l = 0;
								for (int i = 0; i < Height; i++)
								{
									erri = i;
									for (int j = SHP.Data[x].HeaderImage.x; j < Width; j++)
									{
										errj = j;
										errl = l;
										errk = k;
										arroff = i + j + l;

										if (((j + 1) > (SHP.Data[x].HeaderImage.cx + SHP.Data[x].HeaderImage.x)) || ((i + 1) > (SHP.Data[x].HeaderImage.cy)))
											cp = 0;
										else
											cp = SHP.Data[x].Databuffer[i + (j - SHP.Data[x].HeaderImage.x) + l];

										FData[i + j + k] = cp;

										if (j == (SHP.Data[x].HeaderImage.cx + SHP.Data[x].HeaderImage.x - 1))
											l = l + (SHP.Data[x].HeaderImage.cx - 1);

										if (j == (Width - 1))
											k = k + (Width - 1);
									}
								}
								//FData = headers[x - 1].Image;
								k = 0;
								for (int i = 0; i < (Height - SHP.Data[x].HeaderImage.y); i++)
								{
									for (int j = 0; j < Width; j++)
									{
										headers[x - 1].Image[i + j + k + (Width * SHP.Data[x].HeaderImage.y)] = FData[i + j + k];
										if (j == (Width - 1))
										{
											k = k + (Width - 1);
										}
									}
								}
								// Compression 2
							}
						}
						else
						{
							// Compression 1
							ImageSize = (int)(SHP.Data[x].HeaderImage.cx * SHP.Data[x].HeaderImage.cy);
							Databuffer = new byte[ImageSize];
							for (int i = 0; i < ImageSize; i++)
							{
								sReader.BaseStream.Position = SHP.Data[x].HeaderImage.offset + i;
								Databuffer[i] = sReader.ReadByte();
							}
							SHP.Data[x].Databuffer = new byte[(SHP.Data[x].HeaderImage.cx * SHP.Data[x].HeaderImage.cy)];
							SHP.Data[x].Databuffer = Databuffer;

							k = 0;
							l = 0;
							for (int i = 0; i < Height; i++)
							{
								erri = i;
								for (int j = SHP.Data[x].HeaderImage.x; j < Width; j++)
								{
									errj = j;
									errl = l;
									errk = k;
									arroff = i + j + l;

									if (((j + 1) > (SHP.Data[x].HeaderImage.cx + SHP.Data[x].HeaderImage.x)) || ((i + 1) > (SHP.Data[x].HeaderImage.cy)))
										cp = 0;
									else
										cp = SHP.Data[x].Databuffer[i + (j - SHP.Data[x].HeaderImage.x) + l];

									FData[i + j + k] = cp;

									if (j == (SHP.Data[x].HeaderImage.cx + SHP.Data[x].HeaderImage.x - 1))
										l = l + (SHP.Data[x].HeaderImage.cx - 1);

									if (j == (Width - 1))
										k = k + (Width - 1);
								}
							}
							//FData = headers[x - 1].Image;
							k = 0;
							for (int i = 0; i < (Height - SHP.Data[x].HeaderImage.y); i++)
							{
								for (int j = 0; j < Width; j++)
								{
									headers[x - 1].Image[i + j + k + (Width * SHP.Data[x].HeaderImage.y)] = FData[i + j + k];
									if (j == (Width - 1))
									{
										k = k + (Width - 1);
									}
								}
							}
						}
					}
					catch (Exception e)
					{
						Console.WriteLine(e.Message);
					}
				}
				// Set the shp's databuffer to the result after decompression
			}
			//Width = Width2;
			//Height = Height2;
		}

		public HeaderImage this[int index]
		{
			get { return headers[index]; }
		}
		
		public static void ReInterpretWordFromBytes(byte Byte1, byte Byte2, ref ushort FullValue)
		{
			FullValue = (ushort)((Byte2 * 256) + Byte1);
		}

		public static void ReInterpretWordFromBytes(byte Byte1, byte Byte2, ref uint FullValue)
		{
			FullValue = (uint)((Byte2 * 256) + Byte1);
		}

		// Compression 3:
		public static void Decode3(byte[] Source, ref byte[] Dest, int cx, int cy, ref int max)
		{
			int SP;
			int DP;
			int x;
			int y;
			int Count;
			int v;
			int maxdp;
			ushort Pos;
			maxdp = cx * cy;
			SP = 0;
			DP = 0;
			Pos = 0;
			try
			{
				for (y = 1; y <= cy; y++)
				{
					ReInterpretWordFromBytes(Source[SP], Source[SP + 1], ref Pos);

					Count = Pos - 2;

					SP = SP + 2;

					x = 0;
					while (Count > 0)
					{
						Count = Count - 1;
						if ((SP > max) || (DP > maxdp))
						{
							break;
						}
						else
						{
							// SP has reached max value, exit
							v = Source[SP];
							SP++;
							if (v != 0)
							{
								if ((SP > max) || (DP > maxdp))
								{
									break;
								}
								else
								{
									x++;
									Dest[DP] += (byte)v;
								}
								DP++;
							}
							else
							{
								Count -= 1;
								v = Source[SP];

								SP++;
								if ((x + v) > cx)
								{
									v = cx - x;
								}
								x = x + v;
								while (v > 0)
								{
									if ((SP > max) || (DP > maxdp))
									{
										break;
									}
									else
									{
										v -= 1;
										Dest[DP] = 0;

									}
									DP++;
									// SP has reached max value, exit
								}
							}
						}
					}
					if ((SP >= max) || (DP >= maxdp))
					{
						return;
					}
					// SP has reached max value, exit
				}
			}

			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}

		}

		public static void Decode2(byte[] Source, ref byte[] Dest, int cx, int cy, ref int max)
		{
			int SP;
			int DP;
			int y;
			int Count;
			int maxdp;
			ushort Pos;
			maxdp = cx * cy;
			SP = 0;
			DP = 0;
			Pos = 0;
			try
			{
				for (y = 1; y <= cy; y++)
				{
					ReInterpretWordFromBytes(Source[SP], Source[SP + 1], ref Pos);
					Count = Pos - 2;
					SP += 2;
					while (Count > 0)
					{
						Count -= 1;
						if ((SP > max) || (DP > maxdp))
						{
							return;
						}
						// SP has reached max value, exit
						Dest[DP] = Source[SP];
						SP++;
						DP++;
					}
					if ((SP >= max) || (DP >= maxdp))
					{
						return;
					}
					// SP has reached max value, exit
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		public IEnumerator<HeaderImage> GetEnumerator()
		{
			return headers.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public Size Size { get { return new Size(Width, Height); } }
	}
}
