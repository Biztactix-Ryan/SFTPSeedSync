Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Threading
Imports System.Timers
Imports WinSCP

Public Class SFTPSeedSync
    Dim Mystats As New StatsStructure
    Dim Downloads As New List(Of SFTPMan)
    Dim ExistingDirs As List(Of SeedDirInfo)
    Dim ExistingFiles As List(Of SeedFileInfo)
    Dim ToDownload As New Queue(Of SeedFileInfo)
    Dim AppConfig As New AppConfig
    Dim WithEvents SecondCounter As New System.Timers.Timer(1000)
    Dim WithEvents FullScanScounter As New System.Timers.Timer(60000)
    Dim FullScanCount As Integer = 0
    Dim Session As New Session
    Private _threadlock As New Object

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        AppConfig.Initialize()
    End Sub



    Protected Overrides Sub OnStart(ByVal args() As String)
        LogIT.Info("Starting Up")
        If AppConfig.Username = "<UsernameHere>" Then
            LogIT.Error("Check your Application Config File")
            End
        End If
        LoadInProgress()
        FullScan()
        SecondCounter.Start()
        FullScanScounter.Start()
        If Environment.UserInteractive Then
            Do
                If Downloads.Count > 0 Then
                    Try

                        Console.Clear()
                        Dim TotalSpeed As Double = 0
                        Dim TempList As New Dictionary(Of String, Integer)
                        Dim TempThing As New List(Of SFTPMan)
                        For i As Integer = 0 To Downloads.Count - 1
                            If TempList.ContainsKey(Downloads(i).DownInfo.SeedFile.Name) Then Continue For
                            TempThing.Add(Downloads(i))
                            TempList.Add(Downloads(i).DownInfo.SeedFile.Name, i)
                            TotalSpeed += Downloads(i).DownInfo.Speed / 1024
                        Next
                        Dim sorted = From item In TempList Order By item.Key Select item.Value
                        Dim j As Integer = 0
                        Console.WriteLine("{0,-85}{1,-10}{2,15}", "Filename", "Progress", "Speed KB/s")
                        For Each i In sorted
                            If j < 10 Then Console.WriteLine("{0,-85}{1,-10}{2,15}", TempThing(i).DownInfo.SeedFile.Name.PadRight(80, " ").Substring(0, 80), TempThing(i).DownInfo.Progress & "%", TempThing(i).DownInfo.Speed & "KB/s")
                            j += 1
                        Next
                        Console.WriteLine("Total Speed: {0} MB/s", TotalSpeed)
                        System.Threading.Thread.Sleep(500)
                    Catch ex As Exception
                        LogIT.Error(ex, "Problem with Output to Console")
                        System.Threading.Thread.Sleep(1000)
                    End Try
                Else
                    Console.Clear()
                    Console.WriteLine("Nothing to Download - Waiting")
                    System.Threading.Thread.Sleep(5000)
                End If
            Loop While True
        End If
    End Sub

    Private Sub FullScan()
        Dim SW As New Stopwatch
        SW.Start()
        Try
            ' Setup session options
            Dim sessionOptions As New SessionOptions
            With sessionOptions
                .Protocol = AppConfig.ServerType
                .HostName = AppConfig.Server
                .UserName = AppConfig.Username
                .Password = AppConfig.Password
            End With

            ' Connect
            Session.Open(sessionOptions)

        Catch e As Exception

            LogIT.Error(e)
        End Try


        Dim filelist As New List(Of SeedFileInfo)
        Dim DirList As New Queue(Of String)
        ExistingDirs = LoadDirs()
        Mystats.ExistingDirs = ExistingDirs.Count
        ExistingFiles = LoadFiles()
        Mystats.ExistingFiles = ExistingFiles.Count
        ToDownload = LoadDownloads()
        Dim Sonarr As Boolean = True
        For j As Integer = 0 To 1
            If Sonarr Then
                DirList.Enqueue(AppConfig.SonarrServerPath)
            Else
                DirList.Enqueue(AppConfig.RadarrServerPath)
            End If

            LogIT.Info("Starting Directory Listing Phase - " & IIf(Sonarr, "Sonarr Folder", "Radarr Folder"))
            Do Until DirList.Count = 0
                Dim directory As RemoteDirectoryInfo = Session.ListDirectory(DirList.Dequeue)
                For Each file As RemoteFileInfo In directory.Files
                    If file.Name = "." Then Continue For
                    If file.Name = ".." Then Continue For
                    If file.FileType = "D" Then
                        Dim Found As Boolean = False
                        For i As Integer = 0 To ExistingDirs.Count - 1
                            If ExistingDirs(i).FullPath = file.FullName Then
                                If ExistingDirs(i).LastWrite = file.LastWriteTime Then
                                    Found = True
                                    Mystats.SkippedDirs += 1
                                    Exit For
                                Else
                                    Mystats.ChangedDirs += 1
                                    LogIT.Info("Directory has changed - " & file.FullName)
                                    ExistingDirs(i).LastWrite = file.LastWriteTime
                                    ExistingDirs(i).UserID = file.Owner
                                    DirList.Enqueue(file.FullName)
                                    Found = True
                                    Exit For
                                End If

                            End If
                        Next
                        If Found = True Then Continue For
                        Dim SDI As New SeedDirInfo
                        SDI.Name = file.Name
                        SDI.FullPath = file.FullName
                        SDI.LastWrite = file.LastWriteTime
                        SDI.UserID = file.Owner
                        SDI.Sonarr = Sonarr
                        ExistingDirs.Add(SDI)
                        DirList.Enqueue(file.FullName)
                        Mystats.NewDirs += 1
                    Else
                        If file.FileType = "-" Then
                            Dim Found As Boolean = False
                            'LogIT.Debug(String.Format("{0} - {1} - {2}", file.Name, GetFileExtension(file.Name), IsVideoExtension(GetFileExtension(file.Name))))
                            If IsVideoExtension(GetFileExtension(file.Name)) Then
                                For i As Integer = 0 To ExistingFiles.Count - 1
                                    If ExistingFiles(i).FullPath = file.FullName Then
                                        If ExistingFiles(i).LastWrite = file.LastWriteTime Then
                                            Found = True
                                            Mystats.SkippedFiles += 1
                                            Exit For
                                        Else
                                            Mystats.ChangedFiles += 1
                                            LogIT.Info("File has changed - " & file.FullName)
                                            ExistingFiles(i).length = file.Length
                                            ExistingFiles(i).LastWrite = file.LastWriteTime
                                            ExistingFiles(i).UserID = file.Owner
                                            ToDownload.Enqueue(ExistingFiles(i))
                                            Found = True
                                            Exit For
                                        End If

                                    End If
                                Next
                                If Found = True Then Continue For
                                Dim SFI As New SeedFileInfo
                                SFI.Name = file.Name
                                SFI.FullPath = file.FullName
                                SFI.length = file.Length
                                SFI.LastWrite = file.LastWriteTime
                                SFI.UserID = file.Owner
                                SFI.Extension = GetFileExtension(file.Name)
                                SFI.Sonarr = Sonarr
                                ExistingFiles.Add(SFI)
                                ToDownload.Enqueue(SFI)
                                Mystats.NewFiles += 1
                            End If
                        End If


                    End If
                Next
            Loop
            Sonarr = Not Sonarr
        Next j
        Session.Close()
        SW.Stop()
        SaveDirs(ExistingDirs)
        SaveFiles(ExistingFiles)
        SaveDownloads(ToDownload)
        LogIT.Info("Run Completed - Total Time: " & SW.Elapsed.TotalSeconds & " Seconds")
        LogIT.Info("Stats: Found {0} New Files, {1} New Directories", Mystats.NewFiles, Mystats.NewDirs)
        LogIT.Info("Stats: {0} Changed Directories, {1} Changed Files", Mystats.ChangedDirs, Mystats.ChangedFiles)
        LogIT.Info("Stats: {0} Existing Directories, {1} Existing Files Skipped", Mystats.ExistingDirs, Mystats.ExistingFiles)
        CleanupLocalFolder()

    End Sub

    Private Sub LoadInProgress()
        ' Load Already in Progress Downloads
        LogIT.Info("Loading In Progress Downloads")
        Dim counter As Integer = 0
        Dim InProgress As List(Of SeedFileInfo) = LoadProgress()
        If InProgress.Count > 0 Then
            For Each file In InProgress
                Dim DontAdd As Boolean = False
                For i As Integer = 0 To ToDownload.Count - 1
                    If file.FullPath = ToDownload.ToList(i).FullPath Then DontAdd = True
                Next
                If DontAdd Then Continue For
                Dim Path As String = file.FullPath.Replace(AppConfig.TempPath, "")
                Path = Path.Replace(file.Name, "").Trim("/")
                Dim Dir As DirectoryInfo
                If file.Sonarr Then
                    Dir = System.IO.Directory.CreateDirectory(AppConfig.SonarrLocalPath & Path)
                Else
                    Dir = System.IO.Directory.CreateDirectory(AppConfig.RadarrLocalPath & Path)
                End If

                Dim LocalFile = System.IO.Path.Combine(dir.FullName, file.Name)
                If System.IO.File.Exists(LocalFile) Then
                    Dim fi As New System.IO.FileInfo(LocalFile)
                    If fi.Length = file.length Then
                        Dim CompletedFileLocation As String
                        If file.Sonarr Then
                            CompletedFileLocation = System.IO.Path.Combine(AppConfig.SonarrLocalPath, LocalFile.Replace(AppConfig.TempPath, ""))
                        Else
                            CompletedFileLocation = System.IO.Path.Combine(AppConfig.RadarrLocalPath, LocalFile.Replace(AppConfig.TempPath, ""))
                        End If

                        System.IO.Directory.CreateDirectory(CompletedFileLocation.Replace(file.Name, ""))
                        LogIT.Info("Moving Completed Download to {0}", CompletedFileLocation)
                        System.IO.File.Move(LocalFile, CompletedFileLocation)
                    Else
                        ToDownload.Enqueue(file)
                        counter += 1
                        SaveDownloads(ToDownload)
                    End If
                Else
                    ToDownload.Enqueue(file)
                    counter += 1
                    SaveDownloads(ToDownload)
                End If
            Next
        End If
        SaveDownloads(ToDownload)
        SaveProgress()
        LogIT.Info(String.Format("Loaded {0} In Progress Downloads", counter))
    End Sub

    Private Function IsVideoExtension(v As String) As Boolean
        Dim VideoExtensions() As String = {"AVI", "WMV", "MP4", "M4A", "MOV", "MPG", "M4V", "DIVX", "FLV", "MKV"}
        Dim SubtitleExtensions() As String = {"SRT", "SUB"}
        If VideoExtensions.Contains(v.Trim(".").ToUpper) Then Return True
        Return False
    End Function

    Private Shared Function GetFileExtension(Name As String) As String
        If Name.LastIndexOf(".") >= 1 Then
            Return Name.Substring(Name.LastIndexOf(".") + 1)
        Else
            Return ""
        End If
    End Function

    Private Sub KickOffDownloads()
        If ToDownload.Count = 0 Then Exit Sub
        If Downloads.Count = AppConfig.MaxThreads Then Exit Sub
        ThreadPool.SetMaxThreads(AppConfig.MaxThreads + 5, 1000)
        'LogIT.Info("Starting Download Cycle")
        Dim Thread1 As New SFTPMan
        Thread1.DownInfo.SeedFile = ToDownload.Dequeue
        Thread1.DownInfo.Server = AppConfig.Server
        Thread1.DownInfo.Username = AppConfig.Username
        Thread1.DownInfo.Password = AppConfig.Password
        If Thread1.DownInfo.SeedFile.Sonarr Then
            Thread1.DownInfo.ServerBasePath = AppConfig.SonarrServerPath
            Thread1.DownInfo.LocalBasePath = AppConfig.TempPath
        Else
            Thread1.DownInfo.ServerBasePath = AppConfig.RadarrServerPath
            Thread1.DownInfo.LocalBasePath = AppConfig.TempPath
        End If

        Downloads.Add(Thread1)
        AddHandler Thread1.DownloadComplete, AddressOf DownloadFinished
        AddHandler Thread1.DownloadFailed, AddressOf DownloadFailed

        ThreadPool.QueueUserWorkItem(AddressOf Thread1.DownloadFile, Thread1.DownInfo)
        LogIT.Info("Started New Download - " & Thread1.DownInfo.SeedFile.Name)
        SaveDownloads(ToDownload)
        SaveProgress()
    End Sub

    Private Sub DownloadFailed(Info As SFTPMan.SFTPDownJobInfo, ex As Exception)
        LogIT.Error(ex, "Download Failed - " & Info.SeedFile.Name)
        ToDownload.Enqueue(Info.SeedFile)
        SaveDownloads(ToDownload)
        For i As Integer = 0 To Downloads.Count - 1
            If Downloads(i).DownInfo.SeedFile.FullPath = Info.SeedFile.FullPath Then
                Downloads(i).Kill()
                Downloads.Remove(Downloads(i))
                Exit For
            End If
        Next
        SaveProgress()
    End Sub

    Private Sub DownloadFinished(Info As SFTPMan.SFTPDownJobInfo)
        LogIT.Info("Download Completed - " & Info.SeedFile.Name)
        For i As Integer = 0 To Downloads.Count - 1
            If Downloads(i).DownInfo.SeedFile.FullPath = Info.SeedFile.FullPath Then
                Downloads.Remove(Downloads(i))
                Exit For
            End If
        Next
        SaveDownloads(ToDownload)
        SaveProgress()
        MoveCompletedFile(Info)
        CleanupLocalFolder()
        System.Threading.Thread.Sleep(AppConfig.WindUpTime * 1000)

    End Sub

    Private Sub AlertSonarr(localPath As String)
        Try
            Dim wr As WebRequest = WebRequest.Create(AppConfig.SonarrBaseURL & "/api/command")
            Dim wh As New WebHeaderCollection
            wh.Add("X-Api-Key", AppConfig.SonarrAPIKey)
            wr.Headers = wh
            wr.Method = "POST"
            Dim postData As String = String.Format("{{name:'downloadedepisodesscan','path':'{0}'}}", localPath.Replace("\", "\\"))
            LogIT.Trace(postData)
            Dim byteArray As Byte() = Encoding.UTF8.GetBytes(postData)
            wr.ContentType = "application/x-www-form-urlencoded"
            wr.ContentLength = byteArray.Length
            Dim dataStream As Stream = wr.GetRequestStream()
            dataStream.Write(byteArray, 0, byteArray.Length)
            dataStream.Close()
            Dim Response = wr.GetResponse().GetResponseStream
            Dim SR As New StreamReader(Response)
            LogIT.Trace(SR.ReadToEnd())
            SR.Close()
            Response.Close()
        Catch ex As Exception
            LogIT.Error(ex, "Alert Sonarr Error")
        End Try
    End Sub
    Private Sub AlertRadarr(localPath As String)
        Try
            Dim wr As WebRequest = WebRequest.Create(AppConfig.RadarrBaseURL & "/api/command")
            Dim wh As New WebHeaderCollection
            wh.Add("X-Api-Key", AppConfig.RadarrAPIKey)
            wr.Headers = wh
            wr.Method = "POST"
            Dim postData As String = String.Format("{{name:'DownloadedMoviesScan','path':'{0}'}}", localPath.Replace("\", "\\"))
            LogIT.Trace(postData)
            Dim byteArray As Byte() = Encoding.UTF8.GetBytes(postData)
            wr.ContentType = "application/x-www-form-urlencoded"
            wr.ContentLength = byteArray.Length
            Dim dataStream As Stream = wr.GetRequestStream()
            dataStream.Write(byteArray, 0, byteArray.Length)
            dataStream.Close()
            Dim Response = wr.GetResponse().GetResponseStream
            Dim SR As New StreamReader(Response)
            LogIT.Trace(SR.ReadToEnd())
            SR.Close()
            Response.Close()
        Catch ex As Exception
            LogIT.Error(ex, "Alert Sonarr Error")
        End Try
    End Sub

    Private Sub MoveCompletedFile(Info As SFTPMan.SFTPDownJobInfo)
        Try
            Dim CompletedFileLocation As String
            If Info.SeedFile.Sonarr Then
                CompletedFileLocation = System.IO.Path.Combine(AppConfig.SonarrLocalPath, Info.LocalFile.Replace(Info.LocalBasePath, ""))
            Else
                CompletedFileLocation = System.IO.Path.Combine(AppConfig.RadarrLocalPath, Info.LocalFile.Replace(Info.LocalBasePath, ""))
            End If

            System.IO.Directory.CreateDirectory(CompletedFileLocation.Replace(Info.SeedFile.Name, ""))
                If System.IO.File.Exists(CompletedFileLocation) Then
                    If New System.IO.FileInfo(CompletedFileLocation).Length >= New System.IO.FileInfo(Info.LocalFile).Length Then
                        LogIT.Info("Don't want to move smaller File over Larger File, Manual Intervention Required - {0}", CompletedFileLocation)
                        Exit Sub
                    Else
                        LogIT.Info("Deleting Smaller File to Copy New File over the top - {0}", CompletedFileLocation)
                        System.IO.File.Delete(CompletedFileLocation)
                    End If
                End If
                LogIT.Info("Moving Completed Download to {0}", CompletedFileLocation)
            System.IO.File.Move(Info.LocalFile, CompletedFileLocation)
            If Info.SeedFile.Sonarr Then
                LogIT.Info("Alerting Sonarr to New File {0}", CompletedFileLocation)
                AlertSonarr(CompletedFileLocation)
            Else
                LogIT.Info("Alerting Radarr to New File {0}", CompletedFileLocation)
                AlertRadarr(CompletedFileLocation)
            End If

        Catch ex As Exception
            LogIT.Error(ex, "Failed to Move Completed File")
        End Try
    End Sub

    Protected Overrides Sub OnStop()
        ' Add code here to perform any tear-down necessary to stop your service.
    End Sub

    Private Sub DebugStart()
        OnStart(Nothing)
    End Sub

    Public Sub SaveDirs(Dirs As List(Of SeedDirInfo))
        SyncLock _threadlock
            Dim serializer As New Xml.Serialization.XmlSerializer(GetType(List(Of SeedDirInfo)))
            Dim fs As New IO.FileStream("SeenDirs.xml", IO.FileMode.Create)
            serializer.Serialize(fs, Dirs)
            fs.Close()
        End SyncLock
    End Sub
    Public Sub SaveFiles(Files As List(Of SeedFileInfo))
        SyncLock _threadlock
            Dim serializer As New Xml.Serialization.XmlSerializer(GetType(List(Of SeedFileInfo)))
            Dim fs As New IO.FileStream("SeenFiles.xml", IO.FileMode.Create)
            serializer.Serialize(fs, Files)
            fs.Close()
        End SyncLock
    End Sub
    Public Sub SaveDownloads(Files As Queue(Of SeedFileInfo))
        SyncLock _threadlock
            Dim serializer As New Xml.Serialization.XmlSerializer(GetType(List(Of SeedFileInfo)))
            Dim fs As New IO.FileStream("Downloads.xml", IO.FileMode.Create)
            serializer.Serialize(fs, Files.ToList)
            fs.Close()
        End SyncLock
    End Sub
    Public Sub SaveProgress()
        SyncLock _threadlock
            Dim Files As New List(Of SeedFileInfo)
            For Each tmpthread In Downloads
                Files.Add(tmpthread.DownInfo.SeedFile)
            Next
            Dim serializer As New Xml.Serialization.XmlSerializer(GetType(List(Of SeedFileInfo)))
            Dim fs As New IO.FileStream("InProgress.xml", IO.FileMode.Create)
            serializer.Serialize(fs, Files.ToList)
            fs.Close()
        End SyncLock
    End Sub
    Public Function LoadProgress() As List(Of SeedFileInfo)
        SyncLock _threadlock
            Dim Files = New List(Of SeedFileInfo)
            If Not System.IO.File.Exists("InProgress.xml") Then
                Return Files
            End If
            Dim serializer As New Xml.Serialization.XmlSerializer(GetType(List(Of SeedFileInfo)))
            Dim SR As New IO.StreamReader("InProgress.xml")
            Files = DirectCast(serializer.Deserialize(SR), List(Of SeedFileInfo))
            SR.Close()
            Return Files
        End SyncLock
    End Function
    Public Function LoadFiles() As List(Of SeedFileInfo)
        SyncLock _threadlock
            Dim Files = New List(Of SeedFileInfo)
            If Not System.IO.File.Exists("SeenFiles.xml") Then
                Return Files
            End If
            Dim serializer As New Xml.Serialization.XmlSerializer(GetType(List(Of SeedFileInfo)))
            Dim SR As New IO.StreamReader("SeenFiles.xml")
            Files = DirectCast(serializer.Deserialize(SR), List(Of SeedFileInfo))
            SR.Close()
            Return Files
        End SyncLock
    End Function
    Public Function LoadDownloads() As Queue(Of SeedFileInfo)
        SyncLock _threadlock
            Dim Files = New List(Of SeedFileInfo)
            If Not System.IO.File.Exists("Downloads.xml") Then
                Return New Queue(Of SeedFileInfo)(Files)
            End If
            Dim serializer As New Xml.Serialization.XmlSerializer(GetType(List(Of SeedFileInfo)))
            Dim SR As New IO.StreamReader("Downloads.xml")
            Files = DirectCast(serializer.Deserialize(SR), List(Of SeedFileInfo))
            SR.Close()
            Return New Queue(Of SeedFileInfo)(Files)
        End SyncLock
    End Function
    Public Function LoadDirs() As List(Of SeedDirInfo)
        SyncLock _threadlock
            Dim Dirs = New List(Of SeedDirInfo)
            If Not System.IO.File.Exists("SeenDirs.xml") Then
                Return Dirs
            End If
            Dim serializer As New Xml.Serialization.XmlSerializer(GetType(List(Of SeedDirInfo)))
            Dim SR As New IO.StreamReader("SeenDirs.xml")
            Dirs = DirectCast(serializer.Deserialize(SR), List(Of SeedDirInfo))
            SR.Close()
            Return Dirs
        End SyncLock
    End Function

    Private Sub SecondCounter_Elapsed(sender As Object, e As ElapsedEventArgs) Handles SecondCounter.Elapsed
        If Downloads.Count < AppConfig.MaxThreads Then KickOffDownloads()

    End Sub

    Private Sub FullScanScounter_Elapsed(sender As Object, e As ElapsedEventArgs) Handles FullScanScounter.Elapsed
        If FullScanCount = 6 Then
            FullScan()
            FullScanCount = 0
        End If
        FullScanCount += 1
    End Sub

    Public Sub CleanupLocalFolder()
        Dim listofDirs As New Queue(Of String)
        listofDirs.Enqueue(AppConfig.SonarrLocalPath)
        listofDirs.Enqueue(AppConfig.RadarrLocalPath)
        Do Until listofDirs.Count = 0
            Dim myDir As System.IO.DirectoryInfo = New System.IO.DirectoryInfo(listofDirs.Dequeue)
            Dim NoDirs As Boolean = True
            For Each tmpDir In myDir.EnumerateDirectories
                listofDirs.Enqueue(tmpDir.FullName)
                NoDirs = False
            Next
            If myDir.EnumerateFiles.Count = 0 And NoDirs Then
                If myDir.FullName = AppConfig.SonarrLocalPath Then

                ElseIf myDir.FullName = AppConfig.RadarrLocalPath Then

                Else
                    LogIT.Info(String.Format("Deleting Empty Folder - {0}", myDir.FullName))
                    myDir.Delete()
                End If

            End If
        Loop
    End Sub
End Class

Public Class StatsStructure
    Property SkippedDirs As Integer
    Property SkippedFiles As Integer
    Property ExistingFiles As Integer
    Property ExistingDirs As Integer
    Property NewFiles As Integer
    Property NewDirs As Integer
    Property ChangedDirs As Integer
    Property ChangedFiles As Integer
End Class


