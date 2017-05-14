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

using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using TickProc.Properties;

namespace TickProc {
	partial class Program {
		static void EditConfigFile() { Process.Start("\""+Paths.ConfigFile+"\""); }

		static NotifyIcon CreateNotifyIcon() {
			var ni = new NotifyIcon() {
				Icon = Icon.FromHandle(Resources.NotifyIcon.GetHicon()),
				Text = "TickProc",
				ContextMenu = new ContextMenu() {
					MenuItems = {
						// TODO: sentry.io link
						{ "Edit Config File", delegate { EditConfigFile(); } },
						"-",
						{ "E&xit", delegate { Close(); } }
					}
				}
			};
			ni.DoubleClick += delegate { EditConfigFile(); };
			ni.Visible = true;
			return ni;
		}
	}
}
