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
//   Original filename: natix/natix/SimilaritySearch/Indexes/LC_FixedM.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NDesk.Options;
using natix.CompactDS;
using natix.SortingSearching;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// LC with fixed percentiles (M)
	/// </summary>
	/// <exception cref='ArgumentNullException'>
	/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
	/// </exception>
	public class LC_FixedM<T> : LC_RNN<T>
	{
		/// <summary>
		/// Initializes a new instance of the index
		/// </summary>
		public LC_FixedM () : base()
		{
		}
		
		
		/// <summary>
		/// Build the index with the specified args.
		/// </summary>
		public override void Build (IEnumerable<string> args)
		{
			string space_class = null;
			string db_name = null;
			string output_name = null;
			int M = 12;
			OptionSet ops = new OptionSet () {
				{"spaceclass=", "space class", (v) => space_class = v },
				{"space=", "database", (v) => db_name = v},
				{"index|indexname=", "output index", (v) => output_name = v},
				{"m|bucketsize=", "bucket size", (v) => M = int.Parse (v)}
			};
			bool successful = true;
			try {
				ops.Parse (args);
			} catch (Exception e) {
				Console.WriteLine (e.StackTrace);
				successful = false;
			}
			if (!successful || space_class == null || db_name == null || output_name == null) {
				Console.WriteLine ("Please check the arguments");
				Console.WriteLine ("spaceclass: {0}, space: {1}, index: {2}",
					space_class, db_name, output_name);
				ops.WriteOptionDescriptions (Console.Out);
				throw new ArgumentNullException ();
			}
			this.Build (output_name, db_name, space_class, M);
		}
		
		/// <summary>
		/// SearchKNN method to be performed at build time
		/// </summary>
		/*public static void BuildSearchKNN (Space<T> sp, SkipList2<int> queue, int docid, IResult res, IList<int> PERM)
		{
			foreach (var u in queue.Traverse()) {
				var dist = sp.Dist (sp [docid], sp [PERM [u]]);
				res.Push (u, dist);
			}
		}*/
		public static void BuildSearchKNN (Space<T> sp, ref List<int> rest_list, T center, IResult res)
		{
			var n = rest_list.Count;
			int nullcount = 0;
			var R = new Result (res.K, res.Ceiling);
			for (int i = 0; i < n; ++i) {
				var oid = rest_list [i];
				if (oid < 0) {
					++nullcount;
					continue;
				}
				var dist = sp.Dist (center, sp [oid]);
				R.Push (i, dist);
			}
			foreach (var p in R) {
				var i = p.docid;
				res.Push (rest_list [i], p.dist);
				rest_list [i] = -1;
				++nullcount;
			}
			// an amortized algorithm to handle deletions
			// the idea is to keep the order of review to improve cache
			// 0.33n because it works well for my tests, but it can be any constant proportion
			// of the database
			if (nullcount >= (int)(0.33 * n)) {
				var L = new List<int> (n - nullcount);
				foreach (var u in rest_list) {
					if (u >= 0) {
						L.Add (u);
					}
				}
				rest_list = L;
			}
			// Console.WriteLine ("XXX NULLCOUNT END: rest_list.Count: {0}, nullcount: {1}", rest_list.Count, nullcount);
		}
		
		/// <summary>
		/// Builds the LC with fixed bucket size (static version).
		/// </summary>
		public static void BuildFixedM (string nick, Space<T> sp, ref List<int> rest_list,
		                                IList<int> CENTERS, IList<IList<int>> invindex,
		                                IList<float> COV, int M)
		{
			int iteration = 0;
			int numiterations = rest_list.Count / M + 1;
			var rand = new Random ();
			Console.WriteLine ("XXX BEGIN BuildFixedM rest_list.Count: {0}", rest_list.Count);
			while (rest_list.Count > 0) {
				int center;
				int i;
				do {
					i = rand.Next (rest_list.Count);
					center = rest_list [i];
				} while (center < 0);
				rest_list [i] = -1;
				CENTERS.Add (center);
				IResult res = sp.CreateResult (M, false);
				BuildSearchKNN (sp, ref rest_list, sp [center], res);
				var list = new List<int> ();
				invindex.Add (list);
				double covrad = double.MaxValue;
				foreach (var p in res) {
					list.Add (p.docid);
					covrad = p.dist;
				}
				COV.Add ((float)covrad);
				if (iteration % 1000 == 0) {
					Console.WriteLine ("docid {0}, iteration {1}/{2}, {3}, date: {4}", center, iteration, numiterations, nick, DateTime.Now);
				}
				Console.WriteLine ("docid {0}, iteration {1}/{2}, {3}, date: {4}", center, iteration, numiterations, nick, DateTime.Now);
				iteration++;
			}
			Console.WriteLine ("XXX END BuildFixedM rest_list.Count: {0}, iterations: {1}", rest_list.Count, iteration);
		}
		
		/// <summary>
		/// Build the LC_FixedM
		/// </summary>

		public override void Build (string output_name, string db_name, string space_class, int M)
		{
			var sp = SpaceCache.Load (space_class, db_name);
			int n = sp.Count;
			this.spaceClass = space_class;
			this.spaceName = db_name;
			this.SetMainSpace ((Space<T>)sp);
			this.CENTERS = new List<int> (n / M + 1);
			var rest_list = new List<int> (n);
			var invindex = new List<IList<int>> (this.CENTERS.Count);
			this.COV = new List<float> (this.CENTERS.Count);
			// Randomization
			Console.WriteLine ("XXXXX LC_FixedM building {0}, n: {1}", output_name, n);
			for (int i = 0; i < n; ++i) {
				rest_list.Add (i);
			}
			BuildFixedM (output_name, this.MainSpace, ref rest_list, this.CENTERS, invindex, COV, M);
			this.SaveLC (output_name, invindex);
			this.CompileLC (output_name);
			Dirty.SaveIndexXml (output_name, this);
		}
	}
}
