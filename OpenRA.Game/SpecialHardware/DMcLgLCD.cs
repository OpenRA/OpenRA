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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace OpenRA
{
    class DMcLgLCD
    {
        // Windows Constants
        public const uint ERROR_SUCCESS = 0;

        // LCD Constants
        public const int LGLCD_DEVICE_BW = 1;
        public const int LGLCD_DEVICE_QVGA = 2;
        public const int LGLCD_FORE_YES = 1;
        public const int LGLCD_FORE_NO = 0;
        public const int LGLCD_INVALID_DEVICE = -1;
        public const int LGLCD_INVALID_CONNECTION = -1;

        // G-15 Button Constants
        public const uint LGLCD_BUTTON_1 = 1;
        public const uint LGLCD_BUTTON_2 = 2;
        public const uint LGLCD_BUTTON_3 = 4;
        public const uint LGLCD_BUTTON_4 = 8;

        // G-19 Button Constants
        public const uint LGLCD_BUTTON_LEFT = 0x0100;     //Decimal 256
        public const uint LGLCD_BUTTON_RIGHT = 0x0200;     //Decimal 512
        public const uint LGLCD_BUTTON_OK = 0x0400;     //Decimal 1024
        public const uint LGLCD_BUTTON_CANCEL = 0x0800;     //Decimal 2048
        public const uint LGLCD_BUTTON_UP = 0x1000;     //Decimal 4096
        public const uint LGLCD_BUTTON_DOWN = 0x2000;     //Decimal 8192
        public const uint LGLCD_BUTTON_MENU = 0x4000;     //Decimal 16384


        /// <summary>
        /// Initialize LCD Library. 
        /// </summary>
        /// <returns>ERROR_SUCCESS on success, standard Windows error otherwise.</returns>
        /// <remarks>Must be paired with LcdDeInit.  Put LcdInit in your Form_Load
        /// or main and LcdDeInit in your exit function (ie. Form_Closing).</remarks>
        [DllImport("DMcLgLCD.dll", EntryPoint = "LcdInit")]
        public static extern uint LcdInit();

        /// <summary>
        /// Deinitialize LCD Library.  
        /// </summary>
        /// <returns>ERROR_SUCCESS on success, standard Windows error otherwise.</returns>
        /// <remarks>Put LcdDeInit in your exit function (ie. Form_Closing).</remarks>
        [DllImport("DMcLgLCD.dll", EntryPoint = "LcdDeInit")]
        public static extern uint LcdDeInit();

        /// <summary>
        /// Makes a connection to the LCD Library.  Use connection to open a device.
        /// </summary>
        /// <param name="appFriendlyName">The name that appears in the Logitech LCD Manager.</param>
        /// <param name="isPersistent">Not currently used.  Always acts as persistent.</param>
        /// <param name="isAutostartable">Sets your program to be startable by the Logitech LCD Manager when Windows starts up.  Use 0 for most types of applications.</param>
        /// <returns>Connection on success.  LGLCD_INVALID_CONNECTION on failure.</returns>
        /// <remarks>Use only after LcdInit.  Multiple connections are allowed.</remarks>
        [DllImport("DMcLgLCD.dll", EntryPoint = "LcdConnectA")]
        public static extern int LcdConnect(
            [MarshalAs(UnmanagedType.LPStr)] string appFriendlyName,
            int isPersistent,
            int isAutostartable);

        /// <summary>
        /// Makes a connection to the LCD Library.  Use connection to open a device.
        /// </summary>
        /// <param name="appFriendlyName">The name that appears in the Logitech LCD Manager.</param>
        /// <param name="isPersistent">Not currently used.  Always acts as persistent.</param>
        /// <param name="isAutostartable">Sets your program to be startable by the Logitech LCD Manager when Windows starts up.  Use 0 for most types of applications.</param>
        /// <returns>Connection on success.  LGLCD_INVALID_CONNECTION on failure.</returns>
        /// <remarks>Use only after LcdInit.  Multiple connections are allowed.
        /// Use LcdConnectEx if you need to read the extended G-19 buttons.  Otherwise only BUTTON_1 through BUTTON_4 will be returned.</remarks>
        [DllImport("DMcLgLCD.dll", EntryPoint = "LcdConnectExA")]
        public static extern int LcdConnectEx(
            [MarshalAs(UnmanagedType.LPStr)] string appFriendlyName,
            int isPersistent,
            int isAutostartable);

        /// <summary>
        /// Disconnects from the LCD Library. 
        /// </summary>
        /// <param name="connection">Connection that you wish to close.</param>
        /// <returns>ERROR_SUCCESS on success, standard Windows error otherwise.</returns>
        /// <remarks>Each LcdConnect requires it's own LcdDisconnect.  Issue before LcdDeInit.</remarks>
        [DllImport("DMcLgLCD.dll", EntryPoint = "LcdDisconnect")]
        public static extern uint LcdDisconnect(
            int connection);

        /// <summary>
        /// Opens an LCD device for writing.
        /// </summary>
        /// <param name="connection">Connection parameter received from an LcdConnect.</param>
        /// <param name="deviceType">Type of device you want to open.  LGLCD_DEVICE_BW or LGLCD_DEVICE_QVGA.</param>
        /// <returns>Device number if successful, LGLCD_INVALID_DEVICE otherwise.</returns>
        /// <remarks>Only one device can be open per connection.</remarks>
        [DllImport("DMcLgLCD.dll", EntryPoint = "LcdOpenByType")]
        public static extern int LcdOpenByType(
            int connection,
            int deviceType);

        /// <summary>
        /// Close an LCD device.
        /// </summary>
        /// <param name="device">Device number to close.</param>
        /// <returns>ERROR_SUCCESS on success, standard Windows error otherwise.</returns>
        /// <remarks>Issue before LcdDisconnect.</remarks>
        [DllImport("DMcLgLCD.dll", EntryPoint = "LcdClose")]
        public static extern uint LcdClose(
            int device);

        /// <summary>
        /// Updates LCD device with an HBITMAP.
        /// </summary>
        /// <param name="device">Device number received from LcdOpenByType.</param>
        /// <param name="HBITMAP">HBITMAP to display.</param>
        /// <param name="format">Image format to send. LGLCD_DEVICE_BW or LGLCD_DEVICE_QVGA.</param>
        /// <returns>ERROR_SUCCESS on success, standard Windows error otherwise.</returns>
        /// <remarks>You can send LGLCD_DEVICE_BW image to both monochrome and color devices.  If you send a
        /// LGLCD_DEVICE_QVGA image to a monochrome device, nothing with display.  If you send a LGLCD_DEVICE_QVGA
        /// image to a color device, it will no longer accept LGLCD_DEVICE_BW type images.
        /// An image too large for the display will be truncated.  One too small will appear in the upper left.</remarks>
        /// <example>
        /// Bitmap LCD = new Bitmap (160, 43);
        /// Graphics g = Graphics.FromImage(LCD);
        /// g.Clear(Color.White);
        /// g.Dispose();
        /// LcdUpdateBitmap (device, LCD.GetHbitmap(), LGLCD_DEVICE_BW);
        /// LCD.Dispose();
        /// </example>
        [DllImport("DMcLgLCD.dll", EntryPoint = "LcdUpdateBitmap")]
        public static extern uint LcdUpdateBitmap(
            int device,
            IntPtr HBITMAP,
            int format);

        /// <summary>
        /// Brings the device to the forground on the LCD.
        /// </summary>
        /// <param name="device">Device received from LcdOpenByType.</param>
        /// <param name="foregroundYesNoFlag">Either LGLCD_FORE_YES or LGLCD_FORE_NO.</param>
        /// <returns>ERROR_SUCCESS on success, standard Windows error otherwise.</returns>
        /// <remarks>Does nothing until after the first LcdUpdateBitmap has been sent.</remarks>
        [DllImport("DMcLgLCD.dll", EntryPoint = "LcdSetAsLCDForegroundApp")]
        public static extern uint LcdSetAsLCDForegroundApp(
            int device,
            int foregroundYesNoFlag);


        /// <summary>
        /// Reads the status of the soft buttons at the time the function is called.
        /// </summary>
        /// <param name="device">Device received from LcdOpenByType.</param>
        /// <returns>OR'd button status.  And with button mask to read status of individual keys.</returns>
        /// <example>
        /// uint buttons = DMcLgLCD.LcdReadSoftButtons(device);
        /// if ((buttons & DMcLgLCD.LCD_BUTTON_1) == DMcLgLCD.LCD_BUTTON_1)
        /// </example>
        [DllImport("DMcLgLCD.dll", EntryPoint = "LcdReadSoftButtons")]
        public static extern uint LcdReadSoftButtons(
            int device);

        /// <summary>
        /// Prototype for the config callback function.
        /// </summary>
        /// <param name="connection">Connection that is issuing the callback.  Compare with connection from LcdConnect/LcdConnectEx
        /// if you have more than one connection to the library to determine which configuration to open.</param>
        public delegate void cfgCallback(int connection);

        /// <summary>
        /// Sets the configuration callback function.
        /// </summary>
        /// <param name="configurationCallback">Name of your callback function.</param>
        /// <returns>ERROR_SUCCESS on success, standard Windows error otherwise.</returns>
        [DllImport("DMcLgLCD.dll", EntryPoint = "LcdSetConfigCallback")]
        public static extern uint LcdSetConfigCallback(
             cfgCallback configurationCallback);


        //Button callback is unstable.  Use polling function instead.
        public delegate void btnCallback(int deviceType, int dwButtons);

        [DllImport("DMcLgLCD.dll", EntryPoint = "LcdSetButtonCallback")]
        public static extern uint LcdSetButtonCallback(
             btnCallback softKeyCallback);

    }
}

