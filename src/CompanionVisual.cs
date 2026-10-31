using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

public class CompanionRing : FrameworkElement {
 public static readonly DependencyProperty ProgressProperty=DependencyProperty.Register("Progress",typeof(double),typeof(CompanionRing),new FrameworkPropertyMetadata(0.0,FrameworkPropertyMetadataOptions.AffectsRender));
 public static readonly DependencyProperty RingBrushProperty=DependencyProperty.Register("RingBrush",typeof(Brush),typeof(CompanionRing),new FrameworkPropertyMetadata(Brushes.Pink,FrameworkPropertyMetadataOptions.AffectsRender));
 public static readonly DependencyProperty TrackBrushProperty=DependencyProperty.Register("TrackBrush",typeof(Brush),typeof(CompanionRing),new FrameworkPropertyMetadata(Brushes.Gray,FrameworkPropertyMetadataOptions.AffectsRender));
 public double Progress{get{return (double)GetValue(ProgressProperty);}set{SetValue(ProgressProperty,value);}}
 protected override void OnRender(DrawingContext d){double radius=Math.Max(1,Math.Min(ActualWidth,ActualHeight)/2-9);Point center=new Point(ActualWidth/2,ActualHeight/2);var track=new Pen((Brush)GetValue(TrackBrushProperty),9);d.DrawEllipse(null,track,center,radius,radius);double ratio=Math.Max(0,Math.Min(1,Progress));if(ratio<=0)return;var stroke=new Pen((Brush)GetValue(RingBrushProperty),9){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};if(ratio>=.99999){d.DrawEllipse(null,stroke,center,radius,radius);return;}double angle=ratio*Math.PI*2-Math.PI/2;var arc=new StreamGeometry();using(var g=arc.Open()){g.BeginFigure(new Point(center.X,center.Y-radius),false,false);g.ArcTo(new Point(center.X+radius*Math.Cos(angle),center.Y+radius*Math.Sin(angle)),new Size(radius,radius),0,ratio>.5,SweepDirection.Clockwise,true,false);}d.DrawGeometry(null,stroke,arc);}
}
public partial class Companion {
 TextBlock dashboardTasks,dashboardIncome;readonly Dictionary<string,CompanionRing> timerRings=new Dictionary<string,CompanionRing>();readonly Dictionary<string,TextBlock> timerStates=new Dictionary<string,TextBlock>();
 void Build(){BuildDashboard();}
 void Theme(){
  bool cute=data.Theme=="cute";string alpha=((byte)(data.Density*255)).ToString("X2"),panelAlpha=Math.Min(255,(int)(data.Density*255)+20).ToString("X2");
  Resources["Bg"]=Brush("#"+alpha+(cute?"19151F":"111317"));Resources["Panel"]=Brush("#"+panelAlpha+(cute?"2B2233":"1C2027"));Resources["Field"]=Brush("#"+alpha+(cute?"302638":"232830"));Resources["Line"]=Brush(cute?"#885D4668":"#A3464E5A");
  Resources["Text"]=Brush("#F7F8FA");Resources["Muted"]=Brush(cute?"#BFA6D6":"#A9B0BC");Resources["Accent"]=Brush(cute?"#F99CDE":"#C7CDD4");Resources["AccentLine"]=Brush(cute?"#CCF99CDE":"#CCC7CDD4");Resources["AccentText"]=Brush("#15171B");Resources["DockBg"]=Brush("#"+(data.Density>=1?"FF":"F2")+(cute?"2D2234":"20242B"));
  Resources["Selection"]=Brush(cute?"#995E366B":"#995B626D");Resources["ScrollThumb"]=Brush(cute?"#AD8D6BA3":"#AD939BA8");Resources["ScrollTrack"]=Brush(cute?"#424F3C5D":"#424B515C");Resources["RingTrack"]=Brush(cute?"#684A3B59":"#68464B55");Resources["NavBg"]=Brush(cute?"#662B2134":"#6621252D");Resources["SelectedTab"]=Brush(cute?"#9A3A2B46":"#9A383E48");
  Resources["AccentFill"]=new LinearGradientBrush((Color)ColorConverter.ConvertFromString(cute?"#FFB5E9":"#E1E5EB"),(Color)ColorConverter.ConvertFromString(cute?"#F685D1":"#ADB5C0"),new Point(0,0),new Point(1,1));ApplyCustomPalette();DashboardColors();if(brand!=null)brand.Text="COMPANION";
 }
 void Summary(){double net=data.Entries.Where(t=>t.Date.Date==DateTime.Today).Sum(t=>t.Income?t.Amount:-t.Amount);if(dashboardTasks!=null)dashboardTasks.Text="Дела "+data.Tasks.Count(t=>t.Done)+"/"+data.Tasks.Count;UpdateWorkspaceMetrics();if(dashboardIncome!=null)dashboardIncome.Text="За сегодня  "+(net<0?"− ":"")+Cash(Math.Abs(net));}
 void TimerTab(){BuildWorkspaceTimers();}
 void RenderTimers(){RenderWorkspaceTimers();}
 void UpdateTimerVisuals(){foreach(var timer in data.Timers){if(timerRings.ContainsKey(timer.Id))timerRings[timer.Id].Progress=Left(timer)/timer.Duration;if(timerStates.ContainsKey(timer.Id))timerStates[timer.Id].Text=Left(timer)<=0?"●  ТАЙМЕР ЗАВЕРШЁН":timer.End==0?"●  НА ПАУЗЕ":"●  ДО "+DateTimeOffset.FromUnixTimeMilliseconds(timer.End).LocalDateTime.ToString("dd.MM.yyyy · HH:mm");}}
}
