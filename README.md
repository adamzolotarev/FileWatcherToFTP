# FileWatcherToFTP
Watch local folder for new files, upload them to FTP

Starts as a windows app without any visible windows.
Use it by itself or use it as a Windows service with nssm (the Non-Sucking Service Manager)

Use App.config to set up options:
* MonitorFolder local folder to monitor for newly files
* FtpServer - ftp server name
* FtpUser - ftp server user
* FtpPassword - ftp password
* WaitMinutesBetweenTryingToReadTheFile - will retry to ftp a file after n minutes when it is locked.
* MaxNumberOfTriesToReadTheFiles - if a file is locked will try to read it it n times; if fails - forget about it.
* ShouldUseYearAsFtpPath - will generate current year as a part of upload path (e.g. ftp://ftpserver/2015/)
* ShouldUseMonthAsFtpPath - will generate current month as a part of upload path (e.g. ftp://ftpserver/2015/1)
* ShouldUseDayAsFtpPath - will generate current day as a part of upload path (e.g. ftp://ftpserver/2015/1/15)
* MaxNumberOfFtpConnections - will create up to n ftp connections
