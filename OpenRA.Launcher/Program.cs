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
using System.Diagnostics;
using System.Linq;

namespace OpenRA.Launcher
{
	static class Program
	{
		[STAThread]
		static int Main(string[] args)
		{
			if (Debugger.IsAttached || args.Contains("--just-die"))
			{
				try
				{
					return (int)Game.InitializeAndRun(args);
				}
				catch
				{
					// Flush logs before rethrowing, i.e. allowing the exception to go unhandled.
					// try-finally won't work - an unhandled exception kills our process without running the finally block!
					Log.Dispose();
					throw;
				}
				finally
				{
					Log.Dispose();
				}
			}

			AppDomain.CurrentDomain.UnhandledException += (_, e) => ExceptionHandler.HandleFatalError((Exception)e.ExceptionObject);

			try
			{
				return (int)Game.InitializeAndRun(args);
			}
			catch (Exception e)
			{
				ExceptionHandler.HandleFatalError(e);
				return (int)RunStatus.Error;
			}
			finally
			{
				// Flushing logs in finally block is okay here, as the catch block handles the exception.
				Log.Dispose();
			}
		}
	}
}
