'use strict';
(()=>{
const $=id=>document.getElementById(id),canvas=$('scene'),game=new RunnerCore();
const gl=canvas.getContext('webgl',{alpha:true,antialias:false,powerPreference:'low-power',preserveDrawingBuffer:false});
let running=false,frameId=0,last=0,lastHud=0,lastRecord=0,frames=0,paused=false,quality='medium',fps=30,best=0;
const telemetry={drawCalls:0,triangles:0,frames:0,running:false};window.runnerTelemetry=telemetry;
function read(key,fallback){try{return localStorage.getItem(key)||fallback;}catch{return fallback;}}
function write(key,value){try{localStorage.setItem(key,String(value));}catch{}}
best=Number(read('best','0'))||0;quality=read('quality','low');if(!['low','medium','high'].includes(quality))quality='medium';
fps=read('fps','30')==='60'?60:30;$('quality').value=quality;$('fps').value=String(fps);
function hostScore(){if(game.score>best){best=game.score;write('best',best);}if(window.chrome?.webview)window.chrome.webview.postMessage({kind:'record',score:best});}
function hud(){ $('score').textContent=game.score;$('coins').textContent=game.coins;$('best').textContent=Math.max(best,game.score);$('chase').textContent=game.over?'АРЕСТОВАН':game.catching?'ДОГОНЯЕТ...':game.pursuit>.01?'ПОЛИЦЕЙСКИЙ НА ХВОСТЕ':'ОТОРВАЛИСЬ';$('chase').classList.toggle('safe',game.pursuit<=.01);$('speed').textContent='СКОРОСТЬ ×'+(game.speed/14).toFixed(2)+(game.speed===32?' / МАКС':'');}
function overlay(title,description,button){$('overlay').classList.toggle('result',game.over);$('title').textContent=title;$('description').textContent=description;$('play').innerHTML=button+' <span>↗</span>';$('overlay').hidden=false;}
function stop(){running=false;telemetry.running=false;cancelAnimationFrame(frameId);frameId=0;}
function pause(){if(!running)return;stop();paused=true;hostScore();overlay('ПАУЗА','Город подождёт. Продолжи, когда будешь готов.','ПРОДОЛЖИТЬ');draw();}
function finish(){stop();paused=false;hostScore();$('eyebrow').textContent=game.score>=best?'НОВЫЙ РЕКОРД / COMPANION':'ЕЩЁ ОДИН ЗАБЕГ?';overlay('АРЕСТОВАН',game.score+' очков · '+game.coins+' монет · '+Math.floor(game.distance)+' метров','ИГРАТЬ ЕЩЁ');hud();draw();}
function play(){if(!gl||document.hidden||!document.hasFocus())return;if(!paused||game.over){game.start();lastRecord=0;}paused=false;$('config').hidden=true;$('overlay').hidden=true;running=true;telemetry.running=true;last=performance.now();lastHud=0;frameId=requestAnimationFrame(frame);}
function frame(now){if(!running)return;frameId=requestAnimationFrame(frame);let dt=(now-last)/1000;if(dt<1/fps-.001)return;if(dt>.5){pause();return;}last=now;game.step(dt);draw();if(now-lastHud>100){hud();lastHud=now;}if(game.time-lastRecord>10){hostScore();lastRecord=game.time;}if(game.over)finish();}
function act(action){if(!running)return;if(action==='left')game.move(-1);if(action==='right')game.move(1);if(action==='jump')game.jump();if(action==='duck')game.duck();}
$('play').onclick=play;$('pause').onclick=()=>running?pause():paused?play():null;
$('settings').onclick=()=>{if(running)pause();$('config').hidden=!$('config').hidden;};
$('quality').onchange=()=>{quality=$('quality').value;write('quality',quality);resize();};
$('fps').onchange=()=>{fps=Number($('fps').value);write('fps',fps);};
document.addEventListener('keydown',e=>{if(e.target.tagName==='SELECT')return;let actions={ArrowLeft:'left',KeyA:'left',ArrowRight:'right',KeyD:'right',ArrowUp:'jump',KeyW:'jump',Space:'jump',ArrowDown:'duck',KeyS:'duck'};if(actions[e.code]){e.preventDefault();if(!e.repeat)act(actions[e.code]);}if(e.code==='KeyP'||e.code==='Escape'){e.preventDefault();if(!e.repeat){if(!$('config').hidden)$('config').hidden=true;else running?pause():paused?play():null;}}});
for(let button of document.querySelectorAll('[data-action]'))button.onpointerdown=e=>{e.preventDefault();act(button.dataset.action);};
let touch=null;canvas.onpointerdown=e=>touch={x:e.clientX,y:e.clientY};canvas.onpointerup=e=>{if(!touch)return;let x=e.clientX-touch.x,y=e.clientY-touch.y;touch=null;if(Math.max(Math.abs(x),Math.abs(y))<15){act('jump');return;}act(Math.abs(x)>Math.abs(y)?x<0?'left':'right':y<0?'jump':'duck');};
window.addEventListener('blur',pause);document.addEventListener('visibilitychange',()=>{if(document.hidden)pause();});
if(window.chrome?.webview)window.chrome.webview.addEventListener('message',e=>{if(e.data?.kind==='pause')pause();if(e.data?.kind==='best'){best=Math.max(best,Number(e.data.score)||0);hud();}});
window.addEventListener('pagehide',()=>{hostScore();stop();});
if(!gl){overlay('3D НЕДОСТУПНО','WebGL не удалось запустить. Проверь WebView2 Runtime и драйвер видеокарты.','НЕДОСТУПНО');$('play').disabled=true;return;}
canvas.addEventListener('webglcontextlost',e=>{e.preventDefault();pause();overlay('ГРАФИКА ОСТАНОВЛЕНА','Видеоконтекст был потерян. Закрой и снова открой игру.','НЕДОСТУПНО');$('play').disabled=true;});
function shader(type,source){let s=gl.createShader(type);gl.shaderSource(s,source);gl.compileShader(s);if(!gl.getShaderParameter(s,gl.COMPILE_STATUS))throw Error(gl.getShaderInfoLog(s));return s;}
const program=gl.createProgram();
gl.attachShader(program,shader(gl.VERTEX_SHADER,`attribute vec3 p;attribute vec3 n;attribute vec3 c;attribute float e;uniform mat4 vp;uniform vec3 eye;varying vec3 color;varying float dist;void main(){gl_Position=vp*vec4(p,1.0);float light=.34+.66*max(0.0,dot(n,normalize(vec3(-.4,1.0,.6))));color=c*mix(light,1.25,e);dist=length(p-eye);}`));
gl.attachShader(program,shader(gl.FRAGMENT_SHADER,`precision mediump float;varying vec3 color;varying float dist;uniform float farFog;void main(){float fog=smoothstep(20.0,farFog,dist);gl_FragColor=vec4(mix(color,vec3(.045,.075,.13),fog),1.0);}`));
gl.linkProgram(program);if(!gl.getProgramParameter(program,gl.LINK_STATUS))throw Error(gl.getProgramInfoLog(program));gl.useProgram(program);
const buffer=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,buffer);const vertices=new Float32Array(500000);gl.bufferData(gl.ARRAY_BUFFER,vertices.byteLength,gl.DYNAMIC_DRAW);let used=0;
for(let [name,size,offset] of [['p',3,0],['n',3,12],['c',3,24],['e',1,36]]){let loc=gl.getAttribLocation(program,name);gl.enableVertexAttribArray(loc);gl.vertexAttribPointer(loc,size,gl.FLOAT,false,40,offset);}
const vpLoc=gl.getUniformLocation(program,'vp'),eyeLoc=gl.getUniformLocation(program,'eye'),fogLoc=gl.getUniformLocation(program,'farFog');
gl.enable(gl.DEPTH_TEST);gl.clearColor(0,0,0,0);
const faces=[[[1,0,0],[[.5,-.5,-.5],[.5,.5,-.5],[.5,.5,.5],[.5,-.5,.5]]],[[-1,0,0],[[-.5,-.5,.5],[-.5,.5,.5],[-.5,.5,-.5],[-.5,-.5,-.5]]],[[0,1,0],[[-.5,.5,-.5],[-.5,.5,.5],[.5,.5,.5],[.5,.5,-.5]]],[[0,-1,0],[[-.5,-.5,.5],[-.5,-.5,-.5],[.5,-.5,-.5],[.5,-.5,.5]]],[[0,0,1],[[.5,-.5,.5],[.5,.5,.5],[-.5,.5,.5],[-.5,-.5,.5]]],[[0,0,-1],[[-.5,-.5,-.5],[-.5,.5,-.5],[.5,.5,-.5],[.5,-.5,-.5]]]];
const P={road:[.065,.09,.14],metal:[.12,.18,.24],dark:[.035,.055,.09],cyan:[.2,.95,.82],pink:[1,.25,.46],gold:[1,.72,.25],white:[.76,.9,1],skin:[.92,.65,.47],cloth:[.13,.24,.35]};
function box(x,y,z,w,h,d,c,e=0){if(used+360>vertices.length)return;for(let [normal,points] of faces)for(let index of [0,1,2,0,2,3]){let v=points[index];vertices[used++]=x+v[0]*w;vertices[used++]=y+v[1]*h;vertices[used++]=z+v[2]*d;for(let n of normal)vertices[used++]=n;for(let k of c)vertices[used++]=k;vertices[used++]=e;}}
function multiply(a,b){let out=new Float32Array(16);for(let c=0;c<4;c++)for(let r=0;r<4;r++)for(let k=0;k<4;k++)out[c*4+r]+=a[k*4+r]*b[c*4+k];return out;}
function norm(a){let n=Math.hypot(...a);return a.map(v=>v/n);}
function cross(a,b){return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]];}
function dot(a,b){return a.reduce((v,x,i)=>v+x*b[i],0);}
function matrix(eye){let z=norm([eye[0],eye[1]-1,eye[2]+14]),x=norm(cross([0,1,0],z)),y=cross(z,x);let view=new Float32Array([x[0],y[0],z[0],0,x[1],y[1],z[1],0,x[2],y[2],z[2],0,-dot(x,eye),-dot(y,eye),-dot(z,eye),1]);let f=1/Math.tan(Math.PI/6),near=.1,far=150;let projection=new Float32Array([f/(canvas.width/canvas.height),0,0,0,0,f,0,0,0,0,(far+near)/(near-far),-1,0,0,2*far*near/(near-far),0]);return multiply(projection,view);}
function city(){
 let far=quality==='low'?65:quality==='high'?115:90;
 box(0,-.14,-45,8.8,.25,112,P.road);box(0,-.32,-45,23,.3,112,P.dark);
 for(let side of [-1,1]){box(side*4.7,0,-45,.7,.2,112,P.metal);box(side*4.35,.035,-45,.045,.05,112,P.cyan,.8);}
 for(let x of [-3.2,-1.6,.8,3.2])box(x,-.005,-45,.035,.05,112,P.metal);
 let shift=game.distance%6;
 for(let z=5+shift;z>-far;z-=6)for(let x of [-1.2,1.2])box(x,.004,z,.055,.035,1.7,P.white,.25);
 for(let side of [-1,1])for(let i=0;i<16;i++){
  let z=RunnerCore.sceneryZ(i,game.distance);if(z<-far)continue;
  let height=5+((i*37+(side+1)*13)%14),x=side*(8+(i%3)*1.4),color=i%3===0?P.cyan:P.pink;
  box(x,height/2,z,3.5,height,6,P.dark);box(x-side*1.8,height*.6,z,.04,height*.7,.16,color,.5);
  for(let j=1;j<height;j+=2.4)box(x-side*1.77,j,z,.04,.07,4.6,P.metal,.3);
  if(i%2===0){box(side*5.2,2.3,z,.12,4.6,.12,P.metal);box(side*5.0,4.5,z,.7,.08,.2,color,1);box(side*5.35,2.9,z,1.25,1.7,.12,P.cloth);box(side*5.35,3.2,z,1.1,.08,.15,color,.9);}
 }
 for(let i=0;i<5;i++){let z=-50-i*12;box(-18+i*9,11+(i%3)*4,z,5,23+(i%3)*8,7,P.dark);}
}
function item(o){let x=(o.lane-1)*2.4,z=o.z;
 if(o.type==='train'){
  box(x,1.55,z,1.85,2.9,4.8,P.cloth);box(x,1.6,z+2.41,1.4,1.0,.04,P.dark);box(x,1.65,z+2.44,1.2,.65,.03,P.cyan,.65);
  box(x,.53,z+2.42,1.7,.18,.06,P.pink,.8);box(x,2.98,z,1.8,.1,4.7,P.metal);
  for(let side of [-1,1]){box(x+side*.55,.85,z+2.46,.25,.15,.08,P.white,1);for(let a of [-1.3,0,1.3])box(x+side*.935,1.9,z+a,.02,.6,.85,P.cyan,.5);box(x+side*.7,.25,z-1.5,.25,.4,.7,P.dark);box(x+side*.7,.25,z+1.5,.25,.4,.7,P.dark);}
 }else if(o.type==='barrier'){
  box(x,.43,z,1.9,.7,.45,P.pink);for(let side of [-1,1])box(x+side*.72,.2,z,.12,.4,.65,P.metal);for(let a=-.6;a<.8;a+=.4)box(x+a,.48,z+.24,.15,.5,.025,P.white,.2);
 }else if(o.type==='gate'){
  for(let side of [-1,1])box(x+side*.9,1.35,z,.12,2.7,.2,P.metal);box(x,1.6,z,1.9,1.1,.3,P.cyan);box(x,1.7,z+.16,1.6,.07,.02,P.white,.7);
 }else{let spin=Math.abs(Math.cos(game.time*3+z*.1));box(x,.96+Math.sin(game.time*3+z)*.08,z,.12+.3*spin,.52,.18,P.gold,.8);box(x,1,z,.06,.27,.22,P.white,.5);}
}
function runner(){
 let x=game.x,y=game.y,duck=game.slide>0,step=game.started&&!game.over&&!game.catching?Math.sin(game.distance*1.4):0;
 box(x,.016,0,.75,.025,.9,P.dark);
 if(duck){box(x,y+.42,0,.5,.4,.8,P.cloth);box(x,y+.65,-.28,.36,.35,.35,P.skin);box(x,y+.7,-.28,.39,.15,.37,P.dark);box(x,y+.2,.2,.65,.23,.6,P.dark);return;}
 box(x,y+1.08,0,.53,.64,.36,P.cloth);box(x,y+1.08,.195,.42,.4,.06,P.cyan,.4);box(x,y+1.57,0,.36,.36,.35,P.skin);box(x,y+1.73,-.01,.39,.15,.38,P.dark);
 for(let side of [-1,1]){let sway=step*side;box(x+side*.16,y+.4+sway*.05,sway*.2,.19,.65,.22,P.dark);box(x+side*.16,y+.1+sway*.05,-.05+sway*.2,.22,.16,.36,P.white);box(x+side*.37,y+1,-sway*.22,.18,.56,.2,P.cloth);box(x+side*.37,y+.7,-sway*.22,.15,.16,.16,P.skin);}
 box(x,y+1.31,.25,.56,.12,.12,P.pink,.5);
}
function officer(){
 if(game.pursuit<=0)return;
 let t=game.catching?Math.min(1,game.catchTime/1.15):0,z=game.catching?1.8*(1-t)+.1:1.8+(1-game.pursuit)*8,x=game.x+(game.catching?t*.65:0),step=game.over?0:Math.sin(game.distance*1.4+game.catchTime*16+2);
 let blue=[.075,.13,.28];
 box(x,.85,z,.56,.7,.4,blue);box(x,1.36,z,.37,.38,.36,P.skin);box(x,1.57,z,.48,.12,.42,blue);box(x,1.51,z+.23,.5,.05,.18,P.dark);box(x,1.55,z+.215,.12,.09,.02,P.gold,.5);
 box(x,.89,z+.22,.08,.55,.03,P.white);box(x+.16,1.04,z+.22,.08,.12,.03,P.gold,.5);
 for(let side of [-1,1]){let swing=step*side;box(x+side*.17,.29,z+swing*.16,.2,.5,.23,blue);box(x+side*.17,.07,z-.06+swing*.16,.24,.13,.37,P.dark);box(x+side*.39,.88,z-swing*.14-t*.25,.17,.6,.2,blue);box(x+side*.39,.56,z-swing*.14-t*.25,.16,.15,.17,P.skin);}
}
function draw(){if(!gl||gl.isContextLost())return;used=0;city();for(let o of game.items)if(!o.hit&&o.z>-(quality==='low'?65:110))item(o);officer();runner();
 gl.viewport(0,0,canvas.width,canvas.height);gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);let eye=[game.x*.12,3.8,8.8];gl.uniformMatrix4fv(vpLoc,false,matrix(eye));gl.uniform3fv(eyeLoc,eye);gl.uniform1f(fogLoc,quality==='low'?65:100);gl.bufferSubData(gl.ARRAY_BUFFER,0,vertices.subarray(0,used));gl.drawArrays(gl.TRIANGLES,0,used/10);telemetry.frames=++frames;telemetry.drawCalls=1;telemetry.triangles=used/30;}
function resize(){let cap=quality==='low'?1:quality==='medium'?1.25:1.5,dpr=Math.min(devicePixelRatio||1,cap),max=quality==='low'?960:quality==='medium'?1440:1920;let scale=Math.min(dpr,max/Math.max(1,innerWidth));canvas.width=Math.max(1,Math.round(innerWidth*scale));canvas.height=Math.max(1,Math.round(innerHeight*scale));draw();}
window.addEventListener('resize',resize);hud();resize();
// Exposed only inside this offline page, for deterministic integration checks.
window.runnerGame=game;window.runnerPause=pause;window.runnerDraw=draw;
})();
