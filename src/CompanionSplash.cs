using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

public sealed class CompanionSplash : Window {
 public CompanionSplash(ResourceDictionary theme){
  Resources=theme;Title="Companion · Запуск";Width=Math.Min(580,SystemParameters.WorkArea.Width-24);Height=Math.Min(310,SystemParameters.WorkArea.Height-24);
  WindowStartupLocation=WindowStartupLocation.CenterScreen;WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;ShowActivated=false;Topmost=true;Opacity=0;FontFamily=new FontFamily("Segoe UI Semibold");
  var accent=(SolidColorBrush)theme["Accent"];
  var surface=new Border{Width=540,Height=270,Margin=new Thickness(16),CornerRadius=new CornerRadius(22),BorderThickness=new Thickness(1),BorderBrush=new SolidColorBrush(Color.FromRgb(62,68,78)),Background=new LinearGradientBrush(Color.FromRgb(17,19,24),Color.FromRgb(30,34,42),new Point(0,0),new Point(1,1)),Padding=new Thickness(28),Effect=new DropShadowEffect{BlurRadius=22,ShadowDepth=4,Opacity=.32,Color=Colors.Black}};
  var body=new StackPanel{VerticalAlignment=VerticalAlignment.Center};surface.Child=body;
  var logo=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,20)};
  logo.Children.Add(new Path{Data=Geometry.Parse("M 4,24 L 27,1 M 24,5 A 12,12 0 1 1 5,24 A 12,12 0 0 1 24,5"),Width=47,Height=47,Stretch=Stretch.Uniform,Stroke=accent,StrokeThickness=2.2,Margin=new Thickness(0,0,13,0),Effect=new DropShadowEffect{BlurRadius=17,ShadowDepth=0,Opacity=.22,Color=accent.Color}});
  logo.Children.Add(new TextBlock{Text="COMPANION",FontSize=29,FontWeight=FontWeights.Bold,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center});body.Children.Add(logo);
  body.Children.Add(new TextBlock{Text="Твой ритм.",FontSize=29,FontWeight=FontWeights.Bold,Foreground=Brushes.White,HorizontalAlignment=HorizontalAlignment.Center});
  body.Children.Add(new TextBlock{Text="Твой Majestic.",FontSize=29,FontWeight=FontWeights.Bold,Foreground=accent,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,18)});
  body.Children.Add(new Border{Height=3,Width=90,CornerRadius=new CornerRadius(2),Background=accent,Opacity=.7,HorizontalAlignment=HorizontalAlignment.Center});
  Content=new Viewbox{Child=surface,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly};
 }
 public async Task PlayAsync(){
  Show();BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(250)));
  await Task.Delay(1500);
  BeginAnimation(OpacityProperty,new DoubleAnimation(0,TimeSpan.FromMilliseconds(250)));
  await Task.Delay(250);Close();
 }
}

public partial class Companion {
 bool startupSplashPending;
 static void RunWithSplash(){
  try{
   Log("start");var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
   app.DispatcherUnhandledException+=(s,e)=>{Log(e.Exception.ToString());MessageBox.Show("Не удалось выполнить действие: "+e.Exception.Message,"Majestic Companion");e.Handled=true;};
   // Construct hidden first: duplicate launches use the existing instance without a new splash.
   var main=new Companion();main.startupSplashPending=true;app.MainWindow=main;
   app.Startup+=async(s,e)=>{
    CompanionSplash splash=null;
    try{splash=new CompanionSplash(main.Resources);await splash.PlayAsync();}catch(Exception error){Log("splash: "+error);if(splash!=null&&splash.IsVisible)splash.Close();}
    main.startupSplashPending=false;if(main.exiting){app.Shutdown();return;}
    app.ShutdownMode=ShutdownMode.OnMainWindowClose;main.Show();main.Activate();
   };
   app.Run();
  }catch(Exception error){Log(error.ToString());MessageBox.Show("Не удалось запустить помощник: "+error.Message,"Majestic Companion");}
 }
}
