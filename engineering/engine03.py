# Executed by apply03.py with its checked file-edit helpers in scope.
f = S + 'MainWindow.xaml.vb'
method(f, '        Private Sub AttachWebEvents(', '        Private Sub OnNavigationStarting(', '''        Private Function IsCurrentView(target As BrowserTab, view As WebView2CompositionControl) As Boolean
            Return Not _isClosing AndAlso Not _fatalShutdown AndAlso Not target.Closed AndAlso target.View Is view
        End Function
        Private Sub AttachWebEvents(browserTab As BrowserTab)
            Dim view = browserTab.View
            Dim core = view.CoreWebView2
            AddHandler core.NavigationStarting,
                Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then
                        OnNavigationStarting(browserTab, args)
                    Else
                        args.Cancel = True
                    End If
                End Sub
            AddHandler core.NavigationCompleted,
                Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then OnNavigationCompleted(browserTab, args)
                End Sub
            AddHandler core.SourceChanged,
                Sub(sender, args)
                    If Not IsCurrentView(browserTab, view) OrElse browserTab.IsHome Then Return
                    If UrlPolicy.IsWebUrl(core.Source) Then browserTab.Address = UrlPolicy.Resolve(core.Source, _state.Settings.SearchEngine)
                    browserTab.NotifyLocation()
                    If browserTab Is _active Then UpdateChrome()
                    ScheduleSave()
                End Sub
            AddHandler core.HistoryChanged,
                Sub(sender, args)
                    If IsCurrentView(browserTab, view) AndAlso browserTab Is _active Then UpdateChrome()
                End Sub
            AddHandler core.DocumentTitleChanged,
                Sub(sender, args)
                    If Not IsCurrentView(browserTab, view) OrElse browserTab.IsHome Then Return
                    Dim title = UrlPolicy.CleanTitle(core.DocumentTitle)
                    browserTab.Title = If(title.Length > 0, title, UrlPolicy.DisplayHost(browserTab.Address))
                    If Not browserTab.IsPrivate Then
                        Dim entry = _state.History.FirstOrDefault(Function(item) item.Url = browserTab.Address)
                        If entry IsNot Nothing Then entry.Title = browserTab.Title
                    End If
                    If browserTab Is _active Then UpdateChrome()
                    ScheduleSave()
                End Sub
            AddHandler core.IsDocumentPlayingAudioChanged,
                Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then browserTab.IsPlayingAudio = core.IsDocumentPlayingAudio
                End Sub
            AddHandler core.IsMutedChanged,
                Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then browserTab.IsMuted = core.IsMuted
                End Sub
            AddHandler core.NewWindowRequested,
                Async Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then
                        Await HandleNewWindowAsync(browserTab, args)
                    Else
                        args.Handled = True
                    End If
                End Sub
            AddHandler core.PermissionRequested,
                Async Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then
                        Await HandlePermissionAsync(browserTab, args)
                    Else
                        args.State = CoreWebView2PermissionState.Deny
                    End If
                End Sub
            AddHandler core.LaunchingExternalUriScheme, Sub(sender, args) args.Cancel = True
            AddHandler core.WindowCloseRequested,
                Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then Dispatcher.BeginInvoke(New Action(Async Sub() Await CloseTabAsync(browserTab)))
                End Sub
            AddHandler core.ProcessFailed,
                Sub(sender, args)
                    If Not IsCurrentView(browserTab, view) Then Return
                    Dim affected = If(args.ProcessFailedKind = CoreWebView2ProcessFailedKind.BrowserProcessExited, _tabs.ToArray(), New BrowserTab() {browserTab}).
                        Select(Function(target) New With {.Target = target, .Epoch = target.ViewEpoch}).ToArray()
                    If args.ProcessFailedKind = CoreWebView2ProcessFailedKind.BrowserProcessExited Then _environment = Nothing
                    Dispatcher.BeginInvoke(New Action(
                        Sub()
                            If _isClosing OrElse _fatalShutdown Then Return
                            For Each item In affected
                                Dim target = item.Target
                                If target.Closed OrElse target.ViewEpoch <> item.Epoch Then Continue For
                                ForgetTabDownloads(target)
                                ReleaseTabView(target)
                                target.IsLoading = False
                                If Not target.IsHome Then target.ErrorMessage = "Nettmotoren stoppet. Velg Prøv igjen for å laste siden på nytt."
                            Next
                            UpdateViewVisibility()
                            UpdateChrome()
                        End Sub))
                End Sub
            AddHandler core.DownloadStarting,
                Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then
                        BeginDownload(browserTab, args)
                    Else
                        args.Cancel = True
                    End If
                End Sub
        End Sub

''')
# Persist the dirty-exit marker as soon as restoration is complete.
edit(f, '            UpdateClock()\n        End Sub\n\n        Private Function CreateTab', '            UpdateClock()\n            PersistState()\n        End Sub\n\n        Private Function CreateTab')
# Native-control failures must stay scoped to the affected tab.
f = S + 'MainWindow.Interactions.vb'
method(f, '        Private Sub NavigateBack()', '        Private Sub CycleTab(', '''        Private Sub NavigateBack()
            Dim target = _active
            If target?.View?.CoreWebView2 Is Nothing Then Return
            Try
                If Not target.View.CoreWebView2.CanGoBack Then Return
                WakeTab(target)
                target.IsHome = False
                target.ErrorMessage = ""
                target.View.CoreWebView2.GoBack()
                UpdateViewVisibility()
            Catch ex As Exception
                ReportTabError(target, ex)
            End Try
        End Sub
        Private Sub NavigateForward()
            Dim target = _active
            If target?.View?.CoreWebView2 Is Nothing Then Return
            Try
                If Not target.View.CoreWebView2.CanGoForward Then Return
                WakeTab(target)
                target.IsHome = False
                target.ErrorMessage = ""
                target.View.CoreWebView2.GoForward()
                UpdateViewVisibility()
            Catch ex As Exception
                ReportTabError(target, ex)
            End Try
        End Sub
        Private Sub ReloadCurrent()
            Dim target = _active
            If target Is Nothing OrElse target.IsHome Then Return
            Try
                If target.ErrorMessage.Length > 0 Then
                    Retry_Click(Me, New RoutedEventArgs())
                ElseIf target.View?.CoreWebView2 IsNot Nothing Then
                    WakeTab(target)
                    If target.IsLoading Then
                        target.View.CoreWebView2.Stop()
                        target.IsLoading = False
                        UpdateChrome()
                    Else
                        target.View.CoreWebView2.Reload()
                    End If
                End If
            Catch ex As Exception
                ReportTabError(target, ex)
            End Try
        End Sub

''')
# Honor the operating system's explicit high-contrast preference.
f = S+'UI/ThemeManager.vb'
edit(f, '            resources("SecondaryHalo") = Halo(If(light, Color.FromRgb(42, 129, 128), Color.FromRgb(50, 169, 167)))', '''            resources("SecondaryHalo") = Halo(If(light, Color.FromRgb(42, 129, 128), Color.FromRgb(50, 169, 167)))
            If SystemParameters.HighContrast Then
                For Each name In New String() {"Bg", "Panel", "Elevated", "Field", "HomeBackground"}
                    resources(name) = SystemColors.WindowBrush
                Next
                For Each name In New String() {"Text", "Muted", "Stroke", "Good", "Danger"}
                    resources(name) = SystemColors.WindowTextBrush
                Next
                resources("Accent") = SystemColors.HighlightBrush
                resources("AccentSoft") = SystemColors.HighlightBrush
                resources("OnAccent") = SystemColors.HighlightTextBrush
                resources("HaloBrush") = Brushes.Transparent
                resources("SecondaryHalo") = Brushes.Transparent
            End If''')
