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
//   Original filename: natix/natix/SimilaritySearch/Indexes/LC_IRNN_CUT_RAD.cs
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
//	public class LC_IRNN_CUT_RAD<T> : LC_RNN<T>
//	{
//		public LC_IRNN_CUT_RAD ()
//		{
//			
//		}
//		
//		
//
//		protected override void SaveLC (string output_name, IList<IList<int>> invindex)
//		{
//			var count = invindex.Count;
//			var perm = new ListGen<int> ((int i) => i, this.MainSpace.Count);
//			float mean_cov;
//			float stddev_cov;
//			_get_stats (this.COV, out mean_cov, out stddev_cov);
//			// float minrad = mean_cov - stddev_cov;
//			float maxrad = mean_cov + stddev_cov;
//			List<float> _COV = new List<float> ();
//			List<int> _CENTERS = new List<int> ();
//			List<IList<int>> _INVINDEX = new List<IList<int>> ();
//			for (int i = 0; i < count; i++) {
//				var list = invindex [i];
//				invindex [i] = null;
//				if (this.COV [i] >= maxrad) {
//					var bucket = new SkipList2<int> (0.5, (int a, int b) => a.CompareTo (b));
//					bucket.Add (this.CENTERS [i], null);
//					foreach (var u in list) {
//						bucket.Add (u, null);
//					}
//					BuildRadius (this.MainSpace, bucket, perm, _CENTERS, _INVINDEX, _COV, mean_cov);
//				} else {
//					_COV.Add (this.COV [i]);
//					_CENTERS.Add (this.CENTERS [i]);
//					_INVINDEX.Add (list);
//				}
//			}
//			this.COV = _COV;
//			this.CENTERS = _CENTERS;
//			base.SaveLC (output_name, _INVINDEX);
//			/*
//			// freeing space (delegating objects to the gc)
//			this.COV = null;
//			this.CENTERS = null;
//			this.INVINDEX = null;
//			invindex = null;
//			// declaring new containers
//			IList<float> __COV;
//			IList<int> __CENTERS;
//			IList<IList<int>> __INVINDEX;
//			// destroying small lists
//			LC_IRNN_CUT_LEN<T>.BuildCutLen (this.MainSpace, 0.25f, false,
//				_COV, _CENTERS, _INVINDEX, out __COV, out __CENTERS, out __INVINDEX);
//			// freeing old structures
//			_COV = null; 
//			_CENTERS = null;
//			_INVINDEX = null;
//			// assigning final structures to this index
//			this.COV = __COV;
//			this.CENTERS = __CENTERS;
//			// saving LC
//			base.SaveLC (output_name, __INVINDEX);*/
//		}
//		
//		public static void BuildSearchRange
//			(Space<T> sp, SkipList2<int> queue, int docid, IResult res, IList<int> PERM, float rad)
//		{
//			foreach (var u in queue.Traverse()) {
//				var dist = sp.Dist (sp [docid], sp [PERM [u]]);
//				if (dist <= rad) {
//					res.Push (u, dist);
//				}
//			}
//		}
//		
//		public static void BuildRadius (Space<T> sp, SkipList2<int> queue, IList<int> PERM,
//			IList<int> CENTERS, IList<IList<int>> invindex, IList<float> COV, float mean_radius)
//		{
//			int iteration = 0;
//			while (queue.Count > 0) {
//				var docid = PERM [queue.RemoveFirst ()];
//				CENTERS.Add (docid);
//				IResult res = sp.CreateResult (queue.Count, false);
//				BuildSearchRange (sp, queue, docid, res, PERM, mean_radius);
//				var list = new List<int> ();
//				invindex.Add (list);
//				double covrad;
//				if (res.Count > 0) {
//					covrad = double.MaxValue;
//				} else {
//					covrad = 0;
//				}
//				foreach (var pair in res) {
//					queue.Remove (pair.docid, null);
//					list.Add (PERM [pair.docid]);
//					covrad = pair.dist;
//				}
//				COV.Add ((float)covrad);
//				if (iteration % 1000 == 0) {
//					Console.WriteLine ("docid {0}, left {1}", docid, queue.Count);
//				}
//				iteration++;
//			}
//		}
//
//		// some statitics to decide what is large and what is small (lists)	
//		public static float _get_mean (IList<float> L)
//		{
//			float acc = 0;
//			foreach (var u in L) {
//				acc += u;
//			}
//			return acc / L.Count;
//		}
//		
//		public static void _get_stats (IList<float> L, out float mean, out float stddev)
//		{
//			mean = _get_mean (L);
//			float sum = 0;
//			foreach (var u in L) {
//				float m = u - mean;
//				sum += m * m;
//			}
//			stddev = (float)Math.Sqrt (sum / L.Count);
//		}		
//	}
//}
//
