using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

public class CompanionWidgetState {
 public string Kind {get;set;} public string Id {get;set;}
 public double X {get;set;} public double Y {get;set;} public double Width {get;set;} public double Height {get;set;}
}
public partial class Companion {
 readonly List<CompanionWidget> widgets=new List<CompanionWidget>();
 Window quickNoteWindow;TextBox quickNoteText;bool quickNoteReturnToGame,widgetShutdown;
 CompanionHotkey quickBinding;TextBox quickKeyEditor;
 CompanionHotkey QuickBinding {get{return quickBinding??CompanionHotkey.OrDefault(data.QuickNoteHotkey,"Ctrl+Shift+N");}}
 void InitializeWidgets(){
  bool restored=false;Loaded+=(s,e)=>{if(restored)return;restored=true;RestoreWidgets();};
 }
 void RestoreWidgets(){
  widgetShutdown=true;foreach(var widget in widgets.ToArray())widget.Close();widgetShutdown=false;
  data.Widgets=(data.Widgets??new List<CompanionWidgetState>()).Where(w=>w!=null&&WidgetExists(w.Kind,w.Id)).GroupBy(w=>w.Kind+":"+w.Id).Select(g=>g.First()).Take(24).ToList();foreach(var state in data.Widgets.ToArray())CreateWidget(state);
 }
 bool WidgetExists(string kind,string id){return kind=="timer"?data.Timers.Any(t=>t.Id==id):kind=="task"?data.Tasks.Any(t=>t.Id==id):kind=="note"&&data.Notes.Any(t=>t.Id==id);}
 public static Rect FindWidgetSlot(Rect work,IEnumerable<Rect> occupied,Size preferred){
  if(work.IsEmpty||work.Width<=0||work.Height<=0)return Rect.Empty;
  const double gap=12;double margin=Math.Min(24,Math.Min(work.Width,work.Height)/10);var area=new Rect(work.Left+margin,work.Top+margin,work.Width-2*margin,work.Height-2*margin);
  double width=Math.Min(preferred.Width,area.Width),height=Math.Min(preferred.Height,area.Height);var others=occupied.Where(r=>!r.IsEmpty&&r.IntersectsWith(work)).ToList();
  for(double x=area.Left;x+width<=area.Right+.01;){
   double y=area.Top;
   while(y+height<=area.Bottom+.01){var candidate=new Rect(x,y,width,height);var hit=others.Where(r=>candidate.Right+gap>r.Left&&candidate.Left<r.Right+gap&&candidate.Bottom+gap>r.Top&&candidate.Top<r.Bottom+gap).ToList();if(hit.Count==0)return candidate;y=hit.Max(r=>r.Bottom)+gap;}
   x=others.Where(r=>r.Right+gap>x+.01&&r.Left<x+width+gap).Select(r=>r.Right+gap).DefaultIfEmpty(x+width+gap).Max();
  }return Rect.Empty;
 }
 void ArrangeWidgets(){
  var placed=new List<Rect>();var work=CurrentWorkArea();foreach(var widget in widgets){var slot=FindWidgetSlot(work,placed,new Size(widget.Width,widget.Height));if(slot.IsEmpty){Say("Для всех виджетов не хватает места. Уменьши их размер и повтори.");return;}placed.Add(slot);}
  for(int i=0;i<widgets.Count;i++){widgets[i].Left=placed[i].Left;widgets[i].Top=placed[i].Top;widgets[i].Width=placed[i].Width;widgets[i].Height=placed[i].Height;widgets[i].CapturePlacement();}Save();Say("Виджеты выстроены колонками.");
 }
 void AddWidget(string kind,string id){
  if(!WidgetExists(kind,id))return;var existing=widgets.FirstOrDefault(w=>w.State.Kind==kind&&w.State.Id==id);if(existing!=null){existing.Show();if(!gameInputLocked)existing.Activate();return;}
  if(widgets.Count>=24){Say("Можно открыть до 24 виджетов одновременно.");return;}
  if(data.Widgets==null)data.Widgets=new List<CompanionWidgetState>();var work=CurrentWorkArea();var slot=FindWidgetSlot(work,widgets.Select(w=>new Rect(w.Left,w.Top,w.Width,w.Height)),new Size(240,110));if(slot.IsEmpty){Say("Для нового виджета не хватает места. Уменьши или перемести открытые виджеты.");return;}var state=new CompanionWidgetState{Kind=kind,Id=id,X=slot.Left,Y=slot.Top,Width=slot.Width,Height=slot.Height};data.Widgets.Add(state);CreateWidget(state);Save();Say("Виджет открыт отдельно. Потяни фон для перемещения, край - для размера.");
 }
 void CreateWidget(CompanionWidgetState state){
  var widget=new CompanionWidget(state,Resources,()=>WidgetContent(state),()=>WidgetAction(state),()=>{Reveal();OpenSectionDialog(state.Kind=="timer"?0:state.Kind=="task"?1:2);});widgets.Add(widget);widget.InputLocked=gameInputLocked;
  widget.Changed+=Save;widget.Closed+=(s,e)=>{widgets.Remove(widget);if(!widgetShutdown){data.Widgets.Remove(state);Save();}};widget.Show();
  var monitor=MonitorFromWindow(new WindowInteropHelper(widget).Handle,2);var info=new CompanionMonitorInfo{Size=System.Runtime.InteropServices.Marshal.SizeOf(typeof(CompanionMonitorInfo))};
  if(monitor!=IntPtr.Zero&&GetMonitorInfo(monitor,ref info)){var source=PresentationSource.FromVisual(widget);var transform=source==null?Matrix.Identity:source.CompositionTarget.TransformFromDevice;var area=new Rect(transform.Transform(new Point(info.Work.Left,info.Work.Top)),transform.Transform(new Point(info.Work.Right,info.Work.Bottom)));widget.Width=Math.Min(widget.Width,area.Width);widget.Height=Math.Min(widget.Height,area.Height);widget.Left=Math.Max(area.Left,Math.Min(widget.Left,area.Right-widget.Width));widget.Top=Math.Max(area.Top,Math.Min(widget.Top,area.Bottom-widget.Height));}
 }
 string[] WidgetContent(CompanionWidgetState state){
  var timer=data.Timers.FirstOrDefault(t=>state.Kind=="timer"&&t.Id==state.Id);if(timer!=null)return new[]{timer.Title,Left(timer)<=0?"ГОТОВО":Clock(Left(timer)),Left(timer)<=0?"↶":timer.End==0?"▶":"Ⅱ"};
  var task=data.Tasks.FirstOrDefault(t=>state.Kind=="task"&&t.Id==state.Id);if(task!=null)return new[]{task.Done?"✓ ВЫПОЛНЕНО":"ДЕЛО",task.Title,task.Done?"↶":"✓"};
  var note=data.Notes.FirstOrDefault(t=>state.Kind=="note"&&t.Id==state.Id);return note==null?null:new[]{note.Title,note.Body,"✎"};
 }
 void WidgetAction(CompanionWidgetState state){
  var timer=data.Timers.FirstOrDefault(t=>state.Kind=="timer"&&t.Id==state.Id);if(timer!=null){if(Left(timer)<=0){timer.Remaining=timer.Duration;timer.End=Now+(long)timer.Duration;timer.Alerted=false;}else if(timer.End>0){timer.Remaining=Left(timer);timer.End=0;}else{timer.End=Now+(long)timer.Remaining;timer.Alerted=false;}Save();RenderTimers();return;}
  var task=data.Tasks.FirstOrDefault(t=>state.Kind=="task"&&t.Id==state.Id);if(task!=null){task.Done=!task.Done;Save();RenderTasks();return;}
  var note=data.Notes.FirstOrDefault(t=>state.Kind=="note"&&t.Id==state.Id);if(note!=null){Reveal();EditNote(note);}
 }
 void TickWidgets(){foreach(var widget in widgets.ToArray()){if(!WidgetExists(widget.State.Kind,widget.State.Id))widget.Close();else widget.Refresh();}}
 void CloseWidgets(){widgetShutdown=true;foreach(var widget in widgets.ToArray())widget.Close();if(quickNoteWindow!=null)quickNoteWindow.Close();Save();}
 void OpenQuickNote(){
  if(startupSplashPending)return;
  if(quickNoteWindow!=null&&quickNoteWindow.IsVisible){quickNoteWindow.Activate();quickNoteText.Focus();return;}
  quickNoteReturnToGame=gameInputLocked;SetGameInput(false);
  if(quickNoteWindow==null){
   var window=new Window{Title="Быстрая заметка",Width=420,Height=295,MinWidth=300,MinHeight=230,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,Topmost=true,ShowInTaskbar=false,ResizeMode=ResizeMode.CanResizeWithGrip,Resources=Resources};quickNoteWindow=window;
   var body=new Grid{Margin=new Thickness(18)};body.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});body.RowDefinitions.Add(new RowDefinition());body.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});body.Children.Add(Text("Быстрая заметка",20));
   quickNoteText=Input("Запиши то, что важно");quickNoteText.MaxLength=10000;quickNoteText.AcceptsReturn=true;quickNoteText.TextWrapping=TextWrapping.Wrap;quickNoteText.VerticalScrollBarVisibility=ScrollBarVisibility.Auto;Grid.SetRow(quickNoteText,1);body.Children.Add(quickNoteText);
   var actions=Row(Btn("Сохранить",SaveQuickNote,true),Btn("Закрыть",CloseQuickNote));Grid.SetRow(actions,2);body.Children.Add(actions);var border=new Border{CornerRadius=new CornerRadius(16),BorderThickness=new Thickness(1),Child=body};Resource(border,Border.BackgroundProperty,"DockBg");Resource(border,Border.BorderBrushProperty,"Line");window.Content=border;
   window.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Escape){e.Handled=true;CloseQuickNote();}else if(e.Key==Key.Enter&&(Keyboard.Modifiers&ModifierKeys.Control)!=0){e.Handled=true;SaveQuickNote();}};CompanionWindowDrag.Attach(window,()=>false);
  }
  var area=CurrentWorkArea();quickNoteWindow.Left=area.Left+(area.Width-quickNoteWindow.Width)/2;quickNoteWindow.Top=area.Top+(area.Height-quickNoteWindow.Height)/2;quickNoteWindow.Show();quickNoteWindow.Activate();quickNoteText.Focus();
 }
 void SaveQuickNote(){var body=quickNoteText.Text.Trim();if(body.Length==0)return;var first=body.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()??"Заметка";data.Notes.Add(new Memo{Id=Guid.NewGuid().ToString(),Title=first.Length>60?first.Substring(0,60)+"…":first,Body=body,Updated=DateTime.Now});Save();RenderNotes();quickNoteText.Clear();CloseQuickNote();}
 void CloseQuickNote(){if(quickNoteWindow!=null)quickNoteWindow.Hide();if(quickNoteReturnToGame){quickNoteReturnToGame=false;Safe(()=>SetGameInput(true));}}
 void RegisterQuickKey(){quickBinding=QuickBinding;if(!RegisterHotKey(source.Handle,19,0x4000|QuickBinding.Modifiers,(uint)QuickBinding.VirtualKey))Say("Клавиши быстрой заметки заняты. Выбери другое сочетание в настройках.");}
 void ChangeQuickKey(string text){var next=CompanionHotkey.Parse(text);if(next.ToString()==ShowBinding.ToString()||next.ToString()==InputBinding.ToString())throw new ArgumentException("Для заметки нужно отдельное сочетание.");var old=QuickBinding;UnregisterHotKey(source.Handle,19);if(!RegisterHotKey(source.Handle,19,0x4000|next.Modifiers,(uint)next.VirtualKey)){RegisterHotKey(source.Handle,19,0x4000|old.Modifiers,(uint)old.VirtualKey);throw new ArgumentException("Сочетание занято. Прежние клавиши сохранены.");}quickBinding=next;data.QuickNoteHotkey=next.ToString();Save();Say("Быстрая заметка: "+next);}
 void QuickNoteSettings(StackPanel body){body.Children.Add(Text("Виджеты",17));body.Children.Add(Text("Новые виджеты располагаются друг под другом. Сохранённые позиции можно выровнять заново.",11,"Muted"));body.Children.Add(Btn("Выровнять виджеты",ArrangeWidgets));body.Children.Add(Text("Быстрая заметка",17));body.Children.Add(Text("Открывается поверх игры. Ctrl+Enter - сохранить, Esc - закрыть.",11,"Muted"));quickKeyEditor=HotkeyEditor("Клавиши быстрой заметки",QuickBinding.ToString());body.Children.Add(quickKeyEditor);body.Children.Add(Row(Btn("Сохранить сочетание",()=>Safe(()=>ChangeQuickKey(quickKeyEditor.Text))),Btn("Открыть заметку",OpenQuickNote)));}
}

