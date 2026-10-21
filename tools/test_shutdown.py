"""Check real speech-child lifetime after graceful shutdown and abrupt parent exit."""
from pathlib import Path
import ctypes, subprocess
root=Path(__file__).resolve().parent.parent
area=root/'test-output';area.mkdir(exist_ok=True)
source=area/'ShutdownHarness.cs';target=root/'Little Notebook v0.4.0/ShutdownHarness.exe'
source.write_text('''using System;using System.IO;using System.Threading;namespace LittleNotebook {
 public class SpeechSegment {public string Text="Shutdown health check",Language="en";}
 public static class AppPaths {public static string DataDirectory=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"../test-output/health-data");}
 class Harness {static void Main(){using(var service=new NeuralSpeechService()){var task=service.RenderAsync(new SpeechSegment{Text="Health check "+Guid.NewGuid()},CancellationToken.None);Console.WriteLine(service.WorkerId);Console.Out.Flush();Console.ReadLine();}}}
}''',encoding='utf-8')
framework=Path('C:/Windows/Microsoft.NET/Framework64/v4.0.30319')
subprocess.run([str(framework/'csc.exe'),'/nologo','/target:exe','/out:'+str(target),'/r:'+str(framework/'System.Web.Extensions.dll'),str(source),str(root/'src/NeuralSpeechService.cs'),str(root/'src/OwnedProcessJob.cs')],check=True,capture_output=True)
kernel=ctypes.windll.kernel32
kernel.OpenProcess.restype=ctypes.c_void_p
kernel.WaitForSingleObject.argtypes=[ctypes.c_void_p,ctypes.c_uint32]
kernel.CloseHandle.argtypes=[ctypes.c_void_p]
try:
 for crash in [False,True]:
  parent=subprocess.Popen([str(target)],stdin=subprocess.PIPE,stdout=subprocess.PIPE,text=True)
  pid=int(parent.stdout.readline().strip());assert pid>0
  child=kernel.OpenProcess(0x100000,False,pid);assert child
  try:
   if crash:parent.kill()
   else:parent.stdin.write('\n');parent.stdin.flush()
   parent.wait(timeout=8)
   assert kernel.WaitForSingleObject(child,5000)==0,'Speech process survived its game owner'
  finally:kernel.CloseHandle(child)
 print('PASS: actual child processes ended after graceful disposal and forced parent termination.')
 (area/'shutdown-processes.txt').write_text('PASS: actual child processes ended after graceful disposal and forced parent termination. No global process-name killing used.\n')
finally:target.unlink(missing_ok=True)
