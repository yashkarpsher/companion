using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public partial class Companion {
 static readonly string[] GameKeys={"flight","runner","durak","blackjack","svin"};
 readonly List<Button> gameButtons=new List<Button>();
 bool gameReorderEditing,gameReorderDragging;
 Button gameOrderToggle,gameOrderReset,gameReorderSource,gameReorderTarget;
 TextBlock gameOrderHint;
 Point gameReorderStart;
 public static List<string> NormalizeGameOrder(IEnumerable<string> saved){
  var order=new List<string>();if(saved!=null)foreach(string key in saved)if(GameKeys.Contains(key)&&!order.Contains(key))order.Add(key);
  foreach(string key in new[]{"svin","flight","runner","durak","blackjack"})if(!order.Contains(key))order.Add(key);return order;
 }
 public static List<string> MoveGameInOrder(IEnumerable<string> saved,string source,string target){
  var order=NormalizeGameOrder(saved);int from=order.IndexOf(source),to=order.IndexOf(target);
  if(from>=0&&to>=0&&from!=to){order[from]=target;order[to]=source;}return order;
 }
 void BuildGameOrderControls(){
  foreach(Button tile in arcadeTiles.Children)gameButtons.Add(tile);
  for(int i=0;i<gameButtons.Count;i++)AttachGameReorder(gameButtons[i],GameKeys[i]);
  var controls=new WrapPanel{Margin=new Thickness(0,0,0,6)};
  gameOrderToggle=Btn("↔ Порядок мини-игр",()=>SetGameReorder(!gameReorderEditing));gameOrderToggle.FontSize=11;
  gameOrderReset=Btn("Сбросить порядок",()=>{CancelGamePointer();data.GameOrder=NormalizeGameOrder(null);ApplyGameOrder();Save();});gameOrderReset.FontSize=11;gameOrderReset.Visibility=Visibility.Collapsed;
  controls.Children.Add(gameOrderToggle);controls.Children.Add(gameOrderReset);hero.Children.Insert(hero.Children.IndexOf(arcadeTiles),controls);
  gameOrderHint=Text("Перетащи плитку на другую — они поменяются местами. Alt + стрелки — обмен с клавиатуры.",11,"Muted");gameOrderHint.Visibility=Visibility.Collapsed;hero.Children.Insert(hero.Children.IndexOf(arcadeTiles),gameOrderHint);
  Deactivated+=(s,e)=>CancelGamePointer();
  ApplyGameOrder();
 }
 void SetGameReorder(bool editing){
  CancelGamePointer();gameReorderEditing=editing;gameOrderToggle.Content=editing?"✓ Готово":"↔ Порядок мини-игр";
  gameOrderReset.Visibility=gameOrderHint.Visibility=editing?Visibility.Visible:Visibility.Collapsed;
  foreach(var tile in gameButtons){CompanionWindowDrag.SetReserved(tile,editing);tile.Cursor=editing?Cursors.SizeAll:Cursors.Hand;tile.ToolTip=editing?"Перетащи плитку или используй Alt + стрелки":null;}
 }
 void ApplyGameOrder(){
  if(arcadeTiles==null||gameButtons.Count!=GameKeys.Length)return;
  CancelGamePointer();data.GameOrder=NormalizeGameOrder(data.GameOrder);arcadeTiles.Children.Clear();
  foreach(string key in data.GameOrder)arcadeTiles.Children.Add(gameButtons[Array.IndexOf(GameKeys,key)]);
  UpdateGameReorderTiles();
 }
 void MoveGame(string source,string target){
  data.GameOrder=MoveGameInOrder(data.GameOrder,source,target);ApplyGameOrder();Save();
 }
 void MoveGameBy(string source,int distance){
  var order=NormalizeGameOrder(data.GameOrder);int from=order.IndexOf(source);if(from<0)return;int to=Math.Max(0,Math.Min(order.Count-1,from+distance));if(to!=from)MoveGame(source,order[to]);
 }
 void CancelGamePointer(){
  var source=gameReorderSource;gameReorderSource=null;gameReorderTarget=null;gameReorderDragging=false;
  if(source!=null){if(source.IsMouseCaptured)source.ReleaseMouseCapture();source.BeginAnimation(UIElement.OpacityProperty,null);source.Opacity=1;}
  if(gameOrderHint!=null)gameOrderHint.Text="Перетащи плитку на другую — они поменяются местами. Alt + стрелки — обмен с клавиатуры.";
  UpdateGameReorderTiles();
 }
 Button GameAt(Point position){
  foreach(var tile in gameButtons){Rect bounds=tile.TransformToAncestor(arcadeTiles).TransformBounds(new Rect(0,0,tile.ActualWidth,tile.ActualHeight));if(bounds.Contains(position))return tile;}return null;
 }
 void UpdateGameReorderTiles(){
  foreach(var tile in gameButtons)tile.SetResourceReference(Control.BorderBrushProperty,gameReorderDragging&&(tile==gameReorderSource||tile==gameReorderTarget)?"Accent":"Line");
 }
 void AttachGameReorder(Button tile,string key){
  tile.Tag=key;
  tile.PreviewMouseLeftButtonDown+=(s,e)=>{
   if(!gameReorderEditing||gameInputLocked)return;
   CancelGamePointer();gameReorderSource=tile;gameReorderStart=e.GetPosition(arcadeTiles);tile.Focus();tile.CaptureMouse();e.Handled=true;
  };
  tile.PreviewMouseMove+=(s,e)=>{
   if(gameReorderSource!=tile)return;if(e.LeftButton!=MouseButtonState.Pressed){CancelGamePointer();return;}
   Point position=e.GetPosition(arcadeTiles);if(!gameReorderDragging&&!CompanionWindowDrag.PastThreshold(position-gameReorderStart))return;
   gameReorderDragging=true;tile.BeginAnimation(UIElement.OpacityProperty,null);tile.Opacity=.65;gameReorderTarget=GameAt(position);UpdateGameReorderTiles();
   gameOrderHint.Text=gameReorderTarget!=null&&gameReorderTarget!=tile?"Отпусти мышь — эти две плитки поменяются местами.":"Перетащи плитку на нужное место.";e.Handled=true;
  };
  tile.PreviewMouseLeftButtonUp+=(s,e)=>{
   if(gameReorderSource!=tile)return;
   var target=gameReorderDragging?GameAt(e.GetPosition(arcadeTiles)):null;CancelGamePointer();
   if(target!=null&&target!=tile)MoveGame(key,(string)target.Tag);e.Handled=true;
  };
  tile.LostMouseCapture+=(s,e)=>{if(gameReorderSource==tile)CancelGamePointer();};
  tile.PreviewKeyDown+=(s,e)=>{
   if(!gameReorderEditing||(Keyboard.Modifiers&ModifierKeys.Alt)==0)return;Key pressed=e.Key==Key.System?e.SystemKey:e.Key;int distance=pressed==Key.Left?-1:pressed==Key.Right?1:pressed==Key.Up?-arcadeTiles.Columns:pressed==Key.Down?arcadeTiles.Columns:0;
   if(distance!=0){MoveGameBy(key,distance);tile.Focus();e.Handled=true;}
  };
  var menu=new ContextMenu();
  foreach(var item in new[]{new{Title="В начало",Distance=-5},new{Title="На место назад",Distance=-1},new{Title="На место вперёд",Distance=1},new{Title="В конец",Distance=5}}){int distance=item.Distance;var option=new MenuItem{Header=item.Title};option.Click+=(s,e)=>MoveGameBy(key,distance);menu.Items.Add(option);}
  tile.ContextMenu=menu;
 }
}
