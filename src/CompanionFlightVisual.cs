using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

public sealed partial class CompanionArcadeField {
 static Brush FlightBrush(string hex){var b=new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(hex));b.Freeze();return b;}
 static Brush FlightGradient(string a,string b,double angle){var g=new LinearGradientBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(a),(System.Windows.Media.Color)ColorConverter.ConvertFromString(b),angle);g.Freeze();return g;}
 static readonly Brush flightSky=FlightGradient("#152442","#52516B",90),flightFar=FlightBrush("#293A58"),flightNear=FlightBrush("#304D5E"),flightCloud=FlightBrush("#1453B7CE"),flightStar=FlightBrush("#75D4E9FF");
 static readonly Brush flightGlow=FlightGlow(),flightMoon=FlightGradient("#FFF2CD","#F7B99D",70),flightInk=FlightBrush("#26354E"),flightWhite=FlightBrush("#F3F8FF"),flightMuted=FlightBrush("#A6BBCF");
 static readonly Brush flightPipe=FlightGradient("#197B80","#42C9AA",0),flightPipeCap=FlightGradient("#337D88","#88EFCE",0),flightPipeLight=FlightBrush("#77DFF4E8"),flightPipeDark=FlightBrush("#17515E"),flightGround=FlightBrush("#183643");
 static readonly Brush flightBird=FlightGradient("#FFE59A","#FFB16C",90),flightWing=FlightGradient("#FFE9B2","#E98B62",90),flightBlush=FlightBrush("#EF907D"),flightBeak=FlightBrush("#FF9D72"),flightTrail=FlightBrush("#50FFE9A6");
 static readonly Brush flightScrim=FlightBrush("#B5101D32"),flightPanel=FlightBrush("#EE16283E"),flightMint=FlightBrush("#90EACD");
 static readonly Geometry flightHill=FlightHill();
 static Brush FlightGlow(){var g=new RadialGradientBrush();g.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(65,255,218,173),0));g.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0,255,218,173),1));g.Freeze();return g;}
 static Geometry FlightHill(){var g=new StreamGeometry();using(var p=g.Open()){p.BeginFigure(new Point(0,280),true,true);p.BezierTo(new Point(50,254),new Point(95,265),new Point(150,280),true,false);p.BezierTo(new Point(215,302),new Point(266,250),new Point(320,280),true,false);p.LineTo(new Point(320,330),true,false);p.LineTo(new Point(0,330),true,false);}g.Freeze();return g;}
 static readonly Brush flightCraterSmall=FlightBrush("#30DD9A91"),flightCraterLarge=FlightBrush("#20DD9A91");
 static Pen FlightPen(Brush brush,double width){var p=new Pen(brush,width);p.Freeze();return p;}
 static readonly Pen flightDarkPen=FlightPen(flightPipeDark,1),flightLightPen=FlightPen(flightPipeLight,1),flightBodyPen=FlightPen(flightInk,.8),flightWingPen=FlightPen(FlightBrush("#D78A64"),.65),flightPanelPen=FlightPen(FlightBrush("#314B65"),1),flightGroundPen=FlightPen(flightMint,2);
 FormattedText flightPointText;
 CompanionArcadeGame flightObserved;
 int flightLastScore;double flightScoreTime=-10;
 string flightCachedTitle,flightCachedSubtitle;FormattedText flightTitleText,flightSubtitleText;
 FormattedText FlightText(string text,double size,Brush brush){return new FormattedText(text,CultureInfo.GetCultureInfo("ru-RU"),FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,FontWeights.SemiBold,FontStretches.Normal),size,brush);}
 void FlightLabel(DrawingContext d,string title,string subtitle){
  if(flightCachedTitle!=title){flightCachedTitle=title;flightTitleText=FlightText(title,30,flightWhite);}
  if(flightCachedSubtitle!=subtitle){flightCachedSubtitle=subtitle;flightSubtitleText=FlightText(subtitle,13,flightMuted);}
  d.DrawRoundedRectangle(flightPanel,flightPanelPen,new Rect(122,94,396,132),18,18);
  d.DrawText(flightTitleText,new Point((640-flightTitleText.Width)/2,119));d.DrawText(flightSubtitleText,new Point((640-flightSubtitleText.Width)/2,165));
  d.DrawRoundedRectangle(flightMint,null,new Rect(292,204,56,3),2,2);
 }
 void FlightPipe(DrawingContext d,double x,double y,double height,bool upper){
  if(height<=0)return;
  d.DrawRoundedRectangle(flightPipe,flightDarkPen,new Rect(x,y,44,height),5,5);
  d.DrawRectangle(flightPipeLight,null,new Rect(x+6,y,3,height));
  double cap=upper?y+height-12:y;
  d.DrawRoundedRectangle(flightPipeCap,flightDarkPen,new Rect(x,cap,44,12),4,4);
  d.DrawLine(flightLightPen,new Point(x+5,cap+3),new Point(x+39,cap+3));
  if(height>35){double seam=upper?cap-14:cap+26;d.DrawLine(flightDarkPen,new Point(x+12,seam),new Point(x+36,seam));}
 }
 void FlightBird(DrawingContext d,CompanionArcadeGame game){
  double y=game.Started?game.Y:160,angle=game.Started?Math.Max(-22,Math.Min(55,game.Velocity*.12)):-8;
  double wing=Math.Sin(game.Elapsed*23)*3;
  if(game.Started&&!game.Over){for(int i=0;i<5;i++){double distance=14+i*8;d.DrawEllipse(flightTrail,null,new Point(140-distance,y+i*game.Velocity*.008+Math.Sin(game.Elapsed*4+i)*1.5),Math.Max(.6,2-i*.3),Math.Max(.6,2-i*.3));}}
  d.PushTransform(new RotateTransform(angle,140,y));
  var tail=new StreamGeometry();using(var p=tail.Open()){p.BeginFigure(new Point(130,y),true,true);p.LineTo(new Point(121,y-4),true,false);p.LineTo(new Point(123,y+5),true,false);}d.DrawGeometry(flightWing,null,tail);
  d.DrawEllipse(flightBird,flightBodyPen,new Point(140,y),12,10);
  d.DrawEllipse(flightWhite,null,new Point(145,y-3),4.5,5);d.DrawEllipse(flightInk,null,new Point(147,y-3),1.7,2.6);d.DrawEllipse(flightWhite,null,new Point(147.4,y-4),.65,.8);
  d.DrawEllipse(flightBlush,null,new Point(146,y+4),2.5,1.6);
  d.DrawEllipse(flightWing,flightWingPen,new Point(134,y+3+wing),6,Math.Max(2,4-wing*.45));
  var beak=new StreamGeometry();using(var p=beak.Open()){p.BeginFigure(new Point(150,y-1),true,true);p.LineTo(new Point(157,y+2),true,false);p.LineTo(new Point(150,y+4),true,false);}d.DrawGeometry(flightBeak,null,beak);
  d.Pop();
 }
 void DrawFlight(DrawingContext d,CompanionArcadeGame game){
  if(!Object.ReferenceEquals(game,flightObserved)||game.Points<flightLastScore){flightObserved=game;flightLastScore=game.Points;flightScoreTime=-10;}
  if(game.Points>flightLastScore){flightLastScore=game.Points;flightScoreTime=game.Elapsed;}
  d.PushClip(new RectangleGeometry(new Rect(0,0,640,320),14,14));d.DrawRectangle(flightSky,null,new Rect(0,0,640,320));
  d.DrawEllipse(flightGlow,null,new Point(532,62),85,85);d.DrawEllipse(flightMoon,null,new Point(532,62),24,24);d.DrawEllipse(flightCraterSmall,null,new Point(540,56),4,4);d.DrawEllipse(flightCraterLarge,null,new Point(524,70),6,6);
  for(int i=0;i<30;i++){double x=(i*83+17-game.Elapsed*(3+i%3))%690;if(x<0)x+=690;d.DrawEllipse(flightStar,null,new Point(x,18+i*31%155),i%5==0?1.2:.65,i%5==0?1.2:.65);}
  for(int i=0;i<6;i++){double x=(i*131+35-game.Elapsed*9)%800;if(x< -80)x+=800;double y=76+i*41%110;d.DrawEllipse(flightCloud,null,new Point(x,y),36,9);d.DrawEllipse(flightCloud,null,new Point(x+18,y-7),23,13);}
  for(int layer=0;layer<2;layer++){double drift=game.Elapsed*(layer==0?10:23)%320;for(int tile=-1;tile<4;tile++){d.PushTransform(new TranslateTransform(tile*320-drift,layer==0?-19:2));d.DrawGeometry(layer==0?flightFar:flightNear,null,flightHill);d.Pop();}}
  foreach(var pipe in game.Obstacles){double edge=pipe.Y-65;FlightPipe(d,pipe.X,-8,edge+8,true);FlightPipe(d,pipe.X,pipe.Y+65,320-pipe.Y-65,false);if(pipe.Moving){d.DrawEllipse(flightMint,null,new Point(pipe.X+22,Math.Max(12,edge-24)),2,2);}}
  d.DrawRectangle(flightGround,null,new Rect(0,306,640,14));d.DrawLine(flightGroundPen,new Point(0,307),new Point(640,307));for(int i=0;i<34;i++){double x=(i*23-game.Elapsed*155)%690;if(x<0)x+=690;d.DrawLine(flightDarkPen,new Point(x,314),new Point(x+7,314));}
  FlightBird(d,game);
  double since=game.Elapsed-flightScoreTime;if(since>=0&&since<.65&&!game.Over){d.PushOpacity(1-since/.65);d.DrawText(flightPointText??(flightPointText=FlightText("+1",16,flightMint)),new Point(160,game.Y-24-since*30));d.Pop();}
  if(!game.Started||game.Over||game.Paused){d.DrawRectangle(flightScrim,null,new Rect(0,0,640,320));FlightLabel(d,game.Over?"ПОПРОБУЕМ ЕЩЁ?":game.Paused?"ПАУЗА":"ДЕРЖИ ВЫСОТУ",game.Over?"Очки: "+game.Score+"   ·   Пробел - ещё один полёт":game.Paused?"Нажми P или «Продолжить»":"Пробел / клик - взмах. Лети между трубами.");}
  d.Pop();
 }
}
