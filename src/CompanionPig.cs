using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.ComponentModel;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

public partial class Companion {
 CompanionPigWindow pigWindow;
 void BuildPigGame(){
  var body=Stack();body.Children.Add(Text("♠  МИСТЕР СВИН",12,"Accent"));
  body.Children.Add(Text("Сундучок",18));body.Children.Add(Text("Карты со свином · без интернета",12,"Muted"));
  var tile=Btn("",()=>{if(!gameReorderEditing)Safe(OpenPigGame);});tile.Content=body;tile.HorizontalContentAlignment=HorizontalAlignment.Stretch;
  tile.Padding=new Thickness(16);tile.Margin=new Thickness(0,0,12,12);tile.MinHeight=126;
  AutomationProperties.SetName(tile,"Играть: Мистер Свин - Сундучок");arcadeTiles.Children.Add(tile);
 }
 public static string EnsurePigAssets(string folder){
  var assembly=typeof(Companion).Assembly;
  string version=assembly.GetName().Version.ToString();
  string target=Path.GetFullPath(Path.Combine(folder,"game-assets","mister-svin",version));
  string marker=Path.Combine(target,".complete");
  if(File.Exists(marker)&&File.Exists(Path.Combine(target,"index.html")))return target;
  using(var stream=assembly.GetManifestResourceStream("CompanionPigAssets")){
   if(stream==null)throw new InvalidDataException("Не найдены встроенные файлы игры.");
   Directory.CreateDirectory(target);
   using(var zip=new ZipArchive(stream,ZipArchiveMode.Read))foreach(var entry in zip.Entries){
    string name=entry.FullName.Replace('\\','/');
    string destination=Path.GetFullPath(Path.Combine(target,name));
    if(!destination.StartsWith(target+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Неверный путь ресурса игры.");
    if(name.EndsWith("/"))continue;
    Directory.CreateDirectory(Path.GetDirectoryName(destination));
    using(var input=entry.Open())using(var output=File.Create(destination))input.CopyTo(output);
   }
  }
  if(!File.Exists(Path.Combine(target,"index.html")))throw new InvalidDataException("Не найден стартовый файл игры.");
  File.WriteAllText(marker,version);return target;
 }
 void OpenPigGame(){
  if(pigWindow==null){
   pigWindow=new CompanionPigWindow(Resources,Path.Combine(Folder,"mister-svin"),EnsurePigAssets(Folder),MiniGameVolume);
   pigWindow.Topmost=data.Top;pigWindow.Closed+=(s,e)=>pigWindow=null;
  }
  pigWindow.Show();if(pigWindow.WindowState==WindowState.Minimized)pigWindow.WindowState=WindowState.Normal;pigWindow.Activate();
 }
}

public sealed class CompanionPigWindow : Window {
 readonly WebView2CompositionControl view;
 readonly TextBlock status;
 readonly string profileFolder,gameFolder;
 readonly CompanionMiniGameVolume gameVolume;readonly PropertyChangedEventHandler volumeChanged;CoreWebView2Environment audioEnvironment;readonly DispatcherTimer audioRefresh=new DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};
 public double Volume {get{return gameVolume.Percent;}set{gameVolume.Percent=value;}}
 void ApplyGameVolume(){if(disposed||view.CoreWebView2==null)return;try{view.CoreWebView2.IsMuted=gameVolume.Percent==0;if(audioEnvironment!=null&&gameVolume.Percent>0)CompanionBrowserAudio.Apply(audioEnvironment.GetProcessInfos().Select(info=>info.ProcessId),gameVolume.Percent);}catch{}}
 bool disposed;
 public CompanionPigWindow(ResourceDictionary theme,string profile,string game,CompanionMiniGameVolume volume=null){
  gameVolume=volume??new CompanionMiniGameVolume(60);volumeChanged=(sender,args)=>ApplyGameVolume();gameVolume.PropertyChanged+=volumeChanged;audioRefresh.Tick+=(sender,args)=>ApplyGameVolume();
  Resources=theme;profileFolder=profile;gameFolder=game;
  Title="Companion · Мистер Свин - Сундучок";Width=1020;Height=700;MinWidth=600;MinHeight=460;
  WindowStartupLocation=WindowStartupLocation.CenterScreen;
  SetResourceReference(BackgroundProperty,"Bg");
  var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition());
  var header=new WrapPanel{Margin=new Thickness(12,8,12,8)};
  var back=new Button{Content="← Закрыть игру",Padding=new Thickness(10,5,10,5),Margin=new Thickness(0,0,14,0)};
  back.Click+=(s,e)=>Close();DockPanel.SetDock(back,Dock.Left);header.Children.Add(back);header.Children.Add(gameVolume.Controls());
  status=new TextBlock{Text="Загрузка игры…",VerticalAlignment=VerticalAlignment.Center};status.SetResourceReference(TextBlock.ForegroundProperty,"Text");header.Children.Add(status);layout.Children.Add(header);
  view=new WebView2CompositionControl{DefaultBackgroundColor=System.Drawing.Color.FromArgb(255,5,87,124)};
  Grid.SetRow(view,1);layout.Children.Add(view);Content=layout;
  Loaded+=async(s,e)=>await Initialize();
  Closed+=(s,e)=>{disposed=true;audioRefresh.Stop();gameVolume.PropertyChanged-=volumeChanged;view.Dispose();};
 }
 async Task Initialize(){
  try{
   if(!File.Exists(Path.Combine(gameFolder,"index.html")))throw new FileNotFoundException("Не найдена папка Games\\MisterSvin рядом с приложением.");
   Directory.CreateDirectory(profileFolder);
   var env=await CoreWebView2Environment.CreateAsync(null,profileFolder);
   if(disposed)return;await view.EnsureCoreWebView2Async(env);if(disposed)return;
   audioEnvironment=env;var core=view.CoreWebView2;ApplyGameVolume();audioRefresh.Start();
   core.Settings.AreHostObjectsAllowed=false;core.Settings.IsWebMessageEnabled=false;
   core.Settings.AreDefaultContextMenusEnabled=false;core.Settings.AreDevToolsEnabled=false;
   core.SetVirtualHostNameToFolderMapping("mister-svin.invalid",gameFolder,CoreWebView2HostResourceAccessKind.DenyCors);
   core.AddWebResourceRequestedFilter("*",CoreWebView2WebResourceContext.All);
   core.WebResourceRequested+=(s,e)=>{
    Uri uri;if(Uri.TryCreate(e.Request.Uri,UriKind.Absolute,out uri)&&
      (uri.Scheme=="data"||uri.Scheme=="blob"||(uri.Scheme=="https"&&uri.Host=="mister-svin.invalid")))return;
    e.Response=env.CreateWebResourceResponse(new MemoryStream(new byte[0]),403,"Offline game","Content-Type: text/plain");
   };
   core.NavigationStarting+=(s,e)=>{Uri uri;if(!Uri.TryCreate(e.Uri,UriKind.Absolute,out uri)||uri.Scheme!="https"||uri.Host!="mister-svin.invalid")e.Cancel=true;};
   core.NewWindowRequested+=(s,e)=>e.Handled=true;
   core.PermissionRequested+=(s,e)=>e.State=CoreWebView2PermissionState.Deny;
   core.NavigationCompleted+=(s,e)=>{status.Text=e.IsSuccess?"":"Не удалось открыть игру: "+e.WebErrorStatus;status.Visibility=e.IsSuccess?Visibility.Collapsed:Visibility.Visible;};
   core.Navigate("https://mister-svin.invalid/index.html");
  }catch(Exception error){if(!disposed)status.Text="Не удалось запустить: "+error.Message;}
 }
}
