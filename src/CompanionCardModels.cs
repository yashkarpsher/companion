using System;
using System.Collections.Generic;
using System.Linq;

public sealed class CompanionPlayingCard {
 public readonly int Rank,Suit;
 public CompanionPlayingCard(int rank,int suit){Rank=rank;Suit=suit;}
 public string Label {get{return Rank==14?"A":Rank==13?"K":Rank==12?"Q":Rank==11?"J":Rank.ToString();}}
 public string Symbol {get{return new[]{"♠","♥","♦","♣"}[Suit];}}
 public bool Red {get{return Suit==1||Suit==2;}}
 public static List<CompanionPlayingCard> Deck(int minimum,Random random){var cards=new List<CompanionPlayingCard>();for(int suit=0;suit<4;suit++)for(int rank=minimum;rank<=14;rank++)cards.Add(new CompanionPlayingCard(rank,suit));for(int i=cards.Count-1;i>0;i--){int j=random.Next(i+1);var card=cards[i];cards[i]=cards[j];cards[j]=card;}return cards;}
}
public sealed class CompanionDurakPair {public CompanionPlayingCard Attack,Defense;}
public sealed class CompanionDurak {
 public readonly bool ThrowIn;
 public readonly List<CompanionPlayingCard> Human=new List<CompanionPlayingCard>(),Bot=new List<CompanionPlayingCard>(),Deck=new List<CompanionPlayingCard>(),Discard=new List<CompanionPlayingCard>();
 public readonly List<CompanionDurakPair> Table=new List<CompanionDurakPair>();
 public readonly CompanionPlayingCard TrumpCard;
 public int Attacker,AttackLimit,Trump;public bool Taking,Finished;public string Message="",Result="";
 public int Defender {get{return 1-Attacker;}}
 public bool Covered {get{return Table.Count>0&&Table.All(p=>p.Defense!=null);}}
 List<CompanionPlayingCard> Hand(int player){return player==0?Human:Bot;}
 public CompanionDurak(bool throwIn,int seed){
  ThrowIn=throwIn;var random=new Random(seed);Deck.AddRange(CompanionPlayingCard.Deck(6,random));TrumpCard=Deck.Last();Trump=TrumpCard.Suit;for(int i=0;i<6;i++){DrawOne(Human);DrawOne(Bot);}int human=Human.Where(c=>c.Suit==Trump).Select(c=>c.Rank).DefaultIfEmpty(99).Min(),bot=Bot.Where(c=>c.Suit==Trump).Select(c=>c.Rank).DefaultIfEmpty(99).Min();Attacker=human==99&&bot==99?random.Next(2):human<bot?0:1;PrepareRound();AdvanceBot();
 }
 void DrawOne(List<CompanionPlayingCard> hand){if(Deck.Count==0)return;hand.Add(Deck[0]);Deck.RemoveAt(0);}
 void DrawUp(int player){while(Hand(player).Count<6&&Deck.Count>0)DrawOne(Hand(player));}
 void PrepareRound(){AttackLimit=Math.Min(ThrowIn?6:1,Hand(Defender).Count);Taking=false;}
 public static bool Beats(CompanionPlayingCard defense,CompanionPlayingCard attack,int trump){return defense.Suit==attack.Suit?defense.Rank>attack.Rank:defense.Suit==trump&&attack.Suit!=trump;}
 public bool CanAttack(CompanionPlayingCard card){return !Finished&&Hand(Attacker).Contains(card)&&Table.Count<AttackLimit&&(Table.Count==0||ThrowIn&&(Taking||Covered)&&Table.Any(p=>p.Attack.Rank==card.Rank||p.Defense!=null&&p.Defense.Rank==card.Rank));}
 public bool CanDefend(CompanionPlayingCard card){var pair=Table.FirstOrDefault(p=>p.Defense==null);return !Finished&&!Taking&&pair!=null&&Hand(Defender).Contains(card)&&Beats(card,pair.Attack,Trump);}
 public bool CanHumanPlay(CompanionPlayingCard card){return Attacker==0?CanAttack(card):CanDefend(card);}
 public bool PlayHuman(CompanionPlayingCard card){if(!CanHumanPlay(card))return false;if(Attacker==0)Attack(card);else Defend(card);AdvanceBot();return true;}
 void Attack(CompanionPlayingCard card){Hand(Attacker).Remove(card);Table.Add(new CompanionDurakPair{Attack=card});}
 void Defend(CompanionPlayingCard card){Hand(Defender).Remove(card);Table.First(p=>p.Defense==null).Defense=card;}
 public bool TakeHuman(){if(Finished||Defender!=0||Taking||!Table.Any(p=>p.Defense==null))return false;Taking=true;AdvanceBot();return true;}
 public bool FinishHuman(){if(Finished||Attacker!=0||!(Taking||Covered))return false;FinishRound(Taking);AdvanceBot();return true;}
 void FinishRound(bool take){
  int previousAttacker=Attacker,previousDefender=Defender;var cards=Table.SelectMany(p=>p.Defense==null?new[]{p.Attack}:new[]{p.Attack,p.Defense}).ToList();if(take)Hand(previousDefender).AddRange(cards);else Discard.AddRange(cards);Table.Clear();DrawUp(previousAttacker);DrawUp(previousDefender);Attacker=take?previousAttacker:previousDefender;
  if(Deck.Count==0&&(Human.Count==0||Bot.Count==0)){Finished=true;Result=Human.Count==0&&Bot.Count==0?"Ничья":Human.Count==0?"Ты выиграл! Бот остался дураком.":"Бот выиграл. Сыграем ещё?";Message=Result;return;}PrepareRound();
 }
 CompanionPlayingCard Cheapest(IEnumerable<CompanionPlayingCard> cards){return cards.OrderBy(c=>c.Suit==Trump?1:0).ThenBy(c=>c.Rank).FirstOrDefault();}
 void AdvanceBot(){
  for(int step=0;step<32&&!Finished;step++){
   if(Attacker==1){
    if(Taking){var extra=Cheapest(Bot.Where(CanAttack));if(extra!=null){Attack(extra);continue;}FinishRound(true);continue;}
    if(Table.Any(p=>p.Defense==null))break;var attack=Cheapest(Bot.Where(CanAttack));if(attack!=null){Attack(attack);break;}if(Covered){FinishRound(false);continue;}break;
   }
   if(Taking)break;if(Table.Any(p=>p.Defense==null)){var defense=Cheapest(Bot.Where(CanDefend));if(defense==null){Taking=true;break;}Defend(defense);break;}break;
  }
  if(Finished)return;Message=Attacker==0?(Taking?(ThrowIn?"Бот берёт. Подкинь подходящие карты или заверши ход.":"Бот берёт. Нажми «Завершить ход»."):Table.Count==0?"Твоя атака. Выбери карту.":(ThrowIn?"Бот отбился. Подкинь карту или нажми «Бито».":"Бот отбился. Нажми «Бито».")):"Тебя атакуют. Выбери карту для защиты или нажми «Взять».";
 }
}

