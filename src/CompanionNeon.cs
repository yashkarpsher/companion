using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

public partial class Companion {
 BitmapImage scenicBitmap;
 void MigrateNeonPreferences(){if(data.ScenicWallpaper!=null)return;data.ScenicWallpaper=String.IsNullOrWhiteSpace(data.Wallpaper);if(data.ScenicWallpaper==true)data.WallpaperOpacity=.38;}
 void UpdateNeonClip(){UpdateWallpaperTransform();if(root.ActualWidth>0&&root.ActualHeight>0)root.Clip=new RectangleGeometry(new Rect(0,0,root.ActualWidth,root.ActualHeight),compact?7:17,compact?7:17);}
 void ApplyNeonPalette(){
  Color seed;if(!TryAccent(data.AccentColor,out seed))seed=Color.FromRgb(187,112,255);
  Color dark=Mix(seed,Color.FromRgb(8,10,20),.93),panel=Mix(dark,seed,.11),secondary=Mix(seed,Color.FromRgb(255,114,223),.35);
  byte alpha=(byte)Math.Round(data.Density*255);Color accent=ReadableAccent(seed,dark);
  Resources["Bg"]=WithAlpha(dark,alpha);Resources["Text"]=Brush("#FAF7FF");Resources["Muted"]=new SolidColorBrush(Mix(dark,Colors.White,.73));Resources["Accent"]=new SolidColorBrush(accent);
  Resources["Panel"]=new LinearGradientBrush(WithAlpha(panel,(byte)Math.Min(225,(int)alpha)).Color,WithAlpha(dark,(byte)Math.Min(214,(int)alpha)).Color,95);
  Resources["Field"]=WithAlpha(Mix(panel,Colors.White,.055),(byte)Math.Min(180,(int)alpha));Resources["DockBg"]=WithAlpha(panel,data.Density>=1?(byte)255:(byte)240);
  Resources["NavBg"]=WithAlpha(dark,(byte)Math.Min(205,(int)alpha));Resources["Line"]=WithAlpha(Mix(accent,Colors.White,.22),85);Resources["AccentLine"]=WithAlpha(accent,180);Resources["FrameLine"]=WithAlpha(accent,105);
  Color shine=HueColor((Hue(seed)+35)%360),shade=HueColor((Hue(seed)+325)%360);if(Math.Max(seed.R,Math.Max(seed.G,seed.B))-Math.Min(seed.R,Math.Min(seed.G,seed.B))<20)shine=shade=accent;Resources["AccentText"]=Brush("#160F22");Resources["AccentFill"]=new LinearGradientBrush(ReadableAccent(Mix(accent,shine,.55),dark),ReadableAccent(Mix(accent,shade,.55),dark),new Point(0,0),new Point(1,1));
  Resources["SelectedTab"]=new LinearGradientBrush(WithAlpha(seed,115).Color,WithAlpha(seed,28).Color,new Point(0,0),new Point(1,0));
  Resources["RingGradient"]=new LinearGradientBrush(new GradientStopCollection{new GradientStop(Mix(seed,Color.FromRgb(115,106,255),.4),0),new GradientStop(accent,.5),new GradientStop(Mix(accent,Color.FromRgb(255,132,224),.45),1)},45);
  Resources["RingTrack"]=WithAlpha(accent,45);Resources["ScrollThumb"]=WithAlpha(accent,185);Resources["ScrollTrack"]=WithAlpha(accent,35);Resources["Selection"]=WithAlpha(seed,110);
  Resources["NeonRingGlow"]=new DropShadowEffect{Color=seed,BlurRadius=18,ShadowDepth=0,Opacity=.38,RenderingBias=RenderingBias.Performance};
  Resources["NeonSmallGlow"]=new DropShadowEffect{Color=seed,BlurRadius=10,ShadowDepth=0,Opacity=.32,RenderingBias=RenderingBias.Performance};
  if(brand!=null){Resource(brand,TextBlock.ForegroundProperty,"Accent");Resource(brand,UIElement.EffectProperty,"NeonSmallGlow");}
  Resources["MetricWash"]=new LinearGradientBrush(WithAlpha(seed,0).Color,WithAlpha(seed,80).Color,new Point(0,0),new Point(1,1));
 }
 void ApplyScenicWallpaper(){
  if(data.ScenicWallpaper==false)return;
  if(scenicBitmap==null){using(var stream=GetType().Assembly.GetManifestResourceStream("CompanionNeonWallpaper")){if(stream==null)return;var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.DecodePixelWidth=1600;image.StreamSource=stream;image.EndInit();image.Freeze();scenicBitmap=image;}}
  wallpaper.Source=scenicBitmap;wallpaper.Opacity=Math.Min(1,data.WallpaperOpacity*2);
 }
 FrameworkElement NeonMetric(TextBlock label,TextBlock number,string icon){
  var body=new Grid{MinHeight=82,ClipToBounds=true};body.Children.Add(new CompanionMetricTrace());var labels=new StackPanel{Margin=new Thickness(12,10,44,10)};label.FontSize=9;label.TextWrapping=TextWrapping.NoWrap;label.TextTrimming=TextTrimming.CharacterEllipsis;number.FontSize=27;number.Margin=new Thickness(0);Resource(number,TextBlock.ForegroundProperty,"Accent");labels.Children.Add(label);labels.Children.Add(number);body.Children.Add(labels);
  var symbol=DashboardIcon(icon,23);symbol.HorizontalAlignment=HorizontalAlignment.Right;symbol.VerticalAlignment=VerticalAlignment.Center;symbol.Margin=new Thickness(0,0,14,0);Resource(symbol,System.Windows.Shapes.Shape.StrokeProperty,"Accent");Resource(symbol,UIElement.EffectProperty,"NeonSmallGlow");body.Children.Add(symbol);body.SizeChanged+=(s,e)=>{bool narrow=body.ActualWidth<160;symbol.Visibility=narrow?Visibility.Collapsed:Visibility.Visible;labels.Margin=new Thickness(12,10,narrow?10:44,10);};label.ToolTip=label.Text;
  var border=new Border{CornerRadius=new CornerRadius(12),Child=body,BorderThickness=new Thickness(1)};Resource(border,Border.BackgroundProperty,"MetricWash");Resource(border,Border.BorderBrushProperty,"Line");return border;
 }
}
public sealed class CompanionMetricTrace:FrameworkElement {
 public CompanionMetricTrace(){IsHitTestVisible=false;SetResourceReference(TraceBrushProperty,"Accent");}
 public static readonly DependencyProperty TraceBrushProperty=DependencyProperty.Register("TraceBrush",typeof(Brush),typeof(CompanionMetricTrace),new FrameworkPropertyMetadata(Brushes.Violet,FrameworkPropertyMetadataOptions.AffectsRender));
 protected override void OnRender(DrawingContext d){double w=ActualWidth,h=ActualHeight;if(w<20||h<20)return;var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(new Point(w*.25,h-3),false,false);c.BezierTo(new Point(w*.5,h-3),new Point(w*.62,h-2),new Point(w*.7,h-7),true,false);c.BezierTo(new Point(w*.8,h-30),new Point(w*.83,h-5),new Point(w*.94,h-15),true,false);}d.PushOpacity(.7);d.DrawGeometry(null,new Pen((Brush)GetValue(TraceBrushProperty),1.4),g);d.Pop();}
}
