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
	public class KnrSeqSearchJaccard<T> : KnrSeqSearch<T>
	{
		public KnrSeqSearchJaccard () : base()
		{
		}
		 
		protected override IResult GetCandidates (IList<ushort> qseq)
		{
			int knrbound = Math.Abs (this.KnrBoundBuild);
			var len_qseq = qseq.Count;
			// var C = new Dictionary<int,short> ();
			var C = new byte[this.MainSpace.Count];
			for (int i = 0; i < len_qseq; ++i) {
				var rs = this.seqindex.Unravel (qseq [i]);
				var count1 = rs.Count1;
				for (int s = 1; s <= count1; ++s) {
					var pos = rs.Select1 (s);
					var docid = pos / knrbound;
					// short dist;
					/*if (!C.TryGetValue (docid, out dist)) {
						dist = 0;
					}
					C [docid] = (short)(dist + 1);
					*/
					C [docid] += 1;
				}
			}
			var res = new ResultTies (Math.Abs (this.Maxcand), false);
			/*
			foreach (var pair in C) {
				res.Push (pair.Key, -pair.Value);
			}*/
			for (int i = 0; i < C.Length; ++i) {
				if (C [i] > 0) {
					res.Push (i, -C [i]);
				}
			}
			return res;
		}		
	}
}