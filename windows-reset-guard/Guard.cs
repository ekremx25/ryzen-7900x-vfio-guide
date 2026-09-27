using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

public static class Guard {
 const string Name="EkremAmdResetGuard";
 static readonly string Dir=AppDomain.CurrentDomain.BaseDirectory;
 static readonly object Operation=new object(), StatusLock=new object(), LogLock=new object();
 static readonly ManualResetEvent Done=new ManualResetEvent(false);
 static int stopping=0;
 static IntPtr handle;
 static MainDelegate main=ServiceMain;
 static HandlerDelegate handler=Control;
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void MainDelegate(int argc,IntPtr argv);
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate uint HandlerDelegate(uint control,uint type,IntPtr data,IntPtr context);
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct Entry { public string name; public MainDelegate main; }
 [StructLayout(LayoutKind.Sequential)] struct Status { public uint kind,state,accepted,error,specific,checkpoint,hint; }
 static Status status;
 [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool StartServiceCtrlDispatcher([In] Entry[] entries);
 [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr RegisterServiceCtrlHandlerEx(string name,HandlerDelegate handler,IntPtr context);
 [DllImport("advapi32.dll",SetLastError=true)] static extern bool SetServiceStatus(IntPtr h,ref Status status);
 static void Log(string s) { lock(LogLock) { try { File.AppendAllText(Path.Combine(Dir,"guard.log"),DateTime.Now.ToString("s")+" "+s+Environment.NewLine); } catch {} } }
 static void Report(uint state,uint checkpoint) { lock(StatusLock) { status.kind=16;status.state=state;status.accepted=state==4?0x101u:0;status.checkpoint=checkpoint;status.hint=state==3?10000u:0;SetServiceStatus(handle,ref status); } }
 static bool Device(string action) {
  try {
   var info=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe"));
   info.Arguments="-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \""+Path.Combine(Dir,"Device.ps1")+"\" -Action "+action;
   info.UseShellExecute=false;info.CreateNoWindow=true;
   info.RedirectStandardOutput=true;info.RedirectStandardError=true;
   using(var p=new Process()) {
    p.StartInfo=info;
    p.OutputDataReceived+=(s,e)=>{if(e.Data!=null)Log(e.Data);};
    p.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)Log("ERROR "+e.Data);};
    p.Start();p.BeginOutputReadLine();p.BeginErrorReadLine();
    if(!p.WaitForExit(60000)){p.Kill();p.WaitForExit();Log(action+" timed out");return false;}
    p.WaitForExit();Log(action+" exit="+p.ExitCode);return p.ExitCode==0;
   }
  } catch(Exception e){Log(e.ToString());return false;}
 }
 static void ServiceMain(int argc,IntPtr argv) {
  handle=RegisterServiceCtrlHandlerEx(Name,handler,IntPtr.Zero);
  if(handle==IntPtr.Zero)return;
  Report(4,0);Log("Service started");
  ThreadPool.QueueUserWorkItem(delegate {lock(Operation){if(Volatile.Read(ref stopping)==0)Device("Enable");}});
  Done.WaitOne();
 }
 static uint Control(uint control,uint type,IntPtr data,IntPtr context) {
  if(control==4){lock(StatusLock){SetServiceStatus(handle,ref status);}return 0;}
  if(control!=1 && control!=15)return 120;
  if(Interlocked.Exchange(ref stopping,1)!=0)return 0;
  Report(3,1);
  ThreadPool.QueueUserWorkItem(delegate {
   uint step=1;
   using(var timer=new Timer(delegate {lock(StatusLock){if(status.state==3)Report(3,++step);}},null,3000,3000)){
    lock(Operation){
     if(control==15){Log("PRESHUTDOWN received");if(!Device("Disable"))Log("WARNING: GPU disable failed; next boot may require host restart");}
     else Log("Ordinary service stop: GPU left enabled");
    }
    timer.Change(Timeout.Infinite,Timeout.Infinite);
   }
   Report(1,0);Done.Set();
  });return 0;
 }
 public static void Main(){if(!StartServiceCtrlDispatcher(new Entry[]{new Entry{name=Name,main=main},new Entry()}))Log("Dispatcher error="+Marshal.GetLastWin32Error());}
}
