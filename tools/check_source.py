"""Static source invariants only. This does not compile or run Visual Basic/WPF."""
from pathlib import Path
import json
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / 'src/NovaBrowser'
X = '{http://schemas.microsoft.com/winfx/2006/xaml}'
P = '{http://schemas.microsoft.com/winfx/2006/xaml/presentation}'
passed = []

def check(label, condition):
    if not condition:
        raise AssertionError(label)
    passed.append(label)


def run():
    for path in sorted(ROOT.rglob('*')):
        if path.suffix in {'.xaml', '.vbproj', '.manifest'}:
            ET.parse(path)
            check('XML parses: ' + str(path.relative_to(ROOT)), True)
    json.loads((ROOT / 'global.json').read_text())
    check('SDK selection JSON parses', True)
    vb = '\n'.join(p.read_text(encoding='utf-8') for p in SRC.rglob('*.vb'))
    markup = (SRC / 'MainWindow.xaml').read_text(encoding='utf-8')
    styles = (SRC / 'UI/Styles.xaml').read_text(encoding='utf-8')
    project = ET.parse(SRC / 'NovaBrowser.vbproj').getroot()
    check('Release version is 0.2.0', project.findtext('.//Version') == '0.2.0')
    check('Versioned Windows target retained', project.findtext('.//TargetFramework') == 'net10.0-windows10.0.17763.0')
    check('WPF enabled', project.findtext('.//UseWPF') == 'true')
    for prop, value in [('CopyLocalLockFileAssemblies','true'),('PublishTrimmed','false'),('PublishSingleFile','false')]:
        check(prop + ' retained', project.findtext('.//' + prop) == value)
    check('Both WinRT runtime output guards retained', 'Microsoft.Windows.SDK.NET.dll;WinRT.Runtime.dll' in (SRC / 'NovaBrowser.vbproj').read_text())
    all_xaml = [ET.parse(p).getroot() for p in SRC.rglob('*.xaml')]
    main = ET.fromstring(markup)
    names = [n.get(X + 'Name') for n in main.iter() if n.get(X + 'Name')]
    check('All main-window control names are unique', len(names) == len(set(names)))
    events = {'Loaded','Closing','PreviewKeyDown','StateChanged','Click','KeyDown','SizeChanged','SelectionChanged','MouseLeftButtonDown','TextChanged','GotKeyboardFocus','LostKeyboardFocus','Checked','Unchecked','MouseUp','PreviewMouseDown'}
    used_handlers = set()
    for root in all_xaml:
        for node in root.iter():
            for attr,value in node.attrib.items():
                if attr in events:
                    used_handlers.add(value)
                    check('Handler declared: ' + value, re.search(r'\b(?:Sub|Function)\s+' + re.escape(value) + r'\s*\(',vb,re.I) is not None)
    for control in ['ReloadIcon','ConnectionIcon','BookmarkIcon','MaximizeIcon','HomeSearchHint','SetupLayer','SetupNextButton']:
        check('Required named control exists: ' + control, names.count(control) == 1)
    check('No MDL2 dependency in current source', 'Segoe MDL2' not in markup + styles + vb)
    check('No private-use icon characters', not re.search('[\ue000-\uf8ff]', markup + styles + vb))
    check('No private-use numeric icon entities', not re.search(r'&#x[Ee][0-9A-Fa-f]{3};', markup + styles))
    check('Old font Glyph property removed', 'Binding Glyph' not in markup and 'NameOf(Glyph)' not in vb)
    icon_source = (SRC / 'UI/NovaIcon.vb').read_text()
    kinds = set(re.findall(r'\{"([^"\n]+)", "M', icon_source))
    check('All 28 vector kinds are present', len(kinds) == 28)
    for root in all_xaml:
        for node in root.iter():
            if node.tag.endswith('}NovaIcon'):
                kind=node.get('Kind','')
                if not kind.startswith('{'):
                    check('Vector reference exists: '+kind, kind in kinds)
    check('Icon geometry is frozen before sharing', 'shape.Freeze()' in icon_source)
    style_root = ET.fromstring(styles)
    textbox_style = next(e for e in style_root if e.tag == P + 'Style' and e.get('TargetType') == 'TextBox' and e.get(X+'Key') is None)
    host = next(e for e in textbox_style.iter() if e.get(X+'Name') == 'PART_ContentHost')
    check('No duplicate text padding via margin', host.get('Margin') == '0')
    check('Text host binds vertical alignment', host.get('VerticalAlignment') == '{TemplateBinding VerticalContentAlignment}')
    address = next(e for e in main.iter() if e.get(X+'Name') == 'AddressBox')
    check('Address uses dedicated input style', address.get('Style') == '{StaticResource AddressInput}')
    address_style = next(e for e in style_root if e.get(X+'Key') == 'AddressInput')
    setters = {e.get('Property'):e.get('Value') for e in address_style if e.tag == P+'Setter'}
    for prop,value in [('Padding','0'),('TextAlignment','Left'),('VerticalContentAlignment','Center'),('TextWrapping','NoWrap')]:
        check('Address ' + prop, setters.get(prop) == value)
    check('Home input also uses corrected style', next(e for e in main.iter() if e.get(X+'Name') == 'HomeSearchBox').get('Style') == '{StaticResource AddressInput}')
    model=(SRC/'Models/AppState.vb').read_text()
    setup=(SRC/'MainWindow.Setup.vb').read_text()
    flow=(SRC/'Models/SetupSession.vb').read_text()
    window=(SRC/'MainWindow.xaml.vb').read_text()
    check('Missing completion marker defaults to False', 'Property SetupCompleted As Boolean = False' in model)
    check('Wizard uses a copied draft', 'Draft = original.Copy()' in flow)
    check('First-run search requires selection', 'If Not original.SetupCompleted Then Draft.SearchEngine = ""' in flow)
    check('Incomplete wizard cannot complete', 'StepIndex <> 2 OrElse Not HasSearchSelection' in flow)
    check('No session persistence before session init', 'If _fatalShutdown OrElse Not _sessionInitialized Then Return' in window)
    check('Startup branches on setup marker', re.search(r'If _state.Settings.SetupCompleted Then\s+StartBrowsingSession\(\)\s+Else\s+ShowSetup\(\)',window) is not None)
    complete_method=setup.split('Private Sub FinishSetup()',1)[1].split('End Sub',1)[0]
    check('Complete setup saves before restoring session', complete_method.index('StateStore.Save(_state)') < complete_method.index('StartBrowsingSession()'))
    check('Save failure restores previous settings', '_state.Settings = oldSettings' in complete_method)
    check('Wizard cancel does not save preview', 'StateStore.Save' not in setup.split('Private Sub CancelSetup()',1)[1].split('End Sub',1)[0])
    check('Search provider catalogue reused by resolver', 'SearchProviders.GetProvider(provider).QueryPrefix' in (SRC/'Services/UrlPolicy.vb').read_text())
    check('Search provider catalogue reused by settings', 'SearchSetting.ItemsSource = SearchProviders.Names' in vb)
    check('Search provider catalogue reused by wizard', 'SetupSearchList.ItemsSource = SearchProviders.All' in setup)
    check('No old untyped tab loops', not re.search(r'For Each tab\b',vb,re.I))
    check('No old key inference collision', not re.search(r'Dim key\s*=\s*If\(.*Key\.',vb,re.I))
    for path in ROOT.rglob('*.vbproj'):
        proj=ET.parse(path).getroot()
        for node in proj.iter():
            if node.tag in {'Compile','Page'} and node.get('Include'):
                check('Linked source exists: '+node.get('Include'), (path.parent / node.get('Include')).exists())
    solution=(ROOT/'NOVA.sln').read_text()
    for proj_path in re.findall(r'"([^"\n]+\.vbproj)"',solution):
        check('Solution project exists: ' + proj_path, (ROOT / proj_path.replace('\\','/')).exists())
    for path in ROOT.glob('*.cmd'):
        data=path.read_bytes()
        check('ASCII launch script: '+path.name, data.isascii())
        check('CRLF launch script: '+path.name, b'\r\n' in data and b'\n' not in data.replace(b'\r\n',b''))
    banned={'.ttf','.otf','.woff','.woff2','.dll','.exe','.pdb','.log'}
    check('No font, compiled, or user-log files shipped', not any(p.suffix.lower() in banned for p in ROOT.rglob('*') if p.is_file()))
    check('No browser profile data shipped', not any(p.name in {'state.json','WebView2'} for p in ROOT.rglob('*')))
    return len(names),len(used_handlers)

if __name__ == '__main__':
    try:
        names, handlers = run()
        print('NOVA 0.2.0 STATIC SOURCE CHECKS - NOT A BUILD OR RUNTIME TEST')
        for label in dict.fromkeys(passed):
            print('PASS: ' + label)
        print(f'PASS: {len(passed)} source assertions; {names} unique controls; {handlers} handlers.')
        print('NOT RUN by this tool: VB compilation, WPF execution, navigation, or privacy/security tests.')
    except (AssertionError, ET.ParseError, OSError) as error:
        print('FAIL: ' + str(error), file=sys.stderr)
        sys.exit(1)
