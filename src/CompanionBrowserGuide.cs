using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

public static class CompanionBrowserGuide {
 public static FrameworkElement Create(ResourceDictionary theme,BitmapSource picture){
  var result=new StackPanel{Resources=theme,MaxWidth=680,Margin=new Thickness(0,6,0,12)};
  result.Children.Add(Strip(picture,0,new[]{1,2,3},new[]{new Rect(727,10,160,42),new Rect(900,12,34,38),new Rect(938,12,34,38)}));
  result.Children.Add(Strip(picture,62,new[]{5,4,6},new[]{new Rect(896,68,36,40),new Rect(950,68,100,40),new Rect(1053,68,34,40)}));return result;
 }
 static FrameworkElement Strip(BitmapSource picture,double top,int[] numbers,Rect[] targets){
  const double scale=1.7,left=700;var canvas=new Canvas{Width=680,Height=145};var viewport=new Canvas{Width=680,Height=108,ClipToBounds=true};var image=new Image{Source=picture,Width=picture.PixelWidth*scale,Height=picture.PixelHeight*scale};Canvas.SetLeft(image,-left*scale);Canvas.SetTop(image,-top*scale);viewport.Children.Add(image);canvas.Children.Add(viewport);
  for(int i=0;i<numbers.Length;i++){
   var rect=targets[i];double x=(rect.X-left)*scale,y=(rect.Y-top)*scale,w=rect.Width*scale,h=rect.Height*scale;var outline=new Rectangle{Width=w,Height=h,RadiusX=9,RadiusY=9,StrokeThickness=2};outline.SetResourceReference(Shape.StrokeProperty,"Accent");Canvas.SetLeft(outline,x);Canvas.SetTop(outline,y);canvas.Children.Add(outline);double center=x+w/2;
   var line=new Line{X1=center,Y1=y+h,X2=center,Y2=118,StrokeThickness=2};line.SetResourceReference(Shape.StrokeProperty,"Accent");canvas.Children.Add(line);
   var label=new TextBlock{Text=numbers[i].ToString(),FontSize=16,FontWeight=FontWeights.Bold,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};label.SetResourceReference(TextBlock.ForegroundProperty,"AccentText");var badge=new Border{Width=26,Height=26,CornerRadius=new CornerRadius(13),Child=label};badge.SetResourceReference(Border.BackgroundProperty,"AccentFill");Canvas.SetLeft(badge,center-13);Canvas.SetTop(badge,116);canvas.Children.Add(badge);
  }
  return new Viewbox{Child=canvas,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,MaxWidth=680,HorizontalAlignment=HorizontalAlignment.Left};
 }
}
