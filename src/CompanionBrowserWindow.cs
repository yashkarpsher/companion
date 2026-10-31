using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

public partial class CompanionBrowserWindow : Window {
 sealed class BrowserPage {
  public WebView2CompositionControl View;public string Pending,Url,Title="Новая вкладка";public bool Ready,Loading,Closed,Succeeded;
  public Border Tab;public TextBlock Caption;
 }
 readonly List<BrowserPage> pages=new List<BrowserPage>();BrowserPage current;
 readonly string userFolder;
 readonly CompanionBrowserLibrary library;
 readonly Grid layout=new Grid(),pageHost=new Grid(),navigation=new Grid();
 readonly StackPanel tabStrip=new StackPanel{Orientation=Orientation.Horizontal};
 readonly Border tabViewport;readonly TranslateTransform tabShift=new TranslateTransform();double tabOffset;
 readonly DockPanel titlebar;
 readonly Border windowFrame,bar,pageFrame;
 readonly TextBox address;
 readonly TextBlock status,volumeLabel,brandText,footerHint;
 readonly Button back,forward,mute,pin,cinema,maximize,bookmarkButton,gameButton,menuButton;
 readonly Slider volumeSlider;
 readonly CompanionInputGate inputGate;
 readonly DispatcherTimer hoverRefresh=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(120)},audioRefresh=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)},volumeSave=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};
 readonly StackPanel navButtons,navActions;
 Window controlsWindow;Grid floatingControls;DateTime lastControlsHover;
 CoreWebView2Environment browserEnvironment;Task<CoreWebView2Environment> environmentTask;
 bool disposed,viewMode,muted,backgroundPlayback=true;
 public string HomeProvider {get;set;}
 public event Action RequestGameMode;public event Action<double> VolumeChanged;
 public double Volume {get{return volumeSlider.Value;}set{volumeSlider.Value=CompanionBrowserAudio.Normalize(value);}}
 public bool InputLocked {get{return inputGate.Locked;}set{if(value){if(controlsWindow!=null)controlsWindow.Hide();if(menuButton.ContextMenu!=null)menuButton.ContextMenu.IsOpen=false;CloseLibrary();}inputGate.Locked=value;}}
 public bool BackgroundPlayback {get{return backgroundPlayback;}set{backgroundPlayback=value;ApplyBackgroundPlayback();}}
 public static string HomeUrl(string provider){return provider=="yandex"?"https://ya.ru/":"https://www.google.com/";}
 public static string ResolveAddress(string text,string provider){
  text=(text??"").Trim();if(text.Length==0)return HomeUrl(provider);Uri uri;
  if(Uri.TryCreate(text,UriKind.Absolute,out uri)&&(uri.Scheme=="https"||uri.Scheme=="http")&&!String.IsNullOrEmpty(uri.Host))return uri.AbsoluteUri;
  if(text.IndexOf(":",StringComparison.Ordinal)>=0&&!text.Contains("://")&&!text.Contains(" ")&&text.IndexOf(':')<text.IndexOf('.'))return Search(text,provider);
  if(!text.Contains(" ")&&text.Contains(".")&&!text.Contains("://")&&Uri.TryCreate("https://"+text,UriKind.Absolute,out uri)&&!String.IsNullOrEmpty(uri.Host))return uri.AbsoluteUri;
  return Search(text,provider);
 }
 static string Search(string query,string provider){return(provider=="yandex"?"https://ya.ru/search/?text=":"https://www.google.com/search?q=")+Uri.EscapeDataString(query);}
 public CompanionBrowserWindow(ResourceDictionary theme,string folder,string provider,double initialVolume=100){
  Resources=theme;userFolder=folder;HomeProvider=provider;library=new CompanionBrowserLibrary(folder);Title="Companion · Браузер";inputGate=new CompanionInputGate(this);
  Width=Math.Min(1100,SystemParameters.WorkArea.Width-32);Height=Math.Min(760,SystemParameters.WorkArea.Height-32);MinWidth=380;MinHeight=300;Topmost=true;WindowStartupLocation=WindowStartupLocation.CenterScreen;WindowStyle=WindowStyle.None;AllowsTransparency=true;ResizeMode=ResizeMode.CanResizeWithGrip;
  WindowChrome.SetWindowChrome(this,new WindowChrome{CaptionHeight=0,ResizeBorderThickness=new Thickness(6),CornerRadius=new CornerRadius(18),GlassFrameThickness=new Thickness(0)});
  Background=Brushes.Transparent;FontFamily=new FontFamily("Segoe UI");InstallChromeStyle();
  windowFrame=new Border{CornerRadius=new CornerRadius(20),BorderThickness=new Thickness(1)};windowFrame.SetResourceReference(Border.BackgroundProperty,"DockBg");windowFrame.SetResourceReference(Border.BorderBrushProperty,"Line");windowFrame.Child=layout;Content=windowFrame;
  for(int i=0;i<4;i++)layout.RowDefinitions.Add(new RowDefinition{Height=i==2?new GridLength(1,GridUnitType.Star):GridLength.Auto});
  titlebar=new DockPanel{LastChildFill=true,Margin=new Thickness(12,8,10,4),MinHeight=44};
  var brand=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(2,0,12,0)};
  var logo=new Ellipse{Width=23,Height=23,StrokeThickness=3,Margin=new Thickness(2,0,10,0)};logo.SetResourceReference(Shape.StrokeProperty,"Accent");brand.Children.Add(logo);brandText=Text("COMPANION",14,"Text");brandText.FontWeight=FontWeights.Bold;brand.Children.Add(brandText);DockPanel.SetDock(brand,Dock.Left);titlebar.Children.Add(brand);
  var actions=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(actions,Dock.Right);
  mute=IconButton("sound",()=>{muted=!muted;UpdateVolume();},"Выключить / включить звук");actions.Children.Add(mute);
  volumeSlider=new Slider{Minimum=0,Maximum=100,Value=CompanionBrowserAudio.Normalize(initialVolume),TickFrequency=1,IsSnapToTickEnabled=true,SmallChange=5,LargeChange=10,Width=82,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(6,0,3,0),IsMoveToPointEnabled=true};volumeSlider.SetResourceReference(FrameworkElement.StyleProperty,"BrowserVolumeSlider");System.Windows.Automation.AutomationProperties.SetName(volumeSlider,"Громкость браузера, процентов");actions.Children.Add(volumeSlider);
  volumeLabel=Text("",10,"Muted");volumeLabel.Width=34;volumeLabel.Margin=new Thickness(2,0,8,0);actions.Children.Add(volumeLabel);
  pin=IconButton("pin",()=>{Topmost=!Topmost;UpdatePin();},"Закрепить поверх окон");actions.Children.Add(pin);
  cinema=IconButton("cinema",()=>SetViewMode(!viewMode),"Режим просмотра (F11). Наведи на край окна, чтобы вернуть кнопки");actions.Children.Add(cinema);
  actions.Children.Add(IconButton("minimize",()=>WindowState=WindowState.Minimized,"Свернуть браузер"));maximize=IconButton("maximize",ToggleMaximize,"Развернуть / восстановить окно");actions.Children.Add(maximize);actions.Children.Add(IconButton("close",Close,"Закрыть браузер и остановить видео"));titlebar.Children.Add(actions);
  var addPage=IconButton("add",()=>AddPage(null),"Новая вкладка (Ctrl+T)");DockPanel.SetDock(addPage,Dock.Right);titlebar.Children.Add(addPage);
  tabStrip.RenderTransform=tabShift;var tabCanvas=new Canvas{Height=44};tabCanvas.Children.Add(tabStrip);tabViewport=new Border{Child=tabCanvas,ClipToBounds=true,Background=Brushes.Transparent,VerticalAlignment=VerticalAlignment.Center};tabViewport.PreviewMouseWheel+=(s,e)=>{MoveTabs(tabOffset-e.Delta);e.Handled=true;};tabViewport.SizeChanged+=(s,e)=>ScrollCurrentTab();titlebar.Children.Add(tabViewport);titlebar.MouseLeftButtonDown+=DragHeader;layout.Children.Add(titlebar);
  navigation.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});navigation.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});navigation.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});navigation.ColumnDefinitions.Add(new ColumnDefinition());navigation.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
  navButtons=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};back=IconButton("back",()=>{if(current.Ready&&current.View.CanGoBack)current.View.GoBack();},"Назад");forward=IconButton("forward",()=>{if(current.Ready&&current.View.CanGoForward)current.View.GoForward();},"Вперёд");navButtons.Children.Add(back);navButtons.Children.Add(forward);navButtons.Children.Add(IconButton("reload",()=>{if(current.Ready)current.View.Reload();else Initialize(current);},"Обновить / повторить запуск"));navigation.Children.Add(navButtons);
  navActions=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};gameButton=TextButton("В игру ↗",()=>{CloseLibrary();if(RequestGameMode!=null)RequestGameMode();},"Передать мышь в игру. Вернуть управление можно своей горячей клавишей");gameButton.MinWidth=100;gameButton.SetResourceReference(Control.BackgroundProperty,"AccentFill");gameButton.SetResourceReference(Control.ForegroundProperty,"AccentText");navActions.Children.Add(gameButton);menuButton=IconButton("menu",OpenMenu,"Меню браузера: история, закладки и сайты");navActions.Children.Add(menuButton);navigation.Children.Add(navActions);
  var addressFrame=new Border{CornerRadius=new CornerRadius(23),BorderThickness=new Thickness(1),Padding=new Thickness(10,2,5,2),Margin=new Thickness(8,0,8,0)};addressFrame.SetResourceReference(Border.BackgroundProperty,"Field");addressFrame.SetResourceReference(Border.BorderBrushProperty,"Line");
  var addressDock=new DockPanel{LastChildFill=true};bookmarkButton=IconButton("star",ToggleBookmark,"Добавить страницу в закладки (Ctrl+D)");DockPanel.SetDock(bookmarkButton,Dock.Right);addressDock.Children.Add(bookmarkButton);var globe=ChromeIcon("globe");globe.Margin=new Thickness(3,0,8,0);DockPanel.SetDock(globe,Dock.Left);addressDock.Children.Add(globe);
  address=new TextBox{FontSize=14,Padding=new Thickness(3,7,3,7),Background=Brushes.Transparent,BorderThickness=new Thickness(0),VerticalContentAlignment=VerticalAlignment.Center,MaxLength=8192,Tag="Адрес сайта или поиск"};address.SetResourceReference(Control.ForegroundProperty,"Text");System.Windows.Automation.AutomationProperties.SetName(address,"Адрес или поисковый запрос");address.KeyDown+=(s,e)=>{if(e.Key==Key.Enter){Go(address.Text);e.Handled=true;}};addressDock.Children.Add(address);addressFrame.Child=addressDock;Grid.SetColumn(addressFrame,1);navigation.Children.Add(addressFrame);
  bar=new Border{Child=navigation,Margin=new Thickness(10,4,10,10)};bar.Tag=addressFrame;Grid.SetRow(bar,1);layout.Children.Add(bar);
  pageFrame=new Border{CornerRadius=new CornerRadius(16),BorderThickness=new Thickness(1),Margin=new Thickness(8,0,8,0),Child=pageHost};pageFrame.SetResourceReference(Border.BackgroundProperty,"Bg");pageFrame.SetResourceReference(Border.BorderBrushProperty,"Line");pageHost.SizeChanged+=(s,e)=>{pageHost.Clip=new RectangleGeometry(new Rect(0,0,Math.Max(0,pageHost.ActualWidth),Math.Max(0,pageHost.ActualHeight)),viewMode?0:15,viewMode?0:15);if(libraryPanel!=null)libraryPanel.MaxWidth=Math.Max(1,pageHost.ActualWidth-16);};Grid.SetRow(pageFrame,2);layout.Children.Add(pageFrame);
  var footer=new DockPanel{Margin=new Thickness(16,10,16,10)};footerHint=Text("F11 - просмотр    ·    Ctrl+L - адрес",10,"Muted");DockPanel.SetDock(footerHint,Dock.Right);footer.Children.Add(footerHint);status=Text("Запускаем браузер…",11,"Muted");status.TextTrimming=TextTrimming.CharacterEllipsis;footer.Children.Add(status);footer.MouseLeftButtonDown+=DragHeader;Grid.SetRow(footer,3);layout.Children.Add(footer);
  AddPage(null);UpdatePin();UpdateVolume();UpdateLayoutSize();SizeChanged+=(s,e)=>UpdateLayoutSize();
  volumeSlider.ValueChanged+=(s,e)=>{if(e.NewValue>0)muted=false;UpdateVolume();volumeSave.Stop();volumeSave.Start();};audioRefresh.Tick+=(s,e)=>ApplyVolume();volumeSave.Tick+=(s,e)=>SaveVolume();hoverRefresh.Tick+=(s,e)=>UpdateHoverControls();
  Loaded+=(s,e)=>{foreach(var page in pages.ToArray())Initialize(page);hoverRefresh.Start();if(library.Warning!=null)status.Text=library.Warning;};
  Closed+=(s,e)=>{disposed=true;audioRefresh.Stop();hoverRefresh.Stop();if(controlsWindow!=null)controlsWindow.Close();SaveVolume();foreach(var page in pages){page.Closed=true;page.View.Dispose();}pages.Clear();};Deactivated+=(s,e)=>ApplyBackgroundPlayback();PreviewKeyDown+=HandleKeys;
 }
 void UpdateLayoutSize(){
  double width=ActualWidth>0?ActualWidth:Width;brandText.Visibility=width<1000?Visibility.Collapsed:Visibility.Visible;volumeLabel.Visibility=width<850?Visibility.Collapsed:Visibility.Visible;mute.Visibility=width<560?Visibility.Collapsed:Visibility.Visible;pin.Visibility=cinema.Visibility=maximize.Visibility=width<560?Visibility.Collapsed:Visibility.Visible;volumeSlider.Width=width<560?55:82;footerHint.Visibility=width<650?Visibility.Collapsed:Visibility.Visible;
  var frame=(Border)bar.Tag;bool small=width<640;Grid.SetRow(frame,small?1:0);Grid.SetColumn(frame,small?0:1);Grid.SetColumnSpan(frame,small?3:1);frame.Margin=new Thickness(small?0:8,small?6:0,small?0:8,0);Grid.SetColumn(navActions,2);Grid.SetRow(navActions,0);navActions.HorizontalAlignment=HorizontalAlignment.Right;
  foreach(var page in pages)page.Tab.Width=width<440?90:width<560?105:width<850?135:180;
  if(libraryPanel!=null)libraryPanel.MaxWidth=Math.Max(1,pageHost.ActualWidth-16);
 }
 void DragHeader(object sender,MouseButtonEventArgs e){if(e.Handled||e.ChangedButton!=MouseButton.Left)return;DependencyObject node=e.OriginalSource as DependencyObject;while(node!=null&&node!=sender){if(node is ButtonBase||node is TextBox||node is Slider||node is ScrollBar||node is Thumb)return;node=node is Visual?VisualTreeHelper.GetParent(node):LogicalTreeHelper.GetParent(node);}if(e.ClickCount==2)ToggleMaximize();else try{DragMove();}catch(InvalidOperationException){} }
 void ToggleMaximize(){WindowState=WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;}
 void UpdatePin(){pin.Opacity=Topmost?1:.5;pin.ToolTip=BrowserHint(Topmost?"Открепить окно":"Поверх остальных окон");if(status!=null&&(current==null||!current.Loading))status.Text=Topmost?"Поверх игры":"Браузер";}
 void HandleKeys(object sender,KeyEventArgs e){
  bool ctrl=(Keyboard.Modifiers&ModifierKeys.Control)!=0,shift=(Keyboard.Modifiers&ModifierKeys.Shift)!=0;
  if(e.Key==Key.Escape&&libraryLayer!=null){CloseLibrary();e.Handled=true;}
  else if(e.Key==Key.Escape&&viewMode){SetViewMode(false);e.Handled=true;}
  else if(e.Key==Key.F11){SetViewMode(!viewMode);e.Handled=true;}
  else if(ctrl&&e.Key==Key.L){SetViewMode(false);CloseLibrary();address.Focus();address.SelectAll();e.Handled=true;}
  else if(ctrl&&e.Key==Key.H){ShowLibrary(true);e.Handled=true;}
  else if(ctrl&&shift&&e.Key==Key.O){ShowLibrary(false);e.Handled=true;}
  else if(ctrl&&e.Key==Key.D){ToggleBookmark();e.Handled=true;}
  else if(ctrl&&e.Key==Key.T){AddPage(null);e.Handled=true;}
  else if(ctrl&&e.Key==Key.W){ClosePage(current);e.Handled=true;}
  else if(ctrl&&e.Key==Key.Tab&&pages.Count>1){int at=pages.IndexOf(current);SelectPage(pages[(at+(shift?pages.Count-1:1))%pages.Count]);e.Handled=true;}
 }
 void AddPage(string destination){
  if(pages.Count>=12){status.Text="Открыто 12 вкладок. Закрой ненужную перед добавлением новой.";return;}
  var page=new BrowserPage{Pending=ResolveAddress(destination,HomeProvider)};page.Url=page.Pending;page.Title=PageTitle("",page.Url);
  page.View=new WebView2CompositionControl{DefaultBackgroundColor=System.Drawing.Color.FromArgb(255,22,24,28),Visibility=Visibility.Collapsed};
  if(libraryLayer==null)pageHost.Children.Add(page.View);else pageHost.Children.Insert(0,page.View);
  var tab=new Border{Width=180,Height=42,CornerRadius=new CornerRadius(13,13,4,4),BorderThickness=new Thickness(1),Margin=new Thickness(2,0,2,0),Padding=new Thickness(9,0,4,0),Cursor=Cursors.Hand};page.Tab=tab;
  var content=new DockPanel{LastChildFill=true};var close=IconButton("close",()=>ClosePage(page),"Закрыть вкладку");close.Width=24;close.Height=26;DockPanel.SetDock(close,Dock.Right);content.Children.Add(close);page.Caption=Text(page.Title,12,"Text");page.Caption.TextTrimming=TextTrimming.CharacterEllipsis;content.Children.Add(page.Caption);tab.Child=content;tab.MouseLeftButtonDown+=(s,e)=>{if(e.OriginalSource is Border||e.OriginalSource is TextBlock){SelectPage(page);e.Handled=true;}};
  pages.Add(page);tabStrip.Children.Add(tab);SelectPage(page);UpdateLayoutSize();if(IsLoaded)Initialize(page);
 }
 void SelectPage(BrowserPage page){
  if(page==null||page.Closed)return;CloseLibrary();current=page;
  foreach(var item in pages){item.View.Visibility=item==page?Visibility.Visible:Visibility.Collapsed;item.Tab.SetResourceReference(Border.BackgroundProperty,item==page?"SelectedTab":"NavBg");item.Tab.SetResourceReference(Border.BorderBrushProperty,item==page?"Line":"NavBg");}
  RefreshPage();page.Tab.BringIntoView();ScrollCurrentTab();
 }
 void MoveTabs(double offset){if(tabViewport==null)return;tabOffset=Math.Max(0,Math.Min(offset,Math.Max(0,pages.Sum(x=>x.Tab.Width+4)-tabViewport.ActualWidth)));tabShift.X=-tabOffset;}
 void ScrollCurrentTab(){if(current==null||tabViewport==null||tabViewport.ActualWidth<=0)return;double left=pages.TakeWhile(x=>x!=current).Sum(x=>x.Tab.Width+4),right=left+current.Tab.Width+4,offset=tabOffset;if(left<offset)offset=left;else if(right>offset+tabViewport.ActualWidth)offset=Math.Max(0,right-tabViewport.ActualWidth);MoveTabs(offset);}
 void ClosePage(BrowserPage page){if(page==null||page.Closed)return;if(pages.Count==1){Close();return;}int at=pages.IndexOf(page);page.Closed=true;pages.Remove(page);tabStrip.Children.Remove(page.Tab);pageHost.Children.Remove(page.View);page.View.Dispose();if(current==page)SelectPage(pages[Math.Min(at,pages.Count-1)]);else ScrollCurrentTab();}
 void RefreshPage(){if(current==null)return;address.Text=current.Url??current.Pending??"";back.IsEnabled=current.Ready&&current.View.CanGoBack;forward.IsEnabled=current.Ready&&current.View.CanGoForward;Title="Companion · "+current.Title;UpdateBookmark();status.Text=current.Loading?"Загрузка…":Topmost?"Поверх игры":"Браузер";}
 public void Go(string target){if(current==null)return;CloseLibrary();current.Pending=ResolveAddress(target,HomeProvider);current.Url=current.Pending;current.Succeeded=false;current.Title=PageTitle("",current.Url);current.Caption.Text=current.Title;address.Text=current.Pending;UpdateBookmark();if(current.Ready){current.View.CoreWebView2.Navigate(current.Pending);current.Pending=null;}else Initialize(current);}
 async void Initialize(BrowserPage page){
  if(page.Ready||page.Loading||page.Closed||disposed||!IsLoaded)return;page.Loading=true;if(page==current)status.Text="Запускаем браузер…";
  try{
   Directory.CreateDirectory(userFolder);if(environmentTask==null)environmentTask=CoreWebView2Environment.CreateAsync(null,userFolder);var environment=await environmentTask;if(disposed||page.Closed)return;
   await page.View.EnsureCoreWebView2Async(environment);if(disposed||page.Closed)return;var core=page.View.CoreWebView2;core.Settings.AreHostObjectsAllowed=false;core.Settings.IsWebMessageEnabled=false;core.Settings.IsPasswordAutosaveEnabled=false;
   core.NavigationStarting+=(s,e)=>{if(page.Closed)return;Uri uri;if(!Uri.TryCreate(e.Uri,UriKind.Absolute,out uri)||(uri.Scheme!="http"&&uri.Scheme!="https")){e.Cancel=true;if(page==current)status.Text="Поддерживаются только веб-адреса HTTP и HTTPS.";return;}page.Loading=true;page.Succeeded=false;if(page==current)status.Text="Загрузка…";};
   core.SourceChanged+=(s,e)=>{if(page.Closed)return;page.Url=core.Source;if(page==current){address.Text=page.Url;UpdateBookmark();}if(!e.IsNewDocument&&page.Succeeded)LibraryAction(()=>library.Visit(page.Url,core.DocumentTitle));};
   core.HistoryChanged+=(s,e)=>{if(page==current&&!page.Closed){back.IsEnabled=core.CanGoBack;forward.IsEnabled=core.CanGoForward;}};
   core.NavigationCompleted+=async(s,e)=>{if(page.Closed)return;page.Loading=false;page.Succeeded=e.IsSuccess;if(page==current){RefreshPage();if(!e.IsSuccess)status.Text="Не удалось открыть страницу: "+e.WebErrorStatus;}if(e.IsSuccess){page.Url=core.Source;page.Title=PageTitle(core.DocumentTitle,page.Url);page.Caption.Text=page.Title;LibraryAction(()=>library.Visit(page.Url,page.Title));if(!library.Data.RecordHistory)await ClearEngineHistory();}ApplyBackgroundPlayback();};
   core.DocumentTitleChanged+=(s,e)=>{if(page.Closed)return;page.Title=PageTitle(core.DocumentTitle,core.Source);page.Caption.Text=page.Title;page.Tab.ToolTip=BrowserHint(page.Title);if(page==current)Title="Companion · "+page.Title;if(page.Succeeded)LibraryAction(()=>library.UpdateTitle(page.Url,page.Title));};
   core.NewWindowRequested+=(s,e)=>{e.Handled=true;if(e.IsUserInitiated)Dispatcher.BeginInvoke((Action)(()=>{if(!disposed)AddPage(e.Uri);}));};
   core.ContainsFullScreenElementChanged+=(s,e)=>{if(page==current)SetViewMode(core.ContainsFullScreenElement);};core.ProcessFailed+=(s,e)=>{if(page==current)status.Text="Процесс браузера остановлен. Закрой вкладку и открой её снова.";};
   browserEnvironment=environment;page.Ready=true;if(library.Data.EngineHistoryClearPending||!library.Data.RecordHistory)await ClearEngineHistory();await ConfigureBackgroundPlaybackAsync(page);if(disposed||page.Closed)return;UpdateVolume();audioRefresh.Start();string target=page.Pending??HomeUrl(HomeProvider);page.Pending=null;page.Url=target;core.Navigate(target);
  }catch(WebView2RuntimeNotFoundException){if(!disposed)status.Text="Нужен Microsoft Edge WebView2 Runtime. Установи его с сайта Microsoft и открой браузер снова.";environmentTask=null;}
  catch(Exception error){if(!disposed&&!page.Closed)status.Text="Не удалось запустить браузер: "+error.Message;environmentTask=null;}
  finally{if(!page.Ready)page.Loading=false;}
 }
 static string PageTitle(string text,string url){if(!String.IsNullOrWhiteSpace(text))return text.Length>300?text.Substring(0,300):text;Uri uri;return Uri.TryCreate(url,UriKind.Absolute,out uri)?uri.Host:"Новая вкладка";}
 async void ApplyBackgroundPlayback(){foreach(var page in pages.ToArray())await ConfigureBackgroundPlaybackAsync(page);}
 async Task ClearEngineHistory(){var page=pages.FirstOrDefault(x=>x.Ready&&!x.Closed);if(page==null)return;try{await page.View.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.BrowsingHistory);library.EngineHistoryCleared();}catch{if(!disposed)status.Text="Не удалось полностью очистить историю браузера. Попробуй снова.";}}
 async Task ConfigureBackgroundPlaybackAsync(BrowserPage page){if(!page.Ready||disposed||page.Closed)return;try{var core=page.View.CoreWebView2;if(backgroundPlayback){if(core.IsSuspended)core.Resume();core.MemoryUsageTargetLevel=CoreWebView2MemoryUsageTargetLevel.Normal;}await core.CallDevToolsProtocolMethodAsync("Emulation.setFocusEmulationEnabled",backgroundPlayback?"{\"enabled\":true}":"{\"enabled\":false}");}catch{if(!disposed)status.Text="Фоновый режим недоступен. Обычный просмотр продолжает работать.";}}
 void SaveVolume(){bool save=volumeSave.IsEnabled;volumeSave.Stop();if(save&&VolumeChanged!=null)VolumeChanged(Volume);}
 void UpdateVolume(){bool silent=muted||Volume==0;mute.Content=ChromeIcon(silent?"silent":"sound");volumeLabel.Text=((int)Volume)+"%";volumeSlider.ToolTip=BrowserHint("Громкость браузера: "+((int)Volume)+"%");foreach(var page in pages)if(page.Ready&&!page.Closed)page.View.CoreWebView2.IsMuted=silent;ApplyVolume();}
 void ApplyVolume(){if(disposed||browserEnvironment==null)return;try{CompanionBrowserAudio.Apply(browserEnvironment.GetProcessInfos().Select(info=>info.ProcessId),Volume);}catch(COMException){}catch(InvalidOperationException){}}
 public bool OwnsWindow(IntPtr hwnd){return hwnd==new System.Windows.Interop.WindowInteropHelper(this).Handle||(controlsWindow!=null&&hwnd==new System.Windows.Interop.WindowInteropHelper(controlsWindow).Handle);}
 void EnsureFloatingControls(){
  if(controlsWindow!=null)return;if(floatingControls==null){floatingControls=new Grid();floatingControls.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});floatingControls.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});}if(new System.Windows.Interop.WindowInteropHelper(this).Handle==IntPtr.Zero)return;
  var border=new Border{Padding=new Thickness(3),CornerRadius=new CornerRadius(14),BorderThickness=new Thickness(1),Child=floatingControls};border.SetResourceReference(Border.BackgroundProperty,"DockBg");border.SetResourceReference(Border.BorderBrushProperty,"Line");controlsWindow=new Window{Title="Companion · Панель браузера",Owner=this,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=false,ShowActivated=false,ResizeMode=ResizeMode.NoResize,SizeToContent=SizeToContent.Height,Resources=Resources,Content=border};controlsWindow.PreviewKeyDown+=HandleKeys;
 }
 void UpdateHoverControls(){CursorPoint cursor;if(GetCursorPos(out cursor))UpdateHoverControls(new Point(cursor.X,cursor.Y));}
 void UpdateHoverControls(Point screen){
  if(!viewMode||disposed||!IsVisible||InputLocked||WindowState==WindowState.Minimized){if(controlsWindow!=null)controlsWindow.Hide();return;}Point here=PointFromScreen(screen);bool inside=here.X>=0&&here.Y>=0&&here.X<ActualWidth&&here.Y<ActualHeight;bool edge=inside&&(here.X<10||here.Y<10||here.X>=ActualWidth-10||here.Y>=ActualHeight-10);bool over=false;
  if(controlsWindow!=null&&controlsWindow.IsVisible){Point c=controlsWindow.PointFromScreen(screen);over=c.X>=0&&c.Y>=0&&c.X<controlsWindow.ActualWidth&&c.Y<controlsWindow.ActualHeight;}
  if(edge||over){lastControlsHover=DateTime.UtcNow;EnsureFloatingControls();if(controlsWindow==null)return;controlsWindow.Width=ActualWidth;controlsWindow.Left=Left;controlsWindow.Top=Top;controlsWindow.Topmost=Topmost;if(!controlsWindow.IsVisible){controlsWindow.Show();controlsWindow.BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(160)));}}
  else if(controlsWindow!=null&&controlsWindow.IsVisible&&(DateTime.UtcNow-lastControlsHover).TotalMilliseconds>900)controlsWindow.Hide();
 }
 void SetViewMode(bool value){
  if(viewMode==value)return;CloseLibrary();viewMode=value;pageFrame.Margin=value?new Thickness(0):new Thickness(8,0,8,0);pageFrame.BorderThickness=new Thickness(value?0:1);pageFrame.CornerRadius=new CornerRadius(value?0:16);pageHost.Clip=new RectangleGeometry(new Rect(0,0,Math.Max(0,pageHost.ActualWidth),Math.Max(0,pageHost.ActualHeight)),value?0:15,value?0:15);
  if(value){EnsureFloatingControls();layout.Children.Remove(titlebar);layout.Children.Remove(bar);floatingControls.Children.Add(titlebar);floatingControls.Children.Add(bar);windowFrame.BorderThickness=new Thickness(0);layout.Children.OfType<DockPanel>().Where(x=>x!=titlebar).ToList().ForEach(x=>x.Visibility=Visibility.Collapsed);}
  else{if(controlsWindow!=null)controlsWindow.Hide();floatingControls.Children.Remove(titlebar);floatingControls.Children.Remove(bar);layout.Children.Add(titlebar);layout.Children.Add(bar);windowFrame.BorderThickness=new Thickness(1);layout.Children.OfType<DockPanel>().Where(x=>x!=titlebar).ToList().ForEach(x=>x.Visibility=Visibility.Visible);}
 }
 [StructLayout(LayoutKind.Sequential)]struct CursorPoint{public int X,Y;}
 [DllImport("user32.dll")]static extern bool GetCursorPos(out CursorPoint point);
}
