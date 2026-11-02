using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

public partial class Companion {
 Slider wallpaperZoom,wallpaperX,wallpaperY;bool syncingWallpaperPlacement;
 DispatcherTimer wallpaperPlacementSave;
 static bool PlacementValue(double? value,double min,double max){return !value.HasValue||!Double.IsNaN(value.Value)&&!Double.IsInfinity(value.Value)&&value.Value>=min&&value.Value<=max;}
 bool ValidWallpaperPlacement(Profile p){return PlacementValue(p.WallpaperZoom,.5,3)&&PlacementValue(p.WallpaperX,-100,100)&&PlacementValue(p.WallpaperY,-100,100);}
 void UpdateWallpaperTransform(){
  var group=new TransformGroup();group.Children.Add(new ScaleTransform(data.WallpaperZoom??1,data.WallpaperZoom??1));group.Children.Add(new TranslateTransform(root.ActualWidth*(data.WallpaperX??0)/100,root.ActualHeight*(data.WallpaperY??0)/100));
  wallpaper.RenderTransformOrigin=new Point(.5,.5);wallpaper.RenderTransform=group;
 }
 void SyncWallpaperPlacement(){if(wallpaperZoom==null)return;syncingWallpaperPlacement=true;wallpaperZoom.Value=(data.WallpaperZoom??1)*100;wallpaperX.Value=data.WallpaperX??0;wallpaperY.Value=data.WallpaperY??0;syncingWallpaperPlacement=false;UpdateWallpaperTransform();}
 void WallpaperPlacementSettings(StackPanel body){
  body.Children.Add(Text("Размер и положение картинки",15));
  body.Children.Add(Text("Меняй масштаб и сдвигай картинку. Изменения сразу видны на фоне.",11,"Muted"));
  wallpaperZoom=WallpaperPlacementSlider(body,"Масштаб",50,300,(data.WallpaperZoom??1)*100);
  wallpaperX=WallpaperPlacementSlider(body,"По горизонтали",-100,100,data.WallpaperX??0);
  wallpaperY=WallpaperPlacementSlider(body,"По вертикали",-100,100,data.WallpaperY??0);
  Action apply=()=>{if(syncingWallpaperPlacement)return;data.WallpaperZoom=wallpaperZoom.Value/100;data.WallpaperX=wallpaperX.Value;data.WallpaperY=wallpaperY.Value;UpdateWallpaperTransform();if(ready){wallpaperPlacementSave.Stop();wallpaperPlacementSave.Start();}};
  wallpaperPlacementSave=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};wallpaperPlacementSave.Tick+=(s,e)=>{wallpaperPlacementSave.Stop();Save();};Closed+=(s,e)=>wallpaperPlacementSave.Stop();
  wallpaperZoom.ValueChanged+=(s,e)=>apply();wallpaperX.ValueChanged+=(s,e)=>apply();wallpaperY.ValueChanged+=(s,e)=>apply();
  body.Children.Add(Btn("Сбросить размер и положение",()=>{syncingWallpaperPlacement=true;wallpaperZoom.Value=100;wallpaperX.Value=wallpaperY.Value=0;syncingWallpaperPlacement=false;apply();}));
 }
 Slider WallpaperPlacementSlider(StackPanel body,string title,double min,double max,double value){
  var header=new DockPanel();var amount=Text(value.ToString("0")+"%",12,"Accent");DockPanel.SetDock(amount,Dock.Right);header.Children.Add(amount);header.Children.Add(Text(title,12));body.Children.Add(header);
  var slider=new Slider{Minimum=min,Maximum=max,Value=value,TickFrequency=1,IsSnapToTickEnabled=true,SmallChange=1,LargeChange=10,Margin=new Thickness(0,0,0,12)};slider.SetResourceReference(FrameworkElement.StyleProperty,"BrowserVolumeSlider");System.Windows.Automation.AutomationProperties.SetName(slider,title+" картинки");slider.ValueChanged+=(s,e)=>amount.Text=e.NewValue.ToString("0")+"%";body.Children.Add(slider);return slider;
 }
}
