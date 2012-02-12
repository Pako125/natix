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
//   Original filename: natix/CompactDS/Bitmaps/SArray64.cs
// 
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using natix.SortingSearching;

namespace natix.CompactDS
{
	/// <summary>
	/// sarray. ALENEX 2007. Okanohara & Sadakane. Practical Rank & Select.
	/// </summary>
	public class SArray64 : RankSelectBase64
	{
		long N;
		public IRankSelect H;
		public ListIFS L;
		
		virtual protected void CreateH (IBitStream BH, short Brank, int Bselect)
		{
			var _H = new DArray ();
			_H.Build (BH, Brank, Bselect);
			//var _H = new GGMN ();
			//_H.Build (BH, Brank);
			this.H = _H;
		}
		
		virtual protected void LoadH (BinaryReader br)
		{
			this.H = new DArray ();
			// this.H = new GGMN ();
			this.H.Load (br);
		}
		
		public override void AssertEquality (IRankSelect64 obj)
		{
			var other = obj as SArray64;
			if (this.N != other.N) {
				throw new ArgumentException (String.Format ("SArray.N inequality. this.N {0}, other.N: {1}",
						this.N, other.N));
			}
			this.H.AssertEquality (other.H);
			Assertions.AssertIList<int> (this.L, other.L, "SArray.L");
		}
	
		public override long Count1 {
			get {
				return this.L.Count;
			}
		}
		
		public int GetNumLowerBits ()
		{
			return this.L.Coder.NumBits;
			//return this.NumLowerBits;
		}
		
		public override long Count {
			get {
				return this.N;
				// return this.L.Count;
			}
		}
				
		public SArray64 ()
		{
		}
		
		long get_mask ()
		{
			return (1 << this.GetNumLowerBits()) - 1;
		}
		
		public void Build (IList<long> orderedList, long n, byte numLowerBits)
		{
			//this.M = orderedList.Count;
			int M = orderedList.Count;
			this.N = n;
			if (M > this.N) {
				Console.WriteLine ("XXXXX LastItem: {0}", orderedList [orderedList.Count - 1]);
				throw new ArgumentOutOfRangeException (String.Format ("SArray N < M, N: {0}, M: {1}", this.N, M));
			}
			if (numLowerBits < 1) {
				numLowerBits = 1;
			}
			// this.NumLowerBits = numLowerBits;
			this.L = new ListIFS (numLowerBits, new BitStream32 ((numLowerBits / 32) * M));
			// Creating bitmaps
			// 2^ (log N - log N / M) = 2^ \log N M / N = M.
			// 2^ (log N - log N / M) = 2^ \log N M / N = M.
			int numpart = (int)Math.Ceiling (Math.Pow (2, (Math.Ceiling (Math.Log (this.N)) - this.GetNumLowerBits ())));
			var H_stream = new BitStream32 (M + (numpart / 32 + 1));
			long mask = this.get_mask ();
			int prevblock = -1;
			for (int i = 0; i < M; i++) {
				this.L.Add ((int)(orderedList [i] & mask));
				int currentblock = (int)(orderedList [i] >> this.GetNumLowerBits ());
				if (prevblock != currentblock) {
					while (prevblock < currentblock) {
						H_stream.Write (false);
						prevblock++;
					}
				}
				H_stream.Write (true);
			}
			//an additional technical zero
			H_stream.Write (false, M - prevblock);
			H_stream.Write (false);
			// Creating indexes for H
			// BH.Seek (0);
			this.CreateH (H_stream, 8, 32);
		}
		
		public static byte Log_N_over_M (long n, long m)
		{
			return (byte)Math.Ceiling( Math.Log(n * 1.0 / m, 2) );
		}
	
		public void Build (IList<long> orderedList, long n)
		{
			byte z = Log_N_over_M(n, orderedList.Count);
			if (z == 0) {
				z++;
			}
			// Console.WriteLine("n: {0}, m: {1}, z: {2}", n, orderedList.Count, z);
			this.Build( orderedList, n, z);
		}

		public void Build (IList<long> orderedList)
		{
			long n = 0;
			if (orderedList.Count > 0) {
				n = orderedList[orderedList.Count - 1] + 1;
			}
			this.Build (orderedList, n);
		}

