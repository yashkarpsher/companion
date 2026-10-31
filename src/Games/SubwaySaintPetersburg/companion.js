'use strict';
const noop=function(){};
window.PokiSDK={init:()=>Promise.resolve(),setDebug:noop,gameLoadingStart:noop,gameLoadingProgress:noop,gameLoadingFinished:noop,gameInteractive:noop,gameplayStart:noop,gameplayStop:noop,happyTime:noop,customEvent:noop,roundStart:noop,roundEnd:noop,setPlayerAge:noop,togglePlayerAdvertisingConsent:noop,displayAd:noop,destroyAd:noop};
window.initPokiBridge=function(name){
 setTimeout(()=>unityGame.SendMessage(name,'ready'),0);
 window.commercialBreak=function(){setTimeout(()=>unityGame.SendMessage(name,'commercialBreakCompleted'),0)};
 window.rewardedBreak=function(){setTimeout(()=>unityGame.SendMessage(name,'rewardedBreakCompleted','false'),0)};
};
let ready=false,paused=false,failed=false;
function notify(kind){if(window.chrome&&window.chrome.webview)window.chrome.webview.postMessage({kind:kind});}
window.companionPause=function(){
 paused=true;notify('paused');
 if(ready&&typeof unityGame.Module.pauseMainLoop==='function')unityGame.Module.pauseMainLoop();
 if(ready)document.getElementById('pause').hidden=false;
};
document.getElementById('resume').onclick=function(){
 if(typeof unityGame.Module.resumeMainLoop==='function')unityGame.Module.resumeMainLoop();
 paused=false;document.getElementById('pause').hidden=true;notify('resumed');
 const canvas=document.querySelector('#game canvas');if(canvas)canvas.focus();
};
window.addEventListener('blur',window.companionPause);
document.addEventListener('visibilitychange',()=>{if(document.hidden)window.companionPause();});
window.unityGame=UnityLoader.instantiate('game','Build/StPetersburg.json',{
 onProgress:function(game,p){if(!ready&&!failed)document.getElementById('status').textContent='Загрузка игры: '+Math.round(p*100)+'%';},
 Module:{onAbort:function(reason){failed=true;document.getElementById('status').textContent='Не удалось запустить игру. Закрой окно и попробуй снова.';console.error(reason);}}
});
function waitForGame(){
 if(failed)return;
 if(!unityGame.Module||!unityGame.Module.calledRun){setTimeout(waitForGame,250);return;}
 ready=true;document.getElementById('status').style.display='none';
 if(paused||document.hidden||!document.hasFocus())window.companionPause();
}
waitForGame();
