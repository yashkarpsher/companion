using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

static class CompanionUpdaterProgram {
 static void WaitForParent(CompanionUpdateJob job){
  Process parent;try{parent=Process.GetProcessById(job.ParentPid);}catch(ArgumentException){return;}
  using(parent){if(parent.HasExited)return;string path;try{path=parent.MainModule.FileName;}catch(InvalidOperationException){if(parent.HasExited)return;throw;}catch(System.ComponentModel.Win32Exception){if(parent.HasExited)return;throw;}if(!String.Equals(Path.GetFullPath(path),Path.Combine(Path.GetFullPath(job.TargetDirectory),CompanionUpdateCore.MainExe),StringComparison.OrdinalIgnoreCase))throw new IOException("Запущенная программа не совпадает с папкой обновления.");if(!parent.WaitForExit(60000))throw new IOException("Программа не закрылась. Закрой её и повтори обновление.");}
 }
 [STAThread]static void Main(string[] args){
  Application.EnableVisualStyles();if(args.Length!=1){MessageBox.Show("Обновление запускается из настроек Companion.","Companion");return;}
  CompanionUpdateJob job;try{job=new JavaScriptSerializer().Deserialize<CompanionUpdateJob>(File.ReadAllText(args[0]));CompanionUpdateCore.ValidateJob(job);}catch(Exception e){MessageBox.Show("Не удалось подготовить обновление: "+e.Message,"Companion");return;}
  var form=new Form{Text="Обновление Companion",Width=500,Height=170,StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Color.FromArgb(24,28,35),ForeColor=Color.White,Font=new Font("Segoe UI",10)};
  var status=new Label{Text="Закрываем Companion…",AutoSize=false,Dock=DockStyle.Fill,Padding=new Padding(20)};form.Controls.Add(status);bool busy=true;form.FormClosing+=(s,e)=>{if(busy)e.Cancel=true;};Action<string> report=text=>{if(!form.IsDisposed)form.BeginInvoke((Action)(()=>status.Text=text));};
  form.Shown+=(s,e)=>{var worker=new Thread(()=>{
   Mutex mutex=null;bool owned=false,restart=false;string error=null;
   try{
    WaitForParent(job);
    mutex=new Mutex(false,"Local\\MajesticDailyCompanionV1");try{owned=mutex.WaitOne(0);}catch(AbandonedMutexException){owned=true;}if(!owned)throw new IOException("Companion снова открыт. Закрой его и повтори обновление.");restart=true;
    CompanionUpdateCore.Apply(job,report);report("Готово. Открываем Companion…");
   }catch(Exception ex){error=ex.Message;try{File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"update-error.txt"),ex.ToString());}catch{}}
   finally{if(owned)mutex.ReleaseMutex();if(mutex!=null)mutex.Dispose();}
   if(restart)try{Process.Start(new ProcessStartInfo{FileName=Path.Combine(job.TargetDirectory,CompanionUpdateCore.MainExe),WorkingDirectory=job.TargetDirectory,UseShellExecute=true});}catch(Exception ex){error=(error??"")+" Не удалось открыть программу: "+ex.Message;}
   form.BeginInvoke((Action)(()=>{busy=false;if(error!=null)MessageBox.Show(form,error,"Обновление Companion",MessageBoxButtons.OK,MessageBoxIcon.Information);form.Close();}));
  });worker.IsBackground=false;worker.Start();};Application.Run(form);
 }
}
