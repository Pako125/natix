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
//   Original filename: natix/Sets/Intersection/BaezaYatesIntersection.cs
// 
using System;
using System.Collections;
using System.Collections.Generic;
using natix.SortingSearching;

namespace natix.Sets
{
	public class BaezaYatesIntersection : IIntersectionAlgorithm
	{
		ISearchAlgorithm<int> SearchAlgorithm;
		public int CompCounter {
			get; set;
		}
		
		public BaezaYatesIntersection (ISearchAlgorithm<int> searchalg)
		{
			this.SearchAlgorithm = searchalg;
		}
		
		void BY (IList<int> A, int startA, int endA, IList<int> B, int startB, int endB, IList<int> Out)
		{
			var sizeA = endA - startA;
			var sizeB = endB - startB;
			if (sizeA <= 0 || sizeB <= 0) {
				return;
			}
			if (sizeA > sizeB) {
				this._BY (B, startB, endB, A, startA, endA, Out);
			} else {
				this._BY (A, startA, endA, B, startB, endB, Out);
			}
		}
		
		void _BY (IList<int> A, int startA, int endA, IList<int> B, int startB, int endB, IList<int> Out)
		{
			int midA = (startA + endA) >> 1;
			int medA = A[midA];
			int posMedAinB;
			if (this.SearchAlgorithm.Search (medA, B, out posMedAinB, startB, endB)) {
				Out.Add (medA);
				this.BY (A, startA, midA, B, startB, posMedAinB, Out);
				this.BY (A, midA + 1, endA, B, posMedAinB + 1, endB, Out);
			} else {
				this.BY (A, startA, midA, B, startB, posMedAinB, Out);
				this.BY (A, midA + 1, endA, B, posMedAinB, endB, Out);
			}
		}
		
		public IEnumerable<int> Intersection (IList< IList<int> > postings)
		{
			int k = postings.Count;
			Sorting.Sort< IList<int> >(postings, (IList<int> a, IList<int> b) => a.Count - b.Count);
			
			/*
			 * Reordering largest vs smallest: Deoesn't work well
			List< IList<int> > L = new List<IList<int>>();
			for (int i = 0, c_2 = postings.Count / 2; i < c_2; i++) {
				L.Add( postings[i] );
				var i_inv = postings.Count - 1 - i;
				if (i != i_inv) {
					L.Add (postings[i_inv]);
				}
			}
			postings = L;
			*/
			List<int> res = new List<int> (1 + (postings[0].Count >> 1));
			List<int> tmp = null;
			List<int> swapAux = null;
			this.BY (postings[0], 0, postings[0].Count, postings[1], 0, postings[1].Count, res);
			res.Sort ();
			if (k > 2) {
				tmp = new List<int> (1 + (res.Count >> 1));
			}
			for (int i = 2; i < k; i++) {
				tmp.Clear ();
				this.BY (res, 0, res.Count, postings[i], 0, postings[i].Count, tmp);
				swapAux = res;
				res = tmp;
				tmp = swapAux;
				res.Sort();
			}
			this.CompCounter = this.SearchAlgorithm.CompCounter;
			return res;
		}
	}
}
