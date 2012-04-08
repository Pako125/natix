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
//   Original filename: natix/SimilaritySearch/Indexes/LC_RNN.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NDesk.Options;
using natix.CompactDS;
using natix.SortingSearching;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// LC with a fixed number of centers
	/// </summary>
	/// <exception cref='ArgumentNullException'>
	/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
	/// </exception>
	public class LC_RNN<T> : BaseIndex<T>
	{
		/// <summary>
		/// The centers ids
		/// </summary>
		protected IList<int> CENTERS;
		// protected IList<IRankSelect> INVINDEX;
		/// <summary>
		/// The index represented as a sequence
		/// </summary>
		protected IRankSelectSeq SEQ;
		/// <summary>
		/// All responses to cov()
		/// </summary>
		protected IList<float> COV;
		
		/// <summary>
		/// Returns the SEQuence index
		/// </summary>
		public IRankSelectSeq GetSEQ ()
		{
			return this.SEQ;
		}
		
		/// <summary>
		/// Returns the table of all covering radii
		/// </summary>
		public IList<float> GetCOV ()
		{
			return this.COV;
		}
		
		/// <summary>
		/// Gets or sets the seq builder. 
		/// </summary>
		public SequenceBuilder SeqBuilder {
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance
		/// </summary>
		public LC_RNN () : base()
		{	
			//this.SeqBuilder = SequenceBuilders.GetIISeq_SArray ();
			//this.SeqBuilder = SequenceBuilders.GetSeqXLB();
			this.SeqBuilder = SequenceBuilders.GetIISeq (BitmapBuilders.GetPlainSortedList ());
			/*
			var SBGMR06 = SequenceBuilders.GetGolynskiListRL2 (24);
			var SBOS07 = SequenceBuilders.GetIISeq_SArray ();
			this.SeqBuilder = delegate (IList<int> seq, int sigma) {
				// mix
				if (seq.Count / (sigma - 1) < 128) {
					// golynski is not good enough (in search time) since cache issues
					// it is very fast for random queries (random symbol, random pos-rank)
					// but working on the same symbol, in sequential access, is clearly
					// suprised by IISeq_SArray
					return SBGMR06 (seq, sigma);
				} else {
					return SBOS07 (seq, sigma);
				}
			};*/
			//this.SeqBuilder = SequenceBuilders.GetGolynskiListRL2 (24);
			//this.SeqBuilder = SequenceBuilders.GetGolynskiSinglePerm (PermutationBuilders.GetCyclicPerms (24));
			//this.SeqBuilder = SequenceBuilders.GetIISeq (BitmapBuilders.GetPlainSortedList ());

		}
		
		/// <summary>
		/// Build the index
		/// </summary>
		public override void Build (IEnumerable<string> args)
		{
			string space_class = null;
			string db_name = null;
			string output_name = null;
			int sample_size = 0;
			OptionSet ops = new OptionSet () {
				{"spaceclass=", "space class", (v) => space_class = v },
				{"space=", "database", (v) => db_name = v},
				{"index|indexname=", "output index", (v) => output_name = v},
				{"numcenters=", "number of centers", (v) => sample_size = int.Parse (v)}
			};
			bool successful = true;
			try {
				ops.Parse (args);
			} catch (Exception e) {
				Console.WriteLine (e.StackTrace);
				successful = false;
			}
			if (!successful || space_class == null || db_name == null || output_name == null || sample_size < 2) {
				Console.WriteLine ("Please check the arguments");
				Console.WriteLine ("spaceclass: {0}, space: {1}, index: {2}, numcenters: {3}",
					space_class, db_name, output_name, sample_size);
				ops.WriteOptionDescriptions (Console.Out);
				throw new ArgumentNullException ();
			}
			this.Build (output_name, db_name, space_class, sample_size);		
		}
		
		/// <summary>
		/// SearchNN method (build time)
		/// </summary>
		public virtual void BuildSearchNN (int docid, out int nn_center, out double nn_dist)
		{
			var u = this.MainSpace [docid];
			int num_centers = this.CENTERS.Count;
			nn_center = 0;
			nn_dist = float.MaxValue;
			for (int center_id = 0; center_id < num_centers; center_id++) {
				var curr_dist = this.MainSpace.Dist (u, this.MainSpace [this.CENTERS [center_id]]);
				if (curr_dist < nn_dist) {
					nn_dist = curr_dist;
					nn_center = center_id;
				}
			}
		}
		
		public virtual void BuildInternal (BitStream32 IsCenter, IList<IList<int>> invindex, string output_name)
		{
			int len = this.MainSpace.Count;
			int pc = len / 100 + 1;
			for (int docid = 0; docid < len; docid++) {
				if (docid % pc == 0) {
					Console.WriteLine ("docid {0} of {1}, advance {2:0.00}%, index: {3}", docid, len, docid * 100.0 / len, output_name);
				}
				// Console.WriteLine ("docid {0} of {1}, advance {2:0.00}%, index: {3}", docid, len, docid * 100.0 / len, output_name);
				if (IsCenter [docid]) {
					continue;
				}
				int nn_center;
				double nn_dist;
				this.BuildSearchNN (docid, out nn_center, out nn_dist);
				invindex [nn_center].Add (docid);
				if (this.COV [nn_center] < nn_dist) {
					this.COV [nn_center] = (float)nn_dist;
				}
			}
		}
		
		/// <summary>
		/// Build the index
		/// </summary>
		public virtual void Build (string output_name, string db_name, string space_class, int num_centers)
		{
			var sp = SpaceCache.Load (space_class, db_name);
			this.spaceClass = space_class;
			this.spaceName = db_name;
			this.SetMainSpace ((Space<T>)sp);
			this.CENTERS = RandomSets.GetRandomSubSet (0, this.MainSpace.Count - 1, num_centers);
			Sorting.Sort<int> (this.CENTERS);
			BitStream32 IsCenter = new BitStream32 ();
			IsCenter.Write (false, sp.Count);
			var invindex = new List<int>[num_centers];
			this.COV = new float[num_centers];
			for (int i = 0; i < num_centers; i++) {
				invindex [i] = new List<int> ();
				IsCenter [this.CENTERS [i]] = true;
			}
			this.BuildInternalIndex (output_name);
			this.BuildInternal (IsCenter, invindex, output_name);
			this.Save (output_name, invindex);
		}
		
		protected virtual void BuildInternalIndex (string outname)
		{
		}
		
		/// <summary>
		/// Save the index
		/// </summary>
		public virtual void Save (string output_name)
		{
			var invindex = new List<IList<int>> ();
			var s = this.COV.Count;
			for (int i = 0; i < s; i++) {
				invindex.Add (new SortedListRS (this.SEQ.Unravel (i)));
			}
			this.Save (output_name, invindex);
		}
		
		/// <summary>
		/// Save the index
		/// </summary>

		public virtual void Save (string output_name, IList<IList<int>> invindex)
		{
			this.SaveLC (output_name, invindex);
			this.CompileLC (output_name);
			Dirty.SaveIndexXml (output_name, this);			
		}
		
		
		protected virtual void SaveLC (string output_name, IList<IList<int>> invindex)
		{
			int num_centers = this.COV.Count;
			using (var Output = new StreamWriter(output_name + ".lc")) {
				Output.WriteLine ("{0}", num_centers);
				for (int center = 0; center < num_centers; center++) {
					Output.WriteLine ("=center {0} {1} {2}", this.CENTERS [center], this.COV [center], invindex [center].Count);
					int i = 0;
					foreach (var u in invindex[center]) {
						if (i > 0) {
							Output.Write (" ");
						}
						i++;
						Output.Write (u);
					}
					Output.WriteLine ();
				}
			}
		}
		
		/// <summary>
		/// Compiles the LC
		/// </summary>
		public void CompileLC (string name)
		{
			var Input = new StreamReader (File.OpenRead (name + ".lc"));
			int num_centers = int.Parse (Input.ReadLine ());
			int n = this.MainSpace.Count;
			var seq = new ListIFS ((int)Math.Ceiling (Math.Log (num_centers + 1, 2)));
			for (int i = 0; i < n; i++) {
				seq.Add (0);
			}
			//var cov = new Dictionary<int,float> ();
			var _cov = new List<float> (num_centers);
			var centers_original_values = new List<int> (num_centers);
			var centers_new_order = new List<int> (num_centers);
			for (int center_id = 0; center_id < num_centers; center_id++) {
				var line = Input.ReadLine ();
				var H = line.Split ();
				// header
				int center = int.Parse (H [1]);
				float covering = float.Parse (H [2]);
				centers_original_values.Add (center);
				centers_new_order.Add (center_id);
				seq [center] = num_centers;
				// cov [center] = covering;
				_cov.Add (covering);
				// list
				line = Input.ReadLine ();
				var L = PrimitiveIO<int>.ReadVectorFromString (line);
				foreach (int item in L) {
					seq [item] = center_id;
				}
			}
			Input.Close ();
			Sorting.Sort<int,int> (centers_original_values, centers_new_order);
			var inv = centers_original_values;
			for (int i = 0; i < num_centers; ++i) {
				inv [centers_new_order [i]] = i;
			}
			centers_original_values = centers_new_order = null;
			var cov = new float[num_centers];
			for (int i = 0; i < num_centers; ++i) {
				cov [inv [i]] = _cov [i];
			}
			_cov = null;
			for (int i = 0; i < n; ++i) {
				var u = seq [i];
				if (u < num_centers) {
					seq [i] = inv [u];
				}
			}
			IRankSelectSeq S = this.SeqBuilder (seq, num_centers + 1);
			seq = null;
			using (var OutputSEQ = new BinaryWriter (File.Create (name + ".lc.seqindex"))) {
				RankSelectSeqGenericIO.Save (OutputSEQ, S);
			}
			using (var OutputSAT = new BinaryWriter (File.Create (name + ".lc.centers"))) {
				OutputSAT.Write (num_centers);
				// var centers_list = new SortedListRS (S.Unravel (num_centers));
				PrimitiveIO<float>.WriteVector (OutputSAT, cov);
			}
		}
		
		/// <summary>
		/// Finalizes the load.
		/// </summary>
		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			base.FinalizeLoad (name, config);
			this.SetMainSpace ((Space<T>)SpaceCache.Load (this.spaceClass, this.spaceName));
			if (!File.Exists (name + ".lc.seqindex")) {
				this.CompileLC (name);
			}
			int numcenters;
			using (var Input = new BinaryReader(File.OpenRead (name + ".lc.centers"))) {
				numcenters = Input.ReadInt32 ();
				this.COV = new float[numcenters];
				PrimitiveIO<float>.ReadFromFile (Input, numcenters, this.COV);
			}
			using (var Input = new BinaryReader(File.OpenRead (name + ".lc.seqindex"))) {
				this.SEQ = RankSelectSeqGenericIO.Load (Input);
			}
			
			this.CENTERS = new List<int> ();
			foreach (int u in (new SortedListRS (this.SEQ.Unravel (numcenters)))) {
				this.CENTERS.Add (u);
			}
		}
		
		/// <summary>
		/// Search the specified q with radius qrad.
		/// </summary>
		public override IResult Search (T q, double qrad)
		{
			var sp = this.MainSpace;
			var R = sp.CreateResult (int.MaxValue, false);
			int len = this.CENTERS.Count;
			for (int center_id = 0; center_id < len; center_id++) {
				var dcq = sp.Dist (this.MainSpace [this.CENTERS [center_id]], q);
				if (dcq <= qrad) {
					R.Push (this.CENTERS [center_id], dcq);
				}
				if (dcq <= qrad + this.COV [center_id]) {
					var rs = this.SEQ.Unravel (center_id);
					var count1 = rs.Count1;
					for (int i = 1; i <= count1; i++) {
						var u = rs.Select1 (i);
						var r = sp.Dist (q, sp [u]);
						if (r <= qrad) {
							R.Push (u, r);
						}
					}
				}
			}
			return R;
		}
		
		/// <summary>
		/// KNN search.
		/// </summary>
		public override IResult KNNSearch (T q, int K, IResult R)
		{
			var sp = this.MainSpace;
			int len = this.CENTERS.Count;
			var C = this.MainSpace.CreateResult (len, false);
			for (int center = 0; center < len; center++) {
				var dcq = sp.Dist (this.MainSpace [this.CENTERS [center]], q);
				R.Push (this.CENTERS [center], dcq);
				//var rm = Math.Abs (dcq - this.COV [center]);
				if (dcq <= R.CoveringRadius + this.COV [center]) {
				// if (rm <= R.CoveringRadius) {
					 C.Push (center, dcq);
					// C.Push (center, rm);
				}
			}
			foreach (ResultPair pair in C) {
				var dcq = pair.dist;
				var center = pair.docid;
				if (dcq <= R.CoveringRadius + this.COV [center]) {
					var rs = this.SEQ.Unravel (center);
					var count1 = rs.Count1;
					for (int i = 1; i <= count1; i++) {
						var u = rs.Select1 (i);
						var r = sp.Dist (q, sp [u]);
						//if (r <= qr) { // already handled by R.Push
						R.Push (u, r);
					}
				}
			}
			return R;
		}
		
		// methods for partial searching (poly metric-index)
		/// <summary>
		/// Partial KNN search
		/// </summary>
		public IEnumerable<IList<int>> Test_PartialKNNSearch (T q, int K, IResult R, IDictionary<int,double> cache)
		{
			//if (cache_space == null) {
			//	cache_space = this.MainSpace;
			//}
			var sp = this.MainSpace;
			int len = this.CENTERS.Count;
			var C = sp.CreateResult (len, false);
			for (int center = 0; center < len; center++) {
				double dcq = -1;
				var oid = this.CENTERS [center];
				if (cache != null) {
					if (!cache.TryGetValue (oid, out dcq)) {
						dcq = -1;
					}
				}
				if (dcq < 0) {
					dcq = sp.Dist (sp [oid], q);
					if (cache != null) {
						cache [oid] = dcq;
					}
				}
				R.Push (oid, dcq);
				if (dcq <= R.CoveringRadius + this.COV [center]) {
					C.Push (center, dcq);
				}
			}
			foreach (ResultPair pair in C) {
				var dcq = pair.dist;
				var center = pair.docid;
				if (dcq <= R.CoveringRadius + this.COV [center]) {
					yield return new SortedListRS(this.SEQ.Unravel(center));
				}
			}
		}
		
		public virtual IEnumerable<IList<int>> PartialKNNSearch (T q, int K, IResult R, IDictionary<int,double> cache)
		{
			//if (cache_space == null) {
			//	cache_space = this.MainSpace;
			//}
			var sp = this.MainSpace;
			int len = this.CENTERS.Count;
			var C = sp.CreateResult (len, false);
			for (int center = 0; center < len; center++) {
				double dcq = -1;
				var oid = this.CENTERS [center];
				if (!cache.TryGetValue (oid, out dcq)) {
					dcq = -1;
				}
				if (dcq < 0) {
					dcq = sp.Dist (sp [oid], q);
					cache [oid] = dcq;
				}
				R.Push (oid, dcq);
				if (dcq <= R.CoveringRadius + this.COV [center]) {
					//C.Push (center, dcq);
					var dcq_less_cov = Math.Abs (dcq - this.COV [center]);
					C.Push (center, dcq_less_cov);
				}
			}
			foreach (ResultPair pair in C) {
				// var dcq_less_cov = pair.dist;
				var center = pair.docid;
				var oid = this.CENTERS [center];
				var dcq = cache [oid];
				if (dcq <= R.CoveringRadius + this.COV [center]) {
					yield return new SortedListRS(this.SEQ.Unravel(center));
				}
			}
		}

		/// <summary>
		/// Partial radius search
		/// </summary>

		public virtual IList<IList<int>> PartialSearch (T q, double qrad, IResult R, IDictionary<int,double> cache)
		{
			//if (cache_space == null) {
			//	cache_space = this.MainSpace;
			//}
			var sp = this.MainSpace;
			int len = this.CENTERS.Count;
			IList<IList<int>> output_list = new List<IList<int>> ();
			for (int center_id = 0; center_id < len; center_id++) {
				double dcq = -1;
				var oid = this.CENTERS [center_id];
				if (cache != null) {
					if (!cache.TryGetValue (oid, out dcq)) {
						dcq = -1;
					}
				}
				if (dcq < 0) {
					dcq = sp.Dist (sp [oid], q);
					if (cache != null) {
						cache [oid] = dcq;
					}
				}
				if (dcq <= qrad) {
					R.Push (this.CENTERS [center_id], dcq);
				}
				if (dcq <= qrad + this.COV [center_id]) {
					// output_list.Add (this.invindex [center_id]);
					output_list.Add (new SortedListRS (this.SEQ.Unravel (center_id)));
				}
			}
			return output_list;
		}
	}
}
