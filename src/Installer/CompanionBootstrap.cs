using System;
using System.IO;
using System.Text;

static class CompanionBootstrap {
 static void Result(string file,string version,string url,string digest,string error){
  Func<string,string> clean=s=>(s??"").Replace("\r"," ").Replace("\n"," ");
  File.WriteAllText(file,"[Release]\r\nVersion="+clean(version)+"\r\nUrl="+clean(url)+"\r\nSHA256="+clean(digest)+"\r\nError="+clean(error)+"\r\n",Encoding.Unicode);
 }
 static string VersionText(string tag){var version=CompanionUpdateCore.ParseVersion(tag);return version.ToString(version.Revision==0?3:4);}
 static int Main(string[] args){
  if(args.Length<3)return 2;
  string report=args[args.Length-1];
  try{
   if(args[0]=="resolve"&&args.Length==3){
    var release=CompanionUpdateCore.ReadRelease(File.ReadAllText(args[1]));
    var asset=CompanionUpdateCore.SelectAsset(release);
    Result(report,VersionText(release.tag_name),asset.browser_download_url,asset.digest.Substring(7),null);
   }else if(args[0]=="verify"&&args.Length==6){
    CompanionUpdateCore.PreparePackage(args[1],args[2],args[3],args[4]);
    Result(report,VersionText(args[4]),null,null,null);
   }else throw new ArgumentException("Неверные параметры подготовки установки.");
   return 0;
  }catch(Exception error){
   try{Result(report,null,null,null,error.Message);}catch{}
   return 1;
  }
 }
}
