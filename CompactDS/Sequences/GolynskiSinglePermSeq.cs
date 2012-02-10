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
//   Original filename: natix/CompactDS/Sequences/GolynskiSinglePermSeq.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using natix.SortingSearching;

namespace natix.CompactDS
{
	public class GolynskiSinglePermSeq : IRankSelectSeq
	{
		IPermutation PERM;
		IRankSelect LENS;
		
#region PROPERTIES
		public int Count {
			get {
				return this.PERM.Count;
			}
		}

		public int Sigma {
			get {
				// we write an additional 1 to the end
				return this.LENS.Count1 - 1;
			}
		}
		
		public PermutationBuilder PermBuilder {
			get;
			set;
		}
		
		public BitmapFromBitStream BitmapBuilder {
			get;
			set;
		}
#endregion
		
		public GolynskiSinglePermSeq ()
		{
			this.PermBuilder = PermutationBuilders.GetSuccRL2CyclicPerms (8);
			this.BitmapBuilder = BitmapBuilders.GetGGMN_wt (16);
		}
		
#region BUILD
		public void BuildPermInvIndex (IList<int> seq, List<int>[] lists)
		{
			var n = seq.Count;
			for (int i = 0; i < n; i++) {
				var symbol = seq [i];
				lists [symbol].Add (i);
			}
		}

		public void Build (IList<int> seq, int sigma)
		{
			var lens = new BitStream32 ();
			var lists = new List<int>[sigma];
			for (int i = 0; i < sigma; i++) {
				lists [i] = new List<int> ();
			}
			var n = seq.Count;
			// TODO replace BuildPermInvIndex per a two passes counting sort like
			this.BuildPermInvIndex (seq, lists);
			var numbits = (int)Math.Ceiling (Math.Log (n, 2));
			var perm = new ListIFS (numbits);
			for (int i = 0; i < sigma; i++) {
				foreach (var u in lists[i]) {
					perm.Add (u);
				}
				lens.Write (true);
				lens.Write (false, lists [i].Count);
				lists [i] = null;
			}
			// a simple hack to simplify the algorithms
			lens.Write (true);
			var bb_lens = new FakeBitmap (lens);
			this.LENS = this.BitmapBuilder (bb_lens);
			this.PERM = this.PermBuilder (perm);
//			Console.WriteLine ("***** GolynskiSinglePermSeq");
//			PrintArray ("PERMUTATION-BUILD:", perm);
//			PrintArray ("PERMUTATION-ENCODED:", this.PERM);
//			Console.WriteLine (new BitStream32 ((this.LENS as GGMN).GetBitBlocks ()));
//			PrintArray ("SEQ:", seq);
		}
		
		void PrintArray (string msg, IList<int> P)
		{
			Console.WriteLine (msg);
			foreach (var x in P) {
				Console.Write (x.ToString () + ",");
			}
			Console.WriteLine ("<end>");

		}
#endregion
		public int Access (int pos)
		{
			
			var inv = this.PERM.Inverse (pos);
			var index = this.LENS.Select0 (inv+1);
			var abs_symbol = this.LENS.Rank1 (index) - 1;
			return abs_symbol;
		}
		
		public int Select (int symbol, int abs_rank)
		{
			if (abs_rank < 1) {
				return -1;
			}
			symbol++;
			var pos = this.LENS.Select1 (symbol);
			var rank0 = pos + 1 - symbol;
			return this.PERM [abs_rank + rank0 - 1];
		}
		
		public int Rank (int symbol, int pos)
		{
			if (pos < 0) {
				return 0;
			}
			symbol++;
			var pos_start = this.LENS.Select1 (symbol);
			var rank0_start = pos_start + 1 - symbol;
			var pos_end = this.LENS.Select1 (symbol + 1);
			var rank0_end = pos_end - symbol;			
			var count = rank0_end - rank0_start;
			// TODO: replace both methods by RL2 primitives to produce a faster rank
			if (count < 32) {
				// fast sequential access for small ranges
				for (int i = 0; i < count; i++) {
					var u = this.PERM [rank0_start + i];
					if (u > pos) {
						return i;
					}
					if (u == pos) {
						return i+1;
					}
				}
				return count;
			} else {
				var list = new ListShiftIndex<int> (this.PERM, rank0_start, count);
				return 1 + GenericSearch.FindFirst<int> (pos, list);
			}
		}
		
		public IRankSelect Unravel (int symbol)
		{
			return new UnraveledSymbol (this, symbol);
		}
		
		#region LOAD_SAVE
		public void Load (BinaryReader Input)
		{
			this.PERM = PermutationGenericIO.Load (Input);
			this.LENS = RankSelectGenericIO.Load (Input);
		}
		
		public void Save (BinaryWriter Output)
		{
			PermutationGenericIO.Save (Output, this.PERM);
			RankSelectGenericIO.Save (Output, this.LENS);
		}
		#endregion
	}
}
