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
//   Original filename: natix/Sets/Tthreshold/SimpleIntersectionTThreshold.cs
// 
using System;
using System.Collections;
using System.Collections.Generic;

namespace natix.Sets
{
	public class SimpleIntersectionTThreshold : ITThresholdAlgorithm
	{
		public int CompCounter {
			get;
			set;
		}
		IIntersectionAlgorithm intersection;
		public SimpleIntersectionTThreshold (IIntersectionAlgorithm ialg)
		{
			this.intersection = ialg;
		}
		
		public void SearchTThreshold (IList<IList<int>> PostingLists, int T, out IList<int> docs, out IList<short> card)
		{
			if (PostingLists.Count == 0) {
				docs = new List<int> ();
			} else if (PostingLists.Count == 1) {
				docs = PostingLists[0];
			} else {
				docs = new List<int> (this.intersection.Intersection (PostingLists));
			}
			this.CompCounter = this.intersection.CompCounter;
			card = new ListGen<short> ((int i) => (short)PostingLists.Count, docs.Count);
		}
	}
}

