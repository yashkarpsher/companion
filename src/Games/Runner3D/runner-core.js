'use strict';
(function(root){
class Run {
 static sceneryZ(index,distance,count=16,spacing=11,near=24){const length=count*spacing;return near-((index*spacing-distance)%length+length)%length;}
 constructor(seed=Date.now()){this.seed=seed>>>0;this.reset();}
 random(){this.seed=(Math.imul(1664525,this.seed)+1013904223)>>>0;return this.seed/4294967296;}
 reset(){this.time=0;this.distance=0;this.coins=0;this.lane=1;this.x=0;this.y=0;this.vy=0;this.slide=0;this.spawn=1;this.items=[];this.over=false;this.started=false;this.pursuit=1;this.catching=false;this.catchTime=0;}
 get speed(){return Math.min(32,14+this.time*.2);}
 get score(){return Math.floor(this.distance/3)+this.coins*5;}
 start(){this.reset();this.started=true;}
 move(d){if(this.started&&!this.over&&!this.catching)this.lane=Math.max(0,Math.min(2,this.lane+d));}
 jump(){if(this.started&&!this.over&&this.y===0){this.slide=0;this.vy=7.3;}}
 duck(){if(this.started&&!this.over&&this.y===0)this.slide=.8;}
 row(){
  let open=Math.floor(this.random()*3),other=(open+1)%3,last=(open+2)%3;
  this.items.push({type:'train',lane:other,z:-94,hit:false});
  this.items.push({type:this.random()<.5?'barrier':'gate',lane:last,z:-94,hit:false});
  for(let i=0;i<5;i++)this.items.push({type:'coin',lane:open,z:-94-i*2.4,hit:false});
 }
 step(dt){
  if(!this.started||this.over||!Number.isFinite(dt)||dt<=0)return;
  // Bounded physics steps avoid tunnelling at the maximum speed.
  let left=Math.min(dt,.12);while(left>0&&!this.over){let t=Math.min(left,1/120);left-=t;this.tick(t);}
 }
 tick(t){
  if(this.catching){this.catchTime+=t;this.pursuit=Math.min(1,this.pursuit+t*3);this.y=Math.max(0,this.y-t*4);if(this.catchTime>=1.2)this.over=true;return;}
  this.pursuit=Math.max(0,1-Math.max(0,this.time-3)/6);
  this.time+=t;let speed=this.speed;this.distance+=speed*t;
  this.x+=( (this.lane-1)*2.4-this.x)*Math.min(1,t*14);
  this.slide=Math.max(0,this.slide-t);
  if(this.vy!==0||this.y>0){this.vy-=20*t;this.y+=this.vy*t;if(this.y<=0){this.y=0;this.vy=0;}}
  this.spawn-=t;if(this.spawn<=0){this.row();this.spawn=1.35+this.random()*.25;}
  for(let item of this.items){item.z+=speed*t;if(item.hit)continue;
   if(Math.abs(this.x-(item.lane-1)*2.4)>.76)continue;
   let half=item.type==='train'?2.4:item.type==='coin'?.65:.55;
   if(Math.abs(item.z)>half)continue;
   if(item.type==='coin'){item.hit=true;this.coins++;continue;}
   if(item.type==='barrier'&&this.y>.74)continue;
   if(item.type==='gate'&&this.slide>0&&this.y<.1)continue;
   this.catching=true;this.catchTime=0;break;
  }
  this.items=this.items.filter(i=>i.z<8&&!i.hit);
 }
}
root.RunnerCore=Run;if(typeof module!=='undefined')module.exports=Run;
})(globalThis);
