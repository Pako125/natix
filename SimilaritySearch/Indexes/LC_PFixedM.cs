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
//   Original filename: natix/natix/SimilaritySearch/Indexes/LC_PFixedM.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NDesk.Options;
using natix.CompactDS;
using natix.SortingSearching;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// LC with fixed percentiles (M) and parallel preprocessing
	/// </summary>
	/// <exception cref='ArgumentNullException'>
	/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
	/// </exception>
	public class LC_PFixedM<T> : LC_FixedM<T>
	{
		/// <summary>
		/// Initializes a new instance of the index
		/// </summary>
		public LC_PFixedM () : base()
		{
		}
		
		List<int> build_rest_list;
		
		void BuildSearchKNN (T center, IResult res)
		{
			var n = this.build_rest_list.Count;
			int nullcount = 0;
			var R = new Result (res.K, res.Ceiling);
			Action<int> action = delegate(int i) {
				//for (int i = 0; i < n; ++i) {
				var oid = this.build_rest_list [i];
				if (oid < 0) {
					lock (this) {
						++nullcount;
					}
					return;
				}
				var dist = this.MainSpace.Dist (center, this.MainSpace [oid]);
				lock (R) {
					R.Push (i, dist);
				}
			};
			var pops = new ParallelOptions ();
			pops.MaxDegreeOfParallelism = -1;
			// var w = new TaskFactory ();

			// pops.TaskScheduler = new FixedSizeScheduler ();
			Parallel.For (0, n, pops, action);
			foreach (var p in R) {
				var i = p.docid;
				res.Push (this.build_rest_list [i], p.dist);
				this.build_rest_list [i] = -1;
				++nullcount;
			}
			// an amortized algorithm to handle deletions
			// the idea is to keep the order of review to improve cache
			// 0.33n because it works well for my tests, but it can be any constant proportion
			// of the database
			if (nullcount >= (int)(0.33 * n)) {
				var L = new List<int> (n - nullcount);
				foreach (var u in this.build_rest_list) {
					if (u >= 0) {
						L.Add (u);
					}
				}
				this.build_rest_list = L;
			}
			// Console.WriteLine ("XXX NULLCOUNT END: rest_list.Count: {0}, nullcount: {1}", rest_list.Count, nullcount);
		}
		
		/// <summary>
		/// Builds the LC with fixed bucket size (static version).
		/// </summary>
		public void BuildFixedM (string nick,
		                                IList<int> CENTERS, IList<IList<int>> invindex,
		                                IList<float> COV, int M)
		{
			int iteration = 0;
			int numiterations = this.build_rest_list.Count / M + 1;
			var rand = new Random ();
			Console.WriteLine ("XXX BEGIN BuildFixedM rest_list.Count: {0}", this.build_rest_list.Count);
			while (this.build_rest_list.Count > 0) {
				int center;
				int i;
				do {
					i = rand.Next (this.build_rest_list.Count);
					center = this.build_rest_list [i];
				} while (center < 0);
				this.build_rest_list [i] = -1;
				CENTERS.Add (center);
				IResult res = this.MainSpace.CreateResult (M, false);
				BuildSearchKNN (this.MainSpace [center], res);
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
			Console.WriteLine ("XXX END BuildFixedM rest_list.Count: {0}, iterations: {1}", this.build_rest_list.Count, iteration);
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
			this.build_rest_list = new List<int> (n);
			var invindex = new List<IList<int>> (this.CENTERS.Count);
			this.COV = new List<float> (this.CENTERS.Count);
			// Randomization
			Console.WriteLine ("XXXXX LC_FixedM building {0}, n: {1}", output_name, n);
			for (int i = 0; i < n; ++i) {
				this.build_rest_list.Add (i);
			}
			this.BuildFixedM (output_name, this.CENTERS, invindex, this.COV, M);
			this.SaveLC (output_name, invindex);
			this.CompileLC (output_name);
			Dirty.SaveIndexXml (output_name, this);
		}
	}
}
