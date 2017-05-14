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

using MaulingMonkey;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace TickProc {
	static partial class Program {
		static ConfigFile CurrentConfig;
		static readonly Dictionary<ConfigFile.RunEntry, MonitoredProcess> MonitoredProcesses = new Dictionary<ConfigFile.RunEntry, MonitoredProcess>();

		static void OnAsyncConfigUpdate(string[] lines, Action<Exception> displayException) {
			lock (Mutex) {
				try { CurrentConfig = ConfigFile.Parse(Paths.ConfigFile, lines); ReportMonitorException(null, displayException); }
				catch (ConfigFileParseException e) { ReportMonitorException(e, displayException); }
				ReparsedConfig = true;
				Monitor.PulseAll(Mutex);
			}
		}

		static void OnAsyncConfigError(Exception e, Action<Exception> displayException) {
			lock (Mutex) {
				//CurrentConfig = null;
				ReportMonitorException(null, displayException);
				Monitor.PulseAll(Mutex);
			}
		}

		static bool ReparsedConfig = true;
		static void MonitorThreadProc(object o) {
			var displayException = (Action<Exception>)o;

			try {
				Watch.FileLines(Paths.ConfigFile,
					lines => OnAsyncConfigUpdate(lines.ToArray(), displayException),
					error => OnAsyncConfigError (error,           displayException));
			}
			// XXX: These are a little more severe than transitory exceptions.  Kill the process?
			catch (IOException e) { ReportMonitorException(e, displayException); }

			try {
				while (!CheckShutdown()) {
					//for (int maxRetries = 10; maxRetries --> 0; ) {
					//	if (CheckShutdown()) return;
					//	try { CurrentConfig = ConfigFile.Load(Paths.ConfigFile); ReportMonitorException(null, displayException); }
					//	catch (IOException) when (maxRetries > 0) { Thread.Sleep(100); continue; }
					//	catch (IOException              e) { ReportMonitorException(e, displayException); }
					//	catch (ConfigFileParseException e) { ReportMonitorException(e, displayException); }
					//}

					lock (Mutex) if (ReparsedConfig && (CurrentConfig != null)) {
						ReparsedConfig = false;

						var newSet = new HashSet<ConfigFile.RunEntry>();
						foreach (var e in CurrentConfig.RunList) newSet.Add(e);

						// Remove old/dead procs
						foreach (var kv in MonitoredProcesses.ToArray()) {
							if (!newSet.Contains(kv.Key)) {
								kv.Value.Dispose();
								MonitoredProcesses.Remove(kv.Key);
							}
						}

						// Add new procs
						foreach (var e in CurrentConfig.RunList) {
							var thisProcEntry = e; // NOTE WELL: Capture scope
							if (MonitoredProcesses.ContainsKey(e)) continue;
							MonitoredProcesses.Add(e, new MonitoredProcess(() => CreateProcFor(thisProcEntry)));
						}
					}

					lock (Mutex) if (!ReparsedConfig) Monitor.Wait(Mutex, 30000); else ReparsedConfig = false;
				}
			}
			catch (Exception e) when (!Debugger.IsAttached) { ReportMonitorException(e, displayException); }
			finally { foreach (var kv in MonitoredProcesses) kv.Value.Dispose(); }
		}

		static Exception LastMonitorException;
		static void ReportMonitorException(Exception e, Action<Exception> displayEx) {
			if (LastMonitorException == e) return; // Unspam (including LME == e == null)

			var sameException // Unspam "equivalent" exceptions
				=  (LastMonitorException != null)
				&& (e                    != null)
				&& (LastMonitorException.GetType() == e.GetType())
				&& (LastMonitorException.Message   == e.Message  );

			LastMonitorException = e;
			if (sameException) return;

			displayEx(e);
		}

		static Process CreateProcFor(ConfigFile.RunEntry e) {
			var split = e.Path.Split(" ".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries);
			var psi = new ProcessStartInfo(split[0], split.Length > 1 ? split[1] : "")
			{
				UseShellExecute        = false,
				CreateNoWindow         = true,
				RedirectStandardError  = true,
				RedirectStandardOutput = true,
				WindowStyle            = ProcessWindowStyle.Hidden
			};
			var p = Process.Start(psi);
			return p;
		}
	}
}
