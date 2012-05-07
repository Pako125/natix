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
	public class KnrSeqSearch<T> : KnrPermPrefix<T>
	{
		protected IRankSelectSeq seqindex;
		
		public KnrSeqSearch () : base()
		{
		}

		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			base.FinalizeLoad (name, config);
		}
		
		
		public override void LoadListOfKnrSeq (string name)
		{
			if (!File.Exists (name + ".seqindex")) {
				this.BuildSeqIndex (name);
			}
			using (var Input = new BinaryReader(File.OpenRead (name + ".seqindex"))) {
				this.seqindex = RankSelectSeqGenericIO.Load (Input);
			}
		}
		
		public IList<UInt16> GetKnrSeq (int docid)
		{
			var knrbound = Math.Abs (this.KnrBoundBuild);
			var L = new ushort[knrbound];
			for (int i = 0, start_pos = knrbound * docid; i < knrbound; ++i) {
				L [i] = (ushort)this.seqindex.Access (start_pos + i);
			}
			return L;
		}

		public void BuildSeqIndex (string name)
		{
			Console.WriteLine ("*** Creating {0}.seqindex", name);
			var knrbound = Math.Abs (this.KnrBoundBuild);
			var len = knrbound * this.MainSpace.Count;
			ushort[] S = new ushort[len];
			using (var Input = File.OpenText(name + ".seqspace.seqs")) {
				var v = new ushort[knrbound];
				for (int i = 0; i < len;) {
					var line = Input.ReadLine ();
					PrimitiveIO<ushort>.ReadVectorFromString (line, v, knrbound);
					for (int k = 0; k < knrbound; ++k) {
						S [i] = v [k];
						++i;
					}
				}
			}
			var seqbuilder = SequenceBuilders.GetSeqXLB_SArray64 (16);
			//var seqbuilder = SequenceBuilders.GetGolynskiSucc (16);
			var S_int = new ListGen<int> ((int i) => (int)S [i], S.Length); 
			this.seqindex = seqbuilder (S_int, this.IndexRefs.MainSpace.Count);
			using (var Output = new BinaryWriter(File.Create (name + ".seqindex"))) {
				RankSelectSeqGenericIO.Save (Output, this.seqindex);
			}
			this.seqindex = null;
		}
		 
		/// <summary>
		/// Gets the candidates. 
		/// </summary>
		protected virtual IResult GetCandidatesSmall (IList<ushort> qseq)
		{
			int knrbound = Math.Abs (this.KnrBoundBuild);
			var len_qseq = qseq.Count;
			var n = this.MainSpace.Count;
			var A = new byte[this.MainSpace.Count];
			// var A = new ListIFS (ListIFS.GetNumBits (knrbound));
			// A.Add (0, n);
			for (int i = 0; i < len_qseq; ++i) {
				var rs = this.seqindex.Unravel (qseq [i]);
				var count1 = rs.Count1;
				for (int j = 1; j <= count1; ++j) {
					var pos = rs.Select1 (j);
					if (pos % knrbound == i) {
						var docid = pos / knrbound;
						if (A [docid] == i) {
							A [docid] += 1;
						}
					}
				}
			}
			var res = new ResultTies (Math.Abs (this.Maxcand), false);
			for (int i = 0; i < n; ++i) {
				if (A [i] == 0) {
					continue;
				}
				res.Push (i, -A [i]);
			}
			return res;
		}
		
		protected virtual IResult GetCandidates (IList<ushort> qseq)
		{
			var n = this.MainSpace.Count;
			if (n < 500000) {
				return this.GetCandidatesSmall (qseq);
			}
			int maxcand = Math.Abs (this.Maxcand);
			int knrbound = Math.Abs (this.KnrBoundBuild);
			var len_qseq = qseq.Count;
			var ialg = new BaezaYatesIntersection (new DoublingSearch<int> ());
			IList<int> C = new SortedListRS (this.seqindex.Unravel (qseq [0]));
			int i = 1;
			while (i < len_qseq && C.Count > maxcand) {
				var rs = this.seqindex.Unravel (qseq [i]);
				var I = new ShiftedSortedListRS (rs, -i);
				var L = new List<IList<int>> () {C, I};
				var tmp = ialg.Intersection (L);
				++i;
				if (tmp.Count < maxcand) {
					break;
				}
				C = tmp;
			}
			var res = new ResultTies (int.MaxValue, false);
			foreach (var c in C) {
				if (c % knrbound == 0) {
					res.Push (c / knrbound, 0);
				}
			}
			return res;
		}
		
		public override IResult KNNSearch (T q, int K, IResult R)
		{
			var qseq = this.GetKnr (q);
			// this.GetCandidatesIntersection (qseq, out C_docs, out C_sim);
			// this.GetCandidatesRelativeMatches (qseq, out C_docs, out C_sim);
			var C = this.GetCandidates (qseq);
			// Console.WriteLine ("XXXXXXXX MAXCAND: {0}", this.Maxcand);
			if (this.Maxcand < 0) {
				return C;
			} else {
				foreach (var p in C) {
					var docid = p.docid;
					double d = this.MainSpace.Dist (q, this.MainSpace [docid]);
					R.Push (docid, d);
				}
			}
			return R;
		}
	}
}