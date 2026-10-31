using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

public partial class Companion {
 CompanionBrowserWindow browserWindow;
 bool CompactHeroTab { get { return false; } }
 void BrowserTab(){
  var body=Stack();body.Children.Add(Text("Браузер поверх игры",23));
  body.Children.Add(Text("Видео, музыка и поиск - в отдельном окне, размер которого выбираешь ты.",13,"Muted"));
  var card=Stack();card.Children.Add(Text("Стартовая страница",15));
  var google=Btn("Google",()=>{});var yandex=Btn("Яндекс",()=>{});
  Action refresh=()=>{bool useYandex=data.BrowserHome=="yandex";Resource(google,Control.BackgroundProperty,useYandex?"Field":"AccentFill");Resource(google,Control.ForegroundProperty,useYandex?"Text":"AccentText");Resource(yandex,Control.BackgroundProperty,useYandex?"AccentFill":"Field");Resource(yandex,Control.ForegroundProperty,useYandex?"AccentText":"Text");System.Windows.Automation.AutomationProperties.SetName(google,useYandex?"Google":"Google - выбрано");System.Windows.Automation.AutomationProperties.SetName(yandex,useYandex?"Яндекс - выбрано":"Яндекс");};
  Action<string> choose=provider=>{data.BrowserHome=provider;Save();if(browserWindow!=null)browserWindow.HomeProvider=provider;refresh();};
  google.Click+=(s,e)=>choose("google");yandex.Click+=(s,e)=>choose("yandex");refresh();
  card.Children.Add(Row(google,yandex));card.Children.Add(Btn("Открыть браузер",()=>OpenBrowser(null),true));
  card.Children.Add(Btn("Открыть YouTube",()=>OpenBrowser("https://www.youtube.com/")));
  card.Children.Add(Btn("Fletcher Wiki",()=>OpenBrowser("https://fletcher-wiki.com/")));
  body.Children.Add(Card(card));body.Children.Add(Text("Закрепи окно поверх игры. Режим просмотра убирает адресную строку; Esc возвращает панели. Для игры используй режим «без рамки».",12,"Muted"));
  var backgroundVideo=new CheckBox{Content="Продолжать видео, когда активна игра",IsChecked=data.BackgroundVideo??true,Margin=new Thickness(0,0,0,12)};Resource(backgroundVideo,Control.ForegroundProperty,"Text");backgroundVideo.Click+=(s,e)=>{data.BackgroundVideo=backgroundVideo.IsChecked==true;Save();if(browserWindow!=null)browserWindow.BackgroundPlayback=data.BackgroundVideo.Value;};body.Children.Add(backgroundVideo);
  body.Children.Add(Text("При потере фокуса страница остаётся активной для воспроизведения. Фокус клавиатуры остаётся игре. Режим не отменяет ограничения сайта или видеодрайвера.",11,"Muted"));
  body.Children.Add(Text("Сайты используют интернет. Их cookies и настройки хранятся отдельно от профиля помощника. Некоторые видеосервисы могут ограничивать просмотр во встроенном браузере.",11,"Muted"));
  Tab("Браузер",body);
 }
 void OpenBrowser(string destination){
  if(browserWindow==null){browserWindow=new CompanionBrowserWindow(Resources,Path.Combine(Folder,"browser"),data.BrowserHome,data.BrowserVolume??100);browserWindow.BackgroundPlayback=data.BackgroundVideo??true;browserWindow.VolumeChanged+=value=>{data.BrowserVolume=value;Save();};browserWindow.RequestGameMode+=()=>Safe(()=>SetGameInput(true));browserWindow.Closed+=(s,e)=>browserWindow=null;}
  browserWindow.Show();browserWindow.WindowState=WindowState.Normal;browserWindow.Activate();if(destination!=null)browserWindow.Go(destination);
 }
}

