using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

public sealed class SavingsGoal {
 public string Id {get;set;}public string Title {get;set;}public double Current {get;set;}public double Target {get;set;}
}
public partial class Companion {
 StackPanel savingsList;Button savingsSave,savingsCancel;string editingSavingsId;
 public static bool ValidSavingsGoals(Profile p){return p!=null&&(p.Goals==null||p.Goals.Count<=500&&p.Goals.All(g=>g!=null&&!String.IsNullOrWhiteSpace(g.Id)&&g.Id.Length<=100&&!String.IsNullOrWhiteSpace(g.Title)&&g.Title.Length<=120&&g.Current>=0&&g.Current<=1e12&&g.Target>0&&g.Target<=1e12&&!Double.IsNaN(g.Current)&&!Double.IsNaN(g.Target))&&p.Goals.Select(g=>g.Id).Distinct().Count()==p.Goals.Count);}
 public static void MigrateSavings(Profile p){
  if(p.Goals==null)p.Goals=new List<SavingsGoal>();
  if(p.GoalsMigrated)return;
  if(p.Goals.Count==0&&!String.IsNullOrWhiteSpace(p.GoalName)&&p.Target>0&&p.Target<=1e12&&p.Current>=0&&p.Current<=1e12)p.Goals.Add(new SavingsGoal{Id=Guid.NewGuid().ToString("N"),Title=p.GoalName.Trim(),Current=p.Current,Target=p.Target});
  p.GoalsMigrated=true;p.GoalName="";p.Current=0;p.Target=0;
 }
 void BuildSavingsFinance(){
  MigrateSavings(data);var p=Stack();p.Children.Add(Text("Цели накопления",20));p.Children.Add(Text("У каждой цели свой накопленный баланс.",12,"Muted"));savingsList=Stack();p.Children.Add(savingsList);
  var editor=Stack();editor.Children.Add(Text("На что копим?",17));editor.Children.Add(Text("Название цели",11,"Muted"));goalName=Input("Например, машина или квартира");goalName.MaxLength=120;editor.Children.Add(goalName);
  editor.Children.Add(Text("Уже накоплено, $",11,"Muted"));current=Input("Накоплено","0");editor.Children.Add(current);editor.Children.Add(Text("Нужно всего, $",11,"Muted"));target=Input("Нужная сумма");editor.Children.Add(target);
  savingsSave=Btn("Добавить цель",()=>Safe(SaveSavingsGoal),true);savingsCancel=Btn("Отмена",ClearSavingsEditor);savingsCancel.Visibility=Visibility.Collapsed;editor.Children.Add(Row(savingsSave,savingsCancel));p.Children.Add(Card(editor));
  p.Children.Add(Text("Доходы и расходы",17));p.Children.Add(Text("Игровые деньги. Балансы целей меняются отдельно.",11,"Muted"));amount=Input("Сумма операции");comment=Input("Комментарий к операции");p.Children.Add(Text("Сумма, $",11,"Muted"));p.Children.Add(amount);p.Children.Add(Text("Комментарий",11,"Muted"));p.Children.Add(comment);p.Children.Add(Row(Btn("+ Доход",()=>Safe(()=>AddEntry(true)),true),Btn("- Расход",()=>Safe(()=>AddEntry(false)))));ledger=Stack();p.Children.Add(ledger);Tab("Финансы",p);
 }
 void ClearSavingsEditor(){editingSavingsId=null;if(goalName==null)return;goalName.Text="";current.Text="0";target.Text="";savingsSave.Content="Добавить цель";savingsCancel.Visibility=Visibility.Collapsed;}
 void SaveSavingsGoal(){
  MigrateSavings(data);string title=goalName.Text.Trim();if(title.Length==0||title.Length>120)throw new Exception("Добавь название цели до 120 символов.");double saved=Number(current,false),total=Number(target,true);
  var goal=data.Goals.FirstOrDefault(g=>g.Id==editingSavingsId);bool added=goal==null;if(added){if(data.Goals.Count>=500)throw new Exception("Можно добавить до 500 целей.");goal=new SavingsGoal{Id=Guid.NewGuid().ToString("N")};data.Goals.Add(goal);}
  string oldTitle=goal.Title;double oldCurrent=goal.Current,oldTarget=goal.Target;goal.Title=title;goal.Current=saved;goal.Target=total;if(!TrySave()){if(added)data.Goals.Remove(goal);else{goal.Title=oldTitle;goal.Current=oldCurrent;goal.Target=oldTarget;}return;}ClearSavingsEditor();RenderSavingsGoals();Say(saved>=total?"Цель достигнута!":"Цель сохранена.");
 }
 void AddSavings(string id,double value){
  var goal=data.Goals.FirstOrDefault(g=>g.Id==id);if(goal==null)throw new Exception("Цель больше не существует.");if(Double.IsNaN(value)||Double.IsInfinity(value)||value<=0||goal.Current+value>1e12)throw new Exception("Введи положительную сумму. Максимальный баланс: 1 триллион.");
  double oldCurrent=goal.Current;goal.Current+=value;if(!TrySave()){goal.Current=oldCurrent;return;}if(editingSavingsId==id)current.Text=goal.Current.ToString(CultureInfo.InvariantCulture);RenderSavingsGoals();Say(goal.Current>=goal.Target?"Цель достигнута!":"Накопления пополнены.");
 }
 void RenderSavingsGoals(){
  if(savingsList==null)return;MigrateSavings(data);savingsList.Children.Clear();if(data.Goals.Count==0)savingsList.Children.Add(Text("Добавь первую цель ниже.",13,"Muted"));
  foreach(var goal in data.Goals){var item=goal;var body=Stack();body.Children.Add(Text(item.Title,17));double progress=Math.Min(100,item.Current/item.Target*100);body.Children.Add(Text(Cash(item.Current)+" / "+Cash(item.Target)+" · "+Math.Floor(progress)+"%"+(item.Current>=item.Target?" · Накоплено!":""),12,"Accent"));var bar=new ProgressBar{Minimum=0,Maximum=100,Value=progress,Height=7,Margin=new Thickness(0,4,0,12)};Resource(bar,Control.ForegroundProperty,"Accent");Resource(bar,Control.BackgroundProperty,"Field");body.Children.Add(bar);
   var deposit=Input("Сумма пополнения");deposit.Width=145;deposit.Margin=new Thickness(0,0,6,6);var actions=new WrapPanel();actions.Children.Add(deposit);actions.Children.Add(Btn("+ Пополнить",()=>Safe(()=>AddSavings(item.Id,Number(deposit,true))),true));actions.Children.Add(Btn("Изменить",()=>{editingSavingsId=item.Id;goalName.Text=item.Title;current.Text=item.Current.ToString(CultureInfo.InvariantCulture);target.Text=item.Target.ToString(CultureInfo.InvariantCulture);savingsSave.Content="Сохранить изменения";savingsCancel.Visibility=Visibility.Visible;goalName.BringIntoView();goalName.Focus();}));actions.Children.Add(Btn("Удалить",()=>{if(MessageBox.Show(this,"Удалить цель «"+item.Title+"»? Остальные цели останутся.","Цель накопления",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)!=MessageBoxResult.Yes)return;data.Goals.Remove(item);if(editingSavingsId==item.Id)ClearSavingsEditor();Save();RenderSavingsGoals();}));body.Children.Add(actions);savingsList.Children.Add(Card(body));
  }
 }
}
