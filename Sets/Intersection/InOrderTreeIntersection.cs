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
//   Original filename: natix/natix/Sets/Intersection/InOrderTreeIntersection.cs
// 
using System;
using System.Collections;
using System.Collections.Generic;
using natix.SortingSearching;

namespace natix.Sets
{
	public class InOrderTreeIntersection : IIntersectionAlgorithm
	{
		protected ISearchAlgorithm<int> SearchAlgorithm;
		public int CompCounter {
			get; set;
		}
		
		public InOrderTreeIntersection (ISearchAlgorithm<int> searchalg)
		{
			this.SearchAlgorithm = searchalg;
		}
		
		protected void InOrderI (IList<int> A, int startA, int endA, IList<int> B, int startB, int endB, IList<int> Out)
		{
			var sizeA = endA - startA;
			var sizeB = endB - startB;
			if (sizeA <= 0 || sizeB <= 0) {
				return;
			}
			if (sizeA > sizeB) {
				this._InOrderI (B, startB, endB, A, startA, endA, Out);
			} else {
				this._InOrderI (A, startA, endA, B, startB, endB, Out);
			}
		}
		
		void _InOrderI (IList<int> A, int startA, int endA, IList<int> B, int startB, int endB, IList<int> Out)
		{
			int midA = (startA + endA) >> 1;
			int medA = A[midA];
			int posMedAinB;
			if (this.SearchAlgorithm.Search (medA, B, out posMedAinB, startB, endB)) {
				this.InOrderI (A, startA, midA, B, startB, posMedAinB, Out);
				Out.Add (medA);
				this.InOrderI (A, midA + 1, endA, B, posMedAinB + 1, endB, Out);
			} else {
				this.InOrderI (A, startA, midA, B, startB, posMedAinB, Out);
				this.InOrderI (A, midA + 1, endA, B, posMedAinB, endB, Out);
			}
		}

		public virtual IEnumerable<int> Intersection (IList< IList<int> > postings)
		{
			int k = postings.Count;
			Sorting.Sort< IList<int> >(postings, (IList<int> a, IList<int> b) => a.Count - b.Count);
			List<int> res = new List<int> (1 + (postings[0].Count >> 1));
			List<int> tmp = null;
			List<int> swapAux = null;
			this.InOrderI(postings[0], 0, postings[0].Count, postings[1], 0, postings[1].Count, res);
			if (k > 2) {
				tmp = new List<int> (1 + (res.Count >> 1));
			}
			for (int i = 2; i < k; i++) {
				tmp.Clear();
				this.InOrderI (res, 0, res.Count, postings[i], 0, postings[i].Count, tmp);
				swapAux = res;
				res = tmp;
				tmp = swapAux;
			}
			
			this.CompCounter = this.SearchAlgorithm.CompCounter;
			return res;
		}
		
		public IEnumerable<int> IntersectionWithEvents (IList<IList<int>> postings, bool doSortByLength, Func< int, IList<int>, int> callback)
		{
			int k = postings.Count;
			if (doSortByLength) {
				Sorting.Sort<IList<int>> (postings, (IList<int> a, IList<int> b) => a.Count - b.Count);
			}
			List<int> res = new List<int> (1 + (postings[0].Count >> 1));
			List<int> tmp = null;
			List<int> swapAux = null;
			this.InOrderI (postings[0], 0, postings[0].Count, postings[1], 0, postings[1].Count, res);
			callback(1, res);
			if (k > 2) {
				tmp = new List<int> (1 + (res.Count >> 1));
			}
			for (int i = 2; i < k; i++) {
				tmp.Clear ();
				this.InOrderI (res, 0, res.Count, postings[i], 0, postings[i].Count, tmp);
				swapAux = res;
				res = tmp;
				tmp = swapAux;
				callback(i, res);
			}
			
			this.CompCounter = this.SearchAlgorithm.CompCounter;
			return res;
		}

	}
}