public enum CompanionBlackjackOutcome {None,Win,Loss,Push,Bust}
public sealed class CompanionBlackjack {
 readonly Random random;readonly List<CompanionPlayingCard> fixedDeck;
 readonly List<CompanionPlayingCard> deck=new List<CompanionPlayingCard>();
 public readonly List<CompanionPlayingCard> Human=new List<CompanionPlayingCard>(),Dealer=new List<CompanionPlayingCard>();
 public int CompletedRounds;public CompanionBlackjackOutcome Outcome;
 public int Chips,Bet;public bool Active;public string Message="Выбери ставку и нажми «Раздать».";
 public CompanionBlackjack(int chips,int seed,IEnumerable<CompanionPlayingCard> orderedDeck=null){Chips=Math.Max(0,Math.Min(100000000,chips));random=new Random(seed);if(orderedDeck!=null)fixedDeck=orderedDeck.ToList();}
 public static int Total(IEnumerable<CompanionPlayingCard> cards){int sum=0,aces=0;foreach(var card in cards){if(card.Rank==14){sum+=11;aces++;}else sum+=Math.Min(10,card.Rank);}while(sum>21&&aces>0){sum-=10;aces--;}return sum;}
 public bool CanDouble {get{return Active&&Human.Count==2&&Chips>=Bet;}}
 void Draw(List<CompanionPlayingCard> hand){if(deck.Count==0)throw new InvalidOperationException("В колоде закончились карты.");hand.Add(deck[0]);deck.RemoveAt(0);}
 public bool Start(int bet){
  if(Active||bet<10||bet%10!=0||bet>Chips)return false;Human.Clear();Dealer.Clear();deck.Clear();deck.AddRange(fixedDeck??CompanionPlayingCard.Deck(2,random));Bet=bet;Chips-=bet;Active=true;Outcome=CompanionBlackjackOutcome.None;Draw(Human);Draw(Dealer);Draw(Human);Draw(Dealer);
  bool player=Total(Human)==21,dealer=Total(Dealer)==21;if(player||dealer){if(player&&dealer)Finish(Bet,"У обоих блэкджек. Ставка возвращена.",CompanionBlackjackOutcome.Push);else if(player)Finish(Bet+Bet*3/2,"Блэкджек! Выплата 3:2.",CompanionBlackjackOutcome.Win);else Finish(0,"У дилера блэкджек. Победа дилера.",CompanionBlackjackOutcome.Loss);}else Message="Твой ход: взять карту, остановиться или удвоить.";return true;
 }
 void Finish(int returned,string message,CompanionBlackjackOutcome outcome){Chips+=returned;Active=false;Message=message;Outcome=outcome;CompletedRounds++;}
 public bool Hit(){if(!Active)return false;Draw(Human);int total=Total(Human);if(total>21)Finish(0,"Перебор. Победа дилера.",CompanionBlackjackOutcome.Bust);else if(total==21)Stand();return true;}
 public bool Stand(){if(!Active)return false;while(Total(Dealer)<17)Draw(Dealer);int human=Total(Human),dealer=Total(Dealer);if(dealer>21)Finish(Bet*2,"У дилера перебор. Ты выиграл!",CompanionBlackjackOutcome.Win);else if(human>dealer)Finish(Bet*2,"Ты выиграл!",CompanionBlackjackOutcome.Win);else if(human==dealer)Finish(Bet,"Ничья. Ставка возвращена.",CompanionBlackjackOutcome.Push);else Finish(0,"Победа дилера.",CompanionBlackjackOutcome.Loss);return true;}
 public bool Double(){if(!CanDouble)return false;Chips-=Bet;Bet*=2;Draw(Human);if(Total(Human)>21)Finish(0,"Перебор после удвоения.",CompanionBlackjackOutcome.Bust);else Stand();return true;}
}
