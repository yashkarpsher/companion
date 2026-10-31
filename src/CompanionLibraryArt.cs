using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public partial class Companion {
 void InstallGameCovers(){
  string[] keys={"flight","runner","durak","blackjack","svin"};
  for(int i=0;i<gameButtons.Count;i++){
   var button=gameButtons[i];var text=button.Content as StackPanel;if(text==null)continue;
   button.Content=null;var card=new StackPanel();string coverKey=keys[i];var image=new Image{Stretch=Stretch.UniformToFill,HorizontalAlignment=HorizontalAlignment.Stretch,VerticalAlignment=VerticalAlignment.Stretch};System.Windows.Automation.AutomationProperties.SetName(image,"Иллюстрация мини-игры: "+coverKey);
   image.IsVisibleChanged+=(sender,args)=>{if(image.IsVisible&&image.Source==null){using(var stream=GetType().Assembly.GetManifestResourceStream("GameCover."+coverKey)){if(stream==null)throw new InvalidDataException("Нет обложки: "+coverKey);var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.DecodePixelWidth=720;bitmap.StreamSource=stream;bitmap.EndInit();bitmap.Freeze();image.Source=bitmap;}}else if(!image.IsVisible)image.Source=null;};
   var cover=new Border{Height=148,Margin=new Thickness(11,11,11,0),Child=image,ClipToBounds=true};cover.SizeChanged+=(s,e)=>{var b=(Border)s;b.Clip=new RectangleGeometry(new Rect(0,0,Math.Max(0,b.ActualWidth),Math.Max(0,b.ActualHeight)),8,8);};card.Children.Add(cover);
   text.Margin=new Thickness(15,12,15,14);foreach(var child in text.Children){var label=child as TextBlock;if(label!=null)label.TextWrapping=TextWrapping.Wrap;}card.Children.Add(text);button.Content=card;button.Padding=new Thickness(0);button.MinHeight=0;button.HorizontalContentAlignment=HorizontalAlignment.Stretch;
   button.Template=(ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'><Border x:Name='card' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='12'><ContentPresenter HorizontalAlignment='Stretch' VerticalAlignment='Stretch'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='card' Property='BorderBrush' Value='{DynamicResource Accent}'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='card' Property='BorderBrush' Value='{DynamicResource Accent}'/></Trigger><Trigger Property='IsPressed' Value='True'><Setter TargetName='card' Property='Opacity' Value='.8'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
  }
 }
 FrameworkElement HeaderTutorialPicture(BitmapSource bitmap){
  // Zoom the real upper-right toolbar. All annotations are UI elements, not baked-in replacements for the screenshot.
  const double origin=950,width=230,height=114;var canvas=new Canvas{Width=width,Height=height,ClipToBounds=true};var image=new Image{Source=bitmap,Width=bitmap.PixelWidth,Height=bitmap.PixelHeight,Stretch=Stretch.Fill};Canvas.SetLeft(image,-origin);canvas.Children.Add(image);
  double[] left={974,1015,1057,1120},sizes={35,35,58,35};string[] names={"Настройки","Полностью выйти","Мини-режим","Свернуть в трей"};var mint=Brush("#70EABC");
  for(int i=0;i<4;i++){
   double x=left[i]-origin,center=x+sizes[i]/2;
   var highlight=new System.Windows.Shapes.Rectangle{Width=sizes[i]+4,Height=39,RadiusX=6,RadiusY=6,Stroke=mint,StrokeThickness=1.5,Fill=Brush("#1470EABC")};Canvas.SetLeft(highlight,x-2);Canvas.SetTop(highlight,14);canvas.Children.Add(highlight);
   var leader=new System.Windows.Shapes.Line{X1=center,Y1=53,X2=center,Y2=71,Stroke=mint,StrokeThickness=1.4};canvas.Children.Add(leader);
   var label=new TextBlock{Text=(i+1).ToString(),FontSize=12,FontWeight=FontWeights.Bold,Foreground=Brush("#101317"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};var badge=new Border{Width=24,Height=24,CornerRadius=new CornerRadius(12),Background=mint,BorderBrush=Brush("#132A23"),BorderThickness=new Thickness(1),Child=label};System.Windows.Automation.AutomationProperties.SetName(badge,(i+1)+". "+names[i]);Canvas.SetLeft(badge,center-12);Canvas.SetTop(badge,71);canvas.Children.Add(badge);
  }
  var zoom=new Viewbox{Child=canvas,Stretch=Stretch.Uniform,MaxWidth=560,Margin=new Thickness(0,6,0,12),HorizontalAlignment=HorizontalAlignment.Left};System.Windows.Automation.AutomationProperties.SetName(zoom,"Верхняя панель: 1 настройки, 2 выход, 3 мини, 4 свернуть");return zoom;
 }
}
