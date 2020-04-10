# SFTPSeedSync
Software to Download from the Seedbox and then Inform Radarr and Sonarr that the file has been downloaded
Scans the Server Periodically to download any movie files, skipping anything not a Video or Subtitle file

###Help Wanted
I'd really love to see someone who has a bit more time than me available to update a couple of things like 
- Turning the list of acceptable file types into a config setting
- Adjustable Rescan Period - Config Settings
- More Robust Download handler
- Auto Database Cleanup *Config Exists*
- Convert to .net Core so it can be run on Linux as well - *I Promise I've thought about it, But I'm running windows for my plex server*

##Config Settings

The default Config File contains all you need for Sonarr and Radarr from an FTP or SFTP server

```
 <AppConfig>
    <add key="Server" value="Localhost" />
    <add key="Username" value="UsernameHere" />
    <add key="Password" value="" />
	<add key="ServerType" value="Ftp" />
    <add key="FTPMode" value="Passive" />
    <add key="FTPSecure" value="None" />
    <add key="IgnoreSSHorTLSKey" value="True" />
    <add key="PortNumber" value="21" />
    <add key="CleanupMonths" value="2" />
	
    <add key="TempPath" value="C:\Videos\New Downloads\Synced\Temp\" />
    
    <add key="MaxThreads" value="2" />
    <add key="WindUpTime" value="10"/>
    
    <add key="SonarrAPIKey" value="SonarrAPI Key" />
    <add key="SonarrBaseURL" value="http://10.99.0.1:8989/" />
    <add key="SonarrServerPath" value="/media/sdl/username/private/deluge/Completed/sonarr" />
    <add key="SonarrLocalPath" value="C:\Videos\New Downloads\Synced\tv-sonarr\" />

    <add key="RadarrAPIKey" value="RadarrAPI Key" />
    <add key="RadarrBaseURL" value="http://10.99.0.1:7878/" />
    <add key="RadarrServerPath" value="/media/sdl/username/private/deluge/Completed/radarr" />
    <add key="RadarrLocalPath" value="C:\Videos\New Downloads\Synced\radarr" />
    
  </AppConfig>
```

**Server** - Just the Server URL
**Username** - Server Username
**Password** - Server Password

**ServerType** - Options for this are FTP|SFTP|SCP|WebDdav|S3
**FTPMode** - Options for this are Passive|Active
**FTPSecure** - Options for this are None|Implicit|Explicit

**IgnoreSSHorTLSKey** - Options are True|False - This just ignores SSL issues
**PortNumber** - FTP/SFTP Port

**CleanupMonths** - How long before we remove the data from the list of seen files and directories **Not Implemented**

**TempPath** - This is where the downloads will go initially, this prevents sonarr or Radarr from grabbing them halfway down

**MaxThreads** - This is how many downloads will happen simultanously 
**WindUpTime** - How many seconds to wait before grabbing the next file

**SonarrAPIKey** - This is the Sonarr API Key found in the Settings of Sonarr
**SonarrBaseURL** - Your Sonarr BaseURL - this is where we send the api requests
**SonarrServerPath** - Where your Sonarr downloads should be living after completetion I recommend utilizing the move when complete and then monitor that folder here
**SonarrLocalPath** - Where you want the Videos downloaded to

**RadarrAPIKey** - This is the Radarr API Key found in the Settings of Radarr
**RadarrBaseURL** - Your Radarr BaseURL - this is where we send the api requests
**RadarrServerPath** - Where your Radarr downloads should be living after completetion I recommend utilizing the move when complete and then monitor that folder here
**RadarrLocalPath** - Where you want the Videos downloaded to



