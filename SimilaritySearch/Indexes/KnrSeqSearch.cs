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
			//var seqbuilder = SequenceBuilders.GetWT_GGMN_BinaryCoding (16);
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
		protected virtual void GetCandidates (IList<ushort> qseq, out IList<int> C_docs, out IList<short> C_sim)
		{
			int knrbound = Math.Abs (this.KnrBoundBuild);
			var len_qseq = qseq.Count;
			var A = new byte[this.MainSpace.Count];
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
			C_docs = new List<int> ();
			C_sim = new List<short> ();
			for (int i = 0; i < A.Length; ++i) {
				if (A [i] == 0) {
					continue;
				}
				C_docs.Add (i);
				C_sim.Add ((short)-A [i]);
			}
			Sorting.Sort<short,int> (C_sim, C_docs);
		}
		
		
		public override IResult KNNSearch (T q, int K, IResult R)
		{
			var qseq = this.GetKnr (q);			
			IList<int> C_docs;
			IList<short> C_sim;
			// this.GetCandidatesIntersection (qseq, out C_docs, out C_sim);
			// this.GetCandidatesRelativeMatches (qseq, out C_docs, out C_sim);
			this.GetCandidates (qseq, out C_docs, out C_sim);
			// possible giving a new order in the candidates
			// res = this.GetOrderingFunctions ().Filter (this, q, qseq, res);
			// computing the final order
			var num_cand = Math.Min (Math.Abs (this.Maxcand), C_docs.Count);
			// Console.WriteLine ("XXXXXXXX MAXCAND: {0}", this.Maxcand);
			if (this.Maxcand < 0) {
				for (int i = 0; i < num_cand; ++i) {		
					var docid = C_docs [i];
					double d = 0;
					if (C_sim != null) {
						d = C_sim [i];
					}
					R.Push (docid, d);
				}
			} else {
				for (int i = 0; i < num_cand; ++i) {		
					var docid = C_docs [i];
					double d = this.MainSpace.Dist (q, this.MainSpace [docid]);
					R.Push (docid, d);
				}
			}
			return R;
		}
	}
}