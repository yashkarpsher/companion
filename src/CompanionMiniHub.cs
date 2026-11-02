using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using System.Windows.Markup;

public partial class Companion {
 StackPanel miniDashboard,miniActiveRows;TextBlock miniTime,miniDate,miniActiveHeading;Border miniCurrentCard;
 string miniActiveSignature="";readonly Dictionary<Countdown,TextBlock> miniActiveClocks=new Dictionary<Countdown,TextBlock>();
 Window miniPanel;Grid miniPanelHost;TextBlock miniPanelFeedback;UIElement miniPanelContent;Panel miniPanelOriginalParent;int miniPanelOriginalIndex;Visibility miniPanelOriginalVisibility;
 readonly Dictionary<UIElement,Visibility> miniHiddenElements=new Dictionary<UIElement,Visibility>();
 ContextMenu CreateMiniMenu(){
  var menu=new ContextMenu{MinWidth=190,MaxHeight=Math.Max(150,SystemParameters.WorkArea.Height-24)};Resource(menu,Control.BackgroundProperty,"DockBg");Resource(menu,Control.ForegroundProperty,"Text");Resource(menu,Control.BorderBrushProperty,"Line");menu.Template=(ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ContextMenu'><Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='1' CornerRadius='11' Padding='5'><ScrollViewer MaxHeight='{TemplateBinding MaxHeight}' VerticalScrollBarVisibility='Auto' HorizontalScrollBarVisibility='Disabled'><ItemsPresenter/></ScrollViewer></Border></ControlTemplate>");
  menu.Resources[typeof(MenuItem)]=(Style)XamlReader.Parse(@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='MenuItem'><Setter Property='Foreground' Value='{DynamicResource Text}'/><Setter Property='FontSize' Value='12'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='MenuItem'><Border x:Name='item' CornerRadius='7' Padding='10,7' Background='Transparent'><ContentPresenter ContentSource='Header'/></Border><ControlTemplate.Triggers><Trigger Property='IsHighlighted' Value='True'><Setter TargetName='item' Property='Background' Value='{DynamicResource Field}'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>");return menu;
 }
 void ApplyMiniTransparency(){var dim=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(0,1),Opacity=Math.Max(0,Math.Min(1,(data.Density-.2)/.8))};dim.GradientStops.Add(new GradientStop(Color.FromArgb(225,8,10,15),0));dim.GradientStops.Add(new GradientStop(Color.FromArgb(190,8,10,15),.5));dim.GradientStops.Add(new GradientStop(Color.FromArgb(150,8,10,15),1));dim.Freeze();Resources["MiniDim"]=dim;}
 void SoftenMiniText(UIElement element){element.Effect=new DropShadowEffect{Color=Colors.Black,BlurRadius=8,ShadowDepth=0,Opacity=.95,RenderingBias=RenderingBias.Performance};}
 void BuildMiniHub(){
  miniDashboard=new StackPanel{Width=252};var header=new DockPanel{Margin=new Thickness(0,0,0,6)};
  var more=MiniHubButton("⋯",OpenMiniMore,"Все инструменты");more.Width=27;DockPanel.SetDock(more,Dock.Right);header.Children.Add(more);
  miniDate=Text("",9,"Muted");miniDate.Margin=new Thickness(5,0,5,0);miniDate.HorizontalAlignment=HorizontalAlignment.Right;DockPanel.SetDock(miniDate,Dock.Right);header.Children.Add(miniDate);miniTime=Text("",21,"Accent");miniTime.Margin=new Thickness(0);SoftenMiniText(miniTime);SoftenMiniText(miniDate);header.Children.Add(miniTime);miniDashboard.Children.Add(header);
  var launch=new System.Windows.Controls.Primitives.UniformGrid{Columns=3,Margin=new Thickness(0,0,0,4)};
  AddMiniLaunch(launch,"Браузер","globe",()=>Safe(()=>OpenBrowser(null)));
  AddMiniLaunch(launch,"Таймер","clock",()=>OpenMiniSection(0));AddMiniLaunch(launch,"Дело","check",()=>OpenMiniSection(1));
  AddMiniLaunch(launch,"Заметка","notes",()=>OpenMiniSection(2));AddMiniLaunch(launch,"Цели","finance",()=>OpenMiniSection(3));AddMiniLaunch(launch,"Считать","calculator",()=>OpenMiniSection(4));miniDashboard.Children.Add(launch);
  var selected=Stack();selected.Children.Add(miniBody);selected.Children.Add(miniActions);var selectedScroll=new ScrollViewer{Content=selected,MaxHeight=83,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
  miniCurrentCard=new Border{Child=selectedScroll,Padding=new Thickness(7),Margin=new Thickness(0,0,0,5),CornerRadius=new CornerRadius(10),BorderThickness=new Thickness(1)};Resource(miniCurrentCard,Border.BackgroundProperty,"Panel");Resource(miniCurrentCard,Border.BorderBrushProperty,"Line");miniDashboard.Children.Add(miniCurrentCard);
  miniActiveHeading=Text("",9,"Muted");miniActiveHeading.Margin=new Thickness(0,1,0,4);miniDashboard.Children.Add(miniActiveHeading);
  miniActiveRows=Stack();miniDashboard.Children.Add(new ScrollViewer{Content=miniActiveRows,MaxHeight=74,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
  var backdrop=new Border{Padding=new Thickness(9),CornerRadius=new CornerRadius(7),Child=miniDashboard};Resource(backdrop,Border.BackgroundProperty,"MiniDim");miniViewport.Child=backdrop;
  Closed+=(s,e)=>CloseMiniPanel();IsVisibleChanged+=(s,e)=>{if(!IsVisible)CloseMiniPanel();};LocationChanged+=(s,e)=>{if(compact&&miniPanel!=null){var fit=PlaceMiniPanel(new Rect(base.Left,base.Top,Width,Height),CurrentWorkArea());miniPanel.Left=fit.Left;miniPanel.Top=fit.Top;}};
 }
 Button MiniHubButton(string text,Action action,string hint){var b=Btn(text,action);b.FontSize=10;b.Padding=new Thickness(5,4,5,4);b.MinHeight=25;b.Margin=new Thickness(1);b.ToolTip=hint;System.Windows.Automation.AutomationProperties.SetName(b,hint);return b;}
 void AddMiniLaunch(Panel panel,string label,string icon,Action action){var button=MiniHubButton("",action,"Открыть: "+label);var content=new StackPanel{Orientation=Orientation.Horizontal};content.Children.Add(DashboardIcon(icon,13));var caption=Text(label,10);caption.Margin=new Thickness(5,0,0,0);content.Children.Add(caption);button.Content=content;panel.Children.Add(button);}
 void TickMiniHub(){
  if(!compact||miniDashboard==null)return;var now=DateTime.Now;miniTime.Text=now.ToString("HH:mm:ss");miniDate.Text=now.ToString("dd MMM\ndddd",CultureInfo.GetCultureInfo("ru-RU"));
  var activeTimers=data.Timers.Where(t=>Left(t)>0).ToList();var activeTasks=data.Tasks.Where(t=>!t.Done).ToList();miniActiveHeading.Text="ТАЙМЕРЫ "+activeTimers.Count+"   ·   ДЕЛА "+activeTasks.Count+"   ·   ЗАМЕТКИ "+data.Notes.Count;
  string signature=String.Join("|",activeTimers.Take(30).Select(t=>t.Id+":"+t.Title+":"+(t.End==0)))+"/"+String.Join("|",activeTasks.Take(30).Select(t=>t.Id+":"+t.Title))+"/"+activeTimers.Count+"/"+activeTasks.Count;
  miniCurrentCard.Visibility=MiniItems().Count>0?Visibility.Visible:Visibility.Collapsed;
  if(signature!=miniActiveSignature){miniActiveSignature=signature;miniActiveRows.Children.Clear();miniActiveClocks.Clear();
   foreach(var timer in activeTimers.Take(30)){var item=timer;var row=new DockPanel{Margin=new Thickness(0,0,0,3)};var pause=MiniHubButton(timer.End==0?"▶":"Ⅱ",()=>WidgetAction(new CompanionWidgetState{Kind="timer",Id=item.Id}),timer.End==0?"Продолжить таймер":"Пауза");pause.Width=25;DockPanel.SetDock(pause,Dock.Right);row.Children.Add(pause);var clock=Text("",10,"Accent");SoftenMiniText(clock);clock.Margin=new Thickness(5,0,4,0);DockPanel.SetDock(clock,Dock.Right);row.Children.Add(clock);miniActiveClocks[timer]=clock;var title=MiniHubButton(timer.Title,()=>SelectMiniRecord("timer",item.Id),timer.Title);title.HorizontalContentAlignment=HorizontalAlignment.Left;title.Content=new TextBlock{Text=timer.Title,TextTrimming=TextTrimming.CharacterEllipsis,FontSize=10};row.Children.Add(title);miniActiveRows.Children.Add(row);}
   foreach(var task in activeTasks.Take(30)){var item=task;var row=new DockPanel{Margin=new Thickness(0,0,0,3)};var done=MiniHubButton("✓",()=>WidgetAction(new CompanionWidgetState{Kind="task",Id=item.Id}),"Выполнить дело");done.Width=25;DockPanel.SetDock(done,Dock.Right);row.Children.Add(done);var title=MiniHubButton(task.Title,()=>SelectMiniRecord("task",item.Id),task.Title);title.HorizontalContentAlignment=HorizontalAlignment.Left;title.Content=new TextBlock{Text=task.Title,TextTrimming=TextTrimming.CharacterEllipsis,FontSize=10};row.Children.Add(title);miniActiveRows.Children.Add(row);}
   if(activeTimers.Count+activeTasks.Count==0){var empty=Text("Нет активных записей. Добавь первую выше.",10,"Muted");empty.Margin=new Thickness(0,2,0,3);miniActiveRows.Children.Add(empty);}
   if(activeTimers.Count>30||activeTasks.Count>30)miniActiveRows.Children.Add(MiniHubButton("Все записи",()=>OpenMiniSection(activeTimers.Count>30?0:1),"Открыть полный список записей"));
   Dispatcher.BeginInvoke(DispatcherPriority.Loaded,(Action)(()=>{if(compact)SizeMiniToContent();}));
  }
  foreach(var pair in miniActiveClocks)pair.Value.Text=Clock(Left(pair.Key));
  foreach(Button action in miniActions.Children){action.FontSize=10;action.Padding=new Thickness(6,3,6,3);action.MinHeight=23;action.Margin=new Thickness(0,3,0,0);}
 }
 void SelectMiniRecord(string kind,string id){data.MiniKind=kind;data.MiniId=id;miniSignature="";Save();TickMini();}
 void OpenMiniMore(){var menu=CreateMiniMenu();
  AddMiniMenu(menu,"Полное меню",()=>SetCompact(false));AddMiniMenu(menu,"Браузер",()=>Safe(()=>OpenBrowser(null)));AddMiniMenu(menu,"Таймеры",()=>OpenMiniSection(0));AddMiniMenu(menu,"Дела",()=>OpenMiniSection(1));AddMiniMenu(menu,"Заметки",()=>OpenMiniSection(2));AddMiniMenu(menu,"Цели и финансы",()=>OpenMiniSection(3));AddMiniMenu(menu,"Калькулятор",()=>OpenMiniSection(4));AddMiniMenu(menu,"Настройки браузера",()=>OpenMiniSection(5));AddMiniMenu(menu,"Настройки",OpenMiniSettings);AddMiniMenu(menu,"О программе",()=>{var text=Text(ReadProgramReadme(),12);ShowMiniPanel(new ScrollViewer{Content=text,VerticalScrollBarVisibility=ScrollBarVisibility.Auto},"О программе");});AddMiniMenu(menu,"О создателе",()=>{var body=Stack();body.Children.Add(CreatorLink("TikTok","@rpsher_yasha",CreatorTikTokUrl));body.Children.Add(CreatorLink("Telegram","@yashkarp",CreatorTelegramUrl));body.Children.Add(CreatorLink("GitHub","Companion","https://github.com/"+CompanionUpdateCore.Repository));ShowMiniPanel(body,"О создателе");});AddMiniMenu(menu,"Выйти",RequestExit);menu.PlacementTarget=miniDate;menu.Placement=PlacementMode.Bottom;menu.IsOpen=true;}
 void OpenMiniSection(int section){
  if(!compact){OpenSectionDialog(section);return;}if(section<0||section>=tabs.Items.Count)return;
  CloseMiniPanel();tabs.SelectedIndex=section;ShowMiniPanel(tabs,new[]{"Таймеры","Дела","Заметки","Цели и финансы","Калькулятор","Браузер"}[section]);
  Dispatcher.BeginInvoke(DispatcherPriority.Loaded,(Action)(()=>{if(miniPanel==null)return;TextBox input=section==0?timerInput:section==1?taskInput:section==2?noteTitle:section==3?goalName:section==4?calculatorInput:null;if(input!=null){input.BringIntoView();input.Focus();}}));
 }
 void OpenMiniSettings(){if(!compact){ShowDesign();return;}ShowMiniPanel(settingsPage,"Настройки");}
 public static Rect PlaceMiniPanel(Rect widget,Rect area){double width=Math.Min(370,area.Width),height=Math.Min(510,area.Height);double x=widget.Right+8;if(x+width>area.Right)x=widget.Left-width-8;if(x<area.Left)x=Math.Max(area.Left,Math.Min(widget.Left,area.Right-width));double y=Math.Max(area.Top,Math.Min(widget.Top,area.Bottom-height));return new Rect(x,y,width,height);}
 void ShowMiniPanel(UIElement content,string title){
  CloseMiniPanel();if(gameInputLocked)SetGameInput(false);miniPanelContent=content;miniPanelOriginalParent=LogicalTreeHelper.GetParent(content) as Panel;miniPanelOriginalIndex=miniPanelOriginalParent==null?-1:miniPanelOriginalParent.Children.IndexOf(content);miniPanelOriginalVisibility=content.Visibility;
  if(miniPanelOriginalParent!=null)miniPanelOriginalParent.Children.Remove(content);content.Visibility=Visibility.Visible;
  if(content==tabs&&tabs.SelectedIndex==0){var page=((TabItem)tabs.Items[0]).Content as ScrollViewer;var body=page==null?null:page.Content as StackPanel;if(body!=null&&body.Children.Count>1)HideForMini(body.Children[1]);HideForMini(timers);if(workspaceTimerRight.Children.Count>1)HideForMini(workspaceTimerRight.Children[1]);}
  var window=new Window{Title="Companion · "+title,Owner=this,Resources=Resources,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,Topmost=data.Top,ShowInTaskbar=false,ResizeMode=ResizeMode.CanResizeWithGrip,MinWidth=280,MinHeight=260};miniPanel=window;
  var layout=new Grid{Margin=new Thickness(12)};layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});var header=new DockPanel{Margin=new Thickness(0,0,0,8)};var back=MiniHubButton("← Назад",CloseMiniPanel,"Закрыть панель и вернуться в мини-режим");DockPanel.SetDock(back,Dock.Right);header.Children.Add(back);var heading=Text(title,17);heading.Margin=new Thickness(0);header.Children.Add(heading);layout.Children.Add(header);
  miniPanelFeedback=Text("",11,"Accent");miniPanelFeedback.MaxHeight=45;miniPanelFeedback.Visibility=Visibility.Collapsed;Grid.SetRow(miniPanelFeedback,2);layout.Children.Add(miniPanelFeedback);
  miniPanelHost=new Grid();miniPanelHost.Children.Add(content);Grid.SetRow(miniPanelHost,1);layout.Children.Add(miniPanelHost);var border=new Border{CornerRadius=new CornerRadius(14),BorderThickness=new Thickness(1),Child=layout};Resource(border,Border.BackgroundProperty,"Bg");Resource(border,Border.BorderBrushProperty,"Line");window.Content=border;
  window.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Escape){CloseMiniPanel();e.Handled=true;}};window.PreviewMouseWheel+=ScrollMainWheel;CompanionWindowDrag.Attach(window,()=>gameInputLocked);
  window.Closed+=(s,e)=>{miniPanelHost.Children.Remove(content);foreach(var item in miniHiddenElements)item.Key.Visibility=item.Value;miniHiddenElements.Clear();if(miniPanelOriginalParent!=null&&!miniPanelOriginalParent.Children.Contains(content))miniPanelOriginalParent.Children.Insert(Math.Min(miniPanelOriginalIndex,miniPanelOriginalParent.Children.Count),content);content.Visibility=miniPanelOriginalVisibility;miniPanel=null;miniPanelContent=null;miniPanelHost=null;miniPanelFeedback=null;miniPanelOriginalParent=null;if(tabs.SelectedIndex>=0)sectionDialogTitle.Text=System.Windows.Automation.AutomationProperties.GetName(sectionButtons[tabs.SelectedIndex]);};
  var fit=PlaceMiniPanel(new Rect(base.Left,base.Top,Width,Height),CurrentWorkArea());window.Width=fit.Width;window.Height=fit.Height;window.Left=fit.Left;window.Top=fit.Top;window.Show();window.Activate();
 }
 void CloseMiniPanel(){if(miniPanel!=null)miniPanel.Close();}
 void HideForMini(UIElement element){if(miniHiddenElements.ContainsKey(element))return;miniHiddenElements[element]=element.Visibility;element.Visibility=Visibility.Collapsed;}
}
