Imports System
Imports System.Collections.ObjectModel
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Windows
Imports System.Windows.Threading
Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Win32

Namespace NovaBrowser
    Partial Public Class MainWindow
        Private ReadOnly _downloadRows As New ObservableCollection(Of DownloadRow)()
        Private ReadOnly _downloadTimer As New DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(500)}
        Private _downloadPromptOpen As Boolean

        Private Sub InitializeDownloadTools()
            DownloadList.ItemsSource = _downloadRows
            AddHandler _downloadTimer.Tick, Sub(sender, args) RefreshDownloads()
        End Sub
        Private Sub BeginDownload(owner As BrowserTab, args As CoreWebView2DownloadStartingEventArgs)
            args.Handled = True
            If owner.Closed OrElse _isClosing OrElse _downloadPromptOpen OrElse _downloads.Count >= 8 Then
                args.Cancel = True
                Return
            End If
            Dim deferral = args.GetDeferral()
            _downloadPromptOpen = True
            ' Native modal UI must be posted outside the WebView2 callback.
            Dispatcher.BeginInvoke(New Action(
                Sub()
                    Try
                        If owner.Closed OrElse _isClosing Then
                            args.Cancel = True
                            Return
                        End If
                        Dim suggested = Path.GetFileName(args.ResultFilePath)
                        For Each invalid In Path.GetInvalidFileNameChars()
                            suggested = suggested.Replace(invalid, "_"c)
                        Next
                        If suggested.Length = 0 Then suggested = "nedlasting"
                        Dim dialog As New SaveFileDialog With {.Title = "Lagre nedlasting", .FileName = suggested, .Filter = "Alle filer|*.*", .AddExtension = False, .OverwritePrompt = True}
                        If dialog.ShowDialog(Me) <> True Then
                            args.Cancel = True
                            Return
                        End If
                        If owner.Closed OrElse _isClosing Then
                            args.Cancel = True
                            Return
                        End If
                        args.ResultFilePath = dialog.FileName
                        Dim row As New DownloadRow(args.DownloadOperation, owner, dialog.FileName)
                        _downloadRows.Insert(0, row)
                        _downloads(args.DownloadOperation) = owner
                        While _downloadRows.Count > 100
                            Dim oldest = _downloadRows.LastOrDefault(Function(item) Not item.IsActive)
                            If oldest Is Nothing Then Exit While
                            _downloadRows.Remove(oldest)
                        End While
                        _downloadTimer.Start()
                        ShowToast("Nedlasting startet. Ctrl+J viser fremdriften.")
                    Catch ex As Exception
                        args.Cancel = True
                        StateStore.LogError(ex)
                        ShowToast("Nedlastingen kunne ikke startes.")
                    Finally
                        _downloadPromptOpen = False
                        Try
                            deferral.Complete()
                        Catch ex As Exception
                            StateStore.LogError(ex)
                        End Try
                    End Try
                End Sub))
        End Sub
        Private Sub RefreshDownloads()
            If _isClosing Then Return
            For Each row In _downloadRows.Where(Function(item) item.IsActive).ToArray()
                Try
                    row.Capture()
                    If Not row.IsActive Then _downloads.Remove(row.Operation)
                Catch ex As Exception
                    _downloads.Remove(row.Operation)
                    row.MarkStopped("Nettsiden er ikke tilgjengelig")
                    StateStore.LogError(ex)
                End Try
            Next
            If _downloads.Count = 0 Then _downloadTimer.Stop()
            DownloadsHint.Text = If(_downloadRows.Count = 0, "Ingen nedlastinger i denne økten.", "Kun denne økten. Filer kjøres aldri automatisk. Opptil åtte aktive nedlastinger.")
        End Sub
        Private Sub DownloadPause_Click(sender As Object, args As RoutedEventArgs)
            Dim row = TryCast(DirectCast(sender, FrameworkElement).DataContext, DownloadRow)
            If row Is Nothing OrElse Not row.IsActive Then Return
            Try
                If row.Operation.State = CoreWebView2DownloadState.InProgress Then
                    row.Operation.Pause()
                ElseIf row.Operation.CanResume Then
                    row.Operation.Resume()
                End If
                row.Capture()
                _downloadTimer.Start()
            Catch ex As Exception
                StateStore.LogError(ex)
                ShowToast("Nedlastingen kan ikke pauses eller fortsette akkurat nå.")
            End Try
        End Sub
        Private Sub DownloadCancel_Click(sender As Object, args As RoutedEventArgs)
            Dim row = TryCast(DirectCast(sender, FrameworkElement).DataContext, DownloadRow)
            If row Is Nothing OrElse Not row.IsActive Then Return
            Try
                row.Operation.Cancel()
                row.MarkStopped("Avbrutt av deg")
                _downloads.Remove(row.Operation)
            Catch ex As Exception
                StateStore.LogError(ex)
                ShowToast("Kunne ikke avbryte nedlastingen.")
            End Try
        End Sub
        Private Sub DownloadReveal_Click(sender As Object, args As RoutedEventArgs)
            Dim row = TryCast(DirectCast(sender, FrameworkElement).DataContext, DownloadRow)
            If row Is Nothing OrElse Not row.IsComplete Then Return
            Try
                If Not File.Exists(row.FilePath) Then
                    ShowToast("Filen er flyttet eller slettet.")
                    Return
                End If
                Dim explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe")
                Dim start As New ProcessStartInfo(explorer) With {.UseShellExecute = False, .Arguments = "/select,""" & row.FilePath.Replace("""", "") & """"}
                Process.Start(start)
            Catch ex As Exception
                StateStore.LogError(ex)
                ShowToast("Kunne ikke vise filen i Utforsker.")
            End Try
        End Sub
        Private Sub ForgetTabDownloads(owner As BrowserTab)
            For Each row In _downloadRows.Where(Function(item) item.OwnerId = owner.Id).ToArray()
                If row.IsActive Then
                    Try
                        row.Operation.Cancel()
                    Catch ex As Exception
                        StateStore.LogError(ex)
                    End Try
                    row.MarkStopped("Fanen ble lukket")
                End If
                _downloads.Remove(row.Operation)
                If row.IsPrivate OrElse Not row.IsComplete Then _downloadRows.Remove(row)
            Next
        End Sub
    End Class
End Namespace
