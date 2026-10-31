using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

public sealed class CompanionHotkey {
 public uint Modifiers;public int VirtualKey;
 public override string ToString(){return ((Modifiers&2)!=0?"Ctrl+":"")+((Modifiers&1)!=0?"Alt+":"")+((Modifiers&4)!=0?"Shift+":"")+KeyInterop.KeyFromVirtualKey(VirtualKey).ToString().Replace("D0","0").Replace("D1","1").Replace("D2","2").Replace("D3","3").Replace("D4","4").Replace("D5","5").Replace("D6","6").Replace("D7","7").Replace("D8","8").Replace("D9","9");}
 public static CompanionHotkey Parse(string text){
  if(String.IsNullOrWhiteSpace(text))throw new ArgumentException("Нажми сочетание Ctrl или Alt с буквой, цифрой или F1–F24.");
  var parts=text.Split('+');uint modifiers=0;for(int i=0;i<parts.Length-1;i++){switch(parts[i].Trim().ToLowerInvariant()){case "ctrl":case "control":modifiers|=2;break;case "alt":modifiers|=1;break;case "shift":modifiers|=4;break;default:throw new ArgumentException("Допустимы Ctrl, Alt и Shift.");}}
  string keyName=parts.Last().Trim();if(keyName.Length==1&&Char.IsDigit(keyName[0]))keyName="D"+keyName;
  Key key;if(!Enum.TryParse<Key>(keyName,true,out key))throw new ArgumentException("Выбери букву, цифру или F1–F24.");int code=KeyInterop.VirtualKeyFromKey(key);
  if((modifiers&3)==0||!((code>=65&&code<=90)||(code>=48&&code<=57)||(code>=112&&code<=135)))throw new ArgumentException("Нужен Ctrl или Alt + буква, цифра или F1–F24.");
  return new CompanionHotkey{Modifiers=modifiers,VirtualKey=code};
 }
 public static CompanionHotkey OrDefault(string text,string fallback){try{return Parse(text);}catch(ArgumentException){return Parse(fallback);}}
}

