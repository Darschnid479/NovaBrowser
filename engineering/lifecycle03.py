from pathlib import Path
R=Path(__file__).resolve().parents[1]
S='src/NovaBrowser/'
def edit(name,old,new,count=1):
    path=R/name; text=path.read_text(encoding='utf-8-sig')
    if old not in text:
        if new in text:return
        raise RuntimeError('Missing anchor: '+name+' '+old[:70])
    path.write_text(text.replace(old,new,count),encoding='utf-8',newline='\n')

edit(S+'Models/BrowserTab.vb','Public Property ViewEpoch As Integer','Public Property ViewEpoch As Integer\n        Public Property InitializationCancellation As System.Threading.CancellationTokenSource')
edit(S+'MainWindow.xaml.vb','            Dim epoch = browserTab.ViewEpoch\n            Try','''            Dim epoch = browserTab.ViewEpoch
            Dim lifetime As New System.Threading.CancellationTokenSource()
            browserTab.InitializationCancellation = lifetime
            Try''')
edit(S+'MainWindow.xaml.vb','                Dim env = Await _environment','                Dim env = Await _environment.WaitAsync(TimeSpan.FromSeconds(30), lifetime.Token)')
edit(S+'MainWindow.xaml.vb','                Await view.EnsureCoreWebView2Async(env, options)','                Await view.EnsureCoreWebView2Async(env, options).WaitAsync(TimeSpan.FromSeconds(30), lifetime.Token)')
edit(S+'MainWindow.xaml.vb','            Catch ex As Exception\n                If epoch <> browserTab.ViewEpoch Then Return False','''            Catch ex As OperationCanceledException When lifetime.IsCancellationRequested
                Return False
            Catch ex As Exception
                If epoch <> browserTab.ViewEpoch Then Return False''')
edit(S+'MainWindow.xaml.vb','''                ReleaseTabView(browserTab)
                ReportTabError(browserTab, ex)
                Return False
            End Try
        End Function

        Private Sub ConfigureWebView''','''                ReleaseTabView(browserTab)
                ReportTabError(browserTab, ex)
                Return False
            Finally
                If browserTab.InitializationCancellation Is lifetime Then browserTab.InitializationCancellation = Nothing
                lifetime.Dispose()
            End Try
        End Function

        Private Sub ConfigureWebView''')
edit(S+'MainWindow.xaml.vb','            browserTab.ViewEpoch += 1\n            browserTab.Initialization = Nothing','''            browserTab.ViewEpoch += 1
            Dim lifetime = browserTab.InitializationCancellation
            browserTab.InitializationCancellation = Nothing
            lifetime?.Cancel()
            browserTab.Initialization = Nothing''')
# A shared environment task is not cancelled; only this tab's wait is cancelled.
# Disposing a WebView while EnsureCoreWebView2Async is pending can otherwise
# leave the caller waiting indefinitely even after the visual has been removed.
edit(S+'UI/Styles.xaml','<ui:NovaIcon Kind="ChevronDown" Width="13"','<ui:NovaIcon Kind="ChevronDown" Foreground="{DynamicResource Text}" Width="13"')
edit('tests/NovaBrowser.ShellChecks/Program.vb','                For Each theme In New String()', '''                window.Width = 1280
                window.Height = 820
                Keyboard.ClearFocus()
                Pump(3000)
                For Each theme In New String()''')
# Add regression assertions for template colors and all new vector states.
edit('tests/NovaBrowser.ShellChecks/Program.vb','                CallMethod(window, "ShowDrawer", "settings")\n                Pump(150)', '''                CallMethod(window, "ShowDrawer", "settings")
                Pump(150)
                Dim themeBox = DirectCast(window.FindName("ThemeSetting"), ComboBox)
                Check("settings arrow contrast", FindIcon(themeBox, "ChevronDown").Foreground Is Application.Current.Resources("Text"))''')
edit('tests/NovaBrowser.ShellChecks/Program.vb','        Friend Function Element(window As MainWindow, name As String) As FrameworkElement','''        Private Function FindIcon(parent As DependencyObject, kind As String) As NovaIcon
            Dim icon = TryCast(parent, NovaIcon)
            If icon IsNot Nothing AndAlso icon.Kind = kind Then Return icon
            For index As Integer = 0 To VisualTreeHelper.GetChildrenCount(parent) - 1
                Dim found = FindIcon(VisualTreeHelper.GetChild(parent, index), kind)
                If found IsNot Nothing Then Return found
            Next
            Return Nothing
        End Function
        Friend Function Element(window As MainWindow, name As String) As FrameworkElement''')
print('Lifecycle cancellation and observed contrast regressions corrected')