public class CompanionBrowserWindow : Window {
 public string HomeProvider {get;set;}
 readonly string userFolder;
 readonly Grid layout=new Grid();
 readonly DockPanel titlebar;
 readonly Border windowFrame;
 readonly DispatcherTimer hoverRefresh=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(120)};
 Window controlsWindow;Grid floatingControls;DateTime lastControlsHover;
 readonly TextBox address;
 readonly TextBlock status;
 readonly Border bar;
 readonly WebView2CompositionControl view;
 readonly CompanionInputGate inputGate;
 bool backgroundPlayback=true;
 public event Action RequestGameMode;
 public bool InputLocked {get{return inputGate.Locked;}set{if(value&&controlsWindow!=null)controlsWindow.Hide();inputGate.Locked=value;}}
 public bool BackgroundPlayback {get{return backgroundPlayback;}set{backgroundPlayback=value;ApplyBackgroundPlayback();}}
 readonly Button back,forward,mute,pin,cinema;
 readonly Slider volumeSlider;
 readonly TextBlock volumeLabel;
 readonly DispatcherTimer audioRefresh=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)};
 readonly DispatcherTimer volumeSave=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};
 CoreWebView2Environment browserEnvironment;
 bool muted;
 public event Action<double> VolumeChanged;
 public double Volume {get{return volumeSlider.Value;}set{volumeSlider.Value=CompanionBrowserAudio.Normalize(value);}}
 bool initialized,initializing,disposed,viewMode;
 string pending;
 public static string HomeUrl(string provider){return provider=="yandex"?"https://ya.ru/":"https://www.google.com/";}
 public static string ResolveAddress(string text,string provider){
  text=(text??"").Trim();if(text.Length==0)return HomeUrl(provider);
  Uri uri;
  if(Uri.TryCreate(text,UriKind.Absolute,out uri)&&(uri.Scheme=="https"||uri.Scheme=="http")&&!String.IsNullOrEmpty(uri.Host))return uri.AbsoluteUri;
  // Other URL schemes cannot launch programs or access local files through the address bar.
  if(text.IndexOf(":",StringComparison.Ordinal)>=0&&!text.Contains("://")&&!text.Contains(" ")&&text.IndexOf(':')<text.IndexOf('.'))return Search(text,provider);
  if(!text.Contains(" ")&&text.Contains(".")&&!text.Contains("://")&&Uri.TryCreate("https://"+text,UriKind.Absolute,out uri)&&!String.IsNullOrEmpty(uri.Host))return uri.AbsoluteUri;
  return Search(text,provider);
 }
 static string Search(string query,string provider){return(provider=="yandex"?"https://ya.ru/search/?text=":"https://www.google.com/search?q=")+Uri.EscapeDataString(query);}
 public CompanionBrowserWindow(ResourceDictionary theme,string folder,string provider,double initialVolume=100){
  Resources=theme;userFolder=folder;HomeProvider=provider;Title="Companion · Браузер";
  inputGate=new CompanionInputGate(this);
  Width=Math.Min(800,SystemParameters.WorkArea.Width-32);Height=Math.Min(510,SystemParameters.WorkArea.Height-32);MinWidth=380;MinHeight=240;Topmost=true;
  WindowStartupLocation=WindowStartupLocation.CenterScreen;WindowStyle=WindowStyle.None;AllowsTransparency=true;ResizeMode=ResizeMode.CanResizeWithGrip;
  WindowChrome.SetWindowChrome(this,new WindowChrome{CaptionHeight=0,ResizeBorderThickness=new Thickness(6),CornerRadius=new CornerRadius(0),GlassFrameThickness=new Thickness(0)});
  SetResourceReference(BackgroundProperty,"DockBg");FontFamily=new FontFamily("Segoe UI Semibold");
  windowFrame=new Border{BorderThickness=new Thickness(1),Padding=new Thickness(5)};windowFrame.SetResourceReference(Border.BorderBrushProperty,"Line");windowFrame.Child=layout;Content=windowFrame;
  layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  var header=new DockPanel{LastChildFill=true};titlebar=header;
  var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
  pin=Button("◆",()=>{Topmost=!Topmost;pin.ToolTip=Topmost?"Открепить окно":"Поверх остальных окон";pin.Opacity=Topmost?1:.5;},"Открепить окно");
  cinema=Button("▣",()=>SetViewMode(!viewMode),"Без рамки / вернуть панели (F11). Наведи на край окна для управления");
  mute=Button("♫",()=>{muted=!muted;UpdateVolume();},"Выключить / включить звук");
  volumeLabel=Label("");volumeLabel.Width=38;volumeLabel.Margin=new Thickness(3,0,1,0);volumeLabel.VerticalAlignment=VerticalAlignment.Center;volumeLabel.FontSize=11;
  volumeSlider=new Slider{Minimum=0,Maximum=100,Value=CompanionBrowserAudio.Normalize(initialVolume),TickFrequency=1,IsSnapToTickEnabled=true,SmallChange=5,LargeChange=10,Width=82,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(4,0,3,0),IsMoveToPointEnabled=true};
  volumeSlider.SetResourceReference(FrameworkElement.StyleProperty,"BrowserVolumeSlider");volumeSlider.SetResourceReference(Control.ForegroundProperty,"Accent");System.Windows.Automation.AutomationProperties.SetName(volumeSlider,"Громкость браузера, процентов");
  volumeSlider.ValueChanged+=(s,e)=>{if(e.NewValue>0)muted=false;UpdateVolume();volumeSave.Stop();volumeSave.Start();};
  audioRefresh.Tick+=(s,e)=>ApplyVolume();volumeSave.Tick+=(s,e)=>SaveVolume();
  actions.Children.Add(pin);actions.Children.Add(mute);actions.Children.Add(volumeSlider);actions.Children.Add(volumeLabel);actions.Children.Add(cinema);actions.Children.Add(Button("−",()=>WindowState=WindowState.Minimized,"Свернуть"));actions.Children.Add(Button("×",Close,"Закрыть браузер и остановить видео"));DockPanel.SetDock(actions,Dock.Right);header.Children.Add(actions);
  var caption=Label("COMPANION / БРАУЗЕР");caption.TextTrimming=TextTrimming.CharacterEllipsis;caption.VerticalAlignment=VerticalAlignment.Center;caption.MouseLeftButtonDown+=(s,e)=>{if(e.ClickCount==2)WindowState=WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;else DragMove();};header.Children.Add(caption);layout.Children.Add(header);
  var navigation=new DockPanel{Margin=new Thickness(0,5,0,5)};
  var navigationButtons=new StackPanel{Orientation=Orientation.Horizontal};
  back=Button("‹",()=>{if(initialized&&view.CanGoBack)view.GoBack();},"Назад");forward=Button("›",()=>{if(initialized&&view.CanGoForward)view.GoForward();},"Вперёд");
  back.IsEnabled=false;forward.IsEnabled=false;navigationButtons.Children.Add(back);navigationButtons.Children.Add(forward);navigationButtons.Children.Add(Button("↻",()=>{if(initialized)view.Reload();else Initialize();},"Обновить / повторить запуск"));navigationButtons.Children.Add(Button("⌂",()=>Go(HomeUrl(HomeProvider)),"Стартовая страница"));DockPanel.SetDock(navigationButtons,Dock.Left);navigation.Children.Add(navigationButtons);
  var gameButton=Button("В ИГРУ",()=>{if(RequestGameMode!=null)RequestGameMode();},"Пропускать мышь в игру. Вернуть управление - горячими клавишами из «Дизайна»");gameButton.Width=65;gameButton.FontSize=10;navigationButtons.Children.Add(gameButton);
  var go=Button("→",()=>Go(address.Text),"Перейти");DockPanel.SetDock(go,Dock.Right);navigation.Children.Add(go);
  address=new TextBox{FontSize=13,Padding=new Thickness(8),Margin=new Thickness(4,0,4,0),VerticalContentAlignment=VerticalAlignment.Center,MaxLength=8192};
  address.SetResourceReference(Control.BackgroundProperty,"Field");address.SetResourceReference(Control.ForegroundProperty,"Text");address.SetResourceReference(Control.BorderBrushProperty,"Line");System.Windows.Automation.AutomationProperties.SetName(address,"Адрес или поисковый запрос");
  address.KeyDown+=(s,e)=>{if(e.Key==Key.Enter){Go(address.Text);e.Handled=true;}};navigation.Children.Add(address);
  bar=new Border{Child=navigation};Grid.SetRow(bar,1);layout.Children.Add(bar);
  view=new WebView2CompositionControl{DefaultBackgroundColor=System.Drawing.Color.FromArgb(255,23,23,28)};Grid.SetRow(view,2);layout.Children.Add(view);
  status=Label("Запускаем браузер…");status.TextWrapping=TextWrapping.Wrap;status.FontSize=11;status.Margin=new Thickness(5,5,5,2);Grid.SetRow(status,3);layout.Children.Add(status);
  hoverRefresh.Tick+=(s,e)=>UpdateHoverControls();
  UpdateVolume();Loaded+=(s,e)=>{Initialize();hoverRefresh.Start();};Closed+=(s,e)=>{disposed=true;audioRefresh.Stop();hoverRefresh.Stop();if(controlsWindow!=null)controlsWindow.Close();SaveVolume();view.Dispose();};
  Deactivated+=(s,e)=>ApplyBackgroundPlayback();
  PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Escape&&viewMode){SetViewMode(false);e.Handled=true;}else if(e.Key==Key.F11){SetViewMode(!viewMode);e.Handled=true;}else if(e.Key==Key.L&&Keyboard.Modifiers==ModifierKeys.Control){SetViewMode(false);address.Focus();address.SelectAll();e.Handled=true;}};
 }
 public bool OwnsWindow(IntPtr hwnd){return hwnd==new System.Windows.Interop.WindowInteropHelper(this).Handle||(controlsWindow!=null&&hwnd==new System.Windows.Interop.WindowInteropHelper(controlsWindow).Handle);}
 void EnsureFloatingControls(){
  if(controlsWindow!=null)return;
  if(floatingControls==null){floatingControls=new Grid();floatingControls.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});floatingControls.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});}
  if(new System.Windows.Interop.WindowInteropHelper(this).Handle==IntPtr.Zero)return;
  var border=new Border{Padding=new Thickness(5),CornerRadius=new CornerRadius(10),BorderThickness=new Thickness(1),Child=floatingControls};border.SetResourceReference(Border.BackgroundProperty,"DockBg");border.SetResourceReference(Border.BorderBrushProperty,"Line");
  controlsWindow=new Window{Title="Companion · Панель браузера",Owner=this,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=false,ShowActivated=false,ResizeMode=ResizeMode.NoResize,SizeToContent=SizeToContent.Height,Resources=Resources,Content=border};
  controlsWindow.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Escape||e.Key==Key.F11){SetViewMode(false);e.Handled=true;}};
 }
 void UpdateHoverControls(){
  CursorPoint cursor;if(GetCursorPos(out cursor))UpdateHoverControls(new Point(cursor.X,cursor.Y));
 }
 void UpdateHoverControls(Point screenPoint){
  if(!viewMode||disposed||!IsVisible||InputLocked||WindowState==WindowState.Minimized){if(controlsWindow!=null)controlsWindow.Hide();return;}
  Point here=PointFromScreen(screenPoint);bool inside=here.X>=0&&here.Y>=0&&here.X<ActualWidth&&here.Y<ActualHeight;
  bool atEdge=inside&&(here.X<9||here.Y<9||here.X>=ActualWidth-9||here.Y>=ActualHeight-9);
  bool overControls=false;if(controlsWindow!=null&&controlsWindow.IsVisible){Point controls=controlsWindow.PointFromScreen(screenPoint);overControls=controls.X>=0&&controls.Y>=0&&controls.X<controlsWindow.ActualWidth&&controls.Y<controlsWindow.ActualHeight;}
  if(atEdge||overControls){lastControlsHover=DateTime.UtcNow;EnsureFloatingControls();controlsWindow.Width=ActualWidth;controlsWindow.Left=Left;controlsWindow.Top=Top;controlsWindow.Topmost=Topmost;
   if(!controlsWindow.IsVisible){controlsWindow.Show();controlsWindow.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(180)));}
  }else if(controlsWindow!=null&&controlsWindow.IsVisible&&(DateTime.UtcNow-lastControlsHover).TotalMilliseconds>400){controlsWindow.Hide();}
 }
 [StructLayout(LayoutKind.Sequential)]struct CursorPoint{public int X,Y;}
 [DllImport("user32.dll")]static extern bool GetCursorPos(out CursorPoint point);
 async void ApplyBackgroundPlayback(){await ConfigureBackgroundPlaybackAsync();}
 async Task ConfigureBackgroundPlaybackAsync(){
  if(!initialized||disposed)return;
  try{
   var core=view.CoreWebView2;if(backgroundPlayback){if(core.IsSuspended)core.Resume();core.MemoryUsageTargetLevel=CoreWebView2MemoryUsageTargetLevel.Normal;}
   // Emulate focus only inside the web page. This never activates a Windows window.
   await core.CallDevToolsProtocolMethodAsync("Emulation.setFocusEmulationEnabled",backgroundPlayback?"{\"enabled\":true}":"{\"enabled\":false}");
  }catch(Exception error){if(!disposed)status.Text="Фоновый режим недоступен: "+error.Message+". Обычный просмотр продолжает работать.";}
 }
 void SaveVolume(){bool pendingSave=volumeSave.IsEnabled;volumeSave.Stop();if(pendingSave&&VolumeChanged!=null)VolumeChanged(Volume);}
 void UpdateVolume(){
  bool silent=muted||Volume==0;mute.Content=silent?"♪ ×":"♫";volumeLabel.Text=((int)Volume)+"%";volumeLabel.Opacity=silent?.5:1;volumeSlider.ToolTip="Громкость браузера: "+((int)Volume)+"%";
  if(initialized&&!disposed){view.CoreWebView2.IsMuted=silent;ApplyVolume();}
 }
 void ApplyVolume(){
  if(!initialized||disposed||browserEnvironment==null)return;
  try{CompanionBrowserAudio.Apply(browserEnvironment.GetProcessInfos().Select(info=>info.ProcessId),Volume);}
  catch(System.Runtime.InteropServices.COMException){volumeSlider.ToolTip="Аудиоустройство недоступно. Уровень применится, когда звук станет доступен.";}
  catch(InvalidOperationException){ /* The browser may be shutting down. */ }
 }
 TextBlock Label(string text){var label=new TextBlock{Text=text,FontSize=12,Margin=new Thickness(8,0,4,0)};label.SetResourceReference(TextBlock.ForegroundProperty,"Muted");return label;}
 Button Button(string label,Action action,string hint){var button=new Button{Content=label,ToolTip=hint,Width=35,Height=32,Margin=new Thickness(2),Padding=new Thickness(0),FontSize=19,Cursor=Cursors.Hand};button.SetResourceReference(Control.BackgroundProperty,"Field");button.SetResourceReference(Control.ForegroundProperty,"Text");button.SetResourceReference(Control.BorderBrushProperty,"Line");System.Windows.Automation.AutomationProperties.SetName(button,hint);button.Click+=(s,e)=>action();return button;}
 void SetViewMode(bool value){
  if(viewMode==value)return;viewMode=value;
  if(value){EnsureFloatingControls();layout.Children.Remove(titlebar);layout.Children.Remove(bar);floatingControls.Children.Add(titlebar);floatingControls.Children.Add(bar);windowFrame.Padding=new Thickness(0);windowFrame.BorderThickness=new Thickness(0);status.Visibility=Visibility.Collapsed;}
  else{if(controlsWindow!=null)controlsWindow.Hide();floatingControls.Children.Remove(titlebar);floatingControls.Children.Remove(bar);layout.Children.Add(titlebar);layout.Children.Add(bar);windowFrame.Padding=new Thickness(5);windowFrame.BorderThickness=new Thickness(1);status.Visibility=Visibility.Visible;}
  cinema.Content=value?"▤":"▣";
 }
 public void Go(string target){pending=ResolveAddress(target,HomeProvider);address.Text=pending;if(initialized){view.CoreWebView2.Navigate(pending);pending=null;}else Initialize();}
 async void Initialize(){
  if(initialized||initializing||disposed||!IsLoaded)return;initializing=true;
  try{
   Directory.CreateDirectory(userFolder);var environment=await CoreWebView2Environment.CreateAsync(null,userFolder);if(disposed)return;
   await view.EnsureCoreWebView2Async(environment);if(disposed)return;
   var core=view.CoreWebView2;core.Settings.AreHostObjectsAllowed=false;core.Settings.IsWebMessageEnabled=false;core.Settings.IsPasswordAutosaveEnabled=false;
   core.NavigationStarting+=(s,e)=>{Uri uri;if(!Uri.TryCreate(e.Uri,UriKind.Absolute,out uri)||(uri.Scheme!="http"&&uri.Scheme!="https")){e.Cancel=true;status.Text="Поддерживаются только веб-адреса HTTP и HTTPS.";return;}status.Text="Загрузка…";};
   core.SourceChanged+=(s,e)=>address.Text=core.Source;
   core.HistoryChanged+=(s,e)=>{back.IsEnabled=core.CanGoBack;forward.IsEnabled=core.CanGoForward;};
   core.NavigationCompleted+=(s,e)=>{status.Text=e.IsSuccess?"ПОВЕРХ ИГРЫ · F11 - РЕЖИМ ПРОСМОТРА · CTRL + L - АДРЕС":"Не удалось открыть страницу: "+e.WebErrorStatus+". Проверь интернет или адрес.";ApplyBackgroundPlayback();};
   core.NewWindowRequested+=(s,e)=>{e.Handled=true;if(e.IsUserInitiated)Dispatcher.BeginInvoke((Action)(()=>Go(e.Uri)));};
   core.ContainsFullScreenElementChanged+=(s,e)=>SetViewMode(core.ContainsFullScreenElement);
   core.ProcessFailed+=(s,e)=>status.Text="Процесс браузера остановлен. Закрой окно браузера и открой его снова.";
   core.DocumentTitleChanged+=(s,e)=>Title="Companion · "+core.DocumentTitle;
   browserEnvironment=environment;initialized=true;await ConfigureBackgroundPlaybackAsync();if(disposed)return;UpdateVolume();audioRefresh.Start();var target=pending??HomeUrl(HomeProvider);pending=null;address.Text=target;core.Navigate(target);
  }catch(WebView2RuntimeNotFoundException){status.Text="Нужен Microsoft Edge WebView2 Runtime. Установи его с https://developer.microsoft.com/microsoft-edge/webview2/ и снова открой браузер. Остальные разделы работают без него.";}
  catch(Exception error){if(!disposed)status.Text="Не удалось запустить браузер: "+error.Message+". Закрой это окно и попробуй снова.";}
  finally{initializing=false;}
 }
}