		public void Build (IBitStream bitmap)
		{
			IList<long> L = new List<long> ();
			for (int i = 0; i < bitmap.CountBits; i++) {
				if (bitmap[i]) {
					L.Add (i);
				}
			}
			this.Build (L, bitmap.CountBits);
		}
		
		public override void Save (BinaryWriter output)
		{
			output.Write ((long)this.N);
			this.H.Save (output);
			this.L.Save (output);
		}
		
		public override void Load (BinaryReader input)
		{
			this.N = input.ReadInt64 ();
			this.LoadH (input);
			var list = new ListIFS ();
			list.Load (input);
			this.L = list;
		}
		
		public override bool Access (long i)
		{
			long rank = this.Rank1 (i);
			return i == this.Select1 (rank);
		}
		
		public override long Rank1 (long pos)
		{
			if (pos < 0L || this.Count1 == 0L) {
				return 0L;
			}
			int rank0_prev = 1 + (int)(pos >> this.GetNumLowerBits ());
			//Console.WriteLine ("XXXXXXXX ========== ");
			//Console.WriteLine ("XXXXXXXX SArray64.Rank1 pos: {0}, rank0_prev: {1}, lower-bits: {2}, m: {3}, n: {4}",
			//                   pos, rank0_prev, this.GetNumLowerBits (), this.Count1, this.Count);
			int pos_prev = this.H.Select0 (rank0_prev);
			// Remember that $pos = rank0 + rank1 - 1$, thus $rank1 = pos - rank0 + 1$
			//int rank1_prevX = this.H.Rank1 (pos_prev); // prevcount = rank1
			int rank1_prev = pos_prev + 1 - rank0_prev;
			//Console.WriteLine ("XXXXXXXX SArray64.Rank1 H.Count: {0}, H.Count1: {1}, rank0_prev: {2}",
			//                   this.H.Count, this.H.Count1, rank0_prev);
			int pos_next = this.H.Select0 (rank0_prev + 1);
			int rank1_next = pos_next - rank0_prev;
			uint pos_masked = (uint)(this.get_mask () & pos);
			int count = rank1_next - rank1_prev;
			//Console.WriteLine ("XXXXXXXX SArray64.Rank1 pos_next: {0}, pos_prev: {1}, rank1_next: {2}, rank1_prev: {3}",
			//                   pos_next, pos_prev, rank1_next, rank1_prev);
			//Console.WriteLine ("XXXXXXXX SArray64.Rank1 pos_masked: {0}", pos_masked);

			if (count < 128) {
				// if (true) {
				// sequential search
				int rank = rank1_prev;
				for (int i = 0; i < count; i++) {
					var u = this.L [rank];
					rank++;
					if (u >= pos_masked) {
						if (u > pos_masked) {
							rank--;
						}
						break;
					}
				}
				return rank;
			} else {
				// binary search
				return 1 + GenericSearch.FindLast<int> ((int)pos_masked, this.L, rank1_prev, rank1_next);
			}
		}
		
		public override long Select1 (long _rank)
		{
			if (_rank <= 0) {
				return -1;
			}
			int rank = (int)_rank;
			long pos_rank = this.H.Select1 (rank);
			// int high_weight = this.H.Rank0 (pos_rank) - 1;
			long high_weight = pos_rank - rank;
			high_weight <<= this.GetNumLowerBits ();
			long ell = this.L [rank - 1];
			return high_weight | ell;
			//return (high_weight << this.GetNumLowerBits ()) | ((long)this.L [rank - 1]);
		}

		public long Select1_UnraveledSymbol (long _rank, long pos_rank)
		{
			if (_rank <= 0) {
				return -1;
			}
			int rank = (int)_rank;
			if (pos_rank == long.MinValue) {
				pos_rank = this.H.Select1 (rank);
			}
			// int high_weight = this.H.Rank0 (pos_rank) - 1;
			long high_weight = pos_rank - rank;
			high_weight <<= this.GetNumLowerBits ();
			long ell = this.L [rank - 1];
			return high_weight | ell;
		}
		
		public ListGenerator64<long> GetAsIList ()
		{
			return new ListGen64<long> (delegate(long i) {
				return this.Select1 (i + 1);
			}, this.Count1);
		}

	}
}
