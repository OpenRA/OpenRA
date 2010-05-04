using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace OpenRAUploader
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 6) { PrintUsage(); return; }
			var server = args[0];
			var user = args[1];
			var password = args[2];
			var platform = args[3];
			var version = args[4];
			var filename = args[5];

			var uri = new UriBuilder("ftp", server);
			uri.UserName = user;
			uri.Password = password;
			uri.Path += platform + "/";

			var mainFileUri = new UriBuilder(uri.Uri);
			mainFileUri.Path += Path.GetFileName(filename);

			if (!File.Exists(filename)) { Console.WriteLine(filename + " does not exist."); return; }

			Console.WriteLine(string.Format("Uploading {0} to {1}", Path.GetFileName(filename), mainFileUri.Uri));
			var fileStream = File.Open(filename, FileMode.Open, FileAccess.Read);
			var size = fileStream.Length;

			var response = UploadFile(mainFileUri.Uri, fileStream, false);
			if (response != null) Console.WriteLine("Response: " + response.StatusDescription);
			fileStream.Close();

			var jsonUri = new UriBuilder(uri.Uri);
			jsonUri.Path += "_version.json";

			string formatString = "{{\n" +
				"\t\"version\":\"{0}\",\n" +
				"\t\"size\":\"{1:F2}MB\"\n" +
				"}}";

			string json = string.Format(formatString, 
				version, 
				((double)size / 1048576));

			MemoryStream jsonStream = new MemoryStream(Encoding.ASCII.GetBytes(json));

			Console.WriteLine("Uploading version JSON file");
			response = UploadFile(jsonUri.Uri, jsonStream, true);
			if (response != null) Console.WriteLine("Response: " + response.StatusDescription);
			jsonStream.Close();

			var latestUri = new UriBuilder(uri.Uri);
			latestUri.Path += "latest.txt";
			MemoryStream latestStream = new MemoryStream(Encoding.ASCII.GetBytes(Path.GetFileName(filename)));

			Console.WriteLine("Uploading latest.txt");
			response = UploadFile(latestUri.Uri, latestStream, true);
			if (response != null) Console.WriteLine("Response: " + response.StatusDescription);
			latestStream.Close();
		}

		static FtpWebResponse UploadFile(Uri uri, Stream file, bool text)
		{
			var ftp = WebRequest.Create(uri) as FtpWebRequest;
			if (ftp == null) { Console.WriteLine("Couldn't create FTP client. URI incorrect?\n" + uri.ToString()); return null; }
			ftp.Method = WebRequestMethods.Ftp.UploadFile;
			ftp.UseBinary = !text;
			var stream = ftp.GetRequestStream();
			const int bufferLength = 2048;
			byte[] buffer = new byte[bufferLength];
			int readBytes = 0;

			while ((readBytes = file.Read(buffer, 0, bufferLength)) != 0)
				stream.Write(buffer, 0, readBytes);

			stream.Close();
			return ftp.GetResponse() as FtpWebResponse;
		}

		static void PrintUsage()
		{
			Console.WriteLine("Usage:\n  OpenRAUploader.exe <ftp path> <username> <password> <platform> <version> <filename>");
		}
	}
}
