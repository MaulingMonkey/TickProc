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

using System.Collections.Generic;

namespace TickProc {
	partial class ConfigFile {
		public struct RunEntry { public string Path; public int No; }
		public readonly List<RunEntry> RunList = new List<RunEntry>();

		public static ConfigFile Load(string path) {
			var config = new ConfigFile();
			var parser = new Parser(config);
			parser.Parse(path);
			return config;
		}
	}
}
