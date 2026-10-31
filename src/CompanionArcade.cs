using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

// Both games use the same small logical field; rendering scales with the window.
public sealed class CompanionArcadeObstacle {
 public double X,Y;public int Lane;public bool Passed,Coin;
 public bool Moving;public double MotionPhase;public int MotionDirection;
}
public sealed class CompanionArcadeGame {
 public readonly bool Runner;
 readonly Random random;
 public readonly List<CompanionArcadeObstacle> Obstacles=new List<CompanionArcadeObstacle>();
 public bool Started,Over,Paused;
 public double Y=160,Velocity,Elapsed,Spawn,Jump;
 public double RoadDistance;
 public const double RunnerStartSpeed=160,RunnerMaximumSpeed=360,RunnerAcceleration=2.5;
 public double RunnerSpeed {get{return SpeedAt(Elapsed);}}
 public bool MovingPipes {get{return !Runner&&Points>=100;}}
 static double SpeedAt(double elapsed){return Math.Min(RunnerMaximumSpeed,RunnerStartSpeed+Math.Max(0,elapsed)*RunnerAcceleration);}
 public int Lane=1,Points;
 public int Score {get{return Runner?(int)(Elapsed*2)+Points:Points;}}
 public CompanionArcadeGame(bool runner,int seed){Runner=runner;random=new Random(seed);}
 public void Start(){Obstacles.Clear();Y=160;Velocity=0;Elapsed=0;Spawn=0;Jump=0;RoadDistance=0;Lane=1;Points=0;Started=true;Over=false;Paused=false;}
 public void Action(){if(!Started||Over||Paused)return;if(Runner){if(Jump<=0)Jump=.72;}else Velocity=-235;}
 public void Move(int direction){if(Started&&!Over&&!Paused)Lane=Math.Max(0,Math.Min(2,Lane+direction));}
 public void Step(double seconds){
  if(!Started||Over||Paused||Double.IsNaN(seconds)||seconds<=0)return;
  double dt=Math.Min(.05,seconds),previousTime=Elapsed;Elapsed+=dt;Spawn-=dt;
  if(Runner){
   Jump=Math.Max(0,Jump-dt);double speed=(SpeedAt(previousTime)+RunnerSpeed)/2;RoadDistance=(RoadDistance+speed*dt)%375;
   if(Spawn<=0){int blocked=random.Next(3);Obstacles.Add(new CompanionArcadeObstacle{Lane=blocked,Y=-30});Obstacles.Add(new CompanionArcadeObstacle{Lane=(blocked+random.Next(1,3))%3,Y=-70,Coin=true});Spawn=1.05;}
   foreach(var item in Obstacles){item.Y+=speed*dt;if(item.Lane!=Lane||Math.Abs(item.Y-266)>22||item.Passed)continue;if(item.Coin){Points+=5;item.Passed=true;}else if(Jump<=0){Over=true;break;}}
   Obstacles.RemoveAll(o=>o.Y>350);
  }else{
   Velocity+=640*dt;Y+=Velocity*dt;if(Y<12||Y>294){Over=true;return;}
   if(Spawn<=0){Obstacles.Add(new CompanionArcadeObstacle{X=680,Y=90+random.Next(140),MotionDirection=random.Next(2)==0?-1:1});Spawn=1.65;}
   bool moving=MovingPipes;
   foreach(var item in Obstacles){if(moving){if(!item.Moving){item.MotionPhase=Math.Asin(Math.Max(-1,Math.Min(1,(item.Y-160)/70)));if(item.MotionDirection==0)item.MotionDirection=1;item.Moving=true;}item.MotionPhase=(item.MotionPhase+item.MotionDirection*.6*dt)%(Math.PI*2);item.Y=160+70*Math.Sin(item.MotionPhase);}item.X-=155*dt;if(!item.Passed&&item.X+44<140){item.Passed=true;Points++;}if(item.X<151&&item.X+44>129&&(Y-10<item.Y-65||Y+10>item.Y+65)){Over=true;break;}}
   Obstacles.RemoveAll(o=>o.X< -50);
  }
 }
}

