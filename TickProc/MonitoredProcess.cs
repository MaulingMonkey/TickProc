/* Copyright 2017 MaulingMonkey

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TickProc {
	class MonitoredProcess {
		const int MaxStoreOutput = 100;
		const int StopKillTimeoutMS = 1000;

		struct Output { public string Line; public bool Stderr; }

		readonly object        Mutex = new object();
		readonly Func<Process> CreateProcess;
		readonly Queue<Output> ProcessOutput = new Queue<Output>();

		void TryQueueOutput(Process process, string line, bool stderr) {
			lock (Mutex) {
				if (process != CurrentProcess) return;
				ProcessOutput.Enqueue(new Output() { Line = line, Stderr = stderr });
				while (ProcessOutput.Count > MaxStoreOutput) ProcessOutput.Dequeue();
			}
		}

		bool ShouldRun = true;
		bool ShouldReportExit = true;
		Process CurrentProcess;

		public MonitoredProcess(Func<Process> createProcess) {
			CreateProcess = createProcess;
			CheckProcessRunning();
		}

		public void Dispose() {
			Process toStop = null;
			lock (Mutex) {
				ShouldReportExit = ShouldRun = false;
				toStop = CurrentProcess;
				CurrentProcess = null;
				ProcessOutput.Clear();
			}
			StopOrKillAndClose(toStop);
		}

		void CheckProcessRunning() {
			lock (Mutex) {
				Debug.Assert(CurrentProcess == null || CurrentProcess.HasExited);
				if (ShouldReportExit && CurrentProcess != null && CurrentProcess.HasExited) {
					// TODO: Report unexpected process exit to sentry.io
				}

				if (ShouldRun && (CurrentProcess == null || CurrentProcess.HasExited)) {
					bool success = false;
					try {
						var process = CreateProcess();
						process.Exited += delegate { CheckProcessRunning(); };
						process.EnableRaisingEvents = true;
						try { process.OutputDataReceived += (sender, args) => { TryQueueOutput(process, args.Data, false); }; } catch (Exception e) { ReportException(e); }
						try { process.ErrorDataReceived  += (sender, args) => { TryQueueOutput(process, args.Data, true ); }; } catch (Exception e) { ReportException(e); }
						ProcessOutput.Clear();

						// OK, we started
						CurrentProcess = process;
						success = true; // Don't retry start

						try { CurrentProcess.BeginOutputReadLine(); } catch (Exception e) { ReportException(e); }
						try { CurrentProcess.BeginErrorReadLine();  } catch (Exception e) { ReportException(e); }
					}
					catch (Exception e) when (!Debugger.IsAttached) { ReportException(e); }
					finally { if (!success) Task.Delay(1000).ContinueWith(delegate { CheckProcessRunning(); }); }
				}
			}
		}

		static bool StopOrKillAndClose(Process p) {
			if (p == null) return true;
			try {
				if (p.HasExited) return true;

				if (SendCtrlEvent(p, ConsoleSpecialKey.ControlC    ) && p.WaitForExit(StopKillTimeoutMS)) return true;
				if (SendCtrlEvent(p, ConsoleSpecialKey.ControlC    ) && p.WaitForExit(StopKillTimeoutMS)) return true;
				if (SendCtrlEvent(p, ConsoleSpecialKey.ControlBreak) && p.WaitForExit(StopKillTimeoutMS)) return true;

				try { if (p.CloseMainWindow() && p.WaitForExit(StopKillTimeoutMS)) return true; } catch (Exception) { }
				try { p.Kill(); if (p.WaitForExit(StopKillTimeoutMS)) return true; } catch (Exception) { }
				return false;
			}
			finally { p.Close(); }
		}

		static void ReportException(Exception e) {
			if (Debugger.IsAttached) Debugger.Break();
			// TODO: Report to sentry.io
		}

		static object ConsoleMutex = new object();
		static bool SendCtrlEvent(Process process, ConsoleSpecialKey csk) {
			if (process == null) throw new ArgumentNullException(nameof(process));

			lock (ConsoleMutex) {
				if (!Kernel32.AttachConsole(unchecked((uint)process.Id))) return false;
				try {
					// Note: Console.CancelKeyPress to squelch the event doesn't seem to work here.
					// Perhaps because we started life as a WinForms application instead of a Console one?
					// Even though we attached to a console?  Oh well - Kernel32.SetConsoleCtrlHandler *does* work.
					Kernel32.SetConsoleCtrlHandler(IgnoreCancelKeyPress, true); // FIXME?: return ignored
					try { return Kernel32.GenerateConsoleCtrlEvent(csk, 0); } // FIXME?: return ignored
					finally { Kernel32.SetConsoleCtrlHandler(IgnoreCancelKeyPress, false); } // FIXME?: return ignored
				}
				finally { Kernel32.FreeConsole(); } // FIXME?: return ignored
			}
		}

		private static bool IgnoreCancelKeyPress(uint arg) {
			if (arg == (uint)ConsoleSpecialKey.ControlC    ) return true;
			if (arg == (uint)ConsoleSpecialKey.ControlBreak) return true;
			// Don't handle: Close, Logoff, Shutdown
			return false;
		}

		private static void Console_IgnoreCancelKeyPress(object sender, ConsoleCancelEventArgs e) {
			e.Cancel = true;
		}
	}
}
