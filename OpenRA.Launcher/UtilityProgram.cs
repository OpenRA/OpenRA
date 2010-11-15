using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.IO.Pipes;

namespace OpenRA.Launcher
{
	class UtilityProgramResponse
	{
		public bool IsError
		{
			get { return response.StartsWith("Error:"); }
		}

		string response;

		public string Response
		{
			get 
			{
				if (IsError)
					return response.Remove(0, 7);
				else
					return response; 
			}
		}

		public string[] ResponseLines
		{
			get 
			{
				string s = response;
				if (IsError)
					s = response.Remove(0, 7);
				return s.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries); 
			}
		}

		public UtilityProgramResponse(string response)
		{
			this.response = response.Trim('\r', '\n');
		}
	}

	static class UtilityProgram
	{
		static string BuildArgs(string command, string[] args)
		{
			StringBuilder arguments = new StringBuilder();
			arguments.Append("\"");
			arguments.Append(command + "=");
			arguments.Append(string.Join(",", args));
			arguments.Append("\"");
			return arguments.ToString();
		}

		public static StreamReader Call(string command, params string[] args)
		{
			Process p = new Process();
			p.StartInfo.FileName = "OpenRA.Utility.exe";
			p.StartInfo.Arguments = BuildArgs(command, args) + " --pipe";
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.CreateNoWindow = true;
						
			p.Start();

			NamedPipeClientStream pipe = new NamedPipeClientStream(".", "OpenRA.Utility", PipeDirection.In);
			pipe.Connect();
			return new StreamReader(pipe);
		}

		public static Process CallWithAdmin(string command, params string[] args)
		{
			Process p = new Process();
			p.StartInfo.FileName = "OpenRA.Utility.exe";
			p.StartInfo.Arguments = BuildArgs(command, args) + " --pipe";
			p.StartInfo.Verb = "runas";

			try
			{
				p.Start();
			}
			catch (Win32Exception e)
			{
				if (e.NativeErrorCode == 1223) //ERROR_CANCELLED
					return null;
				throw e;
			}

			return p;
		}
	}
}
