using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.IO.Pipes;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OpenRA.Launcher
{
	public enum DownloadStatus
	{
		NOT_REGISTERED, AVAILABLE, DOWNLOADING, DOWNLOADED, EXTRACTING, EXTRACTED, ERROR
	}

	class Download : IDisposable
	{
		DownloadStatus status = DownloadStatus.NOT_REGISTERED;
		string url = "", target = "", key = "";
		BackgroundWorker downloadBGWorker, extractBGWorker;
		int bytesTotal = 0, bytesDone = 0;
		string errorMessage = "";
		HtmlDocument document;

		public string ErrorMessage
		{
			get { return errorMessage; }
		}

		public int BytesDone
		{
			get { return bytesDone; }
		}

		public int BytesTotal
		{
			get { return bytesTotal; }
		}

		public DownloadStatus Status
		{
			get { return status; }
		}

		public Download(HtmlDocument document, string key, string url, string filename)
		{
			this.url = url;
			this.key = key;
			this.document = document;
			target = Path.Combine(Path.GetTempPath(), filename);
			if (File.Exists(target))
				status = DownloadStatus.DOWNLOADED;
			else
				status = DownloadStatus.AVAILABLE;

			downloadBGWorker = new BackgroundWorker()
			{
				WorkerReportsProgress = true,
				WorkerSupportsCancellation = true
			};

			downloadBGWorker.DoWork += RunDownload;
			downloadBGWorker.ProgressChanged += UpdateProgress;
			downloadBGWorker.RunWorkerCompleted += DownloadFinished;

			extractBGWorker = new BackgroundWorker();

			extractBGWorker.DoWork += DoExtraction;
			extractBGWorker.RunWorkerCompleted += ExtractionFinished;
		}

		public void StartDownload()
		{
			if (!downloadBGWorker.IsBusy)
			{
				status = DownloadStatus.DOWNLOADING;
				downloadBGWorker.RunWorkerAsync(new string[] { url, target });
			}
		}

		public void CancelDownload()
		{
			if (downloadBGWorker.IsBusy)
				downloadBGWorker.CancelAsync();
		}

		public void ExtractDownload(string destPath)
		{
			if (!extractBGWorker.IsBusy)
			{
				status = DownloadStatus.EXTRACTING;
				extractBGWorker.RunWorkerAsync(new string[] { target, destPath });
			}
		}

		static void RunDownload(object sender, DoWorkEventArgs e)
		{
			var bgWorker = sender as BackgroundWorker;
			string[] args = e.Argument as string[];
			string url = args[0];
			string dest = args[1];
			var p = UtilityProgram.CallWithAdmin("--download-url", url, dest);
			Regex r = new Regex(@"(\d{1,3})% (\d+)/(\d+) bytes");

			NamedPipeClientStream pipe = new NamedPipeClientStream(".", "OpenRA.Utility", PipeDirection.In);
			pipe.Connect();

			using (var response = new StreamReader(pipe))
			{
				while (!p.HasExited)
				{
					string s = response.ReadLine();
					if (string.IsNullOrEmpty(s)) continue;
					if (Util.IsError(ref s))
						throw new Exception(s);

					if (bgWorker.CancellationPending)
					{
						e.Cancel = true;
						p.Kill();
						return;
					}
					if (!r.IsMatch(s)) continue;
					var m = r.Match(s);
					bgWorker.ReportProgress(int.Parse(m.Groups[1].Value), 
						new string[] { m.Groups[2].Value, m.Groups[3].Value });
				}
			}
		}

		void UpdateProgress(object sender, ProgressChangedEventArgs e)
		{
			string[] s = e.UserState as string[];
			bytesDone = int.Parse(s[0]);
			bytesTotal = int.Parse(s[1]);
			document.InvokeScript("downloadProgressed", new object[] { key });
		}

		void DownloadFinished(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				status = DownloadStatus.ERROR;
				errorMessage = e.Error.Message;
				//if (File.Exists(target))
				//    File.Delete(target);
				document.InvokeScript("downloadProgressed", new object[] { key });
				return;
			}

			if (e.Cancelled)
			{
				status = DownloadStatus.ERROR;
				errorMessage = "Download Cancelled";
				//if (File.Exists(target))
				//    File.Delete(target);
				document.InvokeScript("downloadProgressed", new object[] { key });
				return;
			}

			status = DownloadStatus.DOWNLOADED;
			document.InvokeScript("downloadProgressed", new object[] { key });
		}

		void DoExtraction(object sender, DoWorkEventArgs e)
		{
			var bgWorker = sender as BackgroundWorker;
			string[] args = e.Argument as string[];
			string zipFile = args[0];
			string destPath = args[1];

			var p = UtilityProgram.CallWithAdmin("--extract-zip", zipFile, destPath);
			var pipe = new NamedPipeClientStream(".", "OpenRA.Utility", PipeDirection.In);

			pipe.Connect();

			using (var reader = new StreamReader(pipe))
			{
				while (!p.HasExited)
				{
					string s = reader.ReadLine();
					if (string.IsNullOrEmpty(s)) continue;
					if (Util.IsError(ref s))
						throw new Exception(s);
				}
			}
		}

		void ExtractionFinished(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				status = DownloadStatus.ERROR;
				errorMessage = e.Error.Message;
				document.InvokeScript("extractProgressed", new object[] { key });
			}

			
			status = DownloadStatus.EXTRACTED;
			document.InvokeScript("extractProgressed", new object[] { key });
		}

		bool disposed = false;

		~Download()
		{
			if (!disposed)
				Dispose();
		}

		public void Dispose()
		{
			if (status == DownloadStatus.DOWNLOADING && File.Exists(target))
				File.Delete(target);
			disposed = true;
		}
	}
}
