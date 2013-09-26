#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Text;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Diagnostics;
using DBus;

[Interface("org.gnome15.Service")]
interface IService 
{
	string[] GetDevices();
}

[Interface("org.gnome15.Screen")]
interface IScreen 
{
	string CreatePage(string page_id, string title, uint priority);
}

[Interface("org.gnome15.Page")]
interface IPage 
{
	void Delete();
	void Image (string path, double x, double y, double width, double height);
	void NewSurface();
	void DrawSurface();
	void Redraw();
}

namespace OpenRA
{
	public class GSeries
	{
		public static int wincon = DMcLgLCD.LGLCD_INVALID_CONNECTION;
		public static int device = DMcLgLCD.LGLCD_INVALID_DEVICE;
		public static int deviceType = DMcLgLCD.LGLCD_INVALID_DEVICE;

		public static Bitmap frame;

		public static string[] mod;

		const string BUS_NAME = "org.gnome15.Gnome15";
		const string SERVICE_PATH = "/org/gnome15/Service";

		static DBus.Bus lincon;
		static IScreen screen;
		static IPage page;
		static IService service;

		public static bool unsupported_detected = false;

		public static bool ingame = false;
		public static bool lowpower = false;
		public static bool splashdrawn = false;

		public static void Initialize()
		{
				if (Platform.CurrentPlatform == PlatformType.Windows)
				{
					try
					{
						if (DMcLgLCD.ERROR_SUCCESS == DMcLgLCD.LcdInit())
						{
							wincon = DMcLgLCD.LcdConnectEx("OpenRA", 0, 0);

							if (DMcLgLCD.LGLCD_INVALID_DEVICE == device)
							{
								device = DMcLgLCD.LcdOpenByType(wincon, DMcLgLCD.LGLCD_DEVICE_BW);
								if (DMcLgLCD.LGLCD_INVALID_DEVICE != device)
								{
									deviceType = DMcLgLCD.LGLCD_DEVICE_BW;
								}
							}
						}
						Console.WriteLine("GamePanel LCD support for Windows has been initialized");
						Game.Settings.Game.EnableGSeriesLCD = true;
						Splash();
					}
					catch
					{
						Game.Settings.Game.EnableGSeriesLCD = false;
					}
				}
				else if (Platform.CurrentPlatform == PlatformType.Linux)
				{
					try
					{
						lincon = DBus.Bus.Session;

						service = lincon.GetObject<IService>(BUS_NAME, new ObjectPath(SERVICE_PATH));
						string[] condev = service.GetDevices();
						string SCREEN_PATH = condev[0].Replace("Device", "Screen");
						string[] unsupported = { "g11", "g19", "g110", "g930", "g35", "mx5500" };

						foreach (string x in unsupported)
						{
							if (x.Contains(SCREEN_PATH))
							{
								unsupported_detected = true;
							}
						}
						if (!unsupported_detected)
						{
							screen = lincon.GetObject<IScreen>(BUS_NAME, new ObjectPath(SCREEN_PATH));
							string page_path = screen.CreatePage("openra", "OpenRA", 100);
							page = lincon.GetObject<IPage>(BUS_NAME, new ObjectPath(page_path));
							Console.WriteLine("GamePanel LCD support for Linux has been initialized");
							Game.Settings.Game.EnableGSeriesLCD = true;
							Splash();
						}
					}
					catch
					{
						Game.Settings.Game.EnableGSeriesLCD = false;
					}
				}
			}

		public static void ModInit()
		{
			GSeries.mod = Game.Settings.Game.Mods;
			GSeries.splashdrawn = false;
		}

		public static void Shutdown()
		{
			if (Platform.CurrentPlatform == PlatformType.Windows)
			{
				frame.Dispose();
				DMcLgLCD.LcdClose(device);
				DMcLgLCD.LcdDisconnect(wincon);
				DMcLgLCD.LcdDeInit();
			}
			else if (Platform.CurrentPlatform == PlatformType.Linux)
			{
				page.Delete();
				lincon.Close();
			}
		}

		public static void Loading()
		{
			if (Game.Settings.Game.EnableGSeriesLCD)
			{
					frame = new Bitmap("gseries/loading.bmp");
					if (Platform.CurrentPlatform == PlatformType.Windows)
					{
						DMcLgLCD.LcdUpdateBitmap(device, frame.GetHbitmap(), DMcLgLCD.LGLCD_DEVICE_BW);
						DMcLgLCD.LcdSetAsLCDForegroundApp(device, DMcLgLCD.LGLCD_FORE_YES);
					}
					else if (Platform.CurrentPlatform == PlatformType.Linux)
					{
						frame.Save("/dev/shm/ora_lcdfrm", System.Drawing.Imaging.ImageFormat.MemoryBmp);
						page.NewSurface();
						page.Image("/dev/shm/ora_lcdfrm", 0, 0, 160, 43);
						page.DrawSurface();
						page.Redraw();
					}
					frame.Dispose();
			}
		}

		public static void LowPower()
		{
			if (Game.Settings.Game.EnableGSeriesLCD)
			{
				lowpower = true;
				frame = new Bitmap("gseries/lowpower.bmp");
				if (Platform.CurrentPlatform == PlatformType.Windows)
				{
					DMcLgLCD.LcdUpdateBitmap(device, frame.GetHbitmap(), DMcLgLCD.LGLCD_DEVICE_BW);
					DMcLgLCD.LcdSetAsLCDForegroundApp(device, DMcLgLCD.LGLCD_FORE_YES);
				}
				else if (Platform.CurrentPlatform == PlatformType.Linux)
				{
					frame.Save("/dev/shm/ora_lcdfrm", System.Drawing.Imaging.ImageFormat.MemoryBmp);
					page.NewSurface();
					page.Image("/dev/shm/ora_lcdfrm", 0, 0, 160, 43);
					page.DrawSurface();
					page.Redraw();
				}
				frame.Dispose();
			}
		}

