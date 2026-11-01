using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

public partial class Companion {
 TextBlock updateStatus;Button updateCheck,updateInstall,updateCancel;ProgressBar updateProgress;
 CompanionRelease pendingRelease;CompanionReleaseAsset pendingAsset;CancellationTokenSource updateCancellation;bool updateBusy;
 void UpdateSettings(StackPanel body){
  var p=Stack();p.Children.Add(Text("Обновления",17));p.Children.Add(Text("Установлена версия "+GetType().Assembly.GetName().Version.ToString(3),13));p.Children.Add(Text("Источник: GitHub · "+CompanionUpdateCore.Repository,11,"Muted"));
  var auto=new CheckBox{Content="Проверять обновления при запуске",IsChecked=data.AutoCheckUpdates??true,Margin=new Thickness(0,4,0,12)};Resource(auto,Control.ForegroundProperty,"Text");auto.Click+=(s,e)=>{data.AutoCheckUpdates=auto.IsChecked==true;Save();};p.Children.Add(auto);
  updateStatus=Text("Нажми «Проверить обновление».",12,"Muted");p.Children.Add(updateStatus);updateProgress=new ProgressBar{Minimum=0,Maximum=100,Height=5,Visibility=Visibility.Collapsed,Margin=new Thickness(0,0,0,10)};Resource(updateProgress,Control.ForegroundProperty,"Accent");p.Children.Add(updateProgress);
  updateCheck=Btn("Проверить обновление",()=>CheckForUpdate(false));updateInstall=Btn("Скачать и установить",InstallUpdate,true);updateInstall.Visibility=Visibility.Collapsed;updateCancel=Btn("Отмена",()=>{if(updateCancellation!=null)updateCancellation.Cancel();});updateCancel.Visibility=Visibility.Collapsed;p.Children.Add(new WrapPanel{Children={updateCheck,updateInstall,updateCancel}});body.Children.Add(Card(p));
 }
 void InitializeUpdates(){
  bool backupsChecked=false;Loaded+=(s,e)=>{if(backupsChecked)return;backupsChecked=true;var cleanupDelay=new DispatcherTimer{Interval=TimeSpan.FromSeconds(5)};cleanupDelay.Tick+=(sender,args)=>{if(exiting){cleanupDelay.Stop();return;}if(!ready||startupSplashPending)return;cleanupDelay.Stop();string folder=AppDomain.CurrentDomain.BaseDirectory;Version version=GetType().Assembly.GetName().Version;Task.Run(()=>{try{CompanionUpdateCore.CleanupOldBackups(folder,version);}catch(IOException){}catch(UnauthorizedAccessException){}});};cleanupDelay.Start();Closed+=(sender,args)=>cleanupDelay.Stop();};
  bool checkedOnce=false;Loaded+=(s,e)=>{if(checkedOnce)return;checkedOnce=true;var delay=new DispatcherTimer{Interval=TimeSpan.FromSeconds(5)};delay.Tick+=(s2,e2)=>{delay.Stop();if((data.AutoCheckUpdates??true)&&!exiting)CheckForUpdate(true);};delay.Start();};Closed+=(s,e)=>{if(updateCancellation!=null)updateCancellation.Cancel();};
 }
 static bool GitHubDownloadHost(Uri uri){return uri.Scheme=="https"&&uri.Port==443&&(uri.Host=="github.com"||uri.Host=="api.github.com"||uri.Host.EndsWith(".githubusercontent.com",StringComparison.OrdinalIgnoreCase));}
 static string DownloadGitHub(string url,string file,long limit,CancellationToken token,Action<long,long> progress){
  ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;Uri address=new Uri(url);
  for(int redirect=0;redirect<7;redirect++){
   if(!GitHubDownloadHost(address))throw new IOException("GitHub перенаправил запрос на неподдерживаемый адрес.");token.ThrowIfCancellationRequested();var request=(HttpWebRequest)WebRequest.Create(address);request.AllowAutoRedirect=false;request.Timeout=20000;request.ReadWriteTimeout=30000;request.UserAgent="Majestic-Companion-Updater/1.0";request.Accept=file==null?"application/vnd.github+json":"application/octet-stream";
   using(token.Register(()=>request.Abort()))using(var response=(HttpWebResponse)request.GetResponse()){
    int status=(int)response.StatusCode;if(status>=300&&status<=399){var location=response.Headers["Location"];if(String.IsNullOrEmpty(location))throw new IOException("Нет адреса загрузки.");address=new Uri(address,location);continue;}
    if(response.ContentLength>limit)throw new IOException("Файл обновления слишком большой.");using(var input=response.GetResponseStream())using(var output=file==null?(Stream)new MemoryStream():(Stream)File.Create(file)){
     var buffer=new byte[65536];long read=0;int count;while((count=input.Read(buffer,0,buffer.Length))>0){token.ThrowIfCancellationRequested();read+=count;if(read>limit)throw new IOException("Файл обновления слишком большой.");output.Write(buffer,0,count);if(progress!=null)progress(read,response.ContentLength);}if(file==null)return System.Text.Encoding.UTF8.GetString(((MemoryStream)output).ToArray());return null;
    }
   }
  }throw new IOException("Слишком много перенаправлений GitHub.");
 }
 void SetUpdateBusy(bool value){updateBusy=value;updateCheck.IsEnabled=!value;updateInstall.IsEnabled=!value;updateCancel.Visibility=value?Visibility.Visible:Visibility.Collapsed;updateProgress.Visibility=value?Visibility.Visible:Visibility.Collapsed;updateProgress.IsIndeterminate=value;}
 async void CheckForUpdate(bool automatic){
  if(automatic&&!NetworkInterface.GetIsNetworkAvailable())return;
  await RunUpdateCheck(automatic,token=>DownloadGitHub("https://api.github.com/repos/"+CompanionUpdateCore.Repository+"/releases/latest",null,4000000,token,null),()=>MessageBox.Show(this,"Вышла новая версия. Хотите обновить?\n\nВерсия: "+pendingRelease.tag_name+". После обновления программа перезапустится.","Обновление Companion",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)==MessageBoxResult.Yes,BeginApprovedUpdate);
 }
 async Task RunUpdateCheck(bool automatic,Func<CancellationToken,string> download,Func<bool> ask,Action install){
  if(updateBusy)return;string previousStatus=updateStatus.Text;SetUpdateBusy(true);pendingRelease=null;pendingAsset=null;updateInstall.Visibility=Visibility.Collapsed;updateStatus.Text="Проверяем GitHub…";updateCancellation=new CancellationTokenSource();if(automatic)updateCancellation.CancelAfter(6000);var token=updateCancellation.Token;bool accepted=false;
  try{
   string text=await Task.Run(()=>download(token));token.ThrowIfCancellationRequested();var release=CompanionUpdateCore.ReadRelease(text);var current=GetType().Assembly.GetName().Version;
   if(CompanionUpdateCore.ParseVersion(release.tag_name)<=CompanionUpdateCore.ParseVersion(current.ToString())){updateStatus.Text="Установлена последняя версия.";return;}
   var asset=CompanionUpdateCore.SelectAsset(release);pendingRelease=release;pendingAsset=asset;updateStatus.Text="Доступна версия "+release.tag_name+" · "+(asset.size/1024.0/1024.0).ToString("0.#")+" МБ";updateInstall.Visibility=Visibility.Visible;
   updateCancellation.CancelAfter(Timeout.Infinite);if(automatic&&!exiting)accepted=ask();
  }catch(OperationCanceledException){if(!exiting)updateStatus.Text=automatic?previousStatus:"Проверка отменена.";}catch(WebException e){if(!exiting)updateStatus.Text=automatic?previousStatus:token.IsCancellationRequested?"Проверка отменена.":e.Response is HttpWebResponse&&((HttpWebResponse)e.Response).StatusCode==HttpStatusCode.NotFound?"В репозитории пока нет доступного релиза.":"GitHub недоступен или ограничил запросы. Попробуй позже.";}catch(Exception e){if(!exiting)updateStatus.Text=automatic?previousStatus:e.Message;}finally{if(!exiting)SetUpdateBusy(false);}
  if(accepted&&!exiting)install();
 }
 void BeginApprovedUpdate(){Reveal();if(!designing)ShowDesign();Dispatcher.BeginInvoke((Action)(()=>updateStatus.BringIntoView()));InstallApprovedUpdate(true);}
 void InstallUpdate(){InstallApprovedUpdate(false);}
 async void InstallApprovedUpdate(bool approved){
  if(updateBusy||pendingRelease==null||pendingAsset==null)return;var release=pendingRelease;var asset=pendingAsset;SetUpdateBusy(true);updateCancellation=new CancellationTokenSource();var token=updateCancellation.Token;string folder=Path.Combine(Path.GetTempPath(),"CompanionUpdate-"+Guid.NewGuid().ToString("N"));updateStatus.Text="Скачиваем "+release.tag_name+"…";
  try{
   string probe=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,".update-write-"+Guid.NewGuid().ToString("N"));try{using(var check=new FileStream(probe,FileMode.CreateNew,FileAccess.Write,FileShare.None,1,FileOptions.DeleteOnClose))check.WriteByte(0);}catch(UnauthorizedAccessException){throw new IOException("Нет доступа к папке программы. Перенеси Companion в папку, где разрешена запись, и повтори.");}Directory.CreateDirectory(folder);
   string zip=Path.Combine(folder,"update.zip"),stage=Path.Combine(folder,"payload");var progress=new Progress<double>(value=>{if(!exiting){updateProgress.IsIndeterminate=false;updateProgress.Value=value;}});
   var files=await Task.Run(()=>{DownloadGitHub(asset.browser_download_url,zip,CompanionUpdateCore.MaxDownload,token,(read,total)=>((IProgress<double>)progress).Report(total>0?Math.Min(100,read*100.0/total):0));token.ThrowIfCancellationRequested();return CompanionUpdateCore.PreparePackage(zip,asset.digest.Substring(7),stage,release.tag_name);});token.ThrowIfCancellationRequested();
   updateStatus.Text="Обновление проверено и готово к установке.";if(!approved&&MessageBox.Show(this,"Установить "+release.tag_name+" и перезапустить Companion?\n\nСохрани незаконченный ввод перед обновлением. Таймеры, записи и настройки сохранятся.","Обновление Companion",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
   foreach(var widget in widgets)widget.CapturePlacement();CommitArcadeScore();Directory.CreateDirectory(Folder);var text=json.Serialize(data);var temp=ProfileFile+".tmp";File.WriteAllText(temp,text,System.Text.Encoding.UTF8);if(File.Exists(ProfileFile))File.Replace(temp,ProfileFile,ProfileFile+".bak",true);else File.Move(temp,ProfileFile);
   var job=new CompanionUpdateJob{Protocol=1,ParentPid=Process.GetCurrentProcess().Id,TargetDirectory=AppDomain.CurrentDomain.BaseDirectory,StageDirectory=stage,Version=release.tag_name,Files=files};string jobFile=Path.Combine(folder,"job.json");File.WriteAllText(jobFile,new JavaScriptSerializer().Serialize(job));string helper=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,CompanionUpdateCore.UpdaterExe);string copy=Path.Combine(folder,CompanionUpdateCore.UpdaterExe);File.Copy(helper,copy);Process.Start(new ProcessStartInfo{FileName=copy,Arguments="\""+jobFile+"\"",WorkingDirectory=folder,UseShellExecute=false});exiting=true;Close();
  }catch(OperationCanceledException){if(!exiting)updateStatus.Text="Загрузка отменена.";}catch(Exception e){if(!exiting)updateStatus.Text=token.IsCancellationRequested?"Загрузка отменена.":"Не удалось установить обновление: "+e.Message;}finally{if(!exiting)SetUpdateBusy(false);}
 }
}
