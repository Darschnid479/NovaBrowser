Imports System
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.Json

Namespace NovaBrowser
    Public NotInheritable Class StateStore
        Public Shared ReadOnly DataFolder As String = ResolveDataFolder()
        Public Shared ReadOnly WebDataFolder As String = Path.Combine(DataFolder, "WebView2")
        Private Shared ReadOnly StatePath As String = Path.Combine(DataFolder, "state.json")
        Private Shared ReadOnly Gate As New Object()
        Private Shared ReadOnly Options As New JsonSerializerOptions With {.WriteIndented = True}
        Public Shared Property LoadWarning As String = ""
        Private Shared ReadOnly Repository As New ProfileRepository(DataFolder)
        Private Shared Function ResolveDataFolder() As String
            Dim custom = Environment.GetEnvironmentVariable("NOVA_PROFILE_ROOT")
            If Not String.IsNullOrWhiteSpace(custom) AndAlso Path.IsPathRooted(custom) Then Return Path.GetFullPath(custom)
            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NOVA-Browser")
        End Function
        Private Sub New()
        End Sub

        Public Shared Function Load() As AppState
            Try
                Dim state = Repository.Load()
                LoadWarning = Repository.Warning
                Return state
            Catch ex As Exception
                LogError(ex)
                LoadWarning = "Profilen kunne ikke leses. Kontroller tilgang til datamappen."
                Return New AppState()
            End Try
        End Function
        Public Shared Function Save(state As AppState) As Boolean
            Try
                Repository.Save(state)
                Return True
            Catch ex As Exception
                LogError(ex)
                Return False
            End Try
        End Function
        Public Shared Function ForgetRecoveryCopies() As Boolean
            Try
                Repository.ForgetRecoveryCopies()
                Return True
            Catch ex As Exception
                LogError(ex)
                Return False
            End Try
        End Function

        Public Shared Sub LogError(ex As Exception)
            If ex Is Nothing Then Return
            SyncLock Gate
                Try
                    Directory.CreateDirectory(DataFolder)
                    Dim logPath As String = Path.Combine(DataFolder, "error.log")
                    If File.Exists(logPath) AndAlso New FileInfo(logPath).Length > 512 * 1024 Then
                        File.Move(logPath, Path.Combine(DataFolder, "error.previous.log"), True)
                    End If
                    File.AppendAllText(logPath, ErrorDiagnostics.FormatException(ex), New UTF8Encoding(False))
                Catch
                    ' Error reporting must never create a second failure.
                End Try
            End SyncLock
        End Sub

    End Class
End Namespace