public sealed partial class CompanionArcadeField : FrameworkElement {
 public CompanionArcadeGame Game;
 Brush Color(string key,string fallback){return TryFindResource(key) as Brush??new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(fallback));}
 void Label(DrawingContext d,string text,double x,double y,double size,Brush color){var t=new FormattedText(text,CultureInfo.GetCultureInfo("ru-RU"),FlowDirection.LeftToRight,new Typeface("Segoe UI Semibold"),size,color);d.DrawText(t,new Point(x,y));}
 protected override void OnRender(DrawingContext d){
  var accent=Color("Accent","#C7CDD4");var text=Color("Text","#F7F8FA");var muted=Color("Muted","#A9B0BC");var line=Color("Line","#464E5A");var panel=Color("Field","#232830");var game=Game;if(game==null)return;if(!game.Runner){DrawFlight(d,game);return;}
  d.PushClip(new RectangleGeometry(new Rect(0,0,640,320),12,12));d.DrawRectangle(new SolidColorBrush(System.Windows.Media.Color.FromRgb(12,15,20)),null,new Rect(0,0,640,320));
  if(game.Runner){
   for(int i=0;i<3;i++)d.DrawRoundedRectangle(i==game.Lane?panel:Color("Bg","#111317"),new Pen(line,1),new Rect(70+i*170,12,160,296),10,10);
   for(int lane=0;lane<3;lane++)for(int y=0;y<5;y++){double offset=(y*75+game.RoadDistance)%375-35;d.DrawLine(new Pen(line,2),new Point(150+lane*170,offset),new Point(150+lane*170,offset+20));}
   foreach(var o in game.Obstacles){if(o.Passed)continue;double x=150+o.Lane*170;if(o.Coin){d.DrawEllipse(accent,new Pen(text,1),new Point(x,o.Y),8,8);}else{d.DrawRoundedRectangle(panel,new Pen(accent,2),new Rect(x-45,o.Y-13,90,26),5,5);for(int i=-2;i<3;i++)d.DrawLine(new Pen(muted,3),new Point(x+i*17-5,o.Y+7),new Point(x+i*17+5,o.Y-7));}}
   double lift=game.Jump>0?Math.Sin(game.Jump/.72*Math.PI)*33:0;double px=150+game.Lane*170;d.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromArgb(90,0,0,0)),null,new Point(px,283),22,6);d.DrawRoundedRectangle(accent,new Pen(text,1.5),new Rect(px-15-lift*.09,247-lift,30+lift*.18,34+lift*.12),9,9);d.DrawLine(new Pen(panel,3),new Point(px-6,259-lift),new Point(px+6,259-lift));
  }else{
   for(int i=0;i<25;i++){double x=(i*97-game.Elapsed*(15+i%4*6))%680;if(x<0)x+=680;d.DrawEllipse(line,null,new Point(x,25+i*41%250),i%3==0?2:1,i%3==0?2:1);}
   foreach(var o in game.Obstacles){d.DrawRoundedRectangle(panel,new Pen(accent,1.5),new Rect(o.X,-10,44,Math.Max(1,o.Y-55)),7,7);d.DrawRoundedRectangle(panel,new Pen(accent,1.5),new Rect(o.X,o.Y+65,44,330-o.Y-65),7,7);d.DrawLine(new Pen(accent,4),new Point(o.X+5,o.Y-65),new Point(o.X+39,o.Y-65));d.DrawLine(new Pen(accent,4),new Point(o.X+5,o.Y+65),new Point(o.X+39,o.Y+65));}
   d.DrawRectangle(panel,null,new Rect(0,306,640,14));d.DrawLine(new Pen(line,1),new Point(0,306),new Point(640,306));
   d.DrawEllipse(accent,new Pen(text,1.5),new Point(140,game.Y),12,10);d.DrawEllipse(panel,null,new Point(144,game.Y-2),2,2);d.DrawLine(new Pen(text,3),new Point(129,game.Y+1),new Point(120,game.Y+5));d.DrawLine(new Pen(accent,3),new Point(151,game.Y),new Point(158,game.Y+2));
  }
  if(!game.Started||game.Over||game.Paused){d.DrawRectangle(new SolidColorBrush(System.Windows.Media.Color.FromArgb(195,9,12,17)),null,new Rect(0,0,640,320));Label(d,game.Over?"ПОПРОБУЕМ ЕЩЁ?":game.Paused?"ПАУЗА":game.Runner?"ТРИ ДОРОЖКИ. ТВОЙ РИТМ.":"ДЕРЖИ ВЫСОТУ.",45,114,game.Runner&&!game.Started?26:30,text);Label(d,game.Over?"Очки: "+game.Score+"   ·   Пробел - заново":game.Paused?"P или «Продолжить» - вернуться":"Пробел или «Играть» - начать",48,163,17,muted);}
  d.Pop();
 }
}

