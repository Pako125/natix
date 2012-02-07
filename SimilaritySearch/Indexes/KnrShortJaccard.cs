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
//   Original filename: natix/natix/SimilaritySearch/Indexes/KnrShortJaccard.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Mono.Options;
using natix.CompactDS;
using natix.SortingSearching;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Short representation of Jaccard sequences
	/// </summary>
	public class KnrShortJaccard<T> : Knr<T>
	{
		IList<UInt64> KnrSeq;
		UInt16 KnrSeqBitLength = 64;	
		/// <summary>
		/// The length of each sequence
		/// </summary>
		IList<UInt16> KnrLength; // knr's size is fixed this should be an scalar
		
		/// <summary>
		/// Constructor
		/// </summary>
		public KnrShortJaccard() : base()
		{
		}
		
		/// <summary>
		/// Wrap the knr sequence
		/// </summary>
		/// <param name="a">
		protected override IList<UInt16> KnrWrap (IList<UInt16> a)
		{
			return a;
		}
		
		/// <summary>
		/// Knr distance. Not implemented
		/// </summary>
		public override double KnrDist (IList<UInt16> a, IList<UInt16> b)
		{
			throw new System.NotImplementedException ("This method must not be called");
		}
		/// <summary>
		/// Integer hashing, please search for credits
		/// http://www.concentric.net/~Ttwang/tech/inthash.htm
		/// </summary>
		/// <param name="a">
		/// A <see cref="UInt32"/>
		/// </param>
		/// <returns>
		/// A <see cref="Int32"/>
		/// </returns>
		Int32 IntHash(UInt16 a)
		{
			/*a = (a+0x7ed55d16) + (a<<12);
			a = (a^0xc761c23c) ^ (a>>19);
			a = (a+0x165667b1) + (a<<5);
			a = (a+0xd3a2646c) ^ (a<<9);
			a = (a+0xfd7046c5) + (a<<3);
			a = (a^0xb55a4f09) ^ (a>>16);
			return a;*/
			return Math.Abs(a.GetHashCode());
		}

		/// <summary>
		/// Finalize load. <see cref="natix.IndexLoader.Load"/>
		/// </summary>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			this.KnrSeq = new List<UInt64> ();
			this.LoadSpaceAndRefs (name, this.spaceClass, this.spaceName, this.IndexRefsName, false);
			Console.WriteLine ("==== Loading inverted file from {0}.data", name);
			SequenceSpace<ushort> ss = new SequenceSpace<ushort> ();
			ss.LoadFromFile (name + ".seqspace");
			//long len = b.BaseStream.Length;
			int sL = ss.Count;
			// (int)(len / (this.knr * sizeof(UInt16)));
			UInt64 knrseq;
			// sizeof(knrseq) * 8 
			//Dictionary<UInt32, int> counter = new Dictionary<UInt32, int> ();
			this.KnrLength = new UInt16[this.MainSpace.Count];
			for (int docid = 0; docid < sL; docid++) {
				var qseq = ss[docid];
				this.KnrLength[docid] = (ushort)ss.Count;
				knrseq = 0;
				for (int m = 0; m < qseq.Count; m++) {
					knrseq |= (((UInt64)1) << ((IntHash (qseq[m]) % this.KnrSeqBitLength)));
					//knrseq |= (UInt32)(1u << ((IntHash (b.ReadUInt16 ()) % 16)));
				}
				//Console.WriteLine ("LoadKnrSeq: {0}", knrseq);
				this.KnrSeq.Add (knrseq);
				/*try {
					counter[knrseq] += 1;
				} catch (KeyNotFoundException) {
					counter[knrseq] = 1;
				}*/			

				
				/* BinaryWork.Read (b, D, 0, knrlen); */
				if ((docid % 100000) == 0) {
					Console.WriteLine ("docid {0}, advance {1:0.00}%", docid, docid * 100.0 / sL);
				}
			}
			Console.WriteLine ("==== Done");
		}
		/// <summary>
		/// Encodes the Knr sequence into a single UInt16
		/// </summary>
		public UInt64 EncodeKnrSeq (IList<UInt16> seq)
		{
			UInt64 knrseq = 0;
			int shift;
			for (int m = 0; m < seq.Count; m++) {
				shift = (IntHash (seq[m]) % this.KnrSeqBitLength);
				knrseq |= ((UInt64)1u) << shift;
				//Console.WriteLine (shift.ToString());
			}
			// Console.WriteLine (":::::> ShortJaccard: {0}, String: {1}", knrseq, BinaryHammingSpace.ToAsciiString (knrseq));
			return knrseq;
		}

		/// <summary>
		/// KNN Search
		/// </summary>
		/// <param name="q">
		/// The query object
		/// </param>
		/// <param name="k">
		/// The number of nearest neighbor
		/// </param>
		/// <returns>
		/// The result set <see cref="Result"/>
		/// </returns>
		public override IResult KNNSearch (T q, int k, IResult R)
		{
			IList<UInt16> qseq = this.GetKnr (q, false);
			UInt64 knrseq = this.EncodeKnrSeq (qseq);
			IResult cand = new ResultTies (Math.Abs (this.Maxcand), true);
			//double jacc;
			int I;// U;
			// Console.WriteLine("===== knrseq: {0}", knrseq);
			for (int docid = 0, len = this.KnrSeq.Count; docid < len; docid++) {
				UInt64 u = knrseq & this.KnrSeq [docid];
				I = 0;
				for (int i = 0; i < 8; i++) {
					I += Bits.PopCount8 [u & 0xFF];
					u >>= 8;
				}
				//I = BinaryHammingSpace.HammingTable16 [u & 0xFFFF];	
				//I += BinaryHammingSpace.HammingTable16 [(u >> 16) & 0xFFFF];
				//I += BinaryHammingSpace.HammingTable16 [(u >> 32) & 0xFFFF];
				//I += BinaryHammingSpace.HammingTable16 [(u >> 48)];
				
				//U = this.KnrLength[docid] + qseq.Length - I;
				//jacc = 1.0 - ((double)I) / U;
				//cand.Push(docid, jacc);
				//Console.WriteLine ("I: {0}, knrseq: {1}, knrseq[{2}]: {3}", I, knrseq, docid, this.KnrSeq[docid]);
				cand.Push (docid, -I);
			}
			if (this.Maxcand < 0) {
				return cand;
			} else {
				foreach (ResultPair p in cand) {
					R.Push (p.docid, this.MainSpace.Dist (q, this.MainSpace [p.docid]));
				}
				return R;
			}
		}
	}
}