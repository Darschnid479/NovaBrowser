"""One-time finalization of the tested proposal. Never targets main or a release."""
import os, subprocess, base64
from pathlib import Path
R=Path(__file__).resolve().parents[1]
assert os.environ.get('GITHUB_REPOSITORY') == 'Darschnid479/NovaBrowser'
assert os.environ.get('GITHUB_REF') == 'refs/heads/work/nova-0.3.0'
assert 'PASS: real WebView2 smoke test' in (R/'artifacts/shell-tests.log').read_text(encoding='utf-8-sig')
assert (R/'docs/validation/0.3.0/evidence.json').exists()
assert '<Version>0.3.0</Version>' in (R/'src/NovaBrowser/NovaBrowser.vbproj').read_text()

def git(*args, env=None, capture=False):
    result=subprocess.run(['git',*args],cwd=R,env=env,text=True,capture_output=capture,check=True)
    return result.stdout.strip() if capture else ''

token=os.environ['GH_TOKEN']
auth=os.environ.copy()
auth['GIT_CONFIG_COUNT']='1'
auth['GIT_CONFIG_KEY_0']='http.https://github.com/.extraheader'
auth['GIT_CONFIG_VALUE_0']='AUTHORIZATION: basic '+base64.b64encode(('x-access-token:'+token).encode()).decode()
remote=git('ls-remote','--heads','origin','refs/heads/work/nova-0.3.0',env=auth,capture=True).split()[0]
if remote != os.environ['GITHUB_SHA']:
    raise RuntimeError('Development branch advanced during validation. Refusing to overwrite concurrent work.')

# Explicit paths: no workflow files, credentials, profiles or compiled binaries.
allowed=['src','tests','tools','docs','README.md','CHANGELOG.md','LES-MEG-FORST.txt','START-HER.txt','START-NOVA.cmd','BYGG-EXE.cmd','AAPNE-FEILLOGG.cmd','TEST-ALT.cmd','NOVA.sln','Directory.Build.props','.gitignore']
git('add','--',*allowed)
files=git('diff','--cached','--name-only',capture=True).splitlines()
for name in files:
    if name.startswith(('.github/','engineering/')) or any(part in {'bin','obj','out','artifacts','.git','WebView2','profiles'} for part in Path(name).parts):
        raise RuntimeError('Unexpected staged path: '+name)
    if Path(name).name.startswith('state.json') or Path(name).suffix.lower() in {'.exe','.dll','.pfx','.pem','.key','.ttf','.woff','.woff2','.otf'}:
        raise RuntimeError('Private, compiled or font file not allowed: '+name)
if not files:
    raise RuntimeError('No materialized source changes found')
git('config','user.name','github-actions[bot]')
git('config','user.email','41898282+github-actions[bot]@users.noreply.github.com')
git('commit','-m','NOVA 0.3.0: materialize Windows-tested source, real app captures and validation evidence')
commit=git('rev-parse','HEAD',capture=True)
git('push','origin','HEAD:refs/heads/work/nova-0.3.0',env=auth)
(R/'artifacts/materialized-commit.txt').write_text(commit+'\n',encoding='utf-8')
print('Verified source committed to work/nova-0.3.0 only: '+commit)
