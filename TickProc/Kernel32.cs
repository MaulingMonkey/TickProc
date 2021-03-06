﻿/* Copyright 2017 MaulingMonkey

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
using System.Runtime.InteropServices;

namespace TickProc {
	static class Kernel32 {
		[DllImport("kernel32.dll")]                                            public static extern bool GenerateConsoleCtrlEvent([MarshalAs(UnmanagedType.U4)] ConsoleSpecialKey dwCtrlEvent, uint dwProcessGroupId);
		[DllImport("kernel32.dll", SetLastError = true)]                       public static extern bool AttachConsole(uint dwProcessId);
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)] public static extern bool FreeConsole();
		[DllImport("kernel32.dll", SetLastError = true)]                       public static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);
		public delegate bool ConsoleCtrlHandler(uint dwCtrlType);
	}
}
