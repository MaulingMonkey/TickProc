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
using System.IO;

namespace TickProc {
	partial class ConfigFile {
		class Parser {
			readonly ConfigFile ConfigFile;
			readonly Dictionary<string, int> RunCounts = new Dictionary<string, int>();

			string ParsingPath;
			int    ParsingLineNo;

			public Parser(ConfigFile configFile) {
				ConfigFile = configFile;
			}

			void ParseSet(string rest) {
				throw new ConfigFileParseException(ConfigFile, ParsingPath, ParsingLineNo, "Action 'set' not yet implemented");
			}

			void ParseShadow(string rest) {
				throw new ConfigFileParseException(ConfigFile, ParsingPath, ParsingLineNo, "Action 'shadow' not yet implemented");
			}

			void ParseRun(string rest) {
				var path = rest;

				int prevCount = 0;
				if (!RunCounts.TryGetValue(path, out prevCount)) RunCounts.Add(path, 1);
				else                                             RunCounts[path] = prevCount + 1;
				var re = new RunEntry() { Path = path, No = prevCount };
				ConfigFile.RunList.Add(re);
			}

			void ParseUnknown(string action, string rest) {
				throw new ConfigFileParseException(ConfigFile, ParsingPath, ParsingLineNo, "Unrecognized action '"+action+"'");
			}

			public void Parse(string path) { Parse(path, null); }
			public void Parse(string path, string[] linesOverride) {
				try {
					ParsingPath = path;
					ParsingLineNo = 0;

					var lines = linesOverride ?? File.ReadLines(path);
					foreach (var line in lines) {
						++ParsingLineNo;
						if (string.IsNullOrWhiteSpace(line)) continue;

						// I should probably settle on a single commenting style... but eh.
						var trimmed = line.TrimStart(' ','\t');
						if (trimmed.StartsWith("#" )) continue;
						if (trimmed.StartsWith(";" )) continue;
						if (trimmed.StartsWith("//")) continue;

						var tokens = line.Split(new[] {' '}, 2);
						if (tokens.Length == 0) continue; // ...impossible?
						var rest = tokens.Length > 1 ? tokens[1] : null;

						var action = tokens[0].ToLowerInvariant();
						switch (action) {
						case "set":    ParseSet(rest);             break;
						case "run":    ParseRun(rest);             break;
						case "shadow": ParseShadow(rest);          break;
						default:       ParseUnknown(action, rest); break;
						}
					}
				} finally {
					ParsingPath = null;
					ParsingLineNo = 0;
				}
			}
		}
	}
}
