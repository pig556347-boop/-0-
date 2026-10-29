import {ITEM_CATALOG} from './items.mjs';

// Original inventory presentation. Items and rules remain in ItemSystem;
// this view never grants items or changes capacity itself.
export function createInventoryUI({items,game,onClose=()=>{},onAction=()=>{}}) {
 const style=document.createElement('style');
 style.textContent=`
 #inventory-screen{position:fixed;inset:0;z-index:15;display:flex;align-items:center;justify-content:center;background:#061018dd;backdrop-filter:blur(6px);padding:24px;color:#d8ddd0;font-family:School,'Noto Sans',sans-serif}#inventory-screen[hidden]{display:none}
 #inventory-screen .inv-panel{width:min(820px,94vw);max-height:92vh;overflow:auto;padding:27px 30px 25px;background:linear-gradient(140deg,#17292d,#101c22 70%);border:1px solid #8f9b785c;box-shadow:0 24px 90px #0008}
 #inventory-screen .inv-head{display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid #81907a38;padding-bottom:17px;margin-bottom:23px}#inventory-screen .inv-head h2{font-size:27px;font-weight:400;letter-spacing:4px;margin:0 0 7px}#inventory-screen .inv-head p{margin:0;font-size:11px;color:#a4b29e}#inventory-screen .inv-close{color:#d4d4b7;background:#ffffff08;border:1px solid #9b9f7760;padding:8px 13px;font-size:12px}
 #inventory-screen .inv-layout{display:grid;grid-template-columns:minmax(300px,1fr) 255px;gap:27px}#inventory-screen .inv-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:9px}#inventory-screen .inv-slot{min-height:99px;padding:13px 8px 10px;border:1px solid #7e91834d;color:#cfcbb0;background:#09171c66;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:8px;position:relative;font-size:11px;line-height:1.45;transition:border-color .12s,background .12s}#inventory-screen .inv-slot:hover{background:#33433d7a;border-color:#b2b08f}#inventory-screen .inv-slot[aria-pressed=true]{border-color:#d1bb82;background:#70633b26;box-shadow:inset 0 0 0 1px #d1bb8244}#inventory-screen .inv-slot.empty{color:#667c76}#inventory-screen .inv-slot .inv-number{position:absolute;left:8px;top:6px;font:9px monospace;color:#8a9c8e}#inventory-screen .inv-slot svg{width:34px;height:34px;display:block;color:var(--item-color,#c3ba93);flex-shrink:0}#inventory-screen .inv-slot .inv-slot-name{display:block;text-align:center;word-break:keep-all}
 #inventory-screen .inv-wear-head{display:flex;justify-content:space-between;font-size:10px;color:#a5b39f;margin:20px 0 9px;letter-spacing:1px}#inventory-screen .inv-wear{width:100%;min-height:72px;flex-direction:row;justify-content:flex-start;padding:13px 18px;gap:16px}#inventory-screen .inv-wear svg{width:30px;height:30px}#inventory-screen .inv-wear.empty .inv-slot-name{color:#7b9086}
 #inventory-screen .inv-details{border-left:1px solid #83927c30;padding-left:25px;display:flex;flex-direction:column;min-height:340px}#inventory-screen .inv-detail-icon{height:65px;display:flex;align-items:center;color:var(--item-color,#c4bb92)}#inventory-screen .inv-detail-icon svg{width:47px;height:47px}#inventory-screen .inv-type{font:9px monospace;letter-spacing:1px;color:#a6aa8a;margin:12px 0 8px}#inventory-screen .inv-name{font-size:20px;line-height:1.5;font-weight:400;margin:0 0 13px;word-break:keep-all}#inventory-screen .inv-description{font-size:12px;line-height:1.9;color:#b1c0b3;white-space:pre-line;margin:0;word-break:keep-all}#inventory-screen .inv-actions{margin-top:auto;padding-top:23px;display:grid;gap:8px}#inventory-screen .inv-action{padding:11px 13px;background:#c0af88;color:#142328;border:1px solid #c5b991;font-size:12px;text-align:left}#inventory-screen .inv-action.secondary{background:transparent;color:#b7bfa7;border-color:#85937b58}#inventory-screen .inv-action:disabled{opacity:.35;cursor:default}#inventory-screen .inv-action[hidden]{display:none}#inventory-screen .inv-note{font-size:10px;line-height:1.7;color:#7f958b;border-top:1px solid #82907b2f;margin:23px 0 0;padding-top:14px}#inventory-screen .inv-feedback{font-size:11px;min-height:18px;color:#d0bd91;margin:13px 0 0;line-height:1.7}
 @media(max-width:650px){#inventory-screen{padding:12px}#inventory-screen .inv-panel{padding:20px 17px}#inventory-screen .inv-layout{grid-template-columns:1fr;gap:20px}#inventory-screen .inv-details{border-left:0;border-top:1px solid #83927c30;padding:19px 0 0;min-height:0}#inventory-screen .inv-detail-icon{display:none}#inventory-screen .inv-slot{min-height:82px}#inventory-screen .inv-slot svg{width:28px;height:28px}#inventory-screen .inv-type{margin-top:0}#inventory-screen .inv-actions{grid-template-columns:1fr 1fr;padding-top:15px}#inventory-screen .inv-name{font-size:17px}#inventory-screen .inv-description{font-size:11px}#inventory-screen .inv-note{margin-top:16px}}
 `;
 document.head.append(style);
 const root=document.createElement('div');root.id='inventory-screen';root.hidden=true;root.setAttribute('role','dialog');root.setAttribute('aria-modal','true');root.setAttribute('aria-labelledby','inventory-title');
 root.innerHTML=`<section class="inv-panel"><div class="inv-head"><div><h2 id="inventory-title">소지품</h2><p><span id="inventory-count">0 / 9</span> · 가방</p></div><button type="button" class="inv-close" id="inventory-close" aria-label="인벤토리 닫기">닫기 <span aria-hidden="true">×</span></button></div><div class="inv-layout"><div><div class="inv-grid" id="inventory-grid"></div><div class="inv-wear-head"><span>착용 아이템</span><span>1칸</span></div><button type="button" class="inv-slot inv-wear empty" id="inventory-equipped" aria-pressed="false"></button></div><div class="inv-details"><div class="inv-detail-icon" id="inventory-detail-icon" aria-hidden="true"></div><p class="inv-type" id="inventory-type">EMPTY</p><h3 class="inv-name" id="inventory-name">빈칸</h3><p class="inv-description" id="inventory-description">아이템을 선택하면 설명을 볼 수 있습니다.</p><div class="inv-actions"><button type="button" class="inv-action" id="inventory-use" hidden>사용하기</button><button type="button" class="inv-action" id="inventory-equip" hidden>착용하기</button><button type="button" class="inv-action secondary" id="inventory-drop" disabled>바닥에 내려놓기</button></div></div></div><p class="inv-feedback" id="inventory-feedback" role="status"></p><p class="inv-note">가방 9칸 · 착용 1칸. 소지품을 여는 동안 이동과 추격이 멈춥니다.<br>시계초침·시계분침·시계본체를 모으면 나침반이 자동으로 만들어집니다.</p></section>`;
 document.body.append(root);
 const $=id=>root.querySelector('#'+id),slots=[];let selected=0,selectWear=false,signature='',lastFocus=null;
 const definition=id=>items.catalog?.[id]||ITEM_CATALOG[id]||{label:id,name:id,type:'item',description:'',color:'#c3ba93'};
 const slotMarkup=(item,index)=>`<span class="inv-number" aria-hidden="true">${index+1}</span>${item?itemIcon(item.itemId):'<span aria-hidden="true" style="font:22px monospace;opacity:.35">·</span>'}<span class="inv-slot-name"></span>`;
 for(let i=0;i<9;i++){
  const button=document.createElement('button');button.type='button';button.className='inv-slot empty';button.id='inventory-slot-'+i;button.dataset.slot=String(i);button.setAttribute('aria-pressed','false');
  button.onclick=()=>{selected=i;selectWear=false;signature='';update()};$('inventory-grid').append(button);slots.push(button);
 }
 $('inventory-equipped').onclick=()=>{selectWear=true;signature='';update()};
 function action(method){
  const result=method();$('inventory-feedback').textContent=result?.message||'';onAction(result);signature='';update();return result;
 }
 $('inventory-use').onclick=()=>action(()=>items.use(selected));
 $('inventory-equip').onclick=()=>action(()=>selectWear?items.unequip():items.equip(selected));
 $('inventory-drop').onclick=()=>action(()=>selectWear?items.dropEquipped():items.drop(selected));
 function selectedItem(){return selectWear?items.equipped:items.inventory[selected]}
 function update(){
  const key=JSON.stringify([items.inventory.map(i=>[i.uid,i.itemId]),items.equipped&&[items.equipped.uid,items.equipped.itemId],selected,selectWear]);
  if(key===signature)return;signature=key;
  $('inventory-count').textContent=items.inventory.length+' / 9';
  slots.forEach((button,i)=>{
   const item=items.inventory[i],def=item&&definition(item.itemId);button.classList.toggle('empty',!item);button.setAttribute('aria-pressed',String(!selectWear&&i===selected));button.dataset.itemId=item?.itemId||'';
   const contentKey=item?item.uid+':'+item.itemId:'empty';
   if(button.dataset.contentKey!==contentKey){button.innerHTML=slotMarkup(item,i);button.querySelector('.inv-slot-name').textContent=def?(def.label||def.name):'빈칸';button.dataset.contentKey=contentKey;}
   button.style.setProperty('--item-color',def?.color||'#c3ba93');button.setAttribute('aria-label',`${i+1}번 칸 · ${def?.label||def?.name||'빈칸'}`);
  });
  const wear=items.equipped,wearDef=wear&&definition(wear.itemId),wearButton=$('inventory-equipped');
  wearButton.classList.toggle('empty',!wear);wearButton.setAttribute('aria-pressed',String(selectWear));wearButton.dataset.itemId=wear?.itemId||'';
  const wearKey=wear?wear.uid+':'+wear.itemId:'empty';if(wearButton.dataset.contentKey!==wearKey){wearButton.innerHTML=(wear?itemIcon(wear.itemId):itemIcon('smartwatch'))+'<span class="inv-slot-name"></span>';wearButton.querySelector('.inv-slot-name').textContent=wearDef?(wearDef.label||wearDef.name):'착용한 아이템 없음';wearButton.dataset.contentKey=wearKey;}
  wearButton.style.setProperty('--item-color',wearDef?.color||'#73867e');wearButton.setAttribute('aria-label','착용 칸 · '+(wearDef?.label||wearDef?.name||'비어 있음'));
  const item=selectedItem(),def=item&&definition(item.itemId),typeLabels={key:'탈출 열쇠',consumable:'소모품',component:'시계 부품',part:'시계 부품',tool:'도구',wearable:'착용 아이템',equipment:'착용 아이템',note:'쪽지 · 잠금 해제 시 소모'};
  $('inventory-detail-icon').innerHTML=item?itemIcon(item.itemId):'';$('inventory-detail-icon').style.setProperty('--item-color',def?.color||'#c3ba93');
  $('inventory-type').textContent=def?(typeLabels[def.type]||'소지품'):'EMPTY';$('inventory-name').textContent=def?(def.label||def.name):selectWear?'착용한 아이템 없음':'빈칸';
  $('inventory-description').textContent=def?def.description:selectWear?'스마트워치를 착용하면 가까워지는 당직자를 심장박동 소리로 알아챌 수 있습니다.':'책상과 수납장 등을 조사하거나 바닥의 아이템을 주워 보세요.';
  const use=$('inventory-use'),equip=$('inventory-equip');use.hidden=!item||selectWear||!(def.usable||['vitamin-water','chocolate-bar'].includes(item.itemId));
  use.textContent=item?.itemId==='vitamin-water'?'비타민워터 마시기':item?.itemId==='chocolate-bar'?'초코바 먹기':'사용하기';
  equip.hidden=!item||!(selectWear||def.wearable||item.itemId==='smartwatch');equip.textContent=selectWear?'가방으로 벗기':'손목에 착용하기';
  $('inventory-drop').disabled=!item;
 }
 function open(){if(!root.hidden)return;lastFocus=document.activeElement;root.hidden=false;$('inventory-feedback').textContent='';signature='';update();$('inventory-close').focus({preventScroll:true})}
 function close(){if(root.hidden)return;root.hidden=true;onClose();if(lastFocus?.isConnected&&lastFocus.getClientRects().length&&!root.contains(lastFocus))lastFocus.focus({preventScroll:true});else document.getElementById('world')?.focus()}
 function toggle(){if(root.hidden)open();else close()}
 $('inventory-close').onclick=close;
 root.addEventListener('keydown',event=>{
  if(root.hidden)return;
  if(event.code==='Escape'){event.preventDefault();event.stopImmediatePropagation();close();return;}
  if(event.code==='Tab'){
   const nodes=[...root.querySelectorAll('button:not([disabled])')].filter(n=>!n.hidden),first=nodes[0],last=nodes.at(-1);
   if(event.shiftKey&&document.activeElement===first){event.preventDefault();last?.focus()}else if(!event.shiftKey&&document.activeElement===last){event.preventDefault();first?.focus()}
  }
 });
 update();
 return {root,open,close,toggle,get isOpen(){return !root.hidden},update};
}

