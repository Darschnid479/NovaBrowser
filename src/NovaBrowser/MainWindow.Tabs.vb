Imports System
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Threading

Namespace NovaBrowser
    Partial Public Class MainWindow
        Private ReadOnly _idleTimer As New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(30)}
        Private _sleepSweepBusy As Boolean
        Private _sleepPolicyVersion As Integer

        Private Sub InitializeTabTools()
            AddHandler _idleTimer.Tick, Async Sub(sender, args) Await SleepIdleTabsAsync()
            ApplySleepPreference()
        End Sub
        Private Sub ApplySleepPreference(Optional resumeAll As Boolean = False)
            _idleTimer.Stop()
            If Not _isClosing AndAlso _state.Settings.SleepingTabs Then _idleTimer.Start()
            If resumeAll Then
                _sleepPolicyVersion += 1
                For Each target In _tabs.Where(Function(item) item.IsSleeping).ToArray()
                    WakeTab(target)
                Next
            End If
        End Sub
        Private Sub TabList_ContextMenu(sender As Object, args As MouseButtonEventArgs)
            Dim row = TryCast(ItemsControl.ContainerFromElement(TabList, TryCast(args.OriginalSource, DependencyObject)), ListBoxItem)
            Dim target = TryCast(row?.DataContext, BrowserTab)
            If target Is Nothing OrElse target.Closed Then Return
            Dim menu As New ContextMenu()
            menu.SetResourceReference(Control.BackgroundProperty, "Elevated")
            menu.SetResourceReference(Control.ForegroundProperty, "Text")
            AddTabAction(menu, If(target.IsPinned, "Løsne fane", "Fest fane"), Sub() TogglePinned(target))
            AddTabAction(menu, If(target.IsMuted, "Slå på lyd", "Demp fane"), Sub() ToggleMuted(target))
            AddTabAction(menu, "Dupliser fane", Sub() DuplicateTab(target))
            AddTabAction(menu, "La bakgrunnsfanen hvile", Async Sub() Await SleepTabAsync(target), CanSleepTab(target))
            menu.Items.Add(New Separator())
            AddTabAction(menu, "Flytt opp", Sub() MoveTab(target, -1), _tabs.IndexOf(target) > 0)
            AddTabAction(menu, "Flytt ned", Sub() MoveTab(target, 1), _tabs.IndexOf(target) < _tabs.Count - 1)
            menu.Items.Add(New Separator())
            AddTabAction(menu, "Lukk andre vanlige faner", Async Sub() Await CloseOtherTabsAsync(target))
            AddTabAction(menu, "Lukk fane", Async Sub() Await CloseTabAsync(target))
            menu.PlacementTarget = row
            menu.IsOpen = True
            args.Handled = True
        End Sub
        Private Sub AddTabAction(menu As ContextMenu, title As String, action As Action, Optional enabled As Boolean = True)
            Dim item As New MenuItem With {.Header = title, .IsEnabled = enabled, .Style = DirectCast(FindResource("NovaMenuItem"), Style)}
            AddHandler item.Click, Sub(sender, args) Dispatcher.BeginInvoke(action)
            menu.Items.Add(item)
        End Sub
        Private Sub TogglePinned(target As BrowserTab)
            If target Is Nothing OrElse target.Closed Then Return
            Dim selected = _active
            target.IsPinned = Not target.IsPinned
            Dim pinnedCount = _tabs.Where(Function(item) item.IsPinned).Count()
            Dim index = If(target.IsPinned, pinnedCount - 1, pinnedCount)
            _tabs.Move(_tabs.IndexOf(target), Math.Clamp(index, 0, _tabs.Count - 1))
            If selected IsNot Nothing Then TabList.SelectedItem = selected
            ScheduleSave()
        End Sub
        Private Sub MoveTab(target As BrowserTab, direction As Integer)
            If target Is Nothing OrElse target.Closed Then Return
            Dim index = _tabs.IndexOf(target)
            Dim destination = index + direction
            If destination < 0 OrElse destination >= _tabs.Count Then Return
            If _tabs(destination).IsPinned <> target.IsPinned Then Return
            _tabs.Move(index, destination)
            ScheduleSave()
        End Sub
        Private Sub ToggleMuted(target As BrowserTab)
            If target Is Nothing OrElse target.Closed Then Return
            Try
                Dim nextValue = Not target.IsMuted
                If target.View?.CoreWebView2 IsNot Nothing Then target.View.CoreWebView2.IsMuted = nextValue
                target.IsMuted = nextValue
                ScheduleSave()
            Catch ex As Exception
                StateStore.LogError(ex)
                ShowToast("Kunne ikke endre lydstatus for denne fanen.")
            End Try
        End Sub
        Private Sub DuplicateTab(target As BrowserTab)
            If target Is Nothing OrElse target.Closed Then Return
            Dim duplicate = CreateTab(target.Address, target.IsPrivate, False)
            If duplicate Is Nothing Then Return
            duplicate.IsMuted = target.IsMuted
            duplicate.Zoom = target.Zoom
            TabList.SelectedItem = duplicate
        End Sub
        Private Async Function CloseOtherTabsAsync(keep As BrowserTab) As Task
            Dim targets = _tabs.Where(Function(item) item IsNot keep AndAlso Not item.IsPinned AndAlso Not item.IsPrivate).ToArray()
            If targets.Length = 0 Then Return
            If Not Await AskAsync("Lukk andre faner?", "Vanlige faner lukkes. Festede og private faner, samt faner med pågående nedlastinger, beholdes.", "Lukk faner") Then Return
            For Each target In targets
                If Not target.Closed AndAlso Not _downloads.Values.Any(Function(item) item Is target) Then RemoveTab(target)
            Next
        End Function
        Private Function CanSleepTab(target As BrowserTab) As Boolean
            If target Is Nothing OrElse target.View?.CoreWebView2 Is Nothing OrElse target.SuspendPending OrElse target.IsSleeping Then Return False
            If _downloadPromptOpen Then Return False
            Return TabSleepPolicy.CanSleep(target Is _active, target.IsHome, target.IsLoading, target.IsPlayingAudio, target.IsPinned,
                _downloads.Values.Any(Function(item) item Is target), target.HasSensitivePermission, target.Closed OrElse _isClosing)
        End Function
        Private Async Function SleepTabAsync(target As BrowserTab) As Task
            If Not CanSleepTab(target) Then Return
            Dim view = target.View
            Dim core = view.CoreWebView2
            Dim document = target.DocumentVersion
            Dim policyVersion = _sleepPolicyVersion
            target.SuspendPending = True
            Try
                Dim suspended = Await core.TrySuspendAsync()
                If target.Closed OrElse _isClosing OrElse target.View IsNot view Then Return
                If suspended Then
                    If target Is _active OrElse policyVersion <> _sleepPolicyVersion OrElse target.DocumentVersion <> document OrElse target.HasSensitivePermission OrElse target.IsPlayingAudio OrElse
                        _downloads.Values.Any(Function(item) item Is target) Then
                        core.Resume()
                    Else
                        target.IsSleeping = True
                    End If
                End If
            Catch ex As Exception
                StateStore.LogError(ex)
            Finally
                target.SuspendPending = False
            End Try
        End Function
        Private Sub WakeTab(target As BrowserTab)
            If target Is Nothing OrElse target.Closed Then Return
            target.LastActiveUtc = DateTimeOffset.UtcNow
            Try
                If target.View?.CoreWebView2 IsNot Nothing AndAlso target.View.CoreWebView2.IsSuspended Then target.View.CoreWebView2.Resume()
                target.IsSleeping = False
            Catch ex As Exception
                ReportTabError(target, ex)
            End Try
        End Sub
        Private Async Function SleepIdleTabsAsync() As Task
            If _sleepSweepBusy OrElse _isClosing OrElse Not _state.Settings.SleepingTabs Then Return
            _sleepSweepBusy = True
            Try
                For Each target In _tabs.ToArray()
                    If _isClosing OrElse Not _state.Settings.SleepingTabs Then Return
                    If TabSleepPolicy.IsIdle(target.LastActiveUtc, DateTimeOffset.UtcNow, _state.Settings.SleepAfterMinutes) Then Await SleepTabAsync(target)
                Next
            Finally
                _sleepSweepBusy = False
            End Try
        End Function
        Private Async Function SleepBackgroundTabsAsync() As Task
            For Each target In _tabs.ToArray()
                If _isClosing Then Return
                Await SleepTabAsync(target)
            Next
            ShowToast("Bakgrunnsfaner som kunne pauses, hviler nå. Lyd, tillatelser og nedlastinger beskyttes.")
        End Function
    End Class
End Namespace
