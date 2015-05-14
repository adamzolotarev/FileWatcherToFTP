using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FileWatcherFtpUploaderConsole
{
	public class FileWatcherFtpUploader
	{
		private List<Task> _runningTasks = new List<Task>();
		
		public void Init()
		{
			var fileWatcher = new FileSystemWatcher
			{
				Path = ConfigOptions.MonitoredFolderPath,
				EnableRaisingEvents = true
			};
			fileWatcher.Created += FileWatcherOnFileCreated;
		}

		private void FileWatcherOnFileCreated(object sender, FileSystemEventArgs fileSystemEventArgs)
		{	
			while (_runningTasks.Count() >= ConfigOptions.MaxNumberOfFtpConnections)
			{
				Task.WaitAny(_runningTasks.ToArray());
				CleanRunningThreadList();
			}
			
			var task = Task.Factory.StartNew(() => UploadToFtp(fileSystemEventArgs));
			_runningTasks.Add(task);
		}

		private void CleanRunningThreadList()
		{
			var completedTasks = _runningTasks.Where(m => m.IsCompleted).ToList();
			completedTasks.ForEach(c => _runningTasks.Remove(c));
		}

		private void UploadToFtp(FileSystemEventArgs parameters)
		{
			var fileSystemEventArgs = parameters;
			var numOfFtpFails = 0;
			var ftpServerFullPath = GetUploadedFullPath();
			bool ftpUploaded = false;

			while (ShouldTryToUpload(numOfFtpFails, ftpUploaded))
			{
				try
				{
					WaitForFileAccess(
						fileSystemEventArgs.FullPath, 
						ConfigOptions.WaitMillisecondsBetweenTryingToReadTheFile,
						ConfigOptions.MaxNumberOfTriesToReadTheFiles);
					
					if (!IsFileReady(fileSystemEventArgs.FullPath)) return;

					ftpUploaded = SendFileUploadFtpRequest(ftpServerFullPath, fileSystemEventArgs);
				}
				catch
				{
					numOfFtpFails++;
					HandleFtpUploadError(ftpServerFullPath);
				}
			}
		}

		private static bool ShouldTryToUpload(int numOfFtpFails, bool ftpUploaded)
		{
			return numOfFtpFails < ConfigOptions.MaxNumberOfFtpFailsBeforeIgnoringFile 
			       && !ftpUploaded;
		}

		private void HandleFtpUploadError(string ftpServerFullPath)
		{
				Thread.Sleep(1000);
				if (!FtpDirectoryExists(ftpServerFullPath, ConfigOptions.FtpUser, ConfigOptions.FtpPassword))
					CreateFtpDirectory(ftpServerFullPath, ConfigOptions.FtpUser, ConfigOptions.FtpPassword);
		}

		private static bool SendFileUploadFtpRequest(string ftpServerFullPath, FileSystemEventArgs fileSystemEventArgs)
		{
			var ftpRequest = (FtpWebRequest) WebRequest.Create(ftpServerFullPath + fileSystemEventArgs.Name);
			ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
			ftpRequest.Credentials = new NetworkCredential(ConfigOptions.FtpUser, ConfigOptions.FtpPassword);
			// Copy the contents of the file to the request stream.
			byte[] fileContents;
			using (var sourceStream = new StreamReader(fileSystemEventArgs.FullPath))
			{
				fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
			}
			ftpRequest.ContentLength = fileContents.Length;
			ftpRequest.KeepAlive = false;

			using (var requestStream = ftpRequest.GetRequestStream())
			{
				requestStream.Write(fileContents, 0, fileContents.Length);
			}

			using (ftpRequest.GetResponse())
			{
				return true;
			}
		}
		
		private void CreateFtpDirectory(string fullPath, string ftpUser, string ftpPassword)
		{
			try
			{
				CreateFtpFolder(fullPath, ftpUser, ftpPassword);
			}
			catch
			{
				CreateAllSubpaths(fullPath, ftpUser, ftpPassword);
			}
		}

		private static void CreateAllSubpaths(string fullPath, string ftpUser, string ftpPassword)
		{
			var address = Regex.Match(fullPath, @"^(ftp://)?(\w*|.?)*/").Value.Replace("ftp://", "").Replace("/", "");
			var dirs = Regex.Split(fullPath.Replace(address, "").Replace("ftp://", ""), "/").Where(x => x.Length > 0);
			var currentFtpFolder = @"ftp://" + address + @"/";
			foreach (var dir in dirs)
			{
				currentFtpFolder += dir + @"/";
				CreateFtpFolder(currentFtpFolder, ftpUser, ftpPassword);
			}
		}

		private static void CreateFtpFolder(string fullPath, string ftpUser, string ftpPassword)
		{
			var request = WebRequest.Create(fullPath);
			request.Method = WebRequestMethods.Ftp.MakeDirectory;
			request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
			using (request.GetResponse())
			{
			}
		}

		private string GetUploadedFullPath()
		{
			var today = DateTime.Now;
			return ConfigurationManager.AppSettings["FtpServer"] +
				   (ConfigOptions.ShouldUseYearAsFtpPath ? (today.Year + @"/") : "") +
				   (ConfigOptions.ShouldUseMonthAsFtpPath ? (today.Month + @"/") : "") +
				   (ConfigOptions.ShouldUseDayAsFtpPath ? (today.Day + @"/") : "");
		}

		public bool FtpDirectoryExists(string ftpDirectoryPath, string ftpUser, string ftpPassword)
		{
			var ftpRequest = WebRequest.Create(ftpDirectoryPath);
			ftpRequest.Credentials = new NetworkCredential(ftpUser, ftpPassword);
			ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
			
			try
			{
				using (ftpRequest.GetResponse())
				{
					return true;
				}
			}
			catch
			{
				return false;
			}
		}
		
		private void WaitForFileAccess(string fullFileName, int millisecondsToWait, int maxNumberOfTries)
		{
			var numberOfTries = 0;
			while (numberOfTries < maxNumberOfTries && !IsFileReady(fullFileName))
			{
				Thread.Sleep(millisecondsToWait);
				numberOfTries++;
			}
		}

		public bool IsFileReady(String filename)
		{
			// If the file can be opened for exclusive access it means that the file
			// is no longer locked by another process.
			try
			{
				using (var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
				{
					return inputStream.Length > 0;
				}
			}
			catch
			{
				return false;
			}
		}
	}
}
