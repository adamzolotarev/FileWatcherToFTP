using System.Threading;

namespace FileWatcherFtpUploaderConsole
{
	class Program
	{
		static void Main(string[] args)
		{
			Init();
		}

		public static void Init()
		{
			var fileWatcherUploader = new FileWatcherFtpUploader();
			
			var job = new ThreadStart(fileWatcherUploader.Init);
			new Thread(job).Start();
			Thread.CurrentThread.Join();
		}
	}
}