# A flat, accessible context menu style follows NOVA's palette.
f=S+'UI/Styles.xaml'
t=read(f)
pos=t.rfind('</ResourceDictionary>')
t=t[:pos]+'''    <Style x:Key="NovaMenuItem" TargetType="{x:Type MenuItem}">
        <Setter Property="Foreground" Value="{DynamicResource Text}" />
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Padding" Value="14,9" />
        <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="{x:Type MenuItem}">
            <Border x:Name="ItemBorder" Background="{TemplateBinding Background}" CornerRadius="6" Padding="{TemplateBinding Padding}"><ContentPresenter ContentSource="Header" RecognizesAccessKey="True" /></Border>
            <ControlTemplate.Triggers>
                <Trigger Property="IsHighlighted" Value="True"><Setter TargetName="ItemBorder" Property="Background" Value="{DynamicResource Field}" /><Setter Property="Foreground" Value="{DynamicResource Accent}" /></Trigger>
                <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.45" /></Trigger>
            </ControlTemplate.Triggers>
        </ControlTemplate></Setter.Value></Setter>
    </Style>
'''+t[pos:]
put(f,t)
edit(S+'MainWindow.Tabs.vb','Dim item As New MenuItem With {.Header = title, .IsEnabled = enabled}', 'Dim item As New MenuItem With {.Header = title, .IsEnabled = enabled, .Style = DirectCast(FindResource("NovaMenuItem"), Style)}')
