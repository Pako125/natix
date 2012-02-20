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
//   Original filename: natix/SimilaritySearch/Indexes/LC_PFixedM.cs
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
			int max_t = 16;
			Result[] R = new Result[max_t];
			int[] nullC = new int[max_t];
			for (int i = 0; i < max_t; ++i) {
				R [i] = new Result (res.K, res.Ceiling);
			}
			Action<int> action = delegate(int i) {
				//for (int i = 0; i < n; ++i) {
				var t_id = Thread.CurrentThread.ManagedThreadId % max_t;
				var oid = this.build_rest_list [i];
				if (oid < 0) {
					lock (R[t_id]) {
						++nullC [t_id];
					}
					return;
				}
				var dist = this.MainSpace.Dist (center, this.MainSpace [oid]);
				lock (R[t_id]) {
					R [t_id].Push (i, dist);
				}
			};
			var pops = new ParallelOptions ();
			pops.MaxDegreeOfParallelism = -1;
			// var w = new TaskFactory ();
			// pops.TaskScheduler = new FixedSizeScheduler ();
			Parallel.For (0, n, pops, action);
			/*for (int i = 0; i < this.thread_counter.Length; ++i) {
				Console.Write ("{0}, ", this.thread_counter [i]);
			}
			Console.WriteLine ();*/
			int nullcount = 0;
			var _res = new Result (res.K, res.Ceiling);
			for (int x = 0; x < R.Length; ++x) {
				var _R = R [x];
				nullcount += nullC [x];
				foreach (var p in _R) {
					var i = p.docid;
					_res.Push (i, p.dist);
				}
			}
			foreach (var p in _res) {
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
		                         IList<int> CENTERS,
		                         IList<IList<int>> invindex,
		                         IList<float> COV, int M)
		{
			int iteration = 0;
			int numiterations = this.build_rest_list.Count/ M + 1;
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
				this.BuildSearchKNN (this.MainSpace [center], res);
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
