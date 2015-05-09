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
			fileWatcher.Created += FileWatcherOnCreated;
		}

		private void FileWatcherOnCreated(object sender, FileSystemEventArgs fileSystemEventArgs)
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

			while (numOfFtpFails < ConfigOptions.MaxNumberOfFtpFailsBeforeIgnoringFile 
				&& !ftpUploaded)
			{
				FtpWebResponse response = null;
				try
				{
					if (!WaitUntilReadyForFtpTransmission(fileSystemEventArgs.FullPath)) return;

					var ftpRequest = (FtpWebRequest) WebRequest.Create(ftpServerFullPath + fileSystemEventArgs.Name);
					ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
					ftpRequest.Credentials = new NetworkCredential(ConfigOptions.FtpUser, ConfigOptions.FtpPassword);
					// Copy the contents of the file to the request stream.
					var sourceStream = new StreamReader(fileSystemEventArgs.FullPath);
					var fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
					sourceStream.Close();
					ftpRequest.ContentLength = fileContents.Length;
					ftpRequest.KeepAlive = false;

					var requestStream = ftpRequest.GetRequestStream();
					requestStream.Write(fileContents, 0, fileContents.Length);
					requestStream.Close();

					response = (FtpWebResponse) ftpRequest.GetResponse();

					ftpUploaded = true;
					response.Close();
				}
				catch
				{
					Thread.Sleep(1000);
					numOfFtpFails++;
					if (!FtpDirectoryExists(ftpServerFullPath, ConfigOptions.FtpUser, ConfigOptions.FtpPassword))
						CreateFtpDirectory(ftpServerFullPath, ConfigOptions.FtpUser, ConfigOptions.FtpPassword);
				}
				finally
				{
					CloseFtpConnection(response);
				}
			}
		}

		private static void CloseFtpConnection(FtpWebResponse response)
		{
			if (response != null && response.StatusCode != FtpStatusCode.ConnectionClosed)
			{
				response.Close();
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
			FtpWebResponse response = null;
			try
			{
				var request = WebRequest.Create(fullPath);
				request.Method = WebRequestMethods.Ftp.MakeDirectory;
				request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
				response = (FtpWebResponse) request.GetResponse();
				response.Close();
			}
			finally
			{
				CloseFtpConnection(response);
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
			/* Create an FTP Request */
			var ftpRequest = (FtpWebRequest) FtpWebRequest.Create(ftpDirectoryPath);
			/* Log in to the FTP Server with the User Name and Password Provided */
			ftpRequest.Credentials = new NetworkCredential(ftpUser, ftpPassword);
			/* Specify the Type of FTP Request */
			ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
			FtpWebResponse response = null;
			try
			{
				response = (FtpWebResponse) ftpRequest.GetResponse();
				response.Close();
				return true;
			}
			catch
			{
				return false;
			}
			finally
			{
				CloseFtpConnection(response);
				ftpRequest = null;
			}
		}

		private bool WaitUntilReadyForFtpTransmission(string fullFileName)
		{
			var numberOfTries = 0;
			while (numberOfTries < ConfigOptions.MaxNumberOfTriesToReadTheFiles && !IsFileReady(fullFileName))
			{
				Thread.Sleep(ConfigOptions.WaitMillisecondsBetweenTryingToReadTheFile);
				numberOfTries++;
			}

			return IsFileReady(fullFileName);
		}

		public bool IsFileReady(String filename)
		{
			// If the file can be opened for exclusive access it means that the file
			// is no longer locked by another process.
			try
			{
				using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
				{
					return inputStream.Length > 0;
				}
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
