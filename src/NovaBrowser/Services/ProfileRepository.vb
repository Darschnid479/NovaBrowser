Imports System
Imports System.IO
Imports System.Text
Imports System.Text.Json

Namespace NovaBrowser
    ' Local plaintext settings, not a credential vault. The browser never passes
    ' page-supplied paths here. Tests supply a disposable, isolated directory.
    Public NotInheritable Class ProfileRepository
        Private ReadOnly _folder As String
        Private ReadOnly _statePath As String
        Private ReadOnly _gate As New Object()
        Private ReadOnly _json As New JsonSerializerOptions With {.WriteIndented = True}
        Public Property Warning As String = ""
        Public Sub New(folder As String)
            If String.IsNullOrWhiteSpace(folder) OrElse Not Path.IsPathRooted(folder) Then Throw New ArgumentException("Profilstien må være absolutt.", NameOf(folder))
            _folder = Path.GetFullPath(folder)
            _statePath = Path.Combine(_folder, "state.json")
        End Sub
        Public Function Load() As AppState
            SyncLock _gate
                Warning = ""
                Directory.CreateDirectory(_folder)
                If Not File.Exists(_statePath) Then Return New AppState()
                Try
                    Return ReadState(_statePath)
                Catch ex As Exception When Recoverable(ex)
                    Try
                        File.Copy(_statePath, _statePath & ".corrupt-" & Guid.NewGuid().ToString("N"), False)
                    Catch copyError As Exception When Recoverable(copyError)
                        ' Keep the existing primary untouched until a later save.
                    End Try
                    Try
                        Dim recovered = ReadState(_statePath & ".bak")
                        Warning = "Profilen ble gjenopprettet fra siste gyldige sikkerhetskopi."
                        Return recovered
                    Catch backupError As Exception When Recoverable(backupError)
                        Warning = "Profilen kunne ikke leses. NOVA bruker standardvalg. Den gamle filen er ikke slettet."
                        Return New AppState()
                    End Try
                End Try
            End SyncLock
        End Function
        Private Function ReadState(filePath As String) As AppState
            If New FileInfo(filePath).Length > 8 * 1024 * 1024 Then Throw New InvalidDataException("Profilfilen er for stor.")
            Return SessionValidator.Normalize(JsonSerializer.Deserialize(Of AppState)(File.ReadAllText(filePath, Encoding.UTF8), _json))
        End Function
        Public Sub Save(state As AppState)
            If state Is Nothing Then Throw New ArgumentNullException(NameOf(state))
            SyncLock _gate
                Directory.CreateDirectory(_folder)
                Dim temporary = _statePath & ".tmp-" & Guid.NewGuid().ToString("N")
                Try
                    Using stream As New FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)
                        JsonSerializer.Serialize(stream, state, _json)
                        stream.Flush(True)
                    End Using
                    If File.Exists(_statePath) Then
                        Dim backup As String = Nothing
                        Try
                            ReadState(_statePath)
                            backup = _statePath & ".bak"
                        Catch invalidPrimary As Exception When Recoverable(invalidPrimary)
                            ' A corrupt primary must not overwrite a good backup.
                        End Try
                        File.Replace(temporary, _statePath, backup, True)
                    Else
                        File.Move(temporary, _statePath)
                    End If
                Finally
                    If File.Exists(temporary) Then File.Delete(temporary)
                End Try
            End SyncLock
        End Sub
        Public Sub ForgetRecoveryCopies()
            SyncLock _gate
                If Not Directory.Exists(_folder) Then Return
                For Each filePath In Directory.EnumerateFiles(_folder, "state.json*")
                    Dim name = Path.GetFileName(filePath)
                    If name = "state.json.bak" OrElse name.StartsWith("state.json.corrupt-", StringComparison.Ordinal) Then File.Delete(filePath)
                Next
            End SyncLock
        End Sub
        Private Shared Function Recoverable(ex As Exception) As Boolean
            Return TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is JsonException OrElse TypeOf ex Is ArgumentException
        End Function
    End Class
End Namespace
