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
//   Original filename: natix/SimilaritySearch/Spaces/OrderedList.cs
// 
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using natix.CompactDS;
using natix.SortingSearching;

namespace natix.SimilaritySearch
{
	public enum KnrOrderedListCompression
	{
		NoCompression,
		OrderedListDiff,
		OrderedListDiffSet,
		OrderedListRunLen,
		OrderedListRunLen2,
		OrderedListSArray,
		OrderedListRRR,
		OrderedListRRRv2,
		OrderedListRankSelectRL
	}
	
	public class OrderedList : SequenceSpace<int> // ListGenerator< IList<int> >
	{
		KnrOrderedListCompression CompressClass;
		bool is_list_container;
		string Suffix = null;
		
		// IList< IList< int > > OLists;
		public OrderedList (KnrOrderedListCompression comp, string suffix) : base()
		{
			this.CompressClass = comp;
			this.Suffix = suffix;
		}
		
		public override void LoadSequences (string seqFile, int size, bool usedisk)
		{
			//throw new ArgumentException (String.Format("XXXXXXXX {0}", this.CompressClass));
			string basename;
			this.is_list_container = false;
			switch (this.CompressClass) {
			case KnrOrderedListCompression.NoCompression:
				Console.WriteLine ("*** Using plain (non compressed) inverted list: {0}", seqFile);
				base.LoadSequences (seqFile, size, usedisk);
				this.is_list_container = true;
				break;
			case KnrOrderedListCompression.OrderedListDiff:
				if (this.Suffix == null) {
					basename = seqFile + ".compressed";
				} else {
					basename = seqFile + this.Suffix;
				}
				Console.WriteLine ("*** Using compressed inverted list {0}", basename);
				if (usedisk) {
					Console.WriteLine ("*** Ignoring disk recommendation for {0}", basename);
				}
				this.seqs = new CompressedOrderedList (basename, size);
				this.is_list_container = true;
				break;
			case KnrOrderedListCompression.OrderedListDiffSet:
				if (this.Suffix == null) {
					basename = seqFile + ".invindex.diffset";
				} else {
					basename = seqFile + this.Suffix;
				}
				Console.WriteLine ("*** Using diffset compressed inverted list {0}", basename);
				this.seqs = new IList<int>[size];
				using (var R = new BinaryReader (File.OpenRead (basename))) {
					for (int i = 0; i < size; i++) {
						var diffset = new DiffSet ();
						diffset.Load (R);
						this.seqs [i] = new SortedListRSCache (diffset);
					}
				}
				break;
			case KnrOrderedListCompression.OrderedListRunLen:
				if (this.Suffix == null) {
					basename = seqFile + ".invindex.runlen";
				} else {
					basename = seqFile + this.Suffix;
				}
				Console.WriteLine ("*** Using runlen compressed inverted list {0}", basename);
				this.seqs = new IList<int>[size];
				using (var R = new BinaryReader (File.OpenRead (basename))) {
					for (int i = 0; i < size; i++) {
						var rl = new DiffSetRL ();
						rl.Load (R);
						this.seqs [i] = new SortedListRSCache (rl);
					}
				}
				break;
			case KnrOrderedListCompression.OrderedListRunLen2:
				if (this.Suffix == null) {
					basename = seqFile + ".invindex.runlen2";
				} else {
					basename = seqFile + this.Suffix;
				}
				Console.WriteLine ("*** Using runlen2 compressed inverted list {0}", basename);
				this.seqs = new IList<int>[size];
				using (var R = new BinaryReader (File.OpenRead (basename))) {
					for (int i = 0; i < size; i++) {
						var rl = new DiffSetRL2 ();
						rl.Load (R);
						this.seqs [i] = new SortedListRSCache (rl);
					}
				}
				break;
			case KnrOrderedListCompression.OrderedListSArray:
				if (this.Suffix == null) {
					basename = seqFile + ".invindex.sarray";
				} else {
					basename = seqFile + this.Suffix;
				}
				Console.WriteLine ("*** Using sarray compressed inverted list {0}", basename);
				this.seqs = new IList<int>[size];
				using (var R = new BinaryReader (File.OpenRead (basename))) {
					for (int i = 0; i < size; i++) {
						var rl = new SArray ();
						rl.Load (R);
						this.seqs [i] = new SortedListRSCache (rl);
					}
				}
				break;
			case KnrOrderedListCompression.OrderedListRRR:
				if (this.Suffix == null) {
					basename = seqFile + ".invindex.RRR";
				} else {
					basename = seqFile + this.Suffix;
				}
				Console.WriteLine ("*** Using RRR compressed inverted list {0}", basename);
				this.seqs = new IList<int>[size];
				using (var R = new BinaryReader (File.OpenRead (basename))) {
					for (int i = 0; i < size; i++) {
						var rl = new RRR ();
						rl.Load (R);
						this.seqs [i] = new SortedListRSCache (rl);
					}
				}
				break;
			case KnrOrderedListCompression.OrderedListRRRv2:
				if (this.Suffix == null) {
					basename = seqFile + ".invindex.RRRv2";
				} else {
					basename = seqFile + this.Suffix;
				}
				Console.WriteLine ("*** Using RRR compressed inverted list {0}", basename);
				this.seqs = new IList<int>[size];
				using (var R = new BinaryReader (File.OpenRead (basename))) {
					for (int i = 0; i < size; i++) {
						var rl = new RRRv2 ();
						rl.Load (R);
						this.seqs [i] = new SortedListRSCache (rl);
					}
				}
				break;
			case KnrOrderedListCompression.OrderedListRankSelectRL:
				if (this.Suffix == null) {
					basename = seqFile + ".invindex.runlen-on-heads";
				} else {
					basename = seqFile + this.Suffix;
				}
				Console.WriteLine ("*** Using runlen-headers compressed inverted index {0}", basename);
				this.seqs = new IList<int>[size];
				using (var R = new BinaryReader (File.OpenRead (basename))) {
					for (int i = 0; i < size; i++) {
						var rl = new RankSelectRL ();
						rl.Load (R);
						this.seqs [i] = new SortedListRSCache (rl);
					}
				}
				break;
			default:
				throw new ArgumentException (String.Format ("Unknown compression class for KnrInvertedIndex* {0}", this.CompressClass));
			}
		}

		public override int GetSeqLength (int docid)
		{
			if (this.is_list_container) {
				return (this.seqs as IListContainer<int>).GetLengthAtIndex (docid);
			} else {
				return this.seqs[docid].Count;
			}
		}
	}
}
