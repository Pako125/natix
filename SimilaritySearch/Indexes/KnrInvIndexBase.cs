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
//   Original filename: natix/SimilaritySearch/Indexes/KnrInvIndexBase.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using NDesk.Options;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Jaccard's inverted index
	/// </summary>
	public abstract class KnrInvIndexBase<T> : Knr<T>
	{
		/// <summary>
		/// The inverted lists
		/// </summary>
		protected OrderedList InvList;
		// protected IList< IList< Int32 > > InvList;
		//public Int32[][] invlist;
		public int BuildCutKnr = 0;
		/// <summary>
			/// Search cut of the farest knr. Performance option
		/// </summary>
		public int SearchCutKnr = 0; 
		/// <summary>
		/// Threshold error. i.e. Convert into a (|intersection|-Error)-Threshold problem
		/// </summary>
		public int ThresholdError;
		protected IList<int> KnrSeqLength;
		KnrOrderedListCompression CompressionClass = KnrOrderedListCompression.NoCompression;
		
		protected abstract IResult GetCandidates(T q, IList<UInt16> qseq, int k);
		/*public int GetKnrSeqLength (int docid)
		{
			if (this.KnrBoundBuild < 0) {
				return -this.KnrBoundBuild;
			} else {
				return this.InvList.GetSeqLength (docid);
			}
		}*/
		public IList< IList<Int32> > GetInvIndex ()
		{
			return this.InvList.seqs;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public KnrInvIndexBase () : base()
		{
			this.ThresholdError = int.MaxValue;
		}
		
		/// <summary>
		///  Create a new inverted index
		/// </summary>
		public override void Build (string name, string spaceClass, string spaceName, string indexpermsname, int maxcand, int knrbound, Func<int, IList<ushort> > _GetKnr)
		{
			base.Build (name, spaceClass, spaceName, indexpermsname, maxcand, knrbound, _GetKnr);
			this.CreateInvertedFile (name, true);
		}
		
		/// <summary>
		/// Configure the index
		/// </summary>
		/// <param name="args">
		/// </param>
		public override void Configure (IEnumerable<string> args)
		{
			OptionSet op = new OptionSet() {
				{"cutknr|searchcutknr=", "Number of Knr to be used in the search", v => this.SearchCutKnr = int.Parse(v)},
				{"thresholderror|terror=", "Threshold error. i.e. Maximum number of errors, finally (K-terror)-threshold", v => this.ThresholdError = int.Parse(v)}
			};
			op.Parse(args);
			base.Configure (args);
		}
		

		public override void Build (IEnumerable<string> args)
		{
			OptionSet ops = new OptionSet() {
				{"thresholderror|terror=", "Default threshold error", (v) => this.ThresholdError = int.Parse(v) },
			};
			ops.Parse(args);
			// We call the parent's build method at the end of this method
			base.Build (args);
		}
		
		/// <summary>
		/// Wrap the knr sequence
		/// </summary>
		protected override IList<ushort> KnrWrap (IList<ushort> a)
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
		/// Return the length of the KnrSequence for docid
		/// </summary>
		/// <param name="docid">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		/*protected int GetKnrSeqLength (int docid)
		{
			if (this._BuildKnrBound < 0) {
				return -this._BuildKnrBound;
			} else {
				return this._KnrLength[docid];
			}
		}*/
		
		/// <summary>
		/// Finalize load. <see cref="natix.IndexLoader.Load"/>
		/// </summary>
		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			this.LoadSpaceAndRefs (name, this.spaceClass, this.spaceName, this.IndexRefsName, false);
			if (this.CreateInvertedFile (name, false)) {
				Console.WriteLine ("The inverted file index was saved");
			}
			var occname = name + ".occlists";
			Console.WriteLine ("=== Loading inverted file from {0}", occname);
			string suffix_ordered_list = null;
			if (config != null) {
				if (config.ContainsKey ("compression")) {
					this.SetCompressionClass (config["compression"] as string);
				}
				if (config.ContainsKey ("compression-suffix")) {
					suffix_ordered_list = config["compression-suffix"] as string;
				}
			}
			this.InvList = new OrderedList (this.CompressionClass, suffix_ordered_list);
			this.InvList.LoadFromFile (occname);
			if (this.KnrBoundBuild < 0) {
				this.KnrSeqLength = new ListGen<int> (((int i) => -this.KnrBoundBuild),
					this.MainSpace.Count);
			} else {
				this.KnrSeqLength = PrimitiveIO<int>.ReadAllFromFile (name + ".seqspace.seqs.bin.sizes");
			}
			Console.WriteLine ("=== Inverted file loaded");
		}
		
		void SetCompressionClass (string compression_class)
		{
			Console.WriteLine ("XXXXXX CONFIG compression_class: {0}", compression_class);
			switch (compression_class.ToLower ()) {
			case "none":
				this.CompressionClass = KnrOrderedListCompression.NoCompression;
				break;
			case "diff":
				this.CompressionClass = KnrOrderedListCompression.OrderedListDiff;
				break;
			case "diffset":
				this.CompressionClass = KnrOrderedListCompression.OrderedListDiffSet;
				break;
			case "runlen":
				this.CompressionClass = KnrOrderedListCompression.OrderedListRunLen;
				break;
			case "runlen2":
				this.CompressionClass = KnrOrderedListCompression.OrderedListRunLen2;
				break;
			case "sarray":
				this.CompressionClass = KnrOrderedListCompression.OrderedListSArray;
				break;
			case "rrr":
				this.CompressionClass = KnrOrderedListCompression.OrderedListRRR;
				break;
			case "rrrv2":
				this.CompressionClass = KnrOrderedListCompression.OrderedListRRRv2;
				break;
			case "runlen-on-heads":
				this.CompressionClass = KnrOrderedListCompression.OrderedListRankSelectRL;
				break;
			default:
				Console.WriteLine ("Valid compression classes none|diff|diffset|runlen|runlen2|sarray|RRR|RRRv2|runlen-on-heads");
				throw new ArgumentException (String.Format ("Unknown compression class", compression_class));
			}
		}

		
		bool CreateInvertedFile (string name, bool force)
		{
			var occname = name + ".occlists";
			if (force == false && File.Exists (occname)) {
				return false;
			}
			int numrefs = this.IndexRefs.MainSpace.Count;
			var invlist = new List<Int32>[numrefs];
			for (int i = 0; i < numrefs; i++) {
				invlist[i] = new List<Int32> ();
			}
			Console.WriteLine ("==== Creating fast inverted index for {0}.seqspace", name);
			Console.WriteLine ("1. Loading KNR sequences: {0}.seqspace", name);
			SequenceSpace<ushort> ss = new SequenceSpace<ushort> ();
			ss.LoadFromFile (name + ".seqspace");
			for (int docid = 0, sL = ss.Count, C = sL / 100 + 1; docid < sL; docid++) {
				IList<ushort> xseq = ss[docid];
				for (int p = 0; p < xseq.Count; p++) {
					invlist[xseq[p]].Add (docid);
				}
				if (docid % C == 0) {
					Console.WriteLine ("docid {0}, advance {1:0.00}%", docid, docid * 100.0 / sL);
				}
			}
			Console.WriteLine ("2. Writing occlists to {0}", occname);
			var head = String.Format("--sequences {0}.seqs --size {1} --distance None --usedisk False",
				Path.GetFileName(occname), invlist.Length);
			File.WriteAllText(occname, head);
			using(var w = new StreamWriter( File.Create(occname + ".seqs", 1 << 22) )) {
				foreach (IList<int> list in invlist) {
					foreach (int u in list) {
						w.Write("{0} ", u);
					}
					w.WriteLine();
				}
			}
			return true;
		}

		public virtual List<IList<int>> GetPostingLists (IList<UInt16> qseq, out int expectedSize)
		{
			List<IList<int>> posting = new List<IList<int>> ();
			int inputSize = 0;
			for (int i = 0, qL = qseq.Count - this.SearchCutKnr; i < qL; i++) {
				var p = this.InvList [qseq [i]];
				if (p.Count > 0) {
					posting.Add (p);
					inputSize += p.Count;
				}
			}
			expectedSize = inputSize;
			return posting;
		}
		
		protected virtual HashSet<int> GetUnionLists (T q, IList<ushort> qseq)
		{
			int expectedSize;
			var posting = this.GetPostingLists (qseq, out expectedSize);
			HashSet<int> C = new HashSet<int> ();
			foreach (var L in posting) {
				foreach (var item in L) {
					C.Add (item);
				}
			}
			return C;
		}
		
		public override IResult KNNSearch (T q, int k, IResult res)
		{
			foreach (var u in this.KNNSearch (q, k)) {
				res.Push (u.docid, u.dist);
			}
			return res;
		}
		
		/// <summary>
		/// KNN Search
		/// </summary>
		public override IResult KNNSearch (T q, int k)
		{
			IList<UInt16> qseq = this.GetKnr (q, false);
//			Console.WriteLine ("====> qseq");
//			foreach (var s in qseq) {
//				Console.WriteLine ("{0}", s);
//			}
			var cand = this.GetCandidates (q, qseq, k);
			cand = this.GetOrderingFunctions ().Filter (this, q, qseq, cand);
			if (this.Maxcand < 0) {
				return cand;
			}
			// return this.FilterKNNByRealDistances (q, k, false, cand, cand.Count);
			return this.FilterKNNByRealDistances (q, k, false, cand, this.Maxcand);
		}
	}
}