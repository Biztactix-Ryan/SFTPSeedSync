Imports WinSCP

Public Class SFTPMan
    Event DownloadComplete(Info As SFTPDownJobInfo)
    Event DownloadFailed(Info As SFTPDownJobInfo, Ex As Exception)
    Public DownInfo As SFTPDownJobInfo = New SFTPDownJobInfo
    Private RunCount As Integer = 0
    Dim WithEvents WSession As Session
    Class SFTPDownJobInfo
        Public Server As String
        Public Username As String
        Public Password As String
        Public File As String
        Public LocalFile As String
        Public SeedFile As SeedFileInfo
        Public Downloaded As ULong
        Public LastTimestamp As ULong
        Public LastDownloaded As New List(Of ULong)
        Public Speed As Integer
        Public ServerBasePath As String
        Public LocalBasePath As String
        Public Progress As Double


    End Class
    Public Sub New()

    End Sub
    Public Sub DownloadFile(Info As SFTPDownJobInfo)
        'Try
        '    'FTP
        '    'Dim ConnectionInfo = New ConnectionInfo(Info.Server, Info.Username, New PasswordAuthenticationMethod(Info.Username, Info.Password))
        '    'Dim client = New SftpClient(ConnectionInfo)
        '    'client.Connect()

        '    Dim client As New FtpClient(Info.Server)
        '    client.Credentials = New NetworkCredential(Info.Username, Info.Password)
        '    client.Connect()

        '    Dim newfileio As New System.IO.FileStream(Info.SeedFile.Name, IO.FileMode.OpenOrCreate)
        '    'client.DownloadFile(Info.SeedFile.FullPath, newfileio, AddressOf ProgressOfDownload)
        '    client.DownloadFile(Info.SeedFile.Name, Info.SeedFile.FullPath, existsMode:=FtpLocalExists.Overwrite)
        '    newfileio.Close()
        '    RaiseEvent DownloadComplete(Info)
        '    ' Check if the file is the right size?
        'Catch ex As Exception
        '    RaiseEvent DownloadFailed(Info)
        'End Try
        Try
            ' Setup session options
            Dim sessionOptions As New SessionOptions
            With sessionOptions
                .Protocol = Protocol.Ftp
                .HostName = Info.Server
                .UserName = Info.Username
                .Password = Info.Password

            End With

            WSession = New Session
            'WSession.Timeout = TimeSpan.FromSeconds(15)
            ' Connect
            WSession.Open(sessionOptions)

            ' Upload files
            Dim transferOptions As New TransferOptions
            transferOptions.TransferMode = TransferMode.Automatic
            transferOptions.ResumeSupport.State = TransferResumeSupportState.Smart
            transferOptions.OverwriteMode = OverwriteMode.Resume


            Dim transferResult As TransferOperationResult
            Info.LocalFile = CreateFolder(Info, Info.SeedFile.Name)

            transferResult = WSession.GetFiles(RemotePath.EscapeFileMask(Info.SeedFile.FullPath), Info.LocalFile, False, transferOptions)

            ' Throw on any error
            transferResult.Check()

            ' Print results
            If transferResult.IsSuccess Then RaiseEvent DownloadComplete(Info)

            WSession.Close()
            WSession = Nothing

        Catch e As Exception
            RaiseEvent DownloadFailed(Info, e)
        End Try
    End Sub

    Private Function CreateFolder(info As SFTPDownJobInfo, Filename As String) As String
        Dim Path = info.SeedFile.FullPath.Replace(info.ServerBasePath, "")
        Path = Path.Replace(Filename, "").Trim("/")
        Dim dir = System.IO.Directory.CreateDirectory(info.LocalBasePath & Path)
        Return System.IO.Path.Combine(dir.FullName, Filename)
        'Throw New NotImplementedException()
    End Function

    Private Sub ProgressOfDownload(obj As ULong)
        If RunCount = 10 Then
            RunCount = 0
            Dim TimeAmount = (Stopwatch.GetTimestamp - DownInfo.LastTimestamp) / Stopwatch.Frequency
            DownInfo.LastTimestamp = Stopwatch.GetTimestamp
            DownInfo.LastDownloaded.Add(obj - DownInfo.Downloaded)
            If TimeAmount > 0 Then DownInfo.Speed = Math.Round(((obj - DownInfo.Downloaded) / TimeAmount / 1024 / 8), 2)
            DownInfo.Downloaded = obj
            ' Debug.WriteLine(DownInfo.SeedFile.Name & " - " & DownInfo.Progress & "% - " & Speed & "KB/s")
        End If
        RunCount += 1
    End Sub

    Private Sub WSession_FileTransferProgress(sender As Object, e As FileTransferProgressEventArgs) Handles WSession.FileTransferProgress
        'If RunCount = 10 Then
        '    RunCount = 0
        DownInfo.Progress = e.FileProgress * 100
        DownInfo.Speed = e.CPS / 1024
        'End If
        'RunCount += 1
    End Sub

    Friend Sub Kill()
        Try
            Select Case WSession.Opened
                Case True
                    WSession.Close()
                    WSession.Dispose()
                    WSession = Nothing
                Case False
                    WSession.Dispose()
                    WSession = Nothing
            End Select
        Catch ex As Exception
        End Try
    End Sub
End Class
