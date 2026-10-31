using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// Windows per-session volume, scoped to process IDs supplied by our WebView2 environment.
// No endpoint/master volume or other applications' sessions are modified.
internal static class CompanionBrowserAudio {
 internal static double Normalize(double percent){return Double.IsNaN(percent)||Double.IsInfinity(percent)?100:Math.Round(Math.Max(0,Math.Min(100,percent)));}
 internal static int Apply(IEnumerable<int> processIds,double percent){
  var ids=new HashSet<int>(processIds);ids.RemoveWhere(id=>id<=0);
  float level=(float)(Normalize(percent)/100);Guid context=new Guid("1A40F633-BF52-486A-8C38-606A78842E25");
  return Visit(ids,(id,volume)=>{float current;if(volume.GetMasterVolume(out current)>=0&&Math.Abs(current-level)>.0001f)Marshal.ThrowExceptionForHR(volume.SetMasterVolume(level,ref context));});
 }
 internal static int Visit(HashSet<int> ids,Action<int,ISimpleAudioVolume> action){
  if(ids==null||ids.Count==0)return 0;
  IMMDeviceEnumerator devices=null;IMMDeviceCollection endpoints=null;int changed=0;
  try{
   devices=(IMMDeviceEnumerator)new MMDeviceEnumerator();
   Marshal.ThrowExceptionForHR(devices.EnumAudioEndpoints(0,1,out endpoints));uint count;Marshal.ThrowExceptionForHR(endpoints.GetCount(out count));
   for(uint index=0;index<count;index++){
    IMMDevice endpoint=null;object managerObject=null;IAudioSessionEnumerator sessions=null;
    try{
     Marshal.ThrowExceptionForHR(endpoints.Item(index,out endpoint));Guid iid=typeof(IAudioSessionManager2).GUID;
     Marshal.ThrowExceptionForHR(endpoint.Activate(ref iid,23,IntPtr.Zero,out managerObject));
     Marshal.ThrowExceptionForHR(((IAudioSessionManager2)managerObject).GetSessionEnumerator(out sessions));int length;Marshal.ThrowExceptionForHR(sessions.GetCount(out length));
     for(int item=0;item<length;item++){
      IAudioSessionControl session=null;
      try{
       if(sessions.GetSession(item,out session)<0)continue;
       var details=session as IAudioSessionControl2;uint processId;
       if(details==null||details.GetProcessId(out processId)<0||processId==0||!ids.Contains((int)processId))continue;
       var volume=session as ISimpleAudioVolume;if(volume==null)continue;
       action((int)processId,volume);changed++;
      }catch(COMException){ /* A session can disappear while a page or audio device changes. */ }
      finally{Release(session);}
     }
    }catch(COMException){ /* Continue with the remaining active output devices. */ }
    finally{Release(sessions);Release(managerObject);Release(endpoint);}
   }
  }finally{Release(endpoints);Release(devices);}
  return changed;
 }
 static void Release(object value){if(value!=null&&Marshal.IsComObject(value))Marshal.ReleaseComObject(value);}
 [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class MMDeviceEnumerator {}
 [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IMMDeviceEnumerator {
  [PreserveSig] int EnumAudioEndpoints(int flow,uint mask,out IMMDeviceCollection collection);
 }
 [ComImport,Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IMMDeviceCollection {
  [PreserveSig] int GetCount(out uint count);
  [PreserveSig] int Item(uint index,out IMMDevice device);
 }
 [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IMMDevice {
  [PreserveSig] int Activate(ref Guid iid,uint context,IntPtr parameters,[MarshalAs(UnmanagedType.IUnknown)]out object result);
 }
 [ComImport,Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IAudioSessionManager2 {
  [PreserveSig] int GetAudioSessionControl(ref Guid id,uint flags,out IAudioSessionControl session);
  [PreserveSig] int GetSimpleAudioVolume(ref Guid id,uint flags,out ISimpleAudioVolume volume);
  [PreserveSig] int GetSessionEnumerator(out IAudioSessionEnumerator sessions);
 }
 [ComImport,Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IAudioSessionEnumerator {
  [PreserveSig] int GetCount(out int count);
  [PreserveSig] int GetSession(int index,out IAudioSessionControl session);
 }
 [ComImport,Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IAudioSessionControl {
  [PreserveSig] int GetState(out int state);
 }
 [ComImport,Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IAudioSessionControl2 {
  [PreserveSig] int GetState(out int state);
  [PreserveSig] int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)]out string name);
  [PreserveSig] int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string name,IntPtr context);
  [PreserveSig] int GetIconPath([MarshalAs(UnmanagedType.LPWStr)]out string path);
  [PreserveSig] int SetIconPath([MarshalAs(UnmanagedType.LPWStr)]string path,IntPtr context);
  [PreserveSig] int GetGroupingParam(out Guid grouping);
  [PreserveSig] int SetGroupingParam(ref Guid grouping,IntPtr context);
  [PreserveSig] int RegisterAudioSessionNotification(IntPtr events);
  [PreserveSig] int UnregisterAudioSessionNotification(IntPtr events);
  [PreserveSig] int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)]out string id);
  [PreserveSig] int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)]out string id);
  [PreserveSig] int GetProcessId(out uint processId);
 }
 [ComImport,Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] internal interface ISimpleAudioVolume {
  [PreserveSig] int SetMasterVolume(float level,ref Guid context);
  [PreserveSig] int GetMasterVolume(out float level);
 }
}
