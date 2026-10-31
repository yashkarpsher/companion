using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

public sealed class CompanionMiniGameVolume : INotifyPropertyChanged {
 double percent;public event PropertyChangedEventHandler PropertyChanged;
 public CompanionMiniGameVolume(double value){percent=CompanionBrowserAudio.Normalize(value);}
 public double Percent {get{return percent;}set{double next=CompanionBrowserAudio.Normalize(value);if(next==percent)return;percent=next;if(PropertyChanged!=null)PropertyChanged(this,new PropertyChangedEventArgs("Percent"));}}
 public FrameworkElement Controls(){
  var row=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(6,0,0,4)};var name=new TextBlock{Text="Звук",FontSize=11,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,7,0)};name.SetResourceReference(TextBlock.ForegroundProperty,"Muted");row.Children.Add(name);
  var slider=new Slider{Minimum=0,Maximum=100,Width=95,TickFrequency=1,IsSnapToTickEnabled=true,IsMoveToPointEnabled=true,VerticalAlignment=VerticalAlignment.Center};slider.SetBinding(Slider.ValueProperty,new Binding("Percent"){Source=this,Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});slider.SetResourceReference(Control.ForegroundProperty,"Accent");System.Windows.Automation.AutomationProperties.SetName(slider,"Громкость мини-игр");row.Children.Add(slider);
  var label=new TextBlock{FontSize=11,MinWidth=34,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(6,0,0,0)};label.SetResourceReference(TextBlock.ForegroundProperty,"Muted");label.SetBinding(TextBlock.TextProperty,new Binding("Percent"){Source=this,StringFormat="{0:0}%"});row.Children.Add(label);return row;
 }
}
public partial class Companion {
 CompanionMiniGameVolume miniGameVolume;DispatcherTimer gameVolumeSave;
 CompanionMiniGameVolume MiniGameVolume {
  get{if(miniGameVolume==null){miniGameVolume=new CompanionMiniGameVolume(data.MiniGameVolume??60);gameVolumeSave=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};gameVolumeSave.Tick+=(s,e)=>{gameVolumeSave.Stop();Save();};miniGameVolume.PropertyChanged+=(s,e)=>{data.MiniGameVolume=miniGameVolume.Percent;gameVolumeSave.Stop();gameVolumeSave.Start();};Closed+=(s,e)=>{if(gameVolumeSave.IsEnabled){gameVolumeSave.Stop();Save();}};}return miniGameVolume;}
 }
}
