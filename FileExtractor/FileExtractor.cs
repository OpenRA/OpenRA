using System;
using OpenRA.FileFormats;
using System.IO;

namespace FileExtractor
{
	public class FileExtractor
	{
		int Length = 256;
		
		public FileExtractor (string[] args)
		{
			if (args.Length != 2)
			{
				Console.WriteLine("usage: FileExtractor mod[,mod]* filename");
				return;
			}

			var mods = args[0].Split(',');
			var manifest = new Manifest(mods);
			FileSystem.LoadFromManifest( manifest );
			
			try
			{
				var readStream = FileSystem.Open(args[1]);
				var writeStream = new FileStream(args[1], FileMode.OpenOrCreate, FileAccess.Write);
				
				WriteOutFile(readStream, writeStream);
				
			} 
			catch (FileNotFoundException) 
			{
				Console.WriteLine(String.Format("No Such File {0}", args[1]));
			}
		}
		
		void WriteOutFile (Stream readStream, Stream writeStream)
		{
   			Byte[] buffer = new Byte[Length];
   			int bytesRead = readStream.Read(buffer,0,Length);

 			while( bytesRead > 0 ) 
    		{
        		writeStream.Write(buffer,0,bytesRead);
        		bytesRead = readStream.Read(buffer,0,Length);
    		}
    		readStream.Close();
    		writeStream.Close();
		}
	}
}

