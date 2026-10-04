"""Rebuild labelled HTML previews and website screenshots, NOT Windows app captures.
Optional tooling: pip install playwright; playwright install chromium.
Uses only local assets in memory. Set NOVA_CHROMIUM to an existing browser path.
"""
from pathlib import Path
from playwright.sync_api import sync_playwright
import re,base64,mimetypes,json,os
ROOT=Path(__file__).resolve().parents[1]
def data(path):
 return 'data:'+mimetypes.guess_type(path.name)[0]+';base64,'+base64.b64encode(path.read_bytes()).decode()
def html_for(path,query=''):
 s=path.read_text()
 s=re.sub(r'<link rel="stylesheet" href="([^"]+)">',lambda m:'<style>'+ (path.parent/m[1]).read_text()+'</style>',s)
 def script(m):
  code=(path.parent/m[1]).read_text().replace('new URLSearchParams(location.search)','new URLSearchParams('+json.dumps(query)+')')
  if 'site.js' in m[1]:
   mapping={k:data(ROOT/f'docs/assets/previews/nova-{k}.png') for k in ['midnight','dawn','forest','graphite']}
   code=code.replace("'assets/previews/nova-'+key+'.png'",'('+json.dumps(mapping)+')[key]')
  return '<script>'+code+'</script>'
 # Defer scripts are moved to the end for in-memory local rendering.
 scripts=[]
 for match in list(re.finditer(r'<script src="([^"]+)"(?: defer)?></script>',s)):
  scripts.append(script(match));s=s.replace(match[0],'')
 s=re.sub(r'(<img[^>]*src=")([^"]+)(")',lambda m:m[1]+data(path.parent/m[2])+m[3],s)
 s=s.replace('</body>',''.join(scripts)+'</body>')
 return s
if __name__=='__main__':
 with sync_playwright() as p:
  b=p.chromium.launch(executable_path=os.environ.get('NOVA_CHROMIUM'),headless=True,args=['--no-sandbox'])
  page=b.new_page(viewport={'width':1460,'height':960},device_scale_factor=1)
  for theme in ['Midnight','Dawn','Forest','Graphite']:
   page.set_content(html_for(ROOT/'docs/preview.html','theme='+theme));page.wait_for_timeout(50)
   page.screenshot(path=str(ROOT/f'docs/assets/previews/nova-{theme.lower()}.png'))
  for view in ['setup','settings']:
   page.set_content(html_for(ROOT/'docs/preview.html','view='+view));page.wait_for_timeout(50)
   page.screenshot(path=str(ROOT/f'docs/assets/previews/nova-{view}.png'))
  page.set_viewport_size({'width':1280,'height':640})
  page.set_content(html_for(ROOT/'tools/brand-artwork.html'));page.screenshot(path=str(ROOT/'docs/assets/brand/social-card.png'))
  page.set_viewport_size({'width':1440,'height':1080})
  page.set_content(html_for(ROOT/'docs/index.html'));page.evaluate("document.querySelectorAll('img').forEach(i=>i.loading='eager')");page.wait_for_function('Array.from(document.images).every(i=>i.complete && i.naturalWidth>0)')
  # Full-page website captures below are actual HTML screenshots.
  page.screenshot(path=str(ROOT/'docs/assets/screenshots/website-desktop.png'),full_page=True)
  page.set_viewport_size({'width':390,'height':844});page.wait_for_timeout(100)
  page.screenshot(path=str(ROOT/'docs/assets/screenshots/website-mobile.png'),full_page=True)
  b.close()
 print('Rendered HTML previews, brand asset, and actual website screenshots using in-memory local assets.')
