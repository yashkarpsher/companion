using System;
using System.IO;
using System.IO.Compression;
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
   runner3DWindow=new CompanionRunner3DWindow(Resources,Folder,data.Runner3DRecord);
   runner3DWindow.Topmost=data.Top;
   runner3DWindow.RecordChanged+=score=>{if(score>data.Runner3DRecord){data.Runner3DRecord=score;Save();UpdateArcadeRecords();}};
   runner3DWindow.Closed+=(s,e)=>runner3DWindow=null;
  }
  runner3DWindow.Show();if(runner3DWindow.WindowState==WindowState.Minimized)runner3DWindow.WindowState=WindowState.Normal;runner3DWindow.Activate();
 }
 void CloseRunner3D(){if(runner3DWindow!=null)runner3DWindow.Close();}
}

public sealed class CompanionRunner3DWindow : Window {
 public event Action<int> RecordChanged;
 readonly WebView2CompositionControl view;
 readonly TextBlock status;
 readonly string dataFolder;
 readonly int initialBest;
 bool disposed,initializing;
 public sealed class RunnerMessage {public string kind{get;set;}public int score{get;set;}}
 public CompanionRunner3DWindow(ResourceDictionary theme,string folder,int best){
  Resources=theme;dataFolder=folder;initialBest=best;Title="Companion · Беги от мусорочка";Width=1050;Height=760;MinWidth=540;MinHeight=420;WindowStartupLocation=WindowStartupLocation.CenterScreen;
  SetResourceReference(BackgroundProperty,"Bg");var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition());
  var header=new DockPanel{Margin=new Thickness(12,7,12,7)};var back=new Button{Content="← Закрыть игру",Padding=new Thickness(12,5,12,5),Margin=new Thickness(0,0,12,0)};back.Click+=(s,e)=>Close();header.Children.Add(back);
  status=new TextBlock{Text="Загрузка 3D…",VerticalAlignment=VerticalAlignment.Center};status.SetResourceReference(TextBlock.ForegroundProperty,"Text");header.Children.Add(status);layout.Children.Add(header);
  view=new WebView2CompositionControl{DefaultBackgroundColor=System.Drawing.Color.FromArgb(255,8,14,29)};Grid.SetRow(view,1);layout.Children.Add(view);Content=layout;
  Loaded+=async(s,e)=>{
   if(initializing)return;initializing=true;
   try{
    string assets=PrepareAssets(dataFolder);Directory.CreateDirectory(Path.Combine(dataFolder,"runner3d"));
    var env=await CoreWebView2Environment.CreateAsync(null,Path.Combine(dataFolder,"runner3d"));if(disposed)return;
    await view.EnsureCoreWebView2Async(env);if(disposed)return;var core=view.CoreWebView2;
    core.Settings.AreHostObjectsAllowed=false;core.Settings.AreDefaultContextMenusEnabled=false;core.Settings.AreDevToolsEnabled=false;
    core.SetVirtualHostNameToFolderMapping("runner3d.invalid",assets,CoreWebView2HostResourceAccessKind.DenyCors);
    core.AddWebResourceRequestedFilter("*",CoreWebView2WebResourceContext.All);
    core.WebResourceRequested+=(sender,args)=>{Uri uri;if(Uri.TryCreate(args.Request.Uri,UriKind.Absolute,out uri)&&uri.Scheme=="https"&&uri.Host=="runner3d.invalid")return;args.Response=env.CreateWebResourceResponse(new MemoryStream(new byte[0]),403,"Offline game","Content-Type: text/plain");};
    core.NavigationStarting+=(sender,args)=>{Uri uri;if(!Uri.TryCreate(args.Uri,UriKind.Absolute,out uri)||uri.Scheme!="https"||uri.Host!="runner3d.invalid")args.Cancel=true;};
    core.NewWindowRequested+=(sender,args)=>args.Handled=true;core.PermissionRequested+=(sender,args)=>args.State=CoreWebView2PermissionState.Deny;
    core.WebMessageReceived+=(sender,args)=>{
     try{Uri uri;if(!Uri.TryCreate(args.Source,UriKind.Absolute,out uri)||uri.Host!="runner3d.invalid")return;
      var message=new JavaScriptSerializer().Deserialize<RunnerMessage>(args.WebMessageAsJson);
      if(message!=null&&message.kind=="record"&&message.score>=0&&message.score<=100000000&&RecordChanged!=null)RecordChanged(message.score);
     }catch{}
    };
    core.NavigationCompleted+=(sender,args)=>{status.Text=args.IsSuccess?"":"Ошибка загрузки: "+args.WebErrorStatus;status.Visibility=args.IsSuccess?Visibility.Collapsed:Visibility.Visible;if(args.IsSuccess){core.PostWebMessageAsJson("{\"kind\":\"best\",\"score\":"+initialBest+"}");if(!IsActive)PauseGame();}};
    core.Navigate("https://runner3d.invalid/index.html");
   }catch(Exception error){if(!disposed)status.Text="Не удалось запустить 3D: "+error.Message;}
  };
  Deactivated+=(s,e)=>PauseGame();StateChanged+=(s,e)=>{if(WindowState==WindowState.Minimized)PauseGame();};
  Closed+=(s,e)=>{disposed=true;view.Dispose();};
 }
 void PauseGame(){if(!disposed&&view.CoreWebView2!=null)try{view.CoreWebView2.PostWebMessageAsJson("{\"kind\":\"pause\"}");}catch{}}
 public static string PrepareAssets(string folder){
  var assembly=typeof(CompanionRunner3DWindow).Assembly;string target=Path.GetFullPath(Path.Combine(folder,"game-assets","runner3d",assembly.GetName().Version.ToString()));
  if(File.Exists(Path.Combine(target,".complete"))&&File.Exists(Path.Combine(target,"scene.js"))&&File.Exists(Path.Combine(target,"index.html"))&&File.Exists(Path.Combine(target,"style.css"))&&File.Exists(Path.Combine(target,"runner-core.js")))return target;
  using(var stream=assembly.GetManifestResourceStream("CompanionRunner3DAssets")){
   if(stream==null)throw new InvalidDataException("Отсутствуют встроенные ресурсы 3D.");Directory.CreateDirectory(target);
   using(var zip=new ZipArchive(stream,ZipArchiveMode.Read))foreach(var entry in zip.Entries){string name=entry.FullName.Replace('\\','/');string path=Path.GetFullPath(Path.Combine(target,name));if(!path.StartsWith(target+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Неверный путь ресурса.");if(name.EndsWith("/"))continue;Directory.CreateDirectory(Path.GetDirectoryName(path));using(var input=entry.Open())using(var output=File.Create(path))input.CopyTo(output);}
  }
  foreach(string file in new[]{"index.html","runner-core.js","scene.js","style.css"})if(!File.Exists(Path.Combine(target,file)))throw new InvalidDataException("Отсутствует ресурс "+file);
  File.WriteAllText(Path.Combine(target,".complete"),"ok");return target;
 }
}
