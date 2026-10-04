"""Optional browser checks for the static site; requires playwright + Chromium.
These are website checks, not tests of the NOVA Windows application.
"""
import os, json
from pathlib import Path
from playwright.sync_api import sync_playwright
from render_previews import html_for, ROOT, data

def main():
    passed = 0
    def check(label, ok):
        nonlocal passed
        if not ok: raise AssertionError(label)
        passed += 1
    with sync_playwright() as p:
        browser=p.chromium.launch(executable_path=os.environ.get('NOVA_CHROMIUM'),headless=True,args=['--no-sandbox'])
        for width in [1440,1024,768,390,320]:
            page=browser.new_page(viewport={'width':width,'height':900})
            errors=[];page.on('pageerror',lambda e:errors.append(str(e)))
            page.set_content(html_for(ROOT/'docs/index.html'))
            page.evaluate("document.querySelectorAll('img').forEach(i=>i.loading='eager')")
            page.wait_for_function('Array.from(document.images).every(i => i.complete && i.naturalWidth > 0)')
            check(f'no horizontal overflow at {width}',page.evaluate('document.documentElement.scrollWidth <= innerWidth'))
            check(f'all image assets loaded at {width}',page.evaluate('Array.from(document.images).every(i=>i.naturalWidth>0)'))
            for key in ['dawn','forest','graphite','midnight']:
                page.locator('[data-theme="'+key+'"]').click()
                check(f'{key} selected at {width}',page.locator('[data-theme][aria-pressed="true"]').count()==1)
                check(f'{key} asset switched at {width}',page.locator('#theme-preview').get_attribute('src')==data(ROOT/f'docs/assets/previews/nova-{key}.png'))
                check(f'{key} announcement at {width}',key in page.locator('#theme-description').inner_text().lower())
            page.locator('details summary').first.click()
            check(f'FAQ opens at {width}',page.locator('details[open]').count()==1)
            page.locator('details summary').first.click()
            check(f'FAQ closes at {width}',page.locator('details[open]').count()==0)
            check(f'no JS exceptions at {width}',not errors)
            page.close()
        page=browser.new_page(viewport={'width':1440,'height':900},reduced_motion='reduce')
        page.set_content(html_for(ROOT/'docs/index.html'))
        check('reduced motion respects scroll preference',page.evaluate('getComputedStyle(document.documentElement).scrollBehavior')=='auto')
        page.keyboard.press('Tab')
        check('keyboard skip link first',page.evaluate('document.activeElement.className')=='skip')
        page.close()
        palettes={'Midnight':'rgb(13, 15, 22)','Dawn':'rgb(243, 243, 248)','Forest':'rgb(12, 22, 21)','Graphite':'rgb(18, 18, 18)'}
        for theme, rgb in palettes.items():
            page=browser.new_page(viewport={'width':1460,'height':960})
            page.set_content(html_for(ROOT/'docs/preview.html','theme='+theme))
            check(theme+' preview palette',page.locator('#app').evaluate('(e)=>getComputedStyle(e).backgroundColor')==rgb)
            check(theme+' icons rendered',page.locator('[data-icon] svg').count()==page.locator('[data-icon]').count())
            check(theme+' visible provenance',page.locator('.disclosure').is_visible())
            page.close()
        for view in ['setup','settings']:
            page=browser.new_page(viewport={'width':1460,'height':960})
            page.set_content(html_for(ROOT/'docs/preview.html','view='+view))
            check(view+' overlay visible',page.locator('#'+view+'-view').is_visible())
            check(view+' provenance still visible',page.locator('.disclosure').is_visible())
            page.close()
        browser.close()
    print(f'PASS: {passed} local HTML browser checks. Not WPF or Windows application tests.')

if __name__=='__main__': main()
