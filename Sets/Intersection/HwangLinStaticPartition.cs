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
//   Original filename: natix/natix/Sets/Intersection/HwangLinStaticPartition.cs
// 
using System;
using System.Collections;
using System.Collections.Generic;
using natix.SortingSearching;

namespace natix.Sets
{
	public class HwangLinStaticPartition : IIntersectionAlgorithm
	{
		ISearchAlgorithm<int> SampleSearch;
		ISearchAlgorithm<int> BlockSearch;
		
		public int CompCounter {
			get; set;
		}
		
		public HwangLinStaticPartition (ISearchAlgorithm<int> samplesearch, ISearchAlgorithm<int> blocksearch)
		{
			this.SampleSearch = samplesearch;
			this.BlockSearch = blocksearch;
		}
		
		void HwangLinTwoLists (IList<int> S, int startS, int endS, IList<int> L, int startL, int endL, IList<int> Out)
		{
			int m = endS - startS;
			int n = endL - startL;
			int currStartL = startL;
			ListGen<int> Lsample = new ListGen<int>( (int iS) => L[iS * m + currStartL], (int)Math.Ceiling(n * 1.0 / m));
			for (int i = startS; i < endS; i++) {
				int data = S[i];
				int occPos;
				if (this.SampleSearch.Search(data, Lsample, out occPos, 0, Lsample.Count)) {
					// occPos++;
					Out.Add( data );
					Lsample.Length -= occPos;
					currStartL += occPos * m;
				} else {
					// if occPos == 0: out of range
					if (occPos > 0) {
						occPos--;
						Lsample.Length -= occPos;
						currStartL += occPos * m;
						int currEndL = Math.Min(currStartL + m, endL);
						if (this.BlockSearch.Search(data, L, out occPos, currStartL, currEndL)) {
							Out.Add( data );
						}
					}
				}
			}
		}
		
		public IEnumerable<int> Intersection (IList<IList<int>> postings)
		{
			int k = postings.Count;
			Sorting.Sort<IList<int>> (postings, (IList<int> a, IList<int> b) => a.Count - b.Count);
			List<int> res = new List<int> (1 + (postings[0].Count >> 1));
			List<int> tmp = null;
			List<int> swapAux = null;
			this.HwangLinTwoLists (postings[0], 0, postings[0].Count, postings[1], 0, postings[1].Count, res);
			if (k > 2) {
				tmp = new List<int> (1 + (res.Count >> 1));
			}
			for (int i = 2; i < k; i++) {
				tmp.Clear ();
				this.HwangLinTwoLists (res, 0, res.Count, postings[i], 0, postings[i].Count, tmp);
				swapAux = res;
				res = tmp;
				tmp = swapAux;
			}

			this.CompCounter = this.BlockSearch.CompCounter + this.SampleSearch.CompCounter;
			// if (blocksearch == samplesearch) compcounter /= 2;
			return res;
		}		
	}
}
