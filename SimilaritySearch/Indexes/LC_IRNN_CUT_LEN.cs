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
//   Original filename: natix/natix/SimilaritySearch/Indexes/LC_IRNN_CUT_LEN.cs
// 
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using natix.CompactDS;
//using natix.SortingSearching;
//
//namespace natix.SimilaritySearch
//{
//	public class LC_IRNN_CUT_LEN<T> : LC_IRNN<T>
//	{
//		public LC_IRNN_CUT_LEN ()
//		{
//		}
//		
//		public static void BuildCutLen (Space<T> sp, float alpha, bool touch_large_lists,
//			    IList<float> cov, IList<int> centers, IList<IList<int>> invindex,
//			out IList<float> COV, out IList<int> CENTERS, out IList<IList<int>> INVINDEX)
//		{
//			var count = invindex.Count;
//			var lens = new ListGen<int> ((int i) => invindex [i].Count, count);
//			var perm = new ListGen<int> ((int i) => i, sp.Count);
//			float mean;
//			float stddev;
//			_get_stats (lens, out mean, out stddev);
//			int minlen = (int)(mean - stddev*alpha);
//			int maxlen = (int)Math.Ceiling (mean + stddev*alpha);
//			COV = new List<float> ();
//			CENTERS = new List<int> ();
//			INVINDEX = new List<IList<int>> ();
//			Console.WriteLine ("=== mean: {0}, stddev: {1}, count: {2}, minlen: {3}, maxlen: {4}",
//				mean, stddev, count, minlen, maxlen);
//			SkipList2<int> smaller = new SkipList2<int> (0.5, (int a, int b) => a.CompareTo (b));
//			for (int i = 0; i < count; i++) {
//				var list = invindex [i];
//				if (list.Count < minlen) {
//					// pass
//					smaller.Add (centers [i], null);
//					foreach (var u in list) {
//						smaller.Add (u, null);
//					}
//					invindex [i] = null;
//				} else if (touch_large_lists && list.Count > maxlen) {
//					var bucket = new SkipList2<int> (0.5, (int a, int b) => a.CompareTo (b));
//					bucket.Add (centers [i], null);
//					foreach (var u in list) {
//						bucket.Add (u, null);
//					}
//					LC_FixedM<T>.BuildFixedM ("called from LC_IRNN_CUT_LEN", sp, bucket, perm, CENTERS, INVINDEX, COV, (int)mean);
//					var last_index = INVINDEX.Count - 1;
//					if (INVINDEX [last_index].Count < minlen) {
//						smaller.Add (CENTERS [last_index], null);
//						foreach (var u in INVINDEX[last_index]) {
//							smaller.Add (u, null);
//						}
//						COV.RemoveAt (last_index);
//						CENTERS.RemoveAt (last_index);
//						INVINDEX.RemoveAt (last_index);
//					}					
//				} else {
//					COV.Add (cov [i]);
//					CENTERS.Add (centers [i]);
//					INVINDEX.Add (invindex [i]);
//				}
//			}
//			Console.WriteLine ("--- BEGIN smaller.Count: {0}, centers: {1}", smaller.Count, INVINDEX.Count);
//			LC_FixedM<T>.BuildFixedM ("called from LC_IRNN_CUT_LEN", sp, smaller, perm, CENTERS, INVINDEX, COV, (int)mean);
//			Console.WriteLine ("--- END smaller.Count: {0}, centers: {1}", smaller.Count, INVINDEX.Count);
//		}
//
//		protected override void SaveLC (string output_name, IList<IList<int>> invindex)
//		{
//			IList<float> _COV;
//			IList<int> _CENTERS;
//			IList<IList<int>> _INVINDEX;
//			BuildCutLen (this.MainSpace, 1, true, this.COV, this.CENTERS, invindex, out _COV, out _CENTERS, out _INVINDEX);
//			invindex = null;
//			this.COV = _COV;
//			this.CENTERS = _CENTERS;
//			base.SaveLC (output_name, _INVINDEX);
//		}
//		
//		// some statitics to decide what is large and what is small (lists)	
//		public static float _get_mean (IList<int> L)
//		{
//			float acc = 0;
//			foreach (var u in L) {
//				acc += u;
//			}
//			return acc / L.Count;
//		}
//		
//		public static void _get_stats (IList<int> L, out float mean, out float stddev)
//		{
//			mean = _get_mean (L);
//			float sum = 0;
//			foreach (var u in L) {
//				float m = u - mean;
//				sum += m * m;
//			}
//			stddev = (float)Math.Sqrt (sum / L.Count);
//		}
//		
//	}
//}
//
