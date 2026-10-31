using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.ComponentModel;
using System.Windows.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

public partial class Companion {
 CompanionRunner3DWindow runner3DWindow;
 void OpenRunner3D(){
  CloseCardGames();CloseHelp();CloseArcade();CloseSectionDialog(false,true);
  if(runner3DWindow==null){
   runner3DWindow=new CompanionRunner3DWindow(Resources,Folder,MiniGameVolume);
   runner3DWindow.Topmost=data.Top;
   runner3DWindow.Closed+=(s,e)=>runner3DWindow=null;
  }
  runner3DWindow.Show();if(runner3DWindow.WindowState==WindowState.Minimized)runner3DWindow.WindowState=WindowState.Normal;runner3DWindow.Activate();
 }
 void CloseRunner3D(){if(runner3DWindow!=null)runner3DWindow.Close();}
}

public sealed class CompanionRunner3DWindow : Window {
 readonly WebView2CompositionControl view;
 readonly TextBlock status;
 readonly string dataFolder;
 readonly CompanionMiniGameVolume gameVolume;readonly PropertyChangedEventHandler volumeChanged;CoreWebView2Environment audioEnvironment;readonly DispatcherTimer audioRefresh=new DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};
 public double Volume {get{return gameVolume.Percent;}set{gameVolume.Percent=value;}}
 void ApplyGameVolume(){if(disposed||view.CoreWebView2==null)return;try{view.CoreWebView2.IsMuted=paused||gameVolume.Percent==0;if(audioEnvironment!=null&&gameVolume.Percent>0)CompanionBrowserAudio.Apply(audioEnvironment.GetProcessInfos().Select(info=>info.ProcessId),gameVolume.Percent);}catch{}}
 bool disposed,initializing,paused;
 public sealed class RunnerMessage {public string kind{get;set;}}
 public CompanionRunner3DWindow(ResourceDictionary theme,string folder,CompanionMiniGameVolume volume=null){
  gameVolume=volume??new CompanionMiniGameVolume(60);volumeChanged=(sender,args)=>ApplyGameVolume();gameVolume.PropertyChanged+=volumeChanged;audioRefresh.Tick+=(sender,args)=>ApplyGameVolume();
  Resources=theme;dataFolder=folder;Title="Companion · Subway Surfers: Санкт-Петербург";Width=1050;Height=760;MinWidth=540;MinHeight=420;WindowStartupLocation=WindowStartupLocation.CenterScreen;
  SetResourceReference(BackgroundProperty,"Bg");var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition());
  var header=new WrapPanel{Margin=new Thickness(12,7,12,7)};var back=new Button{Content="← Закрыть игру",Padding=new Thickness(12,5,12,5),Margin=new Thickness(0,0,12,0)};back.Click+=(s,e)=>Close();header.Children.Add(back);header.Children.Add(gameVolume.Controls());
  status=new TextBlock{Text="Загрузка 3D…",VerticalAlignment=VerticalAlignment.Center};status.SetResourceReference(TextBlock.ForegroundProperty,"Text");header.Children.Add(status);layout.Children.Add(header);
  view=new WebView2CompositionControl{DefaultBackgroundColor=System.Drawing.Color.FromArgb(255,8,14,29)};Grid.SetRow(view,1);layout.Children.Add(view);Content=layout;
  Loaded+=async(s,e)=>{
   if(initializing)return;initializing=true;
   try{
    string assets=PrepareAssets(dataFolder);Directory.CreateDirectory(Path.Combine(dataFolder,"subway-spb"));
    var env=await CoreWebView2Environment.CreateAsync(null,Path.Combine(dataFolder,"subway-spb"));if(disposed)return;
    await view.EnsureCoreWebView2Async(env);if(disposed)return;audioEnvironment=env;var core=view.CoreWebView2;ApplyGameVolume();audioRefresh.Start();
    core.Settings.AreHostObjectsAllowed=false;core.Settings.AreDefaultContextMenusEnabled=false;core.Settings.AreDevToolsEnabled=false;
    core.SetVirtualHostNameToFolderMapping("subway-spb.invalid",assets,CoreWebView2HostResourceAccessKind.DenyCors);
    core.AddWebResourceRequestedFilter("*",CoreWebView2WebResourceContext.All);
    core.WebResourceRequested+=(sender,args)=>{Uri uri;if(Uri.TryCreate(args.Request.Uri,UriKind.Absolute,out uri)&&uri.Scheme=="https"&&uri.Host=="subway-spb.invalid")return;args.Response=env.CreateWebResourceResponse(new MemoryStream(new byte[0]),403,"Offline game","Content-Type: text/plain");};
    core.NavigationStarting+=(sender,args)=>{Uri uri;if(!Uri.TryCreate(args.Uri,UriKind.Absolute,out uri)||uri.Scheme!="https"||uri.Host!="subway-spb.invalid")args.Cancel=true;};
    core.NewWindowRequested+=(sender,args)=>args.Handled=true;core.PermissionRequested+=(sender,args)=>args.State=CoreWebView2PermissionState.Deny;
    core.WebMessageReceived+=(sender,args)=>{
     try{Uri uri;if(!Uri.TryCreate(args.Source,UriKind.Absolute,out uri)||uri.Host!="subway-spb.invalid")return;
      var message=new JavaScriptSerializer().Deserialize<RunnerMessage>(args.WebMessageAsJson);
      if(message!=null&&message.kind=="paused"){paused=true;ApplyGameVolume();}else if(message!=null&&message.kind=="resumed"){paused=false;ApplyGameVolume();};
     }catch{}
    };
    core.NavigationCompleted+=(sender,args)=>{status.Text=args.IsSuccess?"":"Ошибка загрузки: "+args.WebErrorStatus;status.Visibility=args.IsSuccess?Visibility.Collapsed:Visibility.Visible;if(args.IsSuccess){if(!IsActive)PauseGame();}};
    core.Navigate("https://subway-spb.invalid/index.html");
   }catch(Exception error){if(!disposed)status.Text="Не удалось запустить 3D: "+error.Message;}
  };
  Deactivated+=(s,e)=>PauseGame();StateChanged+=(s,e)=>{if(WindowState==WindowState.Minimized)PauseGame();};
  Closed+=(s,e)=>{disposed=true;audioRefresh.Stop();gameVolume.PropertyChanged-=volumeChanged;view.Dispose();};
 }
 void PauseGame(){if(!disposed&&view.CoreWebView2!=null)try{paused=true;ApplyGameVolume();view.CoreWebView2.ExecuteScriptAsync("window.companionPause && window.companionPause()");}catch{}}
 public static string PrepareAssets(string folder){
  var assembly=typeof(CompanionRunner3DWindow).Assembly;string target=Path.GetFullPath(Path.Combine(folder,"game-assets","subway-spb",assembly.GetName().Version.ToString()));
  string[] required={"index.html","companion.js","4399.js","loaders/v2/unity/static/UnityLoader.2019.2.js","Build/StPetersburg.json","Build/StPetersburg.data.unityweb","Build/StPetersburg.wasm.code.unityweb","Build/StPetersburg.wasm.framework.unityweb"};
  if(File.Exists(Path.Combine(target,".complete"))&&required.All(file=>File.Exists(Path.Combine(target,file))))return target;
  using(var stream=assembly.GetManifestResourceStream("CompanionRunner3DAssets")){
   if(stream==null)throw new InvalidDataException("Отсутствуют встроенные ресурсы 3D.");Directory.CreateDirectory(target);
   using(var zip=new ZipArchive(stream,ZipArchiveMode.Read))foreach(var entry in zip.Entries){string name=entry.FullName.Replace('\\','/');string path=Path.GetFullPath(Path.Combine(target,name));if(!path.StartsWith(target+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Неверный путь ресурса.");if(name.EndsWith("/"))continue;Directory.CreateDirectory(Path.GetDirectoryName(path));using(var input=entry.Open())using(var output=File.Create(path))input.CopyTo(output);}
  }
  foreach(string file in required)if(!File.Exists(Path.Combine(target,file)))throw new InvalidDataException("Отсутствует ресурс "+file);
  File.WriteAllText(Path.Combine(target,".complete"),"ok");return target;
 }
}