// The layered WPF overlays pass mouse input through without disabling their
// native windows, so Windows taskbar Close remains available in game mode.
public sealed class CompanionInputGate {
 const long Transparent=0x20,NoActivate=0x8000000,Mask=Transparent|NoActivate;
 readonly Window window;bool locked;long originalBits;bool applied;
 public CompanionInputGate(Window target){window=target;window.SourceInitialized+=(s,e)=>Apply();}
 public bool Locked {get{return locked;}set{if(locked==value)return;locked=value;Apply();}}
 public static long ReadStyle(IntPtr hwnd){return GetWindowLongPtr(hwnd,-20).ToInt64();}
 void Apply(){
  IntPtr handle=new WindowInteropHelper(window).Handle;if(handle==IntPtr.Zero)return;
  long current=ReadStyle(handle);
  if(locked&&!applied){
   originalBits=current&Mask;SetStyle(handle,current|Mask);
   applied=true;
  }else if(!locked&&applied){SetStyle(handle,(current&~Mask)|originalBits);applied=false;window.InvalidateVisual();}
 }
 static void SetStyle(IntPtr hwnd,long style){SetLastError(0);IntPtr result=SetWindowLongPtr(hwnd,-20,new IntPtr(style));int error=Marshal.GetLastWin32Error();if(result==IntPtr.Zero&&error!=0)throw new Win32Exception(error);SetWindowPos(hwnd,IntPtr.Zero,0,0,0,0,0x37);}
 [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW",SetLastError=true)] static extern IntPtr GetWindowLongPtr(IntPtr hwnd,int index);
 [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW",SetLastError=true)] static extern IntPtr SetWindowLongPtr(IntPtr hwnd,int index,IntPtr value);
 [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
 [DllImport("kernel32.dll")] static extern void SetLastError(uint error);
}

public partial class Companion {
 bool gameInputLocked,showHotkeyRegistered,inputHotkeyRegistered;
 CompanionHotkey showBinding,inputBinding;
 CompanionInputGate mainInputGate,noticeInputGate;
 IntPtr lastExternalWindow;
 TextBox showKeyEditor,inputKeyEditor;
 CompanionHotkey ShowBinding {get{return showBinding??CompanionHotkey.OrDefault(data.ShowHotkey,"Ctrl+Shift+M");}}
 CompanionHotkey InputBinding {get{return inputBinding??CompanionHotkey.OrDefault(data.InputHotkey,"Ctrl+Shift+G");}}
 void InitializeHotkeys(){
  showBinding=ShowBinding;inputBinding=InputBinding;
  showHotkeyRegistered=RegisterHotKey(source.Handle,17,0x4000|showBinding.Modifiers,(uint)showBinding.VirtualKey);
  inputHotkeyRegistered=RegisterHotKey(source.Handle,18,0x4000|inputBinding.Modifiers,(uint)inputBinding.VirtualKey);
  UpdateOverlayHelp();if(!showHotkeyRegistered||!inputHotkeyRegistered)Say("Сочетание занято другой программой. Выбери другие клавиши в разделе «Дизайн». Доступ через трей сохраняется.");
 }
 void ChangeHotkeys(string showText,string inputText){
  var nextShow=CompanionHotkey.Parse(showText);var nextInput=CompanionHotkey.Parse(inputText);
  if(nextShow.ToString()==QuickBinding.ToString()||nextInput.ToString()==QuickBinding.ToString())throw new ArgumentException("Это сочетание используется для быстрой заметки.");
  if(nextShow.ToString()==nextInput.ToString())throw new ArgumentException("Для показа окна и управления мышью нужны разные сочетания.");
  if(source==null)throw new InvalidOperationException("Окно ещё запускается. Повтори сохранение.");
  if(gameInputLocked)SetGameInput(false);
  var oldShow=ShowBinding;var oldInput=InputBinding;
  UnregisterHotKey(source.Handle,17);UnregisterHotKey(source.Handle,18);
  bool showOk=RegisterHotKey(source.Handle,17,0x4000|nextShow.Modifiers,(uint)nextShow.VirtualKey);
  bool inputOk=RegisterHotKey(source.Handle,18,0x4000|nextInput.Modifiers,(uint)nextInput.VirtualKey);
  if(!showOk||!inputOk){
   UnregisterHotKey(source.Handle,17);UnregisterHotKey(source.Handle,18);
   showHotkeyRegistered=RegisterHotKey(source.Handle,17,0x4000|oldShow.Modifiers,(uint)oldShow.VirtualKey);
   inputHotkeyRegistered=RegisterHotKey(source.Handle,18,0x4000|oldInput.Modifiers,(uint)oldInput.VirtualKey);
   throw new ArgumentException("Сочетание занято Windows или другой программой. Предыдущие клавиши восстановлены; выбери другое сочетание.");
  }
  showHotkeyRegistered=inputHotkeyRegistered=true;showBinding=nextShow;inputBinding=nextInput;
  data.ShowHotkey=nextShow.ToString();data.InputHotkey=nextInput.ToString();Save();UpdateOverlayHelp();Say("Клавиши сохранены: "+ShowBinding+" — окно; "+InputBinding+" — управление мышью.");
 }
 void UpdateOverlayHelp(){if(tray!=null)tray.Text="Companion · "+ShowBinding;if(feedback!=null)feedback.Text="v"+GetType().Assembly.GetName().Version.ToString(3)+" · "+(gameInputLocked?"Режим игры · "+InputBinding+" — вернуть управление":"Готов к работе · "+InputBinding+" — в игру · "+ShowBinding+" — окно");}
 TextBox HotkeyEditor(string title,string value){
  var editor=Input(title,value);editor.IsReadOnly=true;editor.ToolTip="Нажми Ctrl или Alt с буквой, цифрой либо F1–F24. Изменения применяются кнопкой сохранения.";
  editor.GotKeyboardFocus+=(s,e)=>{if(source==null)return;if(gameInputLocked)SetGameInput(false);UnregisterHotKey(source.Handle,17);UnregisterHotKey(source.Handle,18);UnregisterHotKey(source.Handle,19);showHotkeyRegistered=inputHotkeyRegistered=false;};
  editor.LostKeyboardFocus+=(s,e)=>{if(source==null)return;UnregisterHotKey(source.Handle,19);RegisterHotKey(source.Handle,19,0x4000|QuickBinding.Modifiers,(uint)QuickBinding.VirtualKey);showHotkeyRegistered=RegisterHotKey(source.Handle,17,0x4000|ShowBinding.Modifiers,(uint)ShowBinding.VirtualKey);inputHotkeyRegistered=RegisterHotKey(source.Handle,18,0x4000|InputBinding.Modifiers,(uint)InputBinding.VirtualKey);};
  editor.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Tab)return;e.Handled=true;if((Keyboard.Modifiers&ModifierKeys.Windows)!=0)return;Key key=e.Key==Key.System?e.SystemKey:e.Key;uint mods=0;if((Keyboard.Modifiers&ModifierKeys.Control)!=0)mods|=2;if((Keyboard.Modifiers&ModifierKeys.Alt)!=0)mods|=1;if((Keyboard.Modifiers&ModifierKeys.Shift)!=0)mods|=4;try{var candidate=new CompanionHotkey{Modifiers=mods,VirtualKey=KeyInterop.VirtualKeyFromKey(key)};editor.Text=CompanionHotkey.Parse(candidate.ToString()).ToString();}catch(ArgumentException){}};return editor;
 }
 void OverlaySettings(StackPanel body){QuickNoteSettings(body);
  body.Children.Add(Text("Управление в игре",17));body.Children.Add(Text("Режим игры пропускает мышь сквозь окна помощника и браузера. Курсор и клики остаются игре. Сочетание ниже возвращает управление окнами.",11,"Muted"));
  body.Children.Add(Btn("Включить режим игры",()=>Safe(()=>SetGameInput(true)),true));
  body.Children.Add(Text("Горячие клавиши",17));body.Children.Add(Text("Показать / скрыть уже запущенный помощник",11,"Muted"));showKeyEditor=HotkeyEditor("Клавиши показа окна",ShowBinding.ToString());body.Children.Add(showKeyEditor);
  body.Children.Add(Text("Игра / управление окнами",11,"Muted"));inputKeyEditor=HotkeyEditor("Клавиши управления мышью",InputBinding.ToString());body.Children.Add(inputKeyEditor);
  body.Children.Add(Row(Btn("Сохранить клавиши",()=>Safe(()=>ChangeHotkeys(showKeyEditor.Text,inputKeyEditor.Text)),true),Btn("По умолчанию",()=>{showKeyEditor.Text="Ctrl+Shift+M";inputKeyEditor.Text="Ctrl+Shift+G";})));
  body.Children.Add(Text("Нажми нужное сочетание в поле, затем сохрани. Помощник должен оставаться запущенным, в том числе в трее. Ярлыки и автозапуск не создаются.",11,"Muted"));
 }
 void TrackExternalWindow(){IntPtr foreground=GetForegroundWindow();if(foreground!=IntPtr.Zero&&!OurWindow(foreground))lastExternalWindow=foreground;}
 bool OurWindow(IntPtr hwnd){return widgets.Any(w=>new WindowInteropHelper(w).Handle==hwnd)||(quickNoteWindow!=null&&new WindowInteropHelper(quickNoteWindow).Handle==hwnd)|| hwnd==new WindowInteropHelper(this).Handle||(browserWindow!=null&&browserWindow.OwnsWindow(hwnd))||(miniDock!=null&&hwnd==new WindowInteropHelper(miniDock).Handle)||(notice!=null&&hwnd==new WindowInteropHelper(notice).Handle);}
 void ToggleGameInput(){bool returning=gameInputLocked;SetGameInput(!gameInputLocked);if(returning){if(browserWindow!=null&&browserWindow.IsVisible){browserWindow.Activate();}else Reveal();}}
 void SetGameInput(bool locked){
  if(locked&&!inputHotkeyRegistered)throw new InvalidOperationException("Сначала задай свободные клавиши управления мышью в «Дизайне»: текущее сочетание занято.");
  if(gameInputLocked==locked)return;
  if(mainInputGate==null)mainInputGate=new CompanionInputGate(this);
  try{
   foreach(var widget in widgets)widget.InputLocked=locked;mainInputGate.Locked=locked;if(browserWindow!=null)browserWindow.InputLocked=locked;if(noticeInputGate!=null)noticeInputGate.Locked=locked;
   gameInputLocked=locked;if(locked){CloseCardGames();CloseHelp();SuspendArcade();HideMiniDock(true);if(lastExternalWindow!=IntPtr.Zero&&IsWindow(lastExternalWindow))SetForegroundWindow(lastExternalWindow);}
  }catch{foreach(var widget in widgets)widget.InputLocked=false;mainInputGate.Locked=false;if(browserWindow!=null)browserWindow.InputLocked=false;if(noticeInputGate!=null)noticeInputGate.Locked=false;gameInputLocked=false;throw;}
  UpdateOverlayHelp();
 }
 [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
 [DllImport("user32.dll")] static extern bool IsWindow(IntPtr hwnd);
}
