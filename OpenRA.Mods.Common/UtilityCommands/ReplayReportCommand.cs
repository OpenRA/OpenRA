#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using OpenRA.FileFormats;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ReplayReportCommand : IUtilityCommand
	{
		public string Name { get { return "--replay-report"; } }

		// This covers .rep and .orarep files
		string replayExtension = "*rep";
		string defaultBatchOutputDir = "replay_reports";

		string progressText = "";

		[Desc("REPLAYFILE|REPLAYDIR [OUTPUTFILE|OUTPUTDIR]", "Extract game info and chat from replay files.")]
		public void Run(ModData modData, string[] args)
		{
			if (args.Length == 1 || args.Length > 3)
			{
				Console.WriteLine("Accepted arguments: REPLAYFILE|REPLAYDIR [OUTPUTFILE|OUTPUTDIR]");
				Console.WriteLine("Example use:");
				Console.WriteLine(".\\OpenRA.Utility.exe ra --replay-report replay.orarep");
				return;
			}

			var sourcePath = args[1];
			var outputPath = args.Length == 3 ? args[2] : null;
			var batchMode = false;
			string[] replayList = null;

			try
			{
				// Check if source path exists and determine whether it's a file or a directory.
				if (File.GetAttributes(sourcePath).HasFlag(FileAttributes.Directory))
					batchMode = true;

				if (batchMode)
				{
					replayList = Directory.GetFiles(sourcePath, "*." + replayExtension);
					if (replayList.Length == 0)
					{
						Console.WriteLine("No replay file in source directory.");
						return;
					}

					if (outputPath == null)
						outputPath = Path.Combine(sourcePath, defaultBatchOutputDir);

					Directory.CreateDirectory(outputPath);
				}
				else if (outputPath != null)
					Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
			}
			catch (Exception ex)
			{
				if (CatchException(ex, "Error in source or output argument."))
					return;

				throw;
			}

			Log.AddChannel("debug", "debug.log");
			Log.AddChannel("geoip", "geoip.log");

			if (outputPath == null)
			{
				// Extract from a single replay and print report to console.
				Console.Write(ReadReport(sourcePath));
				return;
			}

			Console.WriteLine("Source: " + Path.GetFullPath(sourcePath));
			Console.WriteLine("Output: " + Path.GetFullPath(outputPath));

			if (!batchMode)
			{
				// Extract from a single replay and save report to disk.
				var report = ReadReport(sourcePath);
				if (report != null)
					WriteReport(outputPath, report);

				return;
			}

			// Extract from multiple replays and save reports to disk.
			Console.CancelKeyPress += new ConsoleCancelEventHandler((object o, ConsoleCancelEventArgs a) =>
				{ Console.WriteLine(); });

			Console.WriteLine("Number of found replays: " + replayList.Length);
			var reportCount = 0;
			foreach (var replayFile in replayList)
			{
				ClearProgressDisplay();
				progressText = string.Format("Extracting: [{0}] {1}", reportCount + 1, Path.GetFileName(replayFile));
				Console.Write(progressText);

				var report = ReadReport(replayFile);
				if (report == null)
					continue;

				var reportFile = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(replayFile) + ".txt");
				if (WriteReport(reportFile, report))
					reportCount++;
			}

			ClearProgressDisplay();
			Console.WriteLine("Number of written reports: " + reportCount);
		}

		string ReadReport(string replayFile)
		{
			var metadata = ReplayMetadata.Read(replayFile);
			var report = ReplayReport.Read(metadata);

			if (report == null)
			{
				ClearProgressDisplay();
				Console.WriteLine(">> Extraction failed: " + Path.GetFileName(replayFile));
			}

			return report;
		}

		bool WriteReport(string reportFile, string report)
		{
			try
			{
				File.WriteAllText(reportFile, report);
			}
			catch (Exception ex)
			{
				if (CatchException(ex, "Failed to write report file."))
					return false;

				throw;
			}

			return true;
		}

		void ClearProgressDisplay()
		{
			// Backspace + Space + Backspace deletes one character from console.
			for (var i = 0; i < progressText.Length; i++)
				Console.Write("\b \b");
		}

		bool CatchException(Exception ex, string errorText)
		{
			if (ex is ArgumentException || ex is NotSupportedException || ex is IOException || ex is UnauthorizedAccessException)
			{
				ClearProgressDisplay();
				Console.WriteLine(">> " + errorText);
				Console.WriteLine("   " + ex.Message);

				Log.AddChannel("utility", "utility.log");
				Log.Write("utility", "Received args: {0}", Environment.GetCommandLineArgs().JoinWith(" "));
				Log.Write("utility", "{0}", ex);

				return true;
			}

			return false;
		}
	}
}
