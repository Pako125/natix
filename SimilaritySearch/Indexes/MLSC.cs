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
//   Original filename: natix/SimilaritySearch/Indexes/MLSC.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NDesk.Options;
using natix.CompactDS;

namespace natix.SimilaritySearch
{

	/// <summary>
	/// Multiple (coupled compressed) locality sensitive hashing sequences
	/// </summary>
	public abstract class MLSC<T> : BaseIndex<T>
	{
		LSC<T>[] lsc_indexes;
		/// <summary>
		/// Constructor
		/// </summary>
		public MLSC () : base()
		{
			this.SeqBuilder = SequenceBuilders.GetSeqXLB_DiffSet64 (16, 31, new EliasDelta64 ());
		}
		
		public SequenceBuilder SeqBuilder {
			get;
			set;
		}
		
		/// <summary>
		/// Configure parameters
		/// </summary>
		public void Configure (IEnumerator<string> args)
		{
		}
		
		protected void LoadSpace ()
		{
			Console.WriteLine ("MLSC loading space: {0}, class: {1}", this.spaceName, this.spaceClass);
			this.SetMainSpace ((Space<T>)SpaceCache.Load (this.spaceClass, this.spaceName));
		}

		public override void FinalizeLoad (string indexName, IDictionary<string, object> config)
		{
			this.LoadSpace ();
			var lines = File.ReadAllLines (indexName + ".lsc_list");
			this.lsc_indexes = new LSC<T>[lines.Length];
			for (int i = 0; i < lines.Length; ++i) {
				this.lsc_indexes [i] = (LSC<T>)IndexLoader.Load (lines [i], "lsc");
			}
		}

		public abstract void Build (string indexName, string spaceClass, string spaceName, int sampleSize, int numInstances);

		public override IResult Search (T q, double radius)
		{
			return this.FilterByRadius (this.KNNSearch (q, 1024), radius);
		}
		
		public override IResult KNNSearch (T q, int K, IResult R)
		{
			var seq_base = this.lsc_indexes [0].GetSeq () as SeqXLB;
			if (seq_base == null) {
				throw new ArgumentNullException ("Currently only SeqXLB instances are allowed");
			}
			var perm = seq_base.GetPERM ();
			var Q = this.lsc_indexes [0].GetCandidates (q);
			for (int i = 1; i < this.lsc_indexes.Length; ++i) {
				var P = this.lsc_indexes [i].GetCandidates (q);
				foreach (var item in P) {
					Q.Add (perm [item]);
				}
			}
			if (K < 0) {
				foreach (var docId in Q) {
					R.Push (docId, -1);
				}
			} else {
				foreach (var docId in Q) {
					double dist = this.MainSpace.Dist (this.MainSpace [docId], q);
					R.Push (docId, dist);
				}
			}
			return R;
		}

		public override void Build (IEnumerable<string> args)
		{
			string name = null;
			string space = null;
			string spaceclass = null;
			int sampleSize = 0;
			int numInstances = 2;
			
			OptionSet ops = new OptionSet () {
				{"indexname=", "Index output name", v => name = v},
				{"space=", "Space filename", v => space = v},
				{"spaceclass=", "Space class", v => spaceclass = v},
				{"samplesize=", "Number of samples", v => sampleSize = int.Parse (v)},
				{"numinstances=", "Number of instances", v => numInstances = int.Parse (v)}
			};
			ops.Parse (args);
			if (name == null || space == null) {
				ops.WriteOptionDescriptions (Console.Out);
				throw new ArgumentNullException (String.Format ("Build name: {0}, space: {1} can't be null", name, space));
			}
			if (sampleSize < 1) {
				ops.WriteOptionDescriptions (Console.Out);
				throw new ArgumentException ("samplesize must be greater than zero");
			}
			this.Build (name, spaceclass, space, sampleSize, numInstances);
		}
	}
	
	public class HammingMLSC : MLSC<IList<byte>>
	{
		public HammingMLSC() : base()
		{
		}
		
		public override void Build (string indexName, string spaceClass, string spaceName, int sampleSize, int numInstances)
		{
			this.spaceClass = spaceClass;
			this.spaceName = spaceName;
			this.LoadSpace ();
			
			var filename_list = new List<string> ();
			IPermutation perm = null;
			for (int i = 0; i < numInstances; ++i) {
				var lsc = new HammingLSC ();
				var _indexName = indexName + String.Format (".instance-{0:00}.xml", i);
				lsc.SeqBuilder = this.SeqBuilder;
				if (i == 0) {
					lsc.Build (_indexName, spaceClass, spaceName, sampleSize);
					lsc = (HammingLSC)IndexLoader.Load (_indexName);
					var seq = lsc.GetSeq () as SeqXLB;
					if (seq == null) {
						throw new ArgumentException ("SeqBuilder should return a SeqXLB instance");
					}
					perm = seq.GetPERM ();
				} else {
					lsc.Build (_indexName, spaceClass, spaceName, sampleSize, (int p) => this.MainSpace [perm [p]]);
				}
				filename_list.Add (_indexName);
			}
			Dirty.SaveIndexXml (indexName, this);
			using (var Output = new StreamWriter(File.Create(indexName + ".lsc_list"))) {
				foreach (var filename in filename_list) {
					Output.WriteLine (filename);
				}
			}
		}
	}
}
