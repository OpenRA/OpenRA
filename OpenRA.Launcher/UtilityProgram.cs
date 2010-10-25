using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

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

		public static UtilityProgramResponse Call(string command, params string[] args)
		{
			Process p = new Process();
			p.StartInfo.FileName = "OpenRA.Utility.exe";
			p.StartInfo.Arguments = BuildArgs(command, args);
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.CreateNoWindow = true;
			
			p.Start();

			return new UtilityProgramResponse(p.StandardOutput.ReadToEnd());
		}

		public static UtilityProgramResponse CallWithAdmin(string command, params string[] args)
		{
			Process p = new Process();
			p.StartInfo.FileName = "OpenRA.Utility.exe";
			p.StartInfo.Arguments = BuildArgs(command, args);
			p.StartInfo.CreateNoWindow = true;
			p.StartInfo.Verb = "runas";

			try
			{
				p.Start();
			}
			catch (Win32Exception e)
			{
				if (e.NativeErrorCode == 1223) //ERROR_CANCELLED
					return new UtilityProgramResponse("Error: User cancelled elevation prompt.");
				throw e;
			}

			p.WaitForExit();

			return new UtilityProgramResponse(File.ReadAllText("output.txt"));
		}
	}
}