public sealed class CompanionWidget : Window {
 public readonly CompanionWidgetState State;public event Action Changed;
 readonly Func<string[]> content;readonly TextBlock title,body;readonly Button action;readonly CompanionInputGate gate;readonly DispatcherTimer saveTimer;readonly Grid tools;bool closing;
 public bool InputLocked {set{gate.Locked=value;}}
 public CompanionWidget(CompanionWidgetState state,ResourceDictionary resources,Func<string[]> read,Action perform,Action expand){
  State=state;content=read;Resources=resources;Title="Companion · виджет";WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;ShowActivated=false;ShowInTaskbar=false;ResizeMode=ResizeMode.CanResizeWithGrip;MinWidth=80;MinHeight=40;Width=Finite(state.Width,240,80,2000);Height=Finite(state.Height,110,40,1400);gate=new CompanionInputGate(this);
  var area=SystemParameters.VirtualScreenWidth>0?new Rect(SystemParameters.VirtualScreenLeft,SystemParameters.VirtualScreenTop,SystemParameters.VirtualScreenWidth,SystemParameters.VirtualScreenHeight):SystemParameters.WorkArea;
  Left=Math.Max(area.Left,Math.Min(Finite(state.X,area.Left+35,area.Left,area.Right),area.Right-Width));Top=Math.Max(area.Top,Math.Min(Finite(state.Y,area.Top+35,area.Top,area.Bottom),area.Bottom-Height));
  var root=new Grid();var border=new Border{CornerRadius=new CornerRadius(9),BorderThickness=new Thickness(1),Child=root};border.SetResourceReference(Border.BackgroundProperty,"Bg");border.SetResourceReference(Border.BorderBrushProperty,"Line");Content=border;
  var stack=new StackPanel{Width=260};title=new TextBlock{FontSize=14,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,6)};title.SetResourceReference(TextBlock.ForegroundProperty,"Muted");body=new TextBlock{FontSize=state.Kind=="timer"?42:22,TextWrapping=TextWrapping.Wrap,FontWeight=FontWeights.Bold};body.SetResourceReference(TextBlock.ForegroundProperty,"Accent");stack.Children.Add(title);stack.Children.Add(body);root.Children.Add(new Viewbox{Stretch=Stretch.Uniform,Child=stack,Margin=new Thickness(10)});
  tools=new Grid{Opacity=0,IsHitTestVisible=false,VerticalAlignment=VerticalAlignment.Top,HorizontalAlignment=HorizontalAlignment.Right};var row=new StackPanel{Orientation=Orientation.Horizontal};tools.Children.Add(new Viewbox{Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,Child=row});action=Tool("✓",perform);action.ToolTip="Пауза / продолжить, выполнить дело или изменить заметку";row.Children.Add(action);var open=Tool("↗",expand);open.ToolTip="Открыть раздел";row.Children.Add(open);var close=Tool("×",()=>Close());close.ToolTip="Убрать виджет, сохранив запись";row.Children.Add(close);root.Children.Add(tools);SizeChanged+=(s,e)=>tools.MaxWidth=Math.Max(1,ActualWidth-4);
  MouseEnter+=(s,e)=>{tools.IsHitTestVisible=true;tools.BeginAnimation(OpacityProperty,new System.Windows.Media.Animation.DoubleAnimation(1,TimeSpan.FromMilliseconds(160)));};MouseLeave+=(s,e)=>{tools.IsHitTestVisible=false;tools.BeginAnimation(OpacityProperty,new System.Windows.Media.Animation.DoubleAnimation(0,TimeSpan.FromMilliseconds(160)));};CompanionWindowDrag.Attach(this,()=>gate.Locked);
  saveTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)};saveTimer.Tick+=(s,e)=>{saveTimer.Stop();Capture();if(Changed!=null)Changed();};LocationChanged+=(s,e)=>QueueSave();SizeChanged+=(s,e)=>QueueSave();Closing+=(s,e)=>{closing=true;saveTimer.Stop();Capture();};Refresh();
 }
 Button Tool(string label,Action click){var b=new Button{Content=label,Width=36,Height=34,Margin=new Thickness(2),FontSize=18,Cursor=Cursors.Hand};b.SetResourceReference(Control.BackgroundProperty,"Field");b.SetResourceReference(Control.ForegroundProperty,"Text");b.Click+=(s,e)=>click();return b;}
 static double Finite(double n,double fallback,double min,double max){return Double.IsNaN(n)||Double.IsInfinity(n)?fallback:Math.Max(min,Math.Min(n,max));}
 void QueueSave(){if(closing||saveTimer==null)return;saveTimer.Stop();saveTimer.Start();}
 void Capture(){State.X=Left;State.Y=Top;State.Width=Width;State.Height=Height;}
 public void CapturePlacement(){Capture();}
 public void Refresh(){var text=content();if(text==null)return;title.Text=text[0];body.Text=text[1];action.Content=text[2];}
}
