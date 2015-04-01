using System.Configuration;

namespace FileWatcherFtpUploaderConsole
{
	public class ConfigOptions
	{
		public static int MaxNumberOfFtpConnections
		{
			get { return int.Parse(ConfigurationManager.AppSettings["MaxNumberOfFtpConnections"]); }
		}

		public static int MillisecondsBetweenTestingForMaxRunningThreads {
			get { return 30000; }
		}

		public static string MonitoredFolderPath
		{
			get { return ConfigurationManager.AppSettings["MonitorFolder"]; }
		}

		public static int MaxNumberOfFtpFailsBeforeIgnoringFile { get { return 3; } }

		public static string FtpUser
		{
			get { return ConfigurationManager.AppSettings["FtpUser"]; }
		}

		public static string FtpPassword
		{
			get { return ConfigurationManager.AppSettings["FtpPassword"]; }
		}

		public static int MaxNumberOfTriesToReadTheFiles
		{
			get { return int.Parse(ConfigurationManager.AppSettings["MaxNumberOfTriesToReadTheFiles"]); }
		}
		public static int WaitMillisecondsBetweenTryingToReadTheFile
		{
			get { return 1000 * 60 * int.Parse(ConfigurationManager.AppSettings["WaitMinutesBetweenTryingToReadTheFile"]); }
		}
		
		public static bool ShouldUseYearAsFtpPath
		{
			get { return bool.Parse(ConfigurationManager.AppSettings["ShouldUseYearAsFtpPath"]); }
		}

		public static bool ShouldUseMonthAsFtpPath
		{
			get { return bool.Parse(ConfigurationManager.AppSettings["ShouldUseMonthAsFtpPath"]); }
		}

		public static bool ShouldUseDayAsFtpPath
		{
			get { return bool.Parse(ConfigurationManager.AppSettings["ShouldUseDayAsFtpPath"]); }
		}
	}
}