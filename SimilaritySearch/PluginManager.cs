//
//   Copyright 2012 Eric Sadit Tellez <sadit@dep.fie.umich.mx>
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
//   Original filename: natix/natix/SimilaritySearch/PluginManager.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace natix.SimilaritySearch
{
	/// <summary>
	///  A general plugin manager
	/// </summary>
	public class PluginManager
	{
		/// <summary>
		/// Creates a plugin manager object
		/// </summary>
		public PluginManager ()
		{
		}
		/// <summary>
		/// Load recursively all the plugins in the specified directory <see cref="PluginManager.LoadPlugin"/> 
		/// </summary>
		/// 
		/// <param name="dirname">
		/// The directory of plugins
		/// </param>
		/// <returns>The loaded plugins in a dictionary</returns>
		public Dictionary<string, object> LoadPluginDirectory (string dirname)
		{
			Dictionary<string, object> plugins = new Dictionary<string, object> ();
			foreach (string p in Directory.GetFileSystemEntries (dirname)) {
				if (Directory.Exists (p)) {
					this.LoadPluginDirectory (p);
				} else {
					string n = p.ToLower ();
					if (n.EndsWith (".dll") || n.EndsWith (".exe")) {
						try {
							plugins[p] = this.LoadPlugin (p);
						} catch (TypeLoadException) {
							Console.WriteLine ("Error loading '{0}', maybe it's a helper assembly... Ignoring", p);
						} catch (ArgumentException e) {
							Console.WriteLine (e.Message);
						}
					}
				}
			}
			return plugins;	
		}
		
		/// <summary>
		/// Load a single plugin
		/// </summary>
		/// <param name="plugin">
		/// <remarks>
		/// Load a plugin into the application. A plugin is an assembly in with an special type named Plugin, which must have
		/// an constructor without parameters. It can modify IndexFactory and/or SpaceFactory or any other action.
		/// It can be called for any method, at any time, and can without breaking the the previous mentionated simple rules
		/// it can implement any useful interface or class.
		/// 
		/// This architecture needs to implement every plugin in different namespaces.
		/// </remarks>
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// The plugin object
		/// </returns>
		public Object LoadPlugin (string plugin)
		{
			string pluginpath = plugin.Substring (0, plugin.Length - 4);
			string plower = Path.GetFileNameWithoutExtension (plugin).ToLower ();
			if (plower == "natix" || plower == "mono.options" || plower == "powercollections") {
				throw new ArgumentException (String.Format ("Skipping assembly ({0})", plugin));
			}
			var a = System.Reflection.Assembly.LoadFrom (plugin);
			Console.WriteLine ("Loading plugin assembly: '{0}' from '{1}'", a, plugin);
			string ns = Path.GetFileNameWithoutExtension (pluginpath);
			/*foreach (var t in a.GetTypes ()) {
				Console.WriteLine ("Type {0}, {1}", t, ns);
			}*/
			Object o = System.Activator.CreateInstance (pluginpath, ns + ".Plugin");
			return o;
		}
	}
}