export function itemIcon(itemId){
 let body='';
 if(itemId.startsWith('key-'))body=`<circle cx="10" cy="10" r="5"/><path d="m13.5 13.5 12 12m-4-4 3-3m-6 0 3-3"/><text x="25" y="12" font-size="10" stroke="none" fill="currentColor" text-anchor="middle" font-family="monospace">${itemId.slice(-1).toUpperCase()}</text>`;
 else if(itemId==='vitamin-water')body='<path d="M13 4h6v4l3 4v15H10V12l3-4Z"/><path d="M13 4h6M11 17h10m-8 5 3-4 3 4"/>';
 else if(itemId==='chocolate-bar')body='<path d="m9 4 16 5-3 22-16-5Z"/><path d="m9 11 15 5m-16 3 15 5m-8-18-4 21M8 8l-3 3m19 15 3 2"/>';
 else if(itemId==='smartwatch')body='<path d="M12 3h8l1 7H11Zm-1 21h10l-1 7h-8Z"/><rect x="7" y="9" width="18" height="16" rx="4"/><path d="M10 18h3l2-4 3 8 2-4h3"/>';
 else if(itemId==='compass')body='<circle cx="16" cy="18" r="11"/><path d="m16 10 5 13-5-3-5 3Z"/><path d="M16 3v4m-2-4h4"/>';
 else if(itemId==='clock-second-hand')body='<circle cx="15" cy="23" r="3"/><path d="m15 20 5-17m-6 23-1 4"/>';
 else if(itemId==='clock-minute-hand')body='<circle cx="14" cy="23" r="3"/><path d="m14 20-1-16 4 2m-4-2-3 2"/>';
 else if(itemId==='clock-body')body='<circle cx="16" cy="17" r="12"/><circle cx="16" cy="17" r="8"/><path d="M16 7v2m0 16v2M6 17h2m16 0h2"/>';
 else body='<path d="M7 10h18v17H7Zm3 0V6h12v4m-7 7h2"/>';
 return `<svg viewBox="0 0 32 34" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${body}</svg>`;
}
