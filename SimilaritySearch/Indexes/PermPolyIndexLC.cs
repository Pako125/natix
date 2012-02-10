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
//   Original filename: natix/SimilaritySearch/Indexes/PermPolyIndexLC.cs
// 
//using System;
//using System.IO;
//using System.Collections;
//using System.Collections.Generic;
//using Mono.Options;
//using natix.CompactDS;
//using natix.SortingSearching;
//
//namespace natix.SimilaritySearch
//{
//	public class PermPolyIndexLC<T> : PolyIndexLC<T>
//	{
//		public PermPolyIndexLC () : base()
//		{
//		}
//		
//		
//		public override void Build (IEnumerable<string> args)
//		{
//			string output_name = null;
//			string pmi_name = null;
//			
//			OptionSet ops = new OptionSet () {
//				{"index|indexname=", "output index", (v) => output_name = v},
//				{"pmi|polyindex=", "poly metric-index lc", (v) => pmi_name = v}
//			};
//			bool successful = true;
//			try {
//				ops.Parse (args);
//			} catch (Exception e) {
//				Console.WriteLine (e.StackTrace);
//				successful = false;
//			}
//			if (!successful || pmi_name == null || output_name == null) {
//				Console.WriteLine ("Please check the arguments");
//				Console.WriteLine ("pmi_name: {0}, index: {1}",
//				                   pmi_name, output_name);
//				ops.WriteOptionDescriptions (Console.Out);
//				throw new ArgumentNullException ();
//			}
//			var pmi = (PolyIndexLC<T>)IndexLoader.Load (pmi_name);
//			this.Build (output_name, pmi);
//		}
//		
//		public void Build (string indexname, PolyIndexLC<T> pmi)
//		{
//			this.spaceClass = pmi.spaceClass;
//			this.spaceName = pmi.spaceName;
//			var lc_list = pmi.GetIndexList ();
//			var first_index = lc_list [0];
//			var first_seq = first_index.GetSEQ ();
//			var first_perm = (first_seq as GolynskiListRL2Seq).GetPERM ();
//			var seq = new ListIFS (ListIFS.GetNumBits (first_seq.Sigma));
//			var perm = new ListIFS (ListIFS.GetNumBits (first_seq.Count - 1));
//			var centers = new float[ first_seq.Sigma ];
//			// NOTE:  solo sirve para medir el espacio, faltaria permutar centers y cambiar los
//			// simbolos al nuevo orden en centers
//			int n = pmi.MainSpace.Count;
//			for (int i = 0; i < n; i++) {
//				seq.Add (0);
//				perm.Add (first_perm [i]);
//			}
//			using (var Output = new BinaryWriter(File.Create(indexname + ".bin"))) {
//				int x = 0;
//				foreach (var lc in lc_list) {
//					var S = lc.GetSEQ ();
//					Console.WriteLine ("=== reviewing lc {0}", x);
//					x++;
//					for (int i = 0; i < n; i++) {
//						seq [i] = S.Access (perm [i]);
//					}
//				
//					var rl = new GolynskiListRL2Seq ();
//					rl.Build (seq, first_seq.Sigma, 24);
//					rl.Save (Output);
//				}
//			}
//			Dirty.SaveIndexXml (indexname, this);
//		}
//	}
//}