public partial class Companion {
 UniformGrid arcadeTiles;TextBlock flightRecord,runnerRecord,arcadeScore,arcadeTitle,arcadeHint;Button arcadeStart,arcadePause;
 double arcadeUiStamp;Grid arcadeLayer;Border arcadeSurface;CompanionArcadeField arcadeField;CompanionArcadeGame arcadeGame;
 DispatcherTimer arcadeTicker;readonly Stopwatch arcadeClock=new Stopwatch();bool arcadeOpen;int arcadeGeneration;
 void BuildArcade(){
  var heading=Text("Мини-игры",21);heading.Margin=new Thickness(0,7,0,4);hero.Children.Add(heading);hero.Children.Add(Text("Короткая пауза, пока идёт таймер",12,"Muted"));
  arcadeTiles=new UniformGrid{Columns=2,Margin=new Thickness(0,0,-12,4)};hero.Children.Add(arcadeTiles);
  for(int i=0;i<2;i++){bool runner=i==1;var body=Stack();var top=Text(runner?"↗  SUBWAY SURFERS":"◇  ORBIT FLIGHT",12,"Accent");body.Children.Add(top);body.Children.Add(Text(runner?"Санкт-Петербург":"Держи высоту",18));body.Children.Add(Text(runner?"Поезда, монеты и погоня по трём дорожкам":"Пролетай между препятствиями",12,"Muted"));var record=Text("",11,"Muted");record.Margin=new Thickness(0);body.Children.Add(record);if(runner)runnerRecord=record;else flightRecord=record;
   var tile=Btn("",()=>{if(!gameReorderEditing)OpenArcade(runner);});tile.Content=body;tile.HorizontalContentAlignment=HorizontalAlignment.Stretch;tile.Margin=new Thickness(0,0,12,12);tile.Padding=new Thickness(16);tile.MinHeight=126;AutomationProperties.SetName(tile,runner?"Играть: Санкт-Петербург":"Играть: Держи высоту");arcadeTiles.Children.Add(tile);
  }UpdateArcadeRecords();
  arcadeLayer=new Grid{Visibility=Visibility.Collapsed,Background=Brushes.Transparent};Panel.SetZIndex(arcadeLayer,60);root.Children.Add(arcadeLayer);
  var shade=new Border{Background=Brush("#B3000000")};CompanionWindowDrag.SetReserved(shade,true);shade.MouseLeftButtonDown+=(s,e)=>{e.Handled=true;CloseArcade();};arcadeLayer.Children.Add(shade);
  arcadeSurface=new Border{CornerRadius=new CornerRadius(18),Padding=new Thickness(18),BorderThickness=new Thickness(1),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};Resource(arcadeSurface,Border.BackgroundProperty,"DockBg");Resource(arcadeSurface,Border.BorderBrushProperty,"Line");arcadeLayer.Children.Add(arcadeSurface);
  var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});arcadeSurface.Child=layout;
  var header=new DockPanel();var back=Btn("← Назад",CloseArcade);DockPanel.SetDock(back,Dock.Left);header.Children.Add(back);arcadeTitle=Text("",20);arcadeTitle.Margin=new Thickness(10,0,0,6);header.Children.Add(arcadeTitle);layout.Children.Add(header);
  arcadeScore=Text("",13,"Muted");Grid.SetRow(arcadeScore,1);layout.Children.Add(arcadeScore);
  arcadeField=new CompanionArcadeField{Width=640,Height=320,Focusable=true,Cursor=Cursors.Hand};CompanionWindowDrag.SetReserved(arcadeField,true);var viewport=new Viewbox{Stretch=Stretch.Uniform,Child=arcadeField,Margin=new Thickness(0,5,0,10)};Grid.SetRow(viewport,2);layout.Children.Add(viewport);KeyboardNavigation.SetTabNavigation(arcadeSurface,KeyboardNavigationMode.Cycle);
  var footer=Stack();var actions=Row();arcadeStart=Btn("Играть",StartArcade,true);arcadePause=Btn("Пауза",PauseArcade);actions.Children.Add(arcadeStart);actions.Children.Add(arcadePause);footer.Children.Add(actions);footer.Children.Add(MiniGameVolume.Controls());arcadeHint=Text("",11,"Muted");arcadeHint.Margin=new Thickness(0);footer.Children.Add(arcadeHint);Grid.SetRow(footer,3);layout.Children.Add(footer);
  arcadeField.MouseLeftButtonDown+=(s,e)=>{e.Handled=true;arcadeField.Focus();if(arcadeGame==null)return;if(!arcadeGame.Started||arcadeGame.Over){StartArcade();return;}if(arcadeGame.Paused)return;if(arcadeGame.Runner){double x=e.GetPosition(arcadeField).X;arcadeGame.Lane=Math.Max(0,Math.Min(2,(int)((x-65)/170)));}else arcadeGame.Action();};
  PreviewKeyDown+=(s,e)=>{if(!arcadeOpen)return;if(e.Key==Key.Escape){e.Handled=true;CloseArcade();return;}if(e.Key==Key.Space||e.Key==Key.Up||e.Key==Key.W||e.Key==Key.Left||e.Key==Key.Right||e.Key==Key.A||e.Key==Key.D||e.Key==Key.P){e.Handled=true;if(e.IsRepeat)return;if(e.Key==Key.P){PauseArcade();return;}if(!arcadeGame.Started||arcadeGame.Over){if(e.Key==Key.Space||e.Key==Key.Up||e.Key==Key.W)StartArcade();return;}if(e.Key==Key.Left||e.Key==Key.A)arcadeGame.Move(-1);else if(e.Key==Key.Right||e.Key==Key.D)arcadeGame.Move(1);else arcadeGame.Action();}};
  arcadeTicker=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(16)};arcadeTicker.Tick+=(s,e)=>ArcadeFrame();root.SizeChanged+=(s,e)=>SizeArcade();Deactivated+=(s,e)=>SuspendArcade();IsVisibleChanged+=(s,e)=>{if(!IsVisible)SuspendArcade();};Closed+=(s,e)=>{CloseRunner3D();arcadeTicker.Stop();arcadeClock.Stop();};
 }
 void SizeArcade(){if(arcadeSurface==null)return;arcadeSurface.Width=Math.Max(1,Math.Min(740,(root.ActualWidth>0?root.ActualWidth:Width)-24));arcadeSurface.Height=Math.Max(1,Math.Min(560,(root.ActualHeight>0?root.ActualHeight:Height)-24));}
 void OpenArcade(bool runner){if(runner){Safe(OpenRunner3D);return;}CloseCardGames();CloseHelp();
  if(compact)SetCompact(false);if(designing)ShowDesign();if(sectionReorderEditing)SetSectionReorder(false);CloseSectionDialog(false,true);CommitArcadeScore();arcadeTicker.Stop();arcadeClock.Reset();arcadeGame=new CompanionArcadeGame(runner,Environment.TickCount);arcadeField.Game=arcadeGame;arcadeTitle.Text=runner?"Санкт-Петербург":"Держи высоту";arcadeHint.Text=runner?"← → / A D - дорожка · Пробел / ↑ - прыжок · P - пауза":"Пробел / ↑ / клик - взмах · P - пауза";
  arcadeOpen=true;arcadeGeneration++;SizeArcade();arcadeLayer.IsHitTestVisible=true;arcadeLayer.Visibility=Visibility.Visible;arcadeLayer.BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(200)));var shift=new TranslateTransform(0,9);arcadeSurface.RenderTransform=shift;shift.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(0,TimeSpan.FromMilliseconds(200)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});UpdateArcadeUi();arcadeField.Focus();
 }
 void StartArcade(){if(!arcadeOpen||arcadeGame==null)return;CommitArcadeScore();arcadeGame.Start();arcadeGame.Action();arcadeClock.Restart();arcadeTicker.Start();UpdateArcadeUi();arcadeField.Focus();}
 void PauseArcade(){if(arcadeGame==null||!arcadeGame.Started||arcadeGame.Over)return;arcadeGame.Paused=!arcadeGame.Paused;if(arcadeGame.Paused){arcadeTicker.Stop();arcadeClock.Stop();}else{arcadeClock.Restart();arcadeTicker.Start();}UpdateArcadeUi();arcadeField.Focus();}
 void SuspendArcade(){if(arcadeOpen&&arcadeGame!=null&&arcadeGame.Started&&!arcadeGame.Over&&!arcadeGame.Paused){arcadeGame.Paused=true;arcadeTicker.Stop();arcadeClock.Stop();UpdateArcadeUi();}}
 void ArcadeFrame(){if(!arcadeOpen||arcadeGame==null)return;double seconds=arcadeClock.Elapsed.TotalSeconds;arcadeClock.Restart();arcadeGame.Step(seconds);if(arcadeGame.Over){arcadeTicker.Stop();arcadeClock.Stop();CommitArcadeScore();}if(arcadeGame.Over||arcadeGame.Elapsed-arcadeUiStamp>=.1)UpdateArcadeUi();else arcadeField.InvalidateVisual();}
 void UpdateArcadeUi(){if(arcadeGame==null)return;arcadeUiStamp=arcadeGame.Elapsed;arcadeScore.Text="Очки: "+arcadeGame.Score+"    ·    Рекорд: "+Math.Max(arcadeGame.Score,arcadeGame.Runner?data.RunnerRecord:data.FlightRecord)+(arcadeGame.Runner?"    ·    Скорость: ×"+(arcadeGame.RunnerSpeed/CompanionArcadeGame.RunnerStartSpeed).ToString("0.00")+(arcadeGame.RunnerSpeed>=CompanionArcadeGame.RunnerMaximumSpeed?" · МАКС":""):arcadeGame.MovingPipes?"    ·    Подвижные препятствия":"");arcadeStart.Content=arcadeGame.Started?"Заново":"Играть";arcadePause.Content=arcadeGame.Paused?"Продолжить":"Пауза";arcadePause.IsEnabled=arcadeGame.Started&&!arcadeGame.Over;arcadeField.InvalidateVisual();}
 void CommitArcadeScore(){if(arcadeGame==null)return;int best=arcadeGame.Runner?data.RunnerRecord:data.FlightRecord;if(arcadeGame.Score<=best)return;if(arcadeGame.Runner)data.RunnerRecord=arcadeGame.Score;else data.FlightRecord=arcadeGame.Score;Save();UpdateArcadeRecords();}
 void UpdateArcadeRecords(){if(flightRecord!=null)flightRecord.Text="Рекорд: "+data.FlightRecord+"   ·   Играть →";if(runnerRecord!=null)runnerRecord.Text="Subway Surfers   ·   Играть →";}
 void CloseArcade(){if(arcadeLayer==null||!arcadeOpen)return;arcadeOpen=false;arcadeTicker.Stop();arcadeClock.Stop();CommitArcadeScore();arcadeLayer.IsHitTestVisible=false;int generation=++arcadeGeneration;var fade=new DoubleAnimation(0,TimeSpan.FromMilliseconds(150));fade.Completed+=(s,e)=>{if(generation!=arcadeGeneration||arcadeOpen)return;arcadeLayer.Visibility=Visibility.Collapsed;if(IsActive&&IsVisible&&!sectionDialogOpen&&!designing&&!compact)gameButtons[arcadeGame.Runner?1:0].Focus();};arcadeLayer.BeginAnimation(OpacityProperty,fade);}
}
