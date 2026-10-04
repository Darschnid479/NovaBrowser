Imports System
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.Json

Namespace NovaBrowser
    Public NotInheritable Class StateStore
        Public Shared ReadOnly DataFolder As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NOVA-Browser")
        Public Shared ReadOnly WebDataFolder As String = Path.Combine(DataFolder, "WebView2")
        Private Shared ReadOnly StatePath As String = Path.Combine(DataFolder, "state.json")
        Private Shared ReadOnly Gate As New Object()
        Private Shared ReadOnly Options As New JsonSerializerOptions With {.WriteIndented = True}
        Public Shared Property LoadWarning As String = ""
        Private Sub New()
        End Sub

        Public Shared Function Load() As AppState
            Try
                Directory.CreateDirectory(DataFolder)
                If Not File.Exists(StatePath) Then Return New AppState()
                If New FileInfo(StatePath).Length > 8 * 1024 * 1024 Then Throw New InvalidDataException("Innstillingsfilen er for stor.")
                Dim state = JsonSerializer.Deserialize(Of AppState)(File.ReadAllText(StatePath, Encoding.UTF8), Options)
                If state Is Nothing OrElse state.SchemaVersion <> 1 Then Throw New InvalidDataException("Ukjent format.")
                If state.Settings Is Nothing Then state.Settings = New BrowserSettings()
                If state.Bookmarks Is Nothing Then state.Bookmarks = New Collections.Generic.List(Of PageEntry)()
                If state.History Is Nothing Then state.History = New Collections.Generic.List(Of PageEntry)()
                If state.Session Is Nothing Then state.Session = New Collections.Generic.List(Of SessionEntry)()
                state.Bookmarks = state.Bookmarks.Where(Function(p) p IsNot Nothing AndAlso UrlPolicy.IsWebUrl(p.Url)).Take(500).ToList()
                state.History = state.History.Where(Function(p) p IsNot Nothing AndAlso UrlPolicy.IsWebUrl(p.Url)).Take(500).ToList()
                state.Session = state.Session.Where(Function(p) p IsNot Nothing AndAlso (p.Url = UrlPolicy.HomeUrl OrElse UrlPolicy.IsWebUrl(p.Url))).Take(50).ToList()
                For Each p In state.Bookmarks.Concat(state.History)
                    p.Title = UrlPolicy.CleanTitle(p.Title)
                Next
                If Not SearchProviders.IsKnown(state.Settings.SearchEngine) Then
                    state.Settings.SearchEngine = "DuckDuckGo"
                    state.Settings.SetupCompleted = False
                End If
                state.Settings.DisplayName = UrlPolicy.CleanTitle(state.Settings.DisplayName)
                If state.Settings.DisplayName.Length > 32 Then state.Settings.DisplayName = state.Settings.DisplayName.Substring(0, 32)
                If Double.IsNaN(state.Settings.DefaultZoom) OrElse Double.IsInfinity(state.Settings.DefaultZoom) Then state.Settings.DefaultZoom = 1.0
                state.Settings.DefaultZoom = Math.Clamp(state.Settings.DefaultZoom, 0.5, 2.0)
                If Not state.Settings.RestoreSession Then state.Session.Clear()
                Return state
            Catch ex As Exception
                LogError(ex)
                LoadWarning = "Lagrede innstillinger kunne ikke leses. NOVA bruker standardvalg. En eventuell gammel fil blir bevart som sikkerhetskopi."
                Try
                    If File.Exists(StatePath) Then File.Copy(StatePath, StatePath & ".corrupt-" & DateTime.UtcNow.ToString("yyyyMMddHHmmss"), True)
                Catch backupError As IOException
                    LogError(backupError)
                End Try
                Return New AppState()
            End Try
        End Function

        Public Shared Function Save(state As AppState) As Boolean
            SyncLock Gate
                Try
                    Directory.CreateDirectory(DataFolder)
                    Dim temp = StatePath & ".tmp"
                    File.WriteAllText(temp, JsonSerializer.Serialize(state, Options), New UTF8Encoding(False))
                    If File.Exists(StatePath) Then
                        File.Replace(temp, StatePath, Nothing)
                    Else
                        File.Move(temp, StatePath)
                    End If
                    Return True
                Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is JsonException
                    LogError(ex)
                    Return False
                End Try
            End SyncLock
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
