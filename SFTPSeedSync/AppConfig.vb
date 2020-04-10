Public Class AppConfig
    Inherits Westwind.Utilities.Configuration.AppConfiguration

    Public Property ServerType As WinSCP.Protocol
    Public Property Server As String
    Public Property Username As String
    Public Property Password As String


    Public Property TempPath As String

    Public Property MaxThreads As Integer
    Public Property FTPMode As WinSCP.FtpMode
    Public Property FTPSecure As WinSCP.FtpSecure
    Public Property IgnoreSSHorTLSKey As Boolean
    Public Property PortNumber As Integer
    Public Property WindUpTime As Integer

    Public Property CleanupMonths As Integer

    Public Property SonarrBaseURL As String
    Public Property SonarrAPIKey As String
    Public Property SonarrServerPath As String
    Public Property SonarrLocalPath As String

    Public Property RadarrBaseURL As String
    Public Property RadarrAPIKey As String
    Public Property RadarrServerPath As String
    Public Property RadarrLocalPath As String



    Public Sub New()
        Server = "Localhost"
        Username = "<UsernameHere>"
        Password = "Password"

        MaxThreads = 10
        ServerType = WinSCP.Protocol.Ftp
        FTPMode = WinSCP.FtpMode.Passive
        FTPSecure = WinSCP.FtpSecure.None
        IgnoreSSHorTLSKey = True
        PortNumber = 21
        WindUpTime = 10
        CleanupMonths = 2

    End Sub

End Class
