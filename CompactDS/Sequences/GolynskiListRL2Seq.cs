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
//   Original filename: natix/natix/CompactDS/Sequences/GolynskiListRL2Seq.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using natix.SortingSearching;

namespace natix.CompactDS
{
	public class GolynskiListRL2Seq : IRankSelectSeq
	{
		SuccRL2CyclicPerms_MRRR PERM;
		IRankSelect LENS;
		
#region PROPERTIES
		public int Count {
			get {
				return this.PERM.Count;
			}
		}
		
		public SuccRL2CyclicPerms_MRRR GetPERM()
		{
			return this.PERM;
		}

		public IRankSelect GetLENS ()
		{
			return this.LENS;
		}

		public int Sigma {
			get {
				// we write an additional 1 to the end
				return this.LENS.Count1 - 1;
			}
		}
		
		/*public Func<IList<int>, IPerms> PermBuilder {
			get;
			set;
		}*/
		
		public BitmapFromBitStream BitmapBuilder {
			get;
			set;
		}
		
		public SuccRL2CyclicPerms_MRRR.BuildParams PermCodingBuildParams {
			get;
			set;
		}
			
#endregion
		
		public GolynskiListRL2Seq ()
		{
			this.BitmapBuilder = BitmapBuilders.GetGGMN_wt (16);
			this.PermCodingBuildParams = new SuccRL2CyclicPerms_MRRR.BuildParams ();
		}
		
#region BUILD
		/// <summary>
		/// Build the specified seq, sigma and t.
		/// </summary>
		public void Build (IList<int> seq, int sigma, short t)
		{
			// A counting sort construction of the permutation
			var counters = new int[sigma];
			foreach (var s in seq) {
				if (s + 1 < sigma) {
					counters [s + 1]++;
				}
			}
			for (int i = 1; i < sigma; i++) {
				counters [i] += counters [i - 1];
			}
			var n = seq.Count;
			var P = new int[n];
			for (int i = 0; i < n; i++) {
				var sym = seq [i];
				var pos = counters [sym];
				P [pos] = i;
				counters [sym] = pos + 1;
			}
			// the bitmap to save the lengths
			var lens = new BitStream32 ();
			int prevc = 0;
			foreach (var c in counters) {
				var len = c - prevc;
				prevc = c;
				lens.Write (true);
				lens.Write (false, len);
			}
			// an additional 1 to the end, to simplify source code
			lens.Write (true);
			var bb_lens = new FakeBitmap (lens);
			this.LENS = this.BitmapBuilder (bb_lens);
			this.PERM = new SuccRL2CyclicPerms_MRRR ();
			this.PERM.Build (P, t, this.PermCodingBuildParams);
		}

//		public void XBuild (IList<int> seq, int sigma, short t)
//		{
//			var lens = new BitStream32 ();
//			var lists = new List<int>[sigma];
//			for (int i = 0; i < sigma; i++) {
//				lists [i] = new List<int> ();
//			}
//			// TODO replace BuildPermInvIndex per a two passes counting sort like
//			var n = seq.Count;
//			for (int i = 0; i < n; i++) {
//				var symbol = seq [i];
//				lists [symbol].Add (i);
//			}
//			var numbits = (int)Math.Ceiling (Math.Log (n, 2));
//			var perm = new ListIFS (numbits);
//			for (int i = 0; i < sigma; i++) {
//				foreach (var u in lists[i]) {
//					perm.Add (u);
//				}
//				lens.Write (true);
//				lens.Write (false, lists [i].Count);
//				lists [i] = null;
//			}
//			// a simple hack to simplify the algorithms
//			lens.Write (true);
//			var bb_lens = new BitmapBuild (lens);
//			this.LENS = this.BitmapBuilder (bb_lens);
//			this.PERM = new SuccRL2CyclicPerms_MRRR ();
//			this.PERM.Build (perm, t);
////			Console.WriteLine ("***** GolynskiSinglePermSeq");
////			PrintArray ("PERMUTATION-BUILD:", perm);
////			PrintArray ("PERMUTATION-ENCODED:", this.PERM);
////			Console.WriteLine (new BitStream32 ((this.LENS as GGMN).GetBitBlocks ()));
////			PrintArray ("SEQ:", seq);
//		}
		
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
		
		public int SelectRL (int symbol, int abs_rank, BitStreamCtx ctx, ref int run_len)
		{
			if (abs_rank < 1) {
				return -1;
			}
			symbol++;
			var pos = this.LENS.Select1 (symbol);
			var rank0 = pos + 1 - symbol;
			return this.PERM.GetListRL2 ().GetItem (abs_rank + rank0 - 1, ctx, ref run_len);
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
			// if (count > 0) {
			//
			if (count > 0) {
				var list = new ListShiftIndex<int> (this.PERM, rank0_start, count);
				return 1 + GenericSearch.FindFirst<int> (pos, list);
				// return this.PERM.GetListRL2 ().FindSumInSortedRange (pos, rank0_start, rank0_end);
			} else {
				return 0;
			}
		}

		public IRankSelect Unravel (int symbol)
		{
			return new UnraveledSymbolGolynskiRL (this, symbol);			
		}
		
		#region LOAD_SAVE
		public void Load (BinaryReader Input)
		{
			this.PERM = new SuccRL2CyclicPerms_MRRR ();
			this.PERM.Load (Input);
			this.LENS = RankSelectGenericIO.Load (Input);
		}
		
		public void Save (BinaryWriter Output)
		{
			this.PERM.Save (Output);
			RankSelectGenericIO.Save (Output, this.LENS);
		}
		#endregion
		
	}
}
