using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public partial class Companion {
 static readonly string[] SectionKeys={"timers","tasks","notes","finance","calculator","browser"};
 bool sectionReorderEditing,sectionReorderDragging;
 Button sectionOrderToggle,sectionOrderReset,sectionReorderSource,sectionReorderTarget;
 TextBlock sectionOrderHint;
 Point sectionReorderStart;
 public static List<string> NormalizeSectionOrder(IEnumerable<string> saved){
  var order=new List<string>();if(saved!=null)foreach(string key in saved)if(SectionKeys.Contains(key)&&!order.Contains(key))order.Add(key);
  foreach(string key in new[]{"browser","timers","tasks","notes","finance","calculator"})if(!order.Contains(key))order.Add(key);return order;
 }
 public static List<string> MoveSectionInOrder(IEnumerable<string> saved,string source,string target){
  var order=NormalizeSectionOrder(saved);int from=order.IndexOf(source),to=order.IndexOf(target);
  if(from>=0&&to>=0&&from!=to){order[from]=target;order[to]=source;}return order;
 }
 void BuildSectionOrderControls(){
  var controls=new WrapPanel{Margin=new Thickness(0,0,0,6)};
  sectionOrderToggle=Btn("↔ Изменить порядок",()=>SetSectionReorder(!sectionReorderEditing));sectionOrderToggle.FontSize=11;
  sectionOrderReset=Btn("Сбросить порядок",()=>{CancelSectionPointer();data.SectionOrder=NormalizeSectionOrder(null);ApplySectionOrder();Save();});sectionOrderReset.FontSize=11;sectionOrderReset.Visibility=Visibility.Collapsed;
  controls.Children.Add(sectionOrderToggle);controls.Children.Add(sectionOrderReset);var about=Btn("О программе",OpenAbout);about.FontSize=11;controls.Children.Add(about);var creator=Btn("О создателе",OpenCreator);creator.FontSize=11;controls.Children.Add(creator);hero.Children.Add(controls);
  sectionOrderHint=Text("Перетащи плитку на другую - они поменяются местами. Alt + стрелки - обмен с клавиатуры.",11,"Muted");sectionOrderHint.Visibility=Visibility.Collapsed;hero.Children.Add(sectionOrderHint);
  Deactivated+=(s,e)=>CancelSectionPointer();
 }
 void SetSectionReorder(bool editing){
  CancelSectionPointer();sectionReorderEditing=editing;sectionOrderToggle.Content=editing?"✓ Готово":"↔ Изменить порядок";
  sectionOrderReset.Visibility=sectionOrderHint.Visibility=editing?Visibility.Visible:Visibility.Collapsed;
  foreach(var tile in sectionButtons){CompanionWindowDrag.SetReserved(tile,editing);tile.Cursor=editing?Cursors.SizeAll:Cursors.Hand;tile.ToolTip=editing?"Перетащи плитку или используй Alt + стрелки":null;}
 }
 void ApplySectionOrder(){
  if(sectionTiles==null||sectionButtons.Count!=SectionKeys.Length)return;
  CancelSectionPointer();data.SectionOrder=NormalizeSectionOrder(data.SectionOrder);sectionTiles.Children.Clear();
  foreach(string key in data.SectionOrder)sectionTiles.Children.Add(sectionButtons[Array.IndexOf(SectionKeys,key)]);
  UpdateSectionTiles();SyncWorkspaceNavOrder();
 }
 void MoveSection(string source,string target){
  data.SectionOrder=MoveSectionInOrder(data.SectionOrder,source,target);ApplySectionOrder();Save();
 }
 void MoveSectionBy(string source,int distance){
  var order=NormalizeSectionOrder(data.SectionOrder);int from=order.IndexOf(source);if(from<0)return;int to=Math.Max(0,Math.Min(order.Count-1,from+distance));if(to!=from)MoveSection(source,order[to]);
 }
 void CancelSectionPointer(){
  var source=sectionReorderSource;sectionReorderSource=null;sectionReorderTarget=null;sectionReorderDragging=false;
  if(source!=null){if(source.IsMouseCaptured)source.ReleaseMouseCapture();source.BeginAnimation(UIElement.OpacityProperty,null);source.Opacity=1;}
  if(sectionOrderHint!=null)sectionOrderHint.Text="Перетащи плитку на другую - они поменяются местами. Alt + стрелки - обмен с клавиатуры.";
  UpdateSectionTiles();
 }
 Button SectionAt(Point position){
  foreach(var tile in sectionButtons){Rect bounds=tile.TransformToAncestor(sectionTiles).TransformBounds(new Rect(0,0,tile.ActualWidth,tile.ActualHeight));if(bounds.Contains(position))return tile;}return null;
 }
 void AttachSectionReorder(Button tile,string key){
  tile.Tag=key;
  tile.PreviewMouseLeftButtonDown+=(s,e)=>{
   if(!sectionReorderEditing||gameInputLocked)return;
   CancelSectionPointer();sectionReorderSource=tile;sectionReorderStart=e.GetPosition(sectionTiles);tile.Focus();tile.CaptureMouse();e.Handled=true;
  };
  tile.PreviewMouseMove+=(s,e)=>{
   if(sectionReorderSource!=tile)return;if(e.LeftButton!=MouseButtonState.Pressed){CancelSectionPointer();return;}
   Point position=e.GetPosition(sectionTiles);if(!sectionReorderDragging&&!CompanionWindowDrag.PastThreshold(position-sectionReorderStart))return;
   sectionReorderDragging=true;tile.BeginAnimation(UIElement.OpacityProperty,null);tile.Opacity=.65;sectionReorderTarget=SectionAt(position);UpdateSectionTiles();
   sectionOrderHint.Text=sectionReorderTarget!=null&&sectionReorderTarget!=tile?"Отпусти мышь - эти две плитки поменяются местами.":"Перетащи плитку на нужное место.";e.Handled=true;
  };
  tile.PreviewMouseLeftButtonUp+=(s,e)=>{
   if(sectionReorderSource!=tile)return;
   var target=sectionReorderDragging?SectionAt(e.GetPosition(sectionTiles)):null;CancelSectionPointer();
   if(target!=null&&target!=tile)MoveSection(key,(string)target.Tag);e.Handled=true;
  };
  tile.LostMouseCapture+=(s,e)=>{if(sectionReorderSource==tile)CancelSectionPointer();};
  tile.PreviewKeyDown+=(s,e)=>{
   if(!sectionReorderEditing||(Keyboard.Modifiers&ModifierKeys.Alt)==0)return;Key pressed=e.Key==Key.System?e.SystemKey:e.Key;int distance=pressed==Key.Left?-1:pressed==Key.Right?1:pressed==Key.Up?-sectionTiles.Columns:pressed==Key.Down?sectionTiles.Columns:0;
   if(distance!=0){MoveSectionBy(key,distance);tile.Focus();e.Handled=true;}
  };
  var menu=new ContextMenu();
  foreach(var item in new[]{new{Title="В начало",Distance=-6},new{Title="На место назад",Distance=-1},new{Title="На место вперёд",Distance=1},new{Title="В конец",Distance=6}}){int distance=item.Distance;var option=new MenuItem{Header=item.Title};option.Click+=(s,e)=>MoveSectionBy(key,distance);menu.Items.Add(option);}
  tile.ContextMenu=menu;
 }
}
