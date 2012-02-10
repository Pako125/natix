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
//   Original filename: natix/Sets/Intersection/SvS.cs
// 
using System;
using System.Collections;
using System.Collections.Generic;
using natix.SortingSearching;

namespace natix.Sets
{
	public class SvS : IIntersectionAlgorithm
	{
		ISearchAlgorithm<int> SearchAlgorithm;
		public int CompCounter {
			get;
			set;
		}
		public SvS (ISearchAlgorithm<int> SearchAlgorithm)
		{
			this.SearchAlgorithm = SearchAlgorithm;
		}
		
		protected void SvS_Fast (IList<int> A, IList<int> B, IList<int> Out)
		{
			Out.Clear ();
			for (int a = 0, b = 0; a < A.Count && b < B.Count; a++) {
				if (this.SearchAlgorithm.Search (A[a], B, out b, b, B.Count)) {
					Out.Add (A[a]);
					b++;
				}
			}
			this.CompCounter = this.SearchAlgorithm.CompCounter;
		}

		public IEnumerable<int> Intersection (IList<IList<int>> postings)
		{
			var pLen = postings.Count;
			Sorting.Sort< IList<int> >(postings, (IList<int> a, IList<int> b) => a.Count - b.Count);
			// postings.Sort ((int[] a, int[] b) => a.Length - b.Length);
			List<int> res = new List<int> ((postings[0].Count >> 1) + 1);
			List<int> tmp = null;
			List<int> swaptmp = null;
			// we always have at least two lists (see Search)
			this.SvS_Fast (postings[0], postings[1], res);
			if (pLen > 2) {
				tmp = new List<int> ((res.Count >> 1) + 1);
			}
			for (int i = 2; i < pLen; i++) {
				this.SvS_Fast (res, postings[i], tmp);
				swaptmp = res;
				res = tmp;
				tmp = swaptmp;
			}
			return res;
		}
	}
}
