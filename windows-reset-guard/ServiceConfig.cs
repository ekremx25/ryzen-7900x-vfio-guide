using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
public static class ResetGuardServiceConfig {
 [DllImport("advapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
 static extern IntPtr OpenSCManager(string machine,string database,uint access);
 [DllImport("advapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
 static extern IntPtr OpenService(IntPtr manager,string name,uint access);
 [DllImport("advapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
 static extern bool ChangeServiceConfig2(IntPtr service,uint level,ref uint timeout);
 [DllImport("advapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
 static extern bool QueryServiceConfig2(IntPtr service,uint level,IntPtr buffer,uint size,out uint needed);
 [DllImport("advapi32.dll")] static extern bool CloseServiceHandle(IntPtr handle);
 public static void SetAndVerify(string name,uint milliseconds) {
  IntPtr manager=OpenSCManager(null,null,1);
  if(manager==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());
  try {
   IntPtr service=OpenService(manager,name,3);
   if(service==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());
   try {
    if(!ChangeServiceConfig2(service,7,ref milliseconds))throw new Win32Exception(Marshal.GetLastWin32Error());
    IntPtr data=Marshal.AllocHGlobal(4);
    try {
     uint needed;
     if(!QueryServiceConfig2(service,7,data,4,out needed))throw new Win32Exception(Marshal.GetLastWin32Error());
     if((uint)Marshal.ReadInt32(data)!=milliseconds)throw new Exception("Preshutdown timeout verification failed");
    } finally {Marshal.FreeHGlobal(data);}
   } finally {CloseServiceHandle(service);}
  } finally {CloseServiceHandle(manager);}
 }
}
