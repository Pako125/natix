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
//   Original filename: natix/natix/SimilaritySearch/Indexes/LC_ParallelSearch.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NDesk.Options;
using natix.CompactDS;
using natix.SortingSearching;
using System.Threading.Tasks;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// LC with a parallel search, can work with any LC
	/// </summary>
	/// <exception cref='ArgumentNullException'>
	/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
	/// </exception>
	public class LC_ParallelSearch<T> : LC_RNN<T>
	{		
		/// <summary>
		/// Initializes a new instance
		/// </summary>
		public LC_ParallelSearch () : base()
		{	
		}
		
		/// <summary>
		/// Search the specified q with radius qrad.
		/// </summary>
		public override IResult Search (T q, double qrad)
		{
			var sp = this.MainSpace;
			var R = sp.CreateResult (int.MaxValue, false);
			int len = this.CENTERS.Count;

			Action<int> S = delegate(int center_id) {
				var dcq = sp.Dist (this.MainSpace [this.CENTERS [center_id]], q);
				if (dcq <= qrad) {
					R.Push (this.CENTERS [center_id], dcq);
				}
				if (dcq <= qrad + this.COV [center_id]) {
					var rs = this.SEQ.Unravel (center_id);
					var count1 = rs.Count1;
					for (int i = 1; i <= count1; i++) {
						var u = rs.Select1 (i);
						var r = sp.Dist (q, sp [u]);
						if (r <= qrad) {
							lock (R) {
								R.Push (u, r);
							}
						}
					}
				}
			};
			var pops = new ParallelOptions ();
			pops.MaxDegreeOfParallelism = -1;
			Parallel.For (0, len, pops, S);
			return R;
		}
		
		/// <summary>
		/// KNN search.
		/// </summary>
		public override IResult KNNSearch (T q, int K, IResult R)
		{
			var sp = this.MainSpace;
			int len = this.CENTERS.Count;
			var C = this.MainSpace.CreateResult (len, false);
			//for (int center = 0; center < len; center++) {
			Action<int> S = delegate(int center) {
				var dcq = sp.Dist (this.MainSpace [this.CENTERS [center]], q);
				lock (C) {
					R.Push (this.CENTERS [center], dcq);
					//var rm = Math.Abs (dcq - this.COV [center]);
					if (dcq <= R.CoveringRadius + this.COV [center]) {
						// if (rm <= R.CoveringRadius) {
						C.Push (center, dcq);
					}
					// C.Push (center, rm);
				}
			};
			var pops = new ParallelOptions ();
			pops.MaxDegreeOfParallelism = -1;
			Parallel.For (0, len, pops, S);
			//foreach (ResultPair pair in C) {
			Action<ResultPair> Scenters = delegate(ResultPair pair) {
				var dcq = pair.dist;
				var center = pair.docid;
				if (dcq <= R.CoveringRadius + this.COV [center]) {
					var rs = this.SEQ.Unravel (center);
					var count1 = rs.Count1;
					for (int i = 1; i <= count1; i++) {
						var u = rs.Select1 (i);
						var r = sp.Dist (q, sp [u]);
						//if (r <= qr) { // already handled by R.Push
						lock (R) {
							R.Push (u, r);
						}
					}
				}
			};
			pops = new ParallelOptions ();
			pops.MaxDegreeOfParallelism = -1;
			Parallel.ForEach<ResultPair> (C, pops, Scenters);
			return R;
		}
		
//		// methods for partial searching (poly metric-index)
//		/// <summary>
//		/// Partial KNN search
//		/// </summary>
//		public IEnumerable<IList<int>> PartialKNNSearch (T q, int K, IResult R)
//		{
//			var sp = this.MainSpace;
//			int len = this.CENTERS.Count;
//			var C = this.MainSpace.CreateResult (len, false);
//			for (int center = 0; center < len; center++) {
//				var dcq = sp.Dist (this.MainSpace [this.CENTERS [center]], q);
//				R.Push (this.CENTERS [center], dcq);
//				if (dcq <= R.CoveringRadius + this.COV [center]) {
//					C.Push (center, dcq);
//				}
//			}
//			foreach (ResultPair pair in C) {
//				var dcq = pair.dist;
//				var center = pair.docid;
//				if (dcq <= R.CoveringRadius + this.COV [center]) {
//					yield return new SortedListRS(this.SEQ.Unravel(center));
//				}
//			}
//		}
//		
//		/// <summary>
//		/// Partial radius search
//		/// </summary>
//
//		public IList<IList<int>> PartialSearch (T q, double qrad, IResult R)
//		{
//			var sp = this.MainSpace;
//			int len = this.CENTERS.Count;
//			IList<IList<int>> output_list = new List<IList<int>> ();
//			for (int center_id = 0; center_id < len; center_id++) {
//				var dcq = sp.Dist (this.MainSpace [this.CENTERS [center_id]], q);
//				if (dcq <= qrad) {
//					R.Push (this.CENTERS [center_id], dcq);
//				}
//				if (dcq <= qrad + this.COV [center_id]) {
//					// output_list.Add (this.invindex [center_id]);
//					output_list.Add (new SortedListRS (this.SEQ.Unravel(center_id)));
//				}
//			}
//			return output_list;
//		}
	}
}
