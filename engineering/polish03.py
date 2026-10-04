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
# Correct the generated site: the original JS concatenates strings, not templates.
edit('engineering/present03.py',"js=js.replace('assets/previews/nova-','assets/app-0.3.0/nova-03-').replace('${theme}.png','${theme}-client.png').replace('${key}.png','${key}-client.png')",'''js=js.replace('assets/previews/nova-','assets/app-0.3.0/nova-03-').replace('${theme}.png','${theme}-client.png').replace('${key}.png','${key}-client.png')
js=js.replace("+key+'.png'", "+key+'-client.png'").replace('HTML-designforhåndsvisning','Faktisk WPF-klientflate')
js=js.replace('Forest + Mint. Dype grønntoner og en frisk aksent.', 'Forest + Iris. Dype grønntoner med lilla aksenter.')
js=js.replace('Graphite + Blue. Nøytral grå med en kjølig blå aksent.', 'Graphite + Iris. Nøytral grå med lilla aksenter.')''')
# Include the now-extended, locally executed uploader test in the evidence bundle.
edit('engineering/present03.py',"'publish.log','source-checks.log','package-checks.log']:","'publish.log','source-checks.log','package-checks.log','upload-tests.log']:")
# Preserve pixel aspect ratios instead of the old reconstruction's fixed dimensions.
edit('engineering/present03.py',"p.write_text(html,encoding='utf-8')",'''html=html.replace('width="1460" height="960"', 'width="1264" height="781"')
p.write_text(html,encoding='utf-8')''')
print('Native branding, backup filtering and screenshot presentation corrected')
