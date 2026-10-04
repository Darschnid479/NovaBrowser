(()=>{
"use strict";
const themes = {
 midnight: ['Midnight', 'Midnight + Iris. Et mørkt utgangspunkt med et mykt, lilla uttrykk.'],
 dawn: ['Dawn', 'Dawn + Iris. En lys variant, med tydelig tekst og rolige flater.'],
 forest: ['Forest', 'Forest + Mint. Dype grønntoner og en frisk aksent.'],
 graphite: ['Graphite', 'Graphite + Blue. Nøytral grå med en kjølig blå aksent.']
};
document.querySelectorAll('[data-theme]').forEach(button => button.addEventListener('click', () => {
 const key=button.dataset.theme;if(!Object.hasOwn(themes,key))return;
 document.querySelectorAll('[data-theme]').forEach(b=>b.setAttribute('aria-pressed',String(b===button)));
 const image=document.querySelector('#theme-preview');image.src='assets/previews/nova-'+key+'.png';image.alt='HTML-designforhåndsvisning av NOVA i '+themes[key][0]+'-tema';
 document.querySelector('#theme-description').textContent=themes[key][1];
}));

})();
