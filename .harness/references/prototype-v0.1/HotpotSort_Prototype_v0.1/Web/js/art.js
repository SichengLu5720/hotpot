/* Original procedural placeholder artwork. No fonts, images or APK art embedded. */
(function(r){'use strict';
function round(ctx,x,y,w,h,radius=10){ctx.beginPath();ctx.roundRect(x,y,w,h,radius);}
function disk(c,x,y,r,fill,stroke=null,line=1){c.beginPath();c.arc(x,y,r,0,Math.PI*2);c.fillStyle=fill;c.fill();if(stroke){c.strokeStyle=stroke;c.lineWidth=line;c.stroke();}}
function oval(c,x,y,rx,ry,fill,stroke=null){c.beginPath();c.ellipse(x,y,rx,ry,0,0,Math.PI*2);c.fillStyle=fill;c.fill();if(stroke){c.strokeStyle=stroke;c.lineWidth=1.1;c.stroke();}}
function line(c,pts,color,width){c.beginPath();c.moveTo(...pts[0]);for(const p of pts.slice(1))c.lineTo(...p);c.strokeStyle=color;c.lineWidth=width;c.lineCap='round';c.lineJoin='round';c.stroke();}
function food(c,kind,x,y,size,letter=false){
 c.save();c.translate(x,y);c.scale(size/22,size/22);c.lineCap='round';c.lineJoin='round';
 // Soft contact shadow; all ingredients use distinct silhouettes and optional IDs.
 oval(c,1,14,15,4,'rgba(61,44,24,.10)');
 switch(kind){
 case 0: // beef roll
   c.rotate(-.32);round(c,-16,-12,31,25,8);c.fillStyle='#D76566';c.fill();c.strokeStyle='#A74851';c.lineWidth=1.2;c.stroke();
   line(c,[[-12,-8],[-3,-3],[2,3],[10,7]],'#FFE2CA',3.6);line(c,[[-9,8],[-4,3],[0,-5],[8,-9]],'#F4AEAB',2.4);
   oval(c,13,0,6,12,'#F6CAC0','#C36767');oval(c,13,0,2.8,6,'#B34E56');break;
 case 1: // shrimp
   c.rotate(-.25);c.beginPath();c.arc(1,-2,12,.2,Math.PI*1.55);c.strokeStyle='#D86F57';c.lineWidth=14;c.stroke();c.beginPath();c.arc(1,-2,12,.2,Math.PI*1.55);c.strokeStyle='#F69D7B';c.lineWidth=10;c.stroke();
   for(let a=.8;a<4.5;a+=.6){line(c,[[Math.cos(a)*8+1,Math.sin(a)*8-2],[Math.cos(a)*16+1,Math.sin(a)*16-2]],'#FFE0BF',1.8);}
   c.beginPath();c.moveTo(13,7);c.lineTo(22,6);c.lineTo(19,16);c.closePath();c.fillStyle='#E67E65';c.fill();disk(c,-2,-14,1.4,'#593D32');break;
 case 2: // shiitake
   round(c,-5,-1,11,20,4);c.fillStyle='#F1DFC1';c.fill();c.strokeStyle='#BDA17C';c.stroke();
   c.beginPath();c.moveTo(-19,3);c.bezierCurveTo(-18,-22,18,-22,19,3);c.bezierCurveTo(11,11,-12,11,-19,3);c.fillStyle='#876048';c.fill();c.strokeStyle='#664735';c.stroke();
   line(c,[[-9,-8],[8,0]],'#DEC7A3',3);line(c,[[-7,0],[7,-9]],'#DEC7A3',3);break;
 case 3: // corn
   c.rotate(.28);round(c,-12,-19,24,38,8);c.fillStyle='#E4A937';c.fill();
   for(let col=0;col<3;col++)for(let row=0;row<5;row++){round(c,-10+col*7,-16+row*6.5,6,5.5,2);c.fillStyle=col===0?'#FFE19A':'#F7CF5F';c.fill();}
   break;
 case 4: // broccoli
   line(c,[[0,17],[0,1],[-10,-8]],'#9BB671',8);line(c,[[1,4],[11,-6]],'#95AB6D',6);
   disk(c,-10,-6,10,'#669260','#49784F');disk(c,9,-6,10,'#76A363','#49784F');disk(c,-1,-14,10,'#85AB6B','#527F52');
   disk(c,-5,-16,3,'#A5C580');disk(c,12,-8,3,'#9FBE77');break;
 case 5: // tofu
   c.rotate(-.15);c.beginPath();c.moveTo(-16,-12);c.lineTo(7,-18);c.lineTo(19,-10);c.lineTo(19,13);c.lineTo(-5,19);c.lineTo(-16,12);c.closePath();c.fillStyle='#D9BE85';c.fill();c.strokeStyle='#B49963';c.stroke();
   c.beginPath();c.moveTo(-16,-12);c.lineTo(7,-18);c.lineTo(19,-10);c.lineTo(-5,-4);c.closePath();c.fillStyle='#FFF0C9';c.fill();c.beginPath();c.moveTo(-5,-4);c.lineTo(19,-10);c.lineTo(19,13);c.lineTo(-5,19);c.closePath();c.fillStyle='#F3DCAB';c.fill();
   for(const [a,b] of [[0,3],[11,0],[3,12]])disk(c,a,b,1.1,'#D8BC85');break;
 case 6: // lotus root
   disk(c,0,0,19,'#D5AF83','#B08F65');disk(c,0,-1,17,'#F1DAB5');disk(c,0,-1,3.2,'#AC8964');
   for(let i=0;i<6;i++){const a=i*Math.PI/3;oval(c,Math.cos(a)*10,Math.sin(a)*10-1,3.5,4.7,'#B89B77');}break;
 case 7: // fish ball
   disk(c,0,0,18,'#E0D9BD','#BDB499');disk(c,-2,-3,15,'#F5EBD0');oval(c,-7,-9,6,3,'#FFFAE6');for(const p of [[6,4],[-4,8],[10,-4]])disk(c,p[0],p[1],1,'#D2C6A7');break;
 case 8: // red pepper
   c.beginPath();c.moveTo(-9,-12);c.bezierCurveTo(24,-15,20,16,-16,19);c.bezierCurveTo(3,9,-7,6,-9,-12);c.fillStyle='#D95142';c.fill();c.strokeStyle='#AB3D37';c.stroke();
   line(c,[[-8,-12],[-9,-18],[-2,-20]],'#597D4E',4);line(c,[[1,-7],[8,-2],[5,6]],'#F88F73',3);break;
 case 9: // pumpkin
   c.beginPath();c.moveTo(-18,12);c.lineTo(-1,-19);c.lineTo(19,12);c.quadraticCurveTo(0,25,-18,12);c.fillStyle='#688258';c.fill();
   c.beginPath();c.moveTo(-14,10);c.lineTo(-1,-15);c.lineTo(15,11);c.quadraticCurveTo(0,20,-14,10);c.fillStyle='#F1A147';c.fill();
   line(c,[[-1,-5],[-6,10]],'#FFD38A',3);line(c,[[3,1],[7,10]],'#FFD38A',3);break;
 case 10: // bok choy
   oval(c,-9,-7,10,14,'#6D9A64','#547E56');oval(c,8,-10,10,15,'#80A66B','#547E56');
   c.beginPath();c.moveTo(-10,-4);c.quadraticCurveTo(-5,8,-7,18);c.quadraticCurveTo(1,23,9,15);c.lineTo(9,-6);c.quadraticCurveTo(3,-2,1,10);c.quadraticCurveTo(-1,0,-10,-4);c.fillStyle='#E4E7BC';c.fill();line(c,[[1,11],[0,18]],'#B7C48D',1.6);break;
 case 11: // crab stick
   c.rotate(-.5);round(c,-9,-19,18,38,4);c.fillStyle='#FFF0D5';c.fill();round(c,-9,-19,9,38,3);c.fillStyle='#D9675D';c.fill();line(c,[[-4,-15],[-4,15]],'#F9A38B',1.5);line(c,[[5,-14],[5,14]],'#DDC2A1',1.3);break;
 case 12: // potato
   c.rotate(.4);oval(c,0,0,15,20,'#D3B26F','#A48C5E');oval(c,-3,-4,10,13,'#E5C68C');for(const p of [[3,10],[-7,4],[7,-5],[-5,-13]])disk(c,p[0],p[1],1.2,'#A99163');break;
 case 13: // egg
   oval(c,0,0,16,20,'#FFF4D9','#CDBFA1');disk(c,1,3,10,'#EABF50');disk(c,-2,0,4,'#F7D978');break;
 case 14: // konjac knot
   for(const dx of [-7,0,7]){c.beginPath();c.ellipse(dx,-1,7,16,.7,0,Math.PI*2);c.strokeStyle='#8EA99B';c.lineWidth=7;c.stroke();c.strokeStyle='#D6E1CE';c.lineWidth=4;c.stroke();}
   line(c,[[-13,11],[13,-9]],'#B4C6B8',7);line(c,[[-13,10],[13,-10]],'#E3E9D7',3);break;
 case 15: // carrot
   c.rotate(.35);c.beginPath();c.moveTo(-11,-10);c.quadraticCurveTo(0,-20,11,-10);c.lineTo(1,22);c.quadraticCurveTo(-3,12,-11,-10);c.fillStyle='#E68B4E';c.fill();c.strokeStyle='#BF713F';c.stroke();
   line(c,[[0,-15],[-7,-21]],'#77945D',4);line(c,[[0,-15],[2,-23]],'#87AA64',4);line(c,[[-6,-3],[2,-3]],'#F8BD78',2);line(c,[[-3,6],[3,6]],'#F8BD78',2);break;
 }
 c.restore();
 if(letter){c.save();disk(c,x+size*.73,y+size*.69,6.5,'#FFFAEE','rgba(72,87,71,.28)',.8);c.font='700 8px system-ui,sans-serif';c.textAlign='center';c.textBaseline='middle';c.fillStyle='#47635D';c.fillText(String.fromCharCode(65+kind),x+size*.73,y+size*.69+.4);c.restore();}
}
function plate(c,x,y,r,id,debug=false){
 c.save();c.shadowColor='rgba(40,60,51,.19)';c.shadowBlur=9;c.shadowOffsetY=4;disk(c,x,y,r,'#E9EDDC');c.shadowColor='transparent';
 const gr=c.createRadialGradient(x-r*.28,y-r*.35,r*.1,x,y,r);gr.addColorStop(0,'#FFFEF4');gr.addColorStop(.77,'#F9F6E7');gr.addColorStop(.88,'#E0E7D4');gr.addColorStop(1,'#CAD5C4');disk(c,x,y,r,gr,'#A3BAAC',1.4);
 disk(c,x,y,r-5,'rgba(255,255,255,.25)','#719589',1.3);disk(c,x,y,r-10,'#F7F3E5','#D6DFCD',1);
 c.beginPath();c.arc(x,y,r-3,3.55,5.45);c.strokeStyle='rgba(255,255,255,.82)';c.lineWidth=3;c.stroke();
 if(debug){c.font='9px monospace';c.textAlign='center';c.fillStyle='#6F8C81';c.fillText('#'+id,x,y-r+14);}c.restore();
}
r.HotpotArt={food,plate,disk,oval,round,line};
})(typeof globalThis!=='undefined'?globalThis:this);
