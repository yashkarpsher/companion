using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

public partial class Companion {
 Grid cardGameLayer;Border cardGameSurface;StackPanel cardGameBody;TextBlock cardGameTitle;ScrollViewer cardGameScroll;
 bool cardGameOpen,cardGameBlackjack,durakThrowIn;int cardGameGeneration,blackjackBet=50;
 readonly HashSet<string> previousCardVisuals=new HashSet<string>();
 readonly HashSet<string> currentCardVisuals=new HashSet<string>();
 int cardDealIndex;
 CompanionDurak durakGame;CompanionBlackjack blackjackGame;
 void BuildCardGames(){
  for(int i=0;i<2;i++){bool blackjack=i==1;var content=Stack();content.Children.Add(Text(blackjack?"♠  BLACKJACK":"♦  ДУРАК",12,"Accent"));content.Children.Add(Text(blackjack?"Блэкджек":"Дурак",18));content.Children.Add(Text(blackjack?"Против дилера · игровые фишки":"Обычный или подкидной · против бота",12,"Muted"));var tile=Btn("",()=>{if(!gameReorderEditing)OpenCardGames(blackjack);});tile.Content=content;tile.HorizontalContentAlignment=HorizontalAlignment.Stretch;tile.Padding=new Thickness(16);tile.Margin=new Thickness(0,0,12,12);tile.MinHeight=126;AutomationProperties.SetName(tile,blackjack?"Играть: Блэкджек":"Играть: Дурак");arcadeTiles.Children.Add(tile);}
  cardGameLayer=new Grid{Visibility=Visibility.Collapsed,Background=Brushes.Transparent};Panel.SetZIndex(cardGameLayer,70);root.Children.Add(cardGameLayer);var shade=new Border{Background=Brush("#B3000000")};CompanionWindowDrag.SetReserved(shade,true);shade.MouseLeftButtonDown+=(s,e)=>{e.Handled=true;CloseCardGames();};cardGameLayer.Children.Add(shade);
  cardGameSurface=new Border{CornerRadius=new CornerRadius(18),BorderThickness=new Thickness(1),Padding=new Thickness(18),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};Resource(cardGameSurface,Border.BackgroundProperty,"DockBg");Resource(cardGameSurface,Border.BorderBrushProperty,"Line");cardGameLayer.Children.Add(cardGameSurface);
  var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition());cardGameSurface.Child=layout;var header=new DockPanel{Margin=new Thickness(0,0,0,10)};var back=Btn("← Назад",CloseCardGames);DockPanel.SetDock(back,Dock.Left);header.Children.Add(back);cardGameTitle=Text("",22);cardGameTitle.Margin=new Thickness(10,0,0,0);cardGameTitle.VerticalAlignment=VerticalAlignment.Center;header.Children.Add(cardGameTitle);layout.Children.Add(header);
  cardGameBody=Stack();cardGameBody.VerticalAlignment=VerticalAlignment.Center;cardGameScroll=new ScrollViewer{Content=cardGameBody,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Center,Padding=new Thickness(0,0,7,0)};Grid.SetRow(cardGameScroll,1);layout.Children.Add(cardGameScroll);KeyboardNavigation.SetTabNavigation(cardGameSurface,KeyboardNavigationMode.Cycle);cardGameLayer.PreviewMouseWheel+=ScrollMainWheel;
  root.SizeChanged+=(s,e)=>SizeCardGames();PreviewKeyDown+=(s,e)=>{if(cardGameOpen&&e.Key==Key.Escape){e.Handled=true;CloseCardGames();}};
 }
 void SizeCardGames(){if(cardGameSurface==null)return;cardGameSurface.Width=Math.Max(1,Math.Min(840,(root.ActualWidth>0?root.ActualWidth:Width)-24));cardGameSurface.Height=Math.Max(1,Math.Min(800,(root.ActualHeight>0?root.ActualHeight:Height)-24));}
 void OpenCardGames(bool blackjack){
  previousCardVisuals.Clear();
  if(compact)SetCompact(false);if(designing)ShowDesign();if(sectionReorderEditing)SetSectionReorder(false);CloseHelp();CloseArcade();CloseSectionDialog(false,true);cardGameBlackjack=blackjack;cardGameTitle.Text=blackjack?"Блэкджек":"Дурак";if(blackjack&&blackjackGame==null)blackjackGame=new CompanionBlackjack(data.BlackjackChips??1000,Environment.TickCount);cardGameOpen=true;cardGameGeneration++;SizeCardGames();RenderCardGame();cardGameLayer.Visibility=Visibility.Visible;cardGameLayer.IsHitTestVisible=true;cardGameLayer.BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(180)));var shift=new TranslateTransform(0,8);cardGameSurface.RenderTransform=shift;shift.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(0,TimeSpan.FromMilliseconds(180)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});cardGameScroll.ScrollToTop();cardGameScroll.Focus();
 }
 void CloseCardGames(){if(cardGameLayer==null||!cardGameOpen)return;cardGameOpen=false;cardGameLayer.IsHitTestVisible=false;int generation=++cardGameGeneration;var fade=new DoubleAnimation(0,TimeSpan.FromMilliseconds(140));fade.Completed+=(s,e)=>{if(generation!=cardGameGeneration||cardGameOpen)return;cardGameLayer.Visibility=Visibility.Collapsed;if(IsActive&&IsVisible&&!arcadeOpen&&!sectionDialogOpen&&!helpOpen&&!designing&&!compact)gameButtons[cardGameBlackjack?3:2].Focus();};cardGameLayer.BeginAnimation(OpacityProperty,fade);}
 FrameworkElement PlayingCard(CompanionPlayingCard card,bool hidden=false,bool small=false,string visualKey=null){
  double width=small?45:58,height=small?63:82;var border=new Border{Width=width,Height=height,CornerRadius=new CornerRadius(8),BorderThickness=new Thickness(1),Margin=new Thickness(0,0,5,6),Padding=new Thickness(5)};Resource(border,Border.BorderBrushProperty,"Line");
  if(hidden){Resource(border,Border.BackgroundProperty,"Field");var symbol=Text("◇",small?27:36,"Accent");symbol.Margin=new Thickness(0);symbol.VerticalAlignment=VerticalAlignment.Center;symbol.HorizontalAlignment=HorizontalAlignment.Center;border.Child=symbol;AutomationProperties.SetName(border,"Закрытая карта");}
  else if(card==null){border.Background=Brush("#20242B");border.Child=new TextBlock{Text="?",FontSize=24,Foreground=Brush("#89929F"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};}
  else{border.Background=Brush("#EEF0F3");var ink=Brush(card.Red?"#B72948":"#17202D");var panel=new StackPanel{VerticalAlignment=VerticalAlignment.Center};panel.Children.Add(new TextBlock{Text=card.Label,FontSize=small?18:23,FontWeight=FontWeights.Bold,Foreground=ink,HorizontalAlignment=HorizontalAlignment.Center});panel.Children.Add(new TextBlock{Text=card.Symbol,FontFamily=new FontFamily("Segoe UI Symbol"),FontSize=small?21:28,Foreground=ink,HorizontalAlignment=HorizontalAlignment.Center});border.Child=panel;AutomationProperties.SetName(border,card.Label+" "+card.Symbol);}
  if(visualKey!=null){currentCardVisuals.Add(visualKey);if(!previousCardVisuals.Contains(visualKey))AnimateDealtCard(border,cardDealIndex++);}
  return border;
 }
 void RenderCardGame(){if(cardGameBody==null)return;double offset=cardGameScroll.VerticalOffset;currentCardVisuals.Clear();cardDealIndex=0;cardGameBody.Children.Clear();if(cardGameBlackjack)RenderBlackjack();else RenderDurak();CenterCardTable();previousCardVisuals.Clear();previousCardVisuals.UnionWith(currentCardVisuals);cardGameScroll.ScrollToVerticalOffset(offset);}
 void RenderDurak(){
  cardGameBody.Children.Add(Text("Режим новой партии",12,"Muted"));var modes=new WrapPanel();var ordinary=new RadioButton{Content="Обычный",IsChecked=!durakThrowIn,Margin=new Thickness(0,0,20,10),GroupName="DurakMode"};var throwIn=new RadioButton{Content="Подкидной",IsChecked=durakThrowIn,Margin=new Thickness(0,0,0,10),GroupName="DurakMode"};Resource(ordinary,Control.ForegroundProperty,"Text");Resource(throwIn,Control.ForegroundProperty,"Text");ordinary.Checked+=(s,e)=>durakThrowIn=false;throwIn.Checked+=(s,e)=>durakThrowIn=true;modes.Children.Add(ordinary);modes.Children.Add(throwIn);cardGameBody.Children.Add(modes);
  cardGameBody.Children.Add(Btn(durakGame==null?"Начать партию":"Новая партия",()=>{previousCardVisuals.Clear();durakGame=new CompanionDurak(durakThrowIn,Environment.TickCount);RenderCardGame();},true));
  if(durakGame==null){cardGameBody.Children.Add(Card(Text("Обычный — атака одной картой. Подкидной — можно добавлять карты достоинства, которое уже лежит на столе.\n\n36 карт, козырь, один бот. Выбери режим и начни партию.",14)));return;}
  var game=durakGame;cardGameBody.Children.Add(Text((game.ThrowIn?"Подкидной":"Обычный")+" · В колоде: "+game.Deck.Count+" · В бито: "+game.Discard.Count,12,"Muted"));cardGameBody.Children.Add(Text(game.Message,17,game.Finished?"Accent":"Text"));
  var info=new WrapPanel();var trump=Stack();trump.Margin=new Thickness(0,0,14,0);trump.Children.Add(Text("Козырь: "+game.TrumpCard.Symbol,12,"Muted"));trump.Children.Add(PlayingCard(game.TrumpCard,false,true,"trump"));info.Children.Add(trump);var opponent=Stack();opponent.Children.Add(Text("У бота: "+game.Bot.Count+" карт",12,"Muted"));var backs=new WrapPanel();for(int i=0;i<Math.Min(3,game.Bot.Count);i++)backs.Children.Add(PlayingCard(null,true,true,"bot:"+i));opponent.Children.Add(backs);info.Children.Add(opponent);cardGameBody.Children.Add(info);
  var table=new WrapPanel();foreach(var pair in game.Table){var group=Stack();group.Margin=new Thickness(0,0,10,8);group.Children.Add(Text("Атака / защита",10,"Muted"));group.Children.Add(Row(PlayingCard(pair.Attack,false,true,CardVisualKey("attack",pair.Attack)),PlayingCard(pair.Defense,false,true,pair.Defense==null?null:CardVisualKey("defense",pair.Defense))));table.Children.Add(group);}if(game.Table.Count==0)cardGameBody.Children.Add(Text(game.Finished?"Партия завершена":"Стол пуст",12,"Muted"));else cardGameBody.Children.Add(table);
  var controls=new WrapPanel();var take=Btn("Взять",()=>{game.TakeHuman();RenderCardGame();});take.IsEnabled=!game.Finished&&game.Defender==0&&!game.Taking&&game.Table.Any(p=>p.Defense==null);controls.Children.Add(take);var finish=Btn(game.Taking?"Завершить ход":"Бито",()=>{game.FinishHuman();RenderCardGame();},true);finish.IsEnabled=!game.Finished&&game.Attacker==0&&(game.Taking||game.Covered);controls.Children.Add(finish);cardGameBody.Children.Add(controls);
  cardGameBody.Children.Add(Text("Твои карты · нажми подходящую карту",12,"Muted"));var hand=new WrapPanel();foreach(var card in game.Human.OrderBy(c=>c.Suit==game.Trump?1:0).ThenBy(c=>c.Suit).ThenBy(c=>c.Rank)){var selected=card;var button=Btn("",()=>{game.PlayHuman(selected);RenderCardGame();});button.Content=PlayingCard(card,false,false,CardVisualKey("hand",card));AttachCardHover(button);button.Padding=new Thickness(0);button.Margin=new Thickness(0,0,4,0);button.BorderThickness=new Thickness(0);button.Background=Brushes.Transparent;button.IsEnabled=game.CanHumanPlay(card);button.ToolTip=card.Label+" "+card.Symbol;CompanionWindowDrag.SetReserved(button,true);AutomationProperties.SetName(button,card.Label+" "+card.Symbol);hand.Children.Add(button);}cardGameBody.Children.Add(hand);
  if(!game.ThrowIn)cardGameBody.Children.Add(Text("Обычный режим: за ход атакуют только одной картой. Выбор режима сверху применяется к новой партии.",11,"Muted"));
 }
 void SaveBlackjackResult(){if(blackjackGame!=null&&!blackjackGame.Active){data.BlackjackChips=blackjackGame.Chips;Save();}}
 void BlackjackAction(Action action){action();SaveBlackjackResult();RenderCardGame();}
 void RenderBlackjack(){
  var game=blackjackGame;cardGameBody.Children.Add(Text("Фишки: "+game.Chips+"    ·    Ставка: "+(game.Active?game.Bet:blackjackBet),18,"Accent"));cardGameBody.Children.Add(Text("Только игровые фишки — без покупок и вывода денег.",11,"Muted"));cardGameBody.Children.Add(Text(game.Message,16));
  var bets=new WrapPanel();foreach(int value in new[]{10,50,100}){int bet=value;var button=Btn(value.ToString(),()=>{blackjackBet=bet;RenderCardGame();});button.IsEnabled=!game.Active;if(blackjackBet==bet)Resource(button,Control.BorderBrushProperty,"Accent");bets.Children.Add(button);}var deal=Btn("Раздать",()=>{previousCardVisuals.Clear();BlackjackAction(()=>game.Start(blackjackBet));},true);deal.IsEnabled=!game.Active&&game.Chips>=blackjackBet;bets.Children.Add(deal);cardGameBody.Children.Add(bets);
  cardGameBody.Children.Add(Text(game.Dealer.Count==0?"Дилер":game.Active?"Дилер: "+CompanionBlackjack.Total(game.Dealer.Take(1))+" + ?":"Дилер: "+CompanionBlackjack.Total(game.Dealer),14,"Muted"));var dealer=new WrapPanel();for(int i=0;i<game.Dealer.Count;i++)dealer.Children.Add(PlayingCard(game.Active&&i==1?null:game.Dealer[i],game.Active&&i==1,false,game.Active&&i==1?"dealer:hidden":CardVisualKey("dealer",game.Dealer[i])));cardGameBody.Children.Add(dealer);
  cardGameBody.Children.Add(Text("Ты: "+CompanionBlackjack.Total(game.Human),16));var player=new WrapPanel();foreach(var card in game.Human)player.Children.Add(PlayingCard(card,false,false,CardVisualKey("player",card)));cardGameBody.Children.Add(player);
  var actions=new WrapPanel();var hit=Btn("Взять карту",()=>BlackjackAction(()=>game.Hit()));hit.IsEnabled=game.Active;var stand=Btn("Остановиться",()=>BlackjackAction(()=>game.Stand()),true);stand.IsEnabled=game.Active;var twice=Btn("Удвоить",()=>BlackjackAction(()=>game.Double()));twice.IsEnabled=game.CanDouble;actions.Children.Add(hit);actions.Children.Add(stand);actions.Children.Add(twice);cardGameBody.Children.Add(actions);
  cardGameBody.Children.Add(Text("Ближе к 21 без перебора. Туз — 1 или 11. Дилер останавливается на 17, блэкджек платит 3:2. Удвоение — одна карта и завершение хода.",12,"Muted"));var reset=Btn("Начать с 1000 фишек",()=>{blackjackGame=new CompanionBlackjack(1000,Environment.TickCount);SaveBlackjackResult();RenderCardGame();});reset.IsEnabled=!game.Active;cardGameBody.Children.Add(reset);
 }

 string CardVisualKey(string place,CompanionPlayingCard card){return place+":"+card.Suit+":"+card.Rank;}
 void AnimateDealtCard(FrameworkElement card,int index){
  card.Opacity=0;var shift=new TranslateTransform(0,18);card.RenderTransform=shift;
  var delay=TimeSpan.FromMilliseconds(Math.Min(index,8)*40);
  card.BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(260)){BeginTime=delay,FillBehavior=FillBehavior.HoldEnd});
  shift.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(18,0,TimeSpan.FromMilliseconds(320)){BeginTime=delay,EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});
 }
 void AttachCardHover(Button card){
  var lift=new TranslateTransform();card.RenderTransform=lift;
  card.MouseEnter+=(s,e)=>lift.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(-6,TimeSpan.FromMilliseconds(140)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});
  card.MouseLeave+=(s,e)=>lift.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(0,TimeSpan.FromMilliseconds(170)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});
 }
 void CenterCardTable(){
  foreach(FrameworkElement item in cardGameBody.Children){
   var text=item as TextBlock;if(text!=null){text.TextAlignment=TextAlignment.Center;continue;}
   item.HorizontalAlignment=HorizontalAlignment.Center;
   var button=item as Button;if(button!=null)button.MinWidth=180;
  }
 }
}
