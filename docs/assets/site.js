(()=>{
"use strict";
const themes = {
 midnight: ['Midnight', 'Midnight + Iris. Et mørkt utgangspunkt med et mykt, lilla uttrykk.'],
 dawn: ['Dawn', 'Dawn + Iris. En lys variant, med tydelig tekst og rolige flater.'],
 forest: ['Forest', 'Forest + Iris. Dype grønntoner med lilla aksenter.'],
 graphite: ['Graphite', 'Graphite + Iris. Nøytral grå med lilla aksenter.']
};
document.querySelectorAll('[data-theme]').forEach(button => button.addEventListener('click', () => {
 const key=button.dataset.theme;if(!Object.hasOwn(themes,key))return;
 document.querySelectorAll('[data-theme]').forEach(b=>b.setAttribute('aria-pressed',String(b===button)));
 const image=document.querySelector('#theme-preview');image.src='assets/app-0.3.0/nova-03-'+key+'-client.png';image.alt='Faktisk WPF-klientflate av NOVA i '+themes[key][0]+'-tema';
 document.querySelector('#theme-description').textContent=themes[key][1];
}));

})();
