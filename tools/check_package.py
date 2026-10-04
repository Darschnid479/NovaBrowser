"""Dependency-free package/link checks. Does NOT compile or execute Windows code."""
from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import urlsplit, unquote
import json, re, struct, sys
ROOT = Path(__file__).resolve().parents[1]
checks = 0

def check(label: str, ok: bool) -> None:
    global checks
    if not ok:
        raise AssertionError(label)
    checks += 1

class Resources(HTMLParser):
    def __init__(self):
        super().__init__(); self.refs = []; self.ids = []; self.images = []
    def handle_starttag(self, tag, attrs):
        d = dict(attrs)
        if 'id' in d: self.ids.append(d['id'])
        if tag == 'img': self.images.append(d)
        for attr in ('src','href'):
            if d.get(attr): self.refs.append(d[attr])

def main() -> None:
    for path in ROOT.rglob('*.html'):
        if any(part in ('.git','out','bin','obj','node_modules') for part in path.parts): continue
        parser = Resources(); parser.feed(path.read_text(encoding='utf-8-sig'))
        check(f'unique IDs in {path.name}',len(parser.ids)==len(set(parser.ids)))
        for img in parser.images: check(f'img alt in {path.name}', 'alt' in img)
        for ref in parser.refs:
            parsed = urlsplit(ref)
            if parsed.scheme or ref.startswith('//'): continue
            if parsed.path:
                target = path.parent / unquote(parsed.path)
                check(f'local resource {path.name}: {ref}', target.exists())
            elif parsed.fragment:
                check(f'anchor {path.name}: {ref}', unquote(parsed.fragment) in parser.ids)
    readme = (ROOT/'README.md').read_text()
    for ref in re.findall(r'\]\(([^)]+)\)|src="([^"]+)"', readme):
        url = ref[0] or ref[1]; p=urlsplit(url)
        if not p.scheme and not url.startswith('#'):
            check('README local resource: '+url,(ROOT/unquote(p.path)).exists())
    for image in ['midnight','dawn','forest','graphite','settings','setup']:
        p=ROOT/f'docs/assets/previews/nova-{image}.png'
        data=p.read_bytes(); check(p.name+' PNG',data[:8]==b'\x89PNG\r\n\x1a\n')
        check(p.name+' dimensions',struct.unpack('>II',data[16:24])==(1460,960))
    for file in ['LAST-OPP-NOVA-TIL-GITHUB.bat','SE-NETTSIDEN.bat','TA-EKTE-SKJERMBILDE.bat']:
        data=(ROOT/file).read_bytes()
        check(file+' has no UTF-8 BOM',not data.startswith(b'\xef\xbb\xbf'))
        check(file+' ASCII',data.isascii())
        check(file+' cmd header',data.startswith(b'@echo off'))
    for file in ['tools/NovaUpload.psm1','tools/Upload-Nova.ps1','tools/Capture-Nova.ps1','tests/Upload.Tests.ps1']:
        check(file+' ASCII for PowerShell 5.1',(ROOT/file).read_bytes().isascii())
    module=(ROOT/'tools/NovaUpload.psm1').read_text()
    check('upload targets requested repo','https://github.com/Darschnid479/NovaBrowser.git' in module)
    check('no forced Git push',not re.search(r"['\"](?:--force|-f|--force-with-lease)['\"]",module))
    check('no unrelated history merge','allow-unrelated-histories' not in module)
    check('explicit upload approval',"-ceq 'JA'" in module)
    check('separate clone','clone' in module and 'NOVA-GitHub-upload-' in module)
    check('pending captures excluded',"'artifacts'" in module and 'artifacts/' in (ROOT/'.gitignore').read_text())
    check('app preview labelled','HTML-rekonstruksjon' in (ROOT/'docs/preview.html').read_text())
    check('README distinguishes previews','Designforh' in readme and 'ikke skjermbilde fra Windows-appen' in readme)
    check('site motion preference','prefers-reduced-motion' in (ROOT/'docs/assets/site.css').read_text())
    for name in ['windows-build.yml','site-checks.yml','pages.yml','release.yml']:
        check('workflow present '+name,(ROOT/'.github/workflows'/name).exists())
    check('app version 0.3.0','<Version>0.3.0</Version>' in (ROOT/'src/NovaBrowser/NovaBrowser.vbproj').read_text())
    # Source archives must never contain font files or a browser user-data profile.
    for p in ROOT.rglob('*'):
        if not p.is_file() or any(part in ('.git','bin','obj','out','node_modules') for part in p.parts): continue
        check('no bundled font '+p.name,p.suffix.lower() not in ('.ttf','.otf','.woff','.woff2'))
        check('no profile data '+p.name,p.name not in ('state.json','Cookies','Login Data','error.log'))
    print(f'PASS: {checks} package assertions. Not a VB build, Windows run, or security audit.')

if __name__ == '__main__':
    try: main()
    except Exception as exc:
        print('FAIL: '+str(exc),file=sys.stderr);sys.exit(1)
