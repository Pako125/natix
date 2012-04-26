// 
//  Copyright 2012  sadit
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
using System;
using System.IO;
using System.Collections.Generic;
using natix.CompactDS;
using natix.Sets;
using natix.SortingSearching;

namespace natix.SimilaritySearch
{
	public class KnrSeqSearchSpearmanRho<T> : KnrSeqSearchJaccard<T>
	{
		public KnrSeqSearchSpearmanRho () : base()
		{
		}

		protected override void GetCandidates (IList<ushort> qseq, out IList<int> C_docs, out IList<short> C_sim)
		{
			int knrbound = Math.Abs (this.KnrBoundBuild);
			var len_qseq = qseq.Count;
			var C = new Dictionary<int,int> ();
			var omega = this.IndexRefs.MainSpace.Count >> 1;
			// omega *= omega;
			for (int i = 0; i < len_qseq; ++i) {
				var rs = this.seqindex.Unravel (qseq [i]);
				var count1 = rs.Count1;
				for (int s = 1; s <= count1; ++s) {
					var pos = rs.Select1 (s);
					var docid = pos / knrbound;
					int dist;
					if (!C.TryGetValue (docid, out dist)) {
						dist = (short)(len_qseq * omega);
					}
					var diff = Math.Abs (i - (pos % knrbound));
					C [docid] = dist + diff * diff - omega;
					//C [docid] = dist + diff - omega;
				}
			}
			C_docs = new List<int> (C.Count);
			C_sim = new List<short> (C.Count);
			foreach (var pair in C) {
				C_docs.Add (pair.Key);
				C_sim.Add ((short)Math.Sqrt (pair.Value));
				// C_sim.Add ((short)pair.Value);
			}
			Sorting.Sort<short,int> (C_sim, C_docs);
			// Console.WriteLine ("XXXXXXXXXX SPEARMAN RHO");
		}
	}
}