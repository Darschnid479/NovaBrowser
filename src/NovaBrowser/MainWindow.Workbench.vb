Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Text.Json
Imports System.Threading.Tasks
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Interop
Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Win32

Namespace NovaBrowser
    Partial Public Class MainWindow
        Private _focusMode As Boolean
        Private _sidebarManual As Boolean

        <DllImport("dwmapi.dll", PreserveSig:=True)>
        Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attribute As Integer, ByRef value As Integer, size As Integer) As Integer
        End Function

        Private Sub InitializeWorkbench()
            Dim area = SystemParameters.WorkArea
            MinWidth = Math.Min(760, area.Width)
            MinHeight = Math.Min(500, area.Height)
            Width = Math.Clamp(_state.Settings.WindowWidth, MinWidth, Math.Max(MinWidth, area.Width))
            Height = Math.Clamp(_state.Settings.WindowHeight, MinHeight, Math.Max(MinHeight, area.Height))
            AddHandler SourceInitialized, Sub(sender, args) UpdateNativeTitleBar()
            AddHandler SizeChanged, Sub(sender, args) UpdateWorkbenchLayout()
            InitializeTabTools()
            InitializeDownloadTools()
        End Sub
        Private Sub UpdateNativeTitleBar()
            Try
                Dim handle = New WindowInteropHelper(Me).Handle
                If handle = IntPtr.Zero Then Return
                Dim dark As Integer = If(_state.Settings.Theme = "Dawn" OrElse SystemParameters.HighContrast, 0, 1)
                DwmSetWindowAttribute(handle, 20, dark, 4)
            Catch ex As Exception When TypeOf ex Is DllNotFoundException OrElse TypeOf ex Is EntryPointNotFoundException
                ' Cosmetic API only; Windows always retains its real caption buttons.
            End Try
        End Sub
        Private Sub UpdateWorkbenchLayout()
            If SidebarColumn Is Nothing OrElse _isClosing Then Return
            Dim showSidebar = Not _focusMode AndAlso _sidebarVisible AndAlso (ActualWidth >= 1080 OrElse _sidebarManual)
            Sidebar.Visibility = If(showSidebar, Visibility.Visible, Visibility.Collapsed)
            SidebarColumn.Width = New GridLength(If(showSidebar, 238, 0))
            AppRail.Visibility = If(_focusMode, Visibility.Collapsed, Visibility.Visible)
            RailColumn.Width = New GridLength(If(_focusMode, 0, 66))
            If IsLoaded AndAlso WindowState = WindowState.Normal AndAlso ActualWidth >= MinWidth AndAlso ActualHeight >= MinHeight Then
                _state.Settings.WindowWidth = ActualWidth
                _state.Settings.WindowHeight = ActualHeight
                ScheduleSave()
            End If
        End Sub
        Private Sub FocusMode_Click(sender As Object, args As RoutedEventArgs)
            ToggleFocusMode()
        End Sub
        Private Sub ToggleFocusMode()
            _focusMode = Not _focusMode
            FocusModeButton.Content = If(_focusMode, "Avslutt fokus", "Fokus")
            UpdateWorkbenchLayout()
            ShowToast(If(_focusMode, "Fokusmodus. Ctrl+Shift+F viser sidefeltene igjen.", "Sidefeltene er tilbake."))
        End Sub
        Private Sub StopWorkbench()
            _idleTimer.Stop()
            _downloadTimer.Stop()
            CloseAddressSuggestions()
        End Sub
        Private Async Sub ExportBookmarks_Click(sender As Object, args As RoutedEventArgs)
            Try
                Dim dialog As New SaveFileDialog With {.Title = "Eksporter bokmerker", .Filter = "NOVA-bokmerker (*.json)|*.json", .FileName = "NOVA-bokmerker.json", .AddExtension = True}
                If dialog.ShowDialog(Me) <> True Then Return
                Dim json = JsonSerializer.Serialize(_state.Bookmarks, New JsonSerializerOptions With {.WriteIndented = True})
                Await File.WriteAllTextAsync(dialog.FileName, json, New UTF8Encoding(False))
                ShowToast("Bokmerkene er eksportert. Historikk og innlogginger er ikke tatt med.")
            Catch ex As Exception
                StateStore.LogError(ex)
                ShowToast("Bokmerkene kunne ikke eksporteres.")
            End Try
        End Sub
        Private Async Sub ImportBookmarks_Click(sender As Object, args As RoutedEventArgs)
            Try
                Dim dialog As New OpenFileDialog With {.Title = "Importer NOVA-bokmerker", .Filter = "NOVA-bokmerker (*.json)|*.json", .Multiselect = False}
                If dialog.ShowDialog(Me) <> True Then Return
                If New FileInfo(dialog.FileName).Length > 2 * 1024 * 1024 Then Throw New InvalidDataException("Filen er for stor.")
                Dim entries = JsonSerializer.Deserialize(Of List(Of PageEntry))(Await File.ReadAllTextAsync(dialog.FileName))
                If entries Is Nothing Then Throw New InvalidDataException("Tom liste.")
                Dim added As Integer = 0
                For Each entry In entries
                    If _state.Bookmarks.Count >= 500 Then Exit For
                    If entry Is Nothing OrElse Not UrlPolicy.IsWebUrl(entry.Url) Then Continue For
                    If _state.Bookmarks.Any(Function(item) String.Equals(item.Url, entry.Url, StringComparison.Ordinal)) Then Continue For
                    entry.Title = UrlPolicy.CleanTitle(entry.Title)
                    _state.Bookmarks.Add(entry)
                    added += 1
                Next
                UpdateHome()
                UpdateChrome()
                ScheduleSave()
                ShowToast(added.ToString() & " bokmerker importert. Ingen nettsteder ble besøkt.")
            Catch ex As Exception
                StateStore.LogError(ex)
                ShowToast("Bruk en gyldig NOVA-eksport i JSON-format, maksimalt 2 MB.")
            End Try
        End Sub
        Private Async Function SavePagePdfAsync() As Task
            Dim target = _active
            If target Is Nothing OrElse target.IsHome OrElse target.View?.CoreWebView2 Is Nothing Then Return
            Dim core = target.View.CoreWebView2
            Try
                Dim dialog As New SaveFileDialog With {.Title = "Lagre nettsiden som PDF", .Filter = "PDF (*.pdf)|*.pdf", .FileName = "Nettside.pdf", .AddExtension = True}
                If dialog.ShowDialog(Me) <> True Then Return
                Dim saved = Await core.PrintToPdfAsync(dialog.FileName)
                ShowToast(If(saved, "Nettsiden er lagret som PDF.", "Nettsiden kunne ikke lagres som PDF."))
            Catch ex As Exception
                StateStore.LogError(ex)
                ShowToast("PDF-eksporten feilet. Vent til siden er ferdig lastet, og prøv igjen.")
            End Try
        End Function
        Private Sub PrintCurrentPage()
            Try
                If _active?.View?.CoreWebView2 IsNot Nothing AndAlso Not _active.IsHome Then _active.View.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser)
            Catch ex As Exception
                StateStore.LogError(ex)
                ShowToast("Utskriftsvinduet kunne ikke åpnes.")
            End Try
        End Sub
    End Class
End Namespace
