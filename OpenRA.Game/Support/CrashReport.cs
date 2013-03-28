#region Copyright & License Information
/*
 * Copyright 2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;
using System.Text;
using System.Reflection;
using OpenRA.FileFormats;
using OpenRA.Exceptions;
using OpenRA.Network;
using OpenRA.GameRules;

namespace OpenRA.Support
{
	public class CrashReport
	{
		static string ReportPath = Platform.SupportDir + "Crash Reports" + Path.DirectorySeparatorChar;

		static readonly string[] Fields = { "ReportUid", "ReportVersion", "Created", "OSPlatform", "OSVersion", "GameUID"};
		public static readonly int ReportVersion = 1;

		public DateTime Created = DateTime.UtcNow;
		public string Error { get { return exception == null ? null : exception.GetType().FullName; }}
		public string Message { get { return exception == null ? null : exception.Message; }}
		public static readonly string OSVersion = Environment.OSVersion.Version.ToString();
		public static readonly string OSPlatform = Environment.OSVersion.Platform.ToString();
		public readonly string ReportUid;
		public Session LobbyInfo;
		public Mod[] Mods;

		Exception exception;

		public CrashReport(Exception e)
		{
			ReportUid = System.Guid.NewGuid().ToString();
			exception = e;
		}

		public static void OpenReportsFolder()
		{
			Process.Start(ReportPath);
		}

		private struct WorkData
		{
			public string url;
			public string yaml;
		}

		private void doWork(object sender, DoWorkEventArgs e)
		{
			BackgroundWorker bw = sender as BackgroundWorker;
			WorkData data = (WorkData) e.Argument;

			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(data.url);

			request.UserAgent = "OpenRA";
			request.Method = "POST";
			request.AllowWriteStreamBuffering = false;
			request.ContentType = "text/x-yaml";


			var enc = new System.Text.UTF8Encoding();
			byte[] ba = enc.GetBytes(data.yaml);

			request.ContentLength = ba.Length;

			Stream dataStream = request.GetRequestStream();

			for (var i = 0; i < ba.Length; i+=1024)
			{
				dataStream.Write(ba,i,Math.Min(1024, ba.Length-i));

				bw.ReportProgress((int)(i/(float)ba.Length*100));
			}

			dataStream.Close();
			HttpWebResponse response = (HttpWebResponse) request.GetResponse();

			e.Result = response.GetResponseStream().ReadAllText();
			response.Close();
		}

		public void Submit(Action<int> onProgress, Action onComplete, Action<WebException> onError)
		{
			BackgroundWorker bw = new BackgroundWorker();

			bw.WorkerReportsProgress = true;
			bw.DoWork += this.doWork;
			bw.ProgressChanged += (sender, e) => { onProgress(e.ProgressPercentage); };
			bw.RunWorkerCompleted += (sender, e) =>
			{
				if (e.Error != null)
				{
					Console.WriteLine(e.Error.ToString());
					if (e.Error is WebException)
						onError(e.Error as WebException);
					onError(null);
				}
				else
				{
					Console.WriteLine(e.Result as String);
					onProgress(100);
					onComplete();
				}
			};

			var data = new WorkData();
			data.yaml = Serialize().WriteToString();

			if (Game.Settings != null)
				data.url = Game.Settings.Server.CrashReportServer;
			else
				data.url = new ServerSettings().CrashReportServer;

			data.url += "submit/";


			onProgress(0);
			bw.RunWorkerAsync(data);
		}

		public void Save()
		{
			Directory.CreateDirectory(ReportPath);
			var filename = ReportPath + Created.ToString("yyyy-MM-dd HHmmss ")+ReportUid+".yaml";
			Serialize().WriteToFile(filename);
		}

		public List<MiniYamlNode> Serialize()
		{
			var root = new List<MiniYamlNode>();
			List<MiniYamlNode> nodes;

			foreach (var field in Fields)
			{
				FieldInfo f = this.GetType().GetField(field);
				if (f == null || f.GetValue(this) == null) continue;
				root.Add( new MiniYamlNode( field, FieldSaver.FormatValue( this, f ) ) );
			}

			// Exception-Stack
			var inner = exception;
			var parent = root;
			while (inner != null)
			{
				nodes = new List<MiniYamlNode>();
				nodes.Add(new MiniYamlNode("Type", inner.GetType().FullName));
				nodes.Add(new MiniYamlNode("Message", inner.Message));

				// Stack-Trace
				var stack = new List<MiniYamlNode>();
				var i = 0;
				foreach (var frame in inner.StackTrace.Split('\n'))
				{
					var line = frame.Trim();
					line = line.StartsWith("at ") ? line.Substring(3) : line;
					stack.Add(new MiniYamlNode(i++.ToString(), line));
				}
				nodes.Add(new MiniYamlNode("StackTrace", new MiniYaml(null, stack)));

				parent.Add(new MiniYamlNode (parent == root ? "Exception" : "InnerException", new MiniYaml(null, nodes)));
				parent = nodes;
				inner = inner.InnerException;
			}

			// SyncReport
			if (exception is OutOfSyncException)
			{
				var e = (OutOfSyncException) exception;
				nodes = new List<MiniYamlNode>();
				nodes.Add(new MiniYamlNode("Frame", e.SyncReport.Frame.ToString()));
				nodes.Add(new MiniYamlNode("SharedRandom", e.SyncReport.SyncedRandom.ToString()));
				nodes.Add(new MiniYamlNode("TotalCount", e.SyncReport.TotalCount.ToString()));

				var i = 0;
				foreach (var a in e.SyncReport.Traits)
					nodes.Add(new MiniYamlNode("Trait@{0}".F(i++), FieldSaver.Save(a)));

				root.Add(new MiniYamlNode ("SyncReport", new MiniYaml(null, nodes)));
			}

			// Mods
			if (Mods != null && Mods.Length > 0)
			{
				nodes = new List<MiniYamlNode>();
				var i = 0;
				foreach (var mod in Mods)
				{
					nodes.Add(new MiniYamlNode("Mod@{0}".F(i++), FieldSaver.Save(mod)));
				}
				root.Add(new MiniYamlNode("Mods", new MiniYaml(null, nodes)));
			}

			// LobbyInfo (Session)
			if (LobbyInfo != null)
			{
				nodes = new List<MiniYamlNode>();
				foreach (var client in LobbyInfo.Clients)
					nodes.Add(new MiniYamlNode("Client@{0}".F(client.Index), FieldSaver.Save(client)));

				foreach (var slot in LobbyInfo.Slots)
					nodes.Add(new MiniYamlNode("Slot@{0}".F(slot.Key), FieldSaver.Save(slot.Value)));

				nodes.Add(new MiniYamlNode("GlobalSettings", FieldSaver.Save(LobbyInfo.GlobalSettings)));
				root.Add(new MiniYamlNode("LobbyInfo", new MiniYaml(null, nodes)));
			}

			return root;
		}
	}
}

