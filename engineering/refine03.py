from pathlib import Path
R=Path(__file__).resolve().parents[1]
S='src/NovaBrowser/'
def edit(name, old, new, count=1):
    path=R/name
    text=path.read_text(encoding='utf-8-sig')
    if old not in text:
        if new in text: return
        raise RuntimeError('Missing refinement anchor: '+name+' / '+old[:70])
    path.write_text(text.replace(old,new,count),encoding='utf-8',newline='\n')

edit(S+'MainWindow.xaml.vb','If args.ProcessFailedKind = CoreWebView2ProcessFailedKind.BrowserProcessExited Then _environment = Nothing','_environment = Nothing')
edit(S+'MainWindow.Workbench.vb','                DwmSetWindowAttribute(handle, 20, dark, 4)', '''                DwmSetWindowAttribute(handle, 20, dark, 4)
                If Not SystemParameters.HighContrast Then
                    Dim background = TryCast(Application.Current.Resources("Bg"), System.Windows.Media.SolidColorBrush)
                    Dim foreground = TryCast(Application.Current.Resources("Text"), System.Windows.Media.SolidColorBrush)
                    If background IsNot Nothing AndAlso foreground IsNot Nothing Then
                        Dim bg = CInt(background.Color.R) Or (CInt(background.Color.G) << 8) Or (CInt(background.Color.B) << 16)
                        Dim fg = CInt(foreground.Color.R) Or (CInt(foreground.Color.G) << 8) Or (CInt(foreground.Color.B) << 16)
                        DwmSetWindowAttribute(handle, 35, bg, 4)
                        DwmSetWindowAttribute(handle, 36, fg, 4)
                    End If
                End If''')
edit(S+'MainWindow.xaml.vb','WindowCaption.Text = If(_active.IsPrivate, "Privat fane", _active.Title)','WindowCaption.Text = If(_active.IsPrivate, "PRIVAT ØKT / Besøkslisten er av", "Din arbeidsflate. Ditt fokus.")')
edit(S+'MainWindow.Tabs.vb','        Private _sleepSweepBusy As Boolean','        Private _sleepSweepBusy As Boolean\n        Private _sleepPolicyVersion As Integer')
edit(S+'MainWindow.Tabs.vb','Private Sub ApplySleepPreference()','Private Sub ApplySleepPreference(Optional resumeAll As Boolean = False)')
edit(S+'MainWindow.Tabs.vb','            If Not _state.Settings.SleepingTabs Then\n                For Each target','            If resumeAll Then\n                _sleepPolicyVersion += 1\n                For Each target')
edit(S+'MainWindow.Tabs.vb','            Dim document = target.DocumentVersion','            Dim document = target.DocumentVersion\n            Dim policyVersion = _sleepPolicyVersion')
edit(S+'MainWindow.Tabs.vb','If target Is _active OrElse target.DocumentVersion <> document OrElse','If target Is _active OrElse policyVersion <> _sleepPolicyVersion OrElse target.DocumentVersion <> document OrElse')
edit(S+'MainWindow.Tabs.vb','                    If _isClosing Then Return\n                    If TabSleepPolicy.IsIdle','                    If _isClosing OrElse Not _state.Settings.SleepingTabs Then Return\n                    If TabSleepPolicy.IsIdle')
edit(S+'MainWindow.Interactions.vb','            s.SleepingTabs = SleepSetting.IsChecked.GetValueOrDefault()\n            ApplySleepPreference()', '''            Dim resumeSleeping = s.SleepingTabs AndAlso Not SleepSetting.IsChecked.GetValueOrDefault()
            s.SleepingTabs = SleepSetting.IsChecked.GetValueOrDefault()
            ApplySleepPreference(resumeSleeping)''')
edit('tests/NovaBrowser.Checks/FeatureChecks.vb','                Dim flags = Enumerable.Range(0, 8).Select(Function(bit) (mask And (1 << bit)) <> 0).ToArray()', '                Dim currentMask = mask\n                Dim flags = Enumerable.Range(0, 8).Select(Function(bit) (currentMask And (1 << bit)) <> 0).ToArray()')
edit('tests/NovaBrowser.Checks/Program.vb','No websites visited and no browser profile modified.','No external websites visited; only disposable test profiles used.')
edit('tests/NovaBrowser.ShellChecks/LiveChecks.vb','                NativeSnapshot.Capture(window, Path.Combine(output, "nova-03-loopback-page-window.png"))', '''                NativeSnapshot.Capture(window, Path.Combine(output, "nova-03-loopback-page-window.png"))
                Using image = File.Create(Path.Combine(output, "nova-03-live-engine.png"))
                    Program.AwaitTask(normal.View.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, image), "capture actual web renderer")
                End Using''')
edit(S+'Services/ProfileRepository.vb','_statePath & ".corrupt-" & Guid.NewGuid().ToString("N")','_statePath & ".corrupt-" & DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") & "-" & Guid.NewGuid().ToString("N")')
# Preserve at most three damaged originals; repeated failed starts must not fill a disk.
edit(S+'Services/ProfileRepository.vb','                    Try\n                        Dim recovered = ReadState(_statePath & ".bak")', '''                    Try
                        Dim copies = Directory.GetFiles(_folder, "state.json.corrupt-*")
                        Array.Sort(copies, StringComparer.Ordinal)
                        For index As Integer = 0 To copies.Length - 4
                            File.Delete(copies(index))
                        Next
                    Catch cleanupError As Exception When Recoverable(cleanupError)
                    End Try
                    Try
                        Dim recovered = ReadState(_statePath & ".bak")''')

# Build the new test harness as part of the solution, not a forgotten side project.
p=R/'NOVA.sln'; t=p.read_text(encoding='utf-8-sig')
guid='{0BCF4C76-63ED-452C-A4A5-9E467760DA91}'
if 'NovaBrowser.ShellChecks' not in t:
    block='Project("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}") = "NovaBrowser.ShellChecks", "tests\\NovaBrowser.ShellChecks\\NovaBrowser.ShellChecks.vbproj", "'+guid+'"\nEndProject\n'
    t=t.replace('Global\n',block+'Global\n',1)
    marker='GlobalSection(ProjectConfigurationPlatforms) = postSolution\n'
    configs=''
    for mode in ['Debug','Release']:
        for kind in ['ActiveCfg','Build.0']:
            configs+='\t\t'+guid+'.'+mode+'|Any CPU.'+kind+' = '+mode+'|Any CPU\n'
    t=t.replace(marker,marker+configs)
    p.write_text(t,encoding='utf-8',newline='\n')
(R/'Directory.Build.props').write_text('''<Project><PropertyGroup>
  <Deterministic>true</Deterministic>
  <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup></Project>
''',encoding='utf-8')

# Native-frame invariants and test project linkage now have explicit assertions.
edit('tools/check_source.py',"    check('WPF enabled',", "    check('Native close/min/max retained', 'WindowStyle=\"SingleBorderWindow\"' in markup and '<shell:WindowChrome.WindowChrome>' not in markup)\n    check('WPF enabled',")
print('0.3.0 refinements applied')