		public static void Splash()
		{
			if (!splashdrawn)
			{
				try
				{
					var random = new Random();
					string[] splashes15 = Directory.GetFiles(@"mods/" + mod[0] + "/gseries/splashes/15/", "*.bmp");
					int index = random.Next(splashes15.Length);
					frame = new Bitmap(splashes15[index]);
					if (Platform.CurrentPlatform == PlatformType.Windows)
					{
						DMcLgLCD.LcdUpdateBitmap(device, frame.GetHbitmap(), DMcLgLCD.LGLCD_DEVICE_BW);
						DMcLgLCD.LcdSetAsLCDForegroundApp(device, DMcLgLCD.LGLCD_FORE_YES);
					}
					else if (Platform.CurrentPlatform == PlatformType.Linux)
					{
						frame.Save("/dev/shm/ora_lcdfrm", System.Drawing.Imaging.ImageFormat.MemoryBmp);
						page.NewSurface();
						page.Image("/dev/shm/ora_lcdfrm", 0, 0, 160, 43);
						page.DrawSurface();
						page.Redraw();
					}
					frame.Dispose();
				}
				catch
				{
					frame = new Bitmap("gseries/splashes/15.bmp");
					if (Platform.CurrentPlatform == PlatformType.Windows)
					{
						DMcLgLCD.LcdUpdateBitmap(device, frame.GetHbitmap(), DMcLgLCD.LGLCD_DEVICE_BW);
						DMcLgLCD.LcdSetAsLCDForegroundApp(device, DMcLgLCD.LGLCD_FORE_YES);
					}
					else if (Platform.CurrentPlatform == PlatformType.Linux)
					{
						frame.Save("/dev/shm/ora_lcdfrm", System.Drawing.Imaging.ImageFormat.MemoryBmp);
						page.NewSurface();
						page.Image("/dev/shm/ora_lcdfrm", 0, 0, 160, 43);
						page.DrawSurface();
						page.Redraw();
					}
					frame.Dispose();
				}
				splashdrawn = true;
			}
		}

		public static void Tick()
		{
			if (Game.Settings.Game.EnableGSeriesLCD)
			{
				try
				{
					if (Game.LocalTick % 25 == 1) //1500 = 60sec, 25 = 1sec
						if (lowpower)
						{
							LowPower();
						}
						else if (ingame)
						{
							Default();
						}
						else
						{
							Splash();
						}
				}
				catch
				{
				}
			}
		}

		public static void InGameInitialize()
		{
			if (Game.Settings.Game.EnableGSeriesLCD)
			{
				if (!ingame)
				{
					ingame = true;
				}
				else
				{
					ingame = false;
					splashdrawn = false;
				}
			}
		}

		public static void Default()
		{
			lowpower = false;
			try
			{
				Font tFont = new Font("Arial Bold", 14);
				DateTime curtime = DateTime.Now;
				frame = new Bitmap(160, 43);
				System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(frame);
				Image eva = Image.FromFile("mods/" + mod[0] + "/gseries/ui/15/time.bmp");
				g.DrawImage(eva, 0, 0);
				Rectangle rect = new Rectangle(81, 0, 80, 42);
				StringFormat format = new StringFormat();
				format.Alignment = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Center;
				g.DrawString(curtime.ToString("HH:mm"), tFont, Brushes.Black, rect, format);
				if (Platform.CurrentPlatform == PlatformType.Windows)
				{
					DMcLgLCD.LcdUpdateBitmap(device, frame.GetHbitmap(), DMcLgLCD.LGLCD_DEVICE_BW);
					DMcLgLCD.LcdSetAsLCDForegroundApp(device, DMcLgLCD.LGLCD_FORE_YES);
				}
				else if (Platform.CurrentPlatform == PlatformType.Linux)
				{
					frame.Save("/dev/shm/ora_lcdfrm", System.Drawing.Imaging.ImageFormat.MemoryBmp);
					page.NewSurface();
					page.Image("/dev/shm/ora_lcdfrm", 0, 0, 160, 43);
					page.DrawSurface();
					page.Redraw();
				}
			}
			catch
			{
				Font tFont = new Font("Arial Bold", 24);
				DateTime curtime = DateTime.Now;
				frame = new Bitmap(160, 43);
				System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(frame);
				Rectangle rect = new Rectangle(0, 0, 160, 43);
				StringFormat format = new StringFormat();
				format.Alignment = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Center;
				g.DrawString(curtime.ToString("HH:mm"), tFont, Brushes.Black, rect, format);
				if (Platform.CurrentPlatform == PlatformType.Windows)
				{
					DMcLgLCD.LcdUpdateBitmap(device, frame.GetHbitmap(), DMcLgLCD.LGLCD_DEVICE_BW);
					DMcLgLCD.LcdSetAsLCDForegroundApp(device, DMcLgLCD.LGLCD_FORE_YES);
				}
				else if (Platform.CurrentPlatform == PlatformType.Linux)
				{
					frame.Save("/dev/shm/ora_lcdfrm", System.Drawing.Imaging.ImageFormat.MemoryBmp);
					page.NewSurface();
					page.Image("/dev/shm/ora_lcdfrm", 0, 0, 160, 43);
					page.DrawSurface();
					page.Redraw();
				}
			}
		}
	}
}
