from pathlib import Path
R=Path(__file__).resolve().parents[1]
def edit(name,old,new,count=1):
    p=R/name;t=p.read_text(encoding='utf-8-sig')
    if old not in t:
        if new in t:return
        raise RuntimeError('Missing polish anchor: '+name+' '+old[:80])
    p.write_text(t.replace(old,new,count),encoding='utf-8',newline='\n')
# Show the shipped application icon even when the real window is hosted by tests.
edit('src/NovaBrowser/NovaBrowser.vbproj','    <PackageReference Include="Microsoft.Web.WebView2"','    <Resource Include="Assets/Nova.ico" />\n    <PackageReference Include="Microsoft.Web.WebView2"')
edit('src/NovaBrowser/MainWindow.xaml','        Title="NOVA Browser"','        Icon="/NOVA;component/Assets/Nova.ico" Title="NOVA Browser"')
# Profile recovery copies are sensitive too. Filter before extension-based handling.
edit('tools/NovaUpload.psm1',"            if ($skipFiles -contains $item.Name -or", "            if ($item.Name -match '^state\\.json($|\\.)') { continue }\n            if ($skipFiles -contains $item.Name -or")
edit('.gitignore','**/state.json\n','**/state.json\n**/state.json.*\n')
edit('tests/Upload.Tests.ps1',"$argsCommon=@{SourceRoot=$source;",'''foreach ($privateName in @('state.json','state.json.bak','state.json.corrupt-test','state.json.tmp-test')) {
    [IO.File]::WriteAllText((Join-Path $source $privateName),'private profile fixture')
}
$argsCommon=@{SourceRoot=$source;''')
edit('tests/Upload.Tests.ps1',"$files -match 'artifacts|bin/'", "$files -match 'artifacts|bin/|state\\.json'")
edit('tests/NovaBrowser.ShellChecks/Program.vb','                Pump(3000)\n                For Each theme', '                Pump(5500)\n                For Each theme')
edit('tests/NovaBrowser.ShellChecks/Program.vb','                If args.Contains("--live") Then LiveChecks.Run(window, output)', '''                CallMethod(window, "ToggleSidebar")
                Pump(150)
                Check("sidebar can be opened on compact desktops", Element(window, "Sidebar").Visibility = Visibility.Visible)
                CaptureClient(window, Path.Combine(output, "nova-03-tabs-client.png"))
                NativeSnapshot.Capture(window, Path.Combine(output, "nova-03-tabs-window.png"))
                If args.Contains("--live") Then LiveChecks.Run(window, output)''')
# Correct the generated site: the original JS concatenates strings, not templates.
edit('engineering/present03.py',"js=js.replace('assets/previews/nova-','assets/app-0.3.0/nova-03-').replace('${theme}.png','${theme}-client.png').replace('${key}.png','${key}-client.png')",'''js=js.replace('assets/previews/nova-','assets/app-0.3.0/nova-03-').replace('${theme}.png','${theme}-client.png').replace('${key}.png','${key}-client.png')
js=js.replace("+key+'.png'", "+key+'-client.png'").replace('HTML-designforhåndsvisning','Faktisk WPF-klientflate')
js=js.replace('Forest + Mint. Dype grønntoner og en frisk aksent.', 'Forest + Iris. Dype grønntoner med lilla aksenter.')
js=js.replace('Graphite + Blue. Nøytral grå med en kjølig blå aksent.', 'Graphite + Iris. Nøytral grå med lilla aksenter.')''')
edit('engineering/present03.py',"'publish.log','source-checks.log','package-checks.log']:","'publish.log','source-checks.log','package-checks.log','upload-tests.log']:")
edit('engineering/present03.py',"hero='nova-03-midnight-window.png' if (shots/'nova-03-midnight-window.png').exists() else 'nova-03-midnight-client.png'", "hero='nova-03-tabs-window.png' if (shots/'nova-03-tabs-window.png').exists() else 'nova-03-midnight-client.png'")
# Use the exact PNG dimensions in HTML rather than legacy reconstruction sizes.
edit('engineering/present03.py',"p.write_text(html,encoding='utf-8')",'''import struct
for match in list(re.finditer(r'<img[^>]+src="(assets/app-0.3.0/[^\"]+)"[^>]*>', html)):
    tag=match.group(0)
    image=R/'docs'/match.group(1)
    if image.is_file():
        width,height=struct.unpack('>II', image.read_bytes()[16:24])
        replacement=re.sub(r'width="[0-9]+"', f'width="{width}"', tag)
        replacement=re.sub(r'height="[0-9]+"', f'height="{height}"', replacement)
        html=html.replace(tag,replacement)
p.write_text(html,encoding='utf-8')''')
print('Native branding, backup filtering and screenshot presentation corrected')
