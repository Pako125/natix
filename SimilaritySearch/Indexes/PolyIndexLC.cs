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
//   Original filename: natix/natix/SimilaritySearch/Indexes/PolyIndexLC.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NDesk.Options;
using natix.Sets;

namespace natix.SimilaritySearch
{
	public class PolyIndexLC<T> : BaseIndex<T>
	{
		protected IList<LC_RNN<T>> lc_list;
		protected IUnionIntersection ui_alg;
		
		public PolyIndexLC ()
		{
		}
		
		public IList<LC_RNN<T>> GetIndexList ()
		{
			return this.lc_list;
		}
		
		public override void Build (IEnumerable<string> args)
		{
			string space_class = null;
			string db_name = null;
			string output_name = null;
			List<string> lc_list = new List<string> ();
			
			OptionSet ops = new OptionSet () {
				{"spaceclass=", "space class", (v) => space_class = v },
				{"space=", "database", (v) => db_name = v},
				{"index|indexname=", "output index", (v) => output_name = v},
				{"lc=", "LC", (v) => lc_list.Add (v)}
			};
			bool successful = true;
			try {
				ops.Parse (args);
			} catch (Exception e) {
				Console.WriteLine (e.StackTrace);
				successful = false;
			}
			if (!successful || space_class == null || db_name == null || output_name == null || lc_list.Count == 0) {
				Console.WriteLine ("Please check the arguments");
				Console.WriteLine ("spaceclass: {0}, space: {1}, index: {2}, lc_list.Count: {3}",
					space_class, db_name, output_name, lc_list.Count);
				ops.WriteOptionDescriptions (Console.Out);
				throw new ArgumentNullException ();
			}
			this.Build (output_name, lc_list.ToArray (), db_name, space_class);
		}
				
		public  void Build (string indexname, string[] indexlist, string spacename, string spaceclass)
		{
			this.spaceClass = spaceclass;
			this.spaceName = spacename;
			File.WriteAllLines (indexname + ".lc-list", indexlist);
			Dirty.SaveIndexXml (indexname, this);
		}

		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			this.SetMainSpace ((Space<T>)SpaceCache.Load (this.spaceClass, this.spaceName));
			this.ui_alg = new FastUIArray8 (this.MainSpace.Count);
			this.lc_list = new List<LC_RNN<T>> ();
			var L = File.ReadAllLines (name + ".lc-list");
			foreach (var line in L) {
				var I = (LC_RNN<T>)IndexLoader.Load (line);
				this.lc_list.Add (I);
			}
		}

		public override IResult Search (T q, double radius)
		{
			IList<IList<IList<int>>> M = new List<IList<IList<int>>> ();
			IResult R = this.MainSpace.CreateResult (this.MainSpace.Count, false);
			R.EnsureUniqueItems ();
			foreach (var I in this.lc_list) {
				var L = I.PartialSearch (q, radius, R);
				M.Add (L);
			}
			var C = this.ui_alg.ComputeUI (M);
			foreach (int docid in C) {
				var dist = this.MainSpace.Dist (q, this.MainSpace [docid]);
				if (dist <= radius) {
					R.Push (docid, dist);
				}
			}
			return R;
		}
		
		public override IResult KNNSearch (T q, int K, IResult R)
		{
			R.EnsureUniqueItems ();
			byte[] A = new byte[ this.MainSpace.Count ];
			var queue = new Queue<IEnumerator<IList<int>>> ();
			foreach (var I in this.lc_list) {
				var L = I.PartialKNNSearch (q, K, R).GetEnumerator ();
				if (L.MoveNext ()) {				
					queue.Enqueue (L);
				}
			}
			int max = queue.Count;
			while (queue.Count > 0) {
				var L = queue.Dequeue ();
				foreach (var item in L.Current) {
					A [item]++;
					if (A [item] == max) {
						var dist = this.MainSpace.Dist (q, this.MainSpace [item]);
						R.Push (item, dist);
					}
				}
				if (L.MoveNext ()) {
					queue.Enqueue (L);
				}
			}
			return R;			
		}
	}
}
