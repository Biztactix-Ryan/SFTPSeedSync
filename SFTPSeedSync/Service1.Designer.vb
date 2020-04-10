Imports System.ComponentModel
Imports System.ServiceProcess
Imports System.Threading
Imports NLog

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class SFTPSeedSync
    Inherits System.ServiceProcess.ServiceBase
    Private Shared LogIT As NLog.Logger

    'UserService overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    ' The main entry point for the process
    <MTAThread()>
    <System.Diagnostics.DebuggerNonUserCode()>
    Shared Sub Main(args As String())
        ConfigureLogging()
        Dim ServicesToRun() As ServiceBase
        Dim currentDomain As AppDomain = AppDomain.CurrentDomain
        AddHandler currentDomain.UnhandledException, AddressOf MYExnHandler


        Try
            Dim path As String = System.Reflection.Assembly.GetExecutingAssembly().Location
            Dim commandLine As String() = Nothing
            Dim msgText As String = ""
            If args.Length > 0 Then
                Select Case args(0).ToUpper()
                    Case "/INSTALL"
                        commandLine = New String() {path}
                    '    msgText = "Service installed sucessfully! Please start it from the Services control panel."
                    Case "/UNINSTALL"
                        commandLine = New String() {"/u", path}
                        '   msgText = "Service uninstalled sucessfully!"
                    Case Else
                        Throw New ArgumentException("Invalid command line argument.")
                End Select
                System.Configuration.Install.ManagedInstallerClass.InstallHelper(commandLine)
                ' MsgBox(msgText, MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "PowerAdmin Service Install")
            Else

                If Environment.UserInteractive Then
                    LogIT.Info("User Interactive Startup")
                    Dim service As New SFTPSeedSync
                    service.DebugStart()
                Else
                    LogIT.Info("Service Startup")
                    ServicesToRun = New ServiceBase() {New SFTPSeedSync()}
                    System.ServiceProcess.ServiceBase.Run(ServicesToRun)
                End If
            End If

        Catch ex As Exception
            If ex.InnerException Is Nothing Then
                LogIT.Error(ex, "Error Starting Up")
            Else
                If CType(ex.InnerException, Win32Exception).NativeErrorCode = 1073 Then
                    LogIT.Info("Service Already Exists - Exiting")
                Else
                    LogIT.Error(ex, "Error Starting Up")
                End If
            End If

        End Try
    End Sub


    Private Shared Sub MYExnHandler(ByVal sender As Object, ByVal e As UnhandledExceptionEventArgs)
        Dim EX As Exception
        EX = e.ExceptionObject
        LogIT.Error(EX, "Uncaught Standard Exception")
    End Sub

    Private Shared Sub MYThreadHandler(ByVal sender As Object, ByVal e As Threading.ThreadExceptionEventArgs)
        LogIT.Error(e.Exception, "Uncaught Thread Error")
    End Sub

    'Required by the Component Designer
    Private components As System.ComponentModel.IContainer

    ' NOTE: The following procedure is required by the Component Designer
    ' It can be modified using the Component Designer.  
    ' Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        '
        'SFTPSeedSync
        '
        Me.ServiceName = "SFTPSeedSync"

    End Sub

    Private Shared Sub ConfigureLogging()
        Dim config = New NLog.Config.LoggingConfiguration()
        Dim logfile = New NLog.Targets.FileTarget("logfile") With {
        .FileName = "Log.txt", .Layout = "${longdate}|${level:uppercase=true}|${logger}|${threadid}|${message}|${exception:format=tostring}",
        .ArchiveNumbering = Targets.ArchiveNumberingMode.Date, .MaxArchiveFiles = 5, .ArchiveFileName = "Log.{#}.txt", .ArchiveEvery = Targets.FileArchivePeriod.Day, .ArchiveDateFormat = "yyyyMMdd"
    }
        Dim logfile1 = New NLog.Targets.FileTarget("logfile") With {
        .FileName = "TraceLog.txt", .ArchiveNumbering = Targets.ArchiveNumberingMode.Date, .MaxArchiveFiles = 5, .ArchiveFileName = "TraceLog.{#}.txt", .ArchiveEvery = Targets.FileArchivePeriod.Day, .ArchiveDateFormat = "yyyyMMdd"
    }
        Dim logconsole = New NLog.Targets.ConsoleTarget("logconsole")
        config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole)
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile)
        config.AddRule(LogLevel.Trace, LogLevel.Debug, logfile1)
        NLog.LogManager.Configuration = config
        LogIT = NLog.LogManager.GetCurrentClassLogger()
    End Sub



End Class
