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
//   Original filename: natix/SimilaritySearch/Indexes/LSC.cs
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
	/// Abstract class for locality sensitive hashing
	/// </summary>
	public abstract class LSC<T> : BaseIndex<T>
	{
		/// <summary>
		/// Matrix. One vector per LSH function 
		/// </summary>
		protected ushort[] H;
		protected IRankSelectSeq Seq;
		
		public SequenceBuilder SeqBuilder {			
			get;
			set;
		}
		
		public IRankSelectSeq GetSeq ()
		{
			return this.Seq;
		}
		/// <summary>
		/// Constructor
		/// </summary>
		public LSC () : base()
		{
			// this.SeqBuilder = SequenceBuilders.GetWT_GGMN_BinaryCoding ();
			this.SeqBuilder = SequenceBuilders.GetGolynskiRL (24);
		}

		/// <summary>
		/// Configure parameters
		/// </summary>
		public void Configure (IEnumerator<string> args)
		{
		}
		
		protected abstract void LoadSpace ();
		
		public override void FinalizeLoad (string indexName, IDictionary<string, object> config)
		{
			this.LoadSpace ();
			this.H = (ushort[])Dirty.DeserializeBinary (indexName + ".hash-fun");
			var name = String.Format ("{0}.seq", indexName);
			using (var Input = new BinaryReader(File.OpenRead(name))) { 
				this.Seq = RankSelectSeqGenericIO.Load (Input);
			}
		}

		/// <summary>
		///  The dimension of the indexed space
		/// </summary>
		protected abstract int GetDimension();

		public virtual void Build (string indexName, string spaceClass, string spaceName, int sampleSize, Func<int,T> get_item = null)
		{
			this.spaceClass = spaceClass;
			this.spaceName = spaceName;
			this.LoadSpace ();
			int dim = this.GetDimension ();
			this.H = new ushort[sampleSize];
			Random rand = new Random ();
			{
				HashSet<int> _coordinates = new HashSet<int> ();
				int i = 0;
				while (_coordinates.Count < sampleSize) {
					var p = (ushort)(rand.Next () % dim);
					if (_coordinates.Add (p)) {
						this.H [i] = p;
						++i;
					}
				}
				Array.Sort (this.H);
			}

			int len = this.MainSpace.Count;
			int pc = len / 100 + 1;
			int numbits = sampleSize > 32 ? 32 : sampleSize;
			var seq = new ListIFS (numbits);
			// Console.WriteLine ("DIMENSION: {0}, LENGTH: {1}", numbits, len);
			for (int docid = 0; docid < len; docid++) {
				if (docid % pc == 0) {
					Console.WriteLine ("Advance: {0:0.00}%, docid: {1}, total: {2}", docid * 100.0 / len, docid, len);
				}
				int hash;
				if (get_item == null) {
					hash = this.ComputeHash (this.MainSpace [docid]);
				} else {
					hash = this.ComputeHash (get_item (docid));
				}
				// Console.WriteLine ("hash: {0}, max: {1}, sample-size: {2}", hash, 1 << sampleSize, sampleSize);
				seq.Add (hash);
			}
			
			Dirty.SaveIndexXml (indexName, this);
			Dirty.SerializeBinary (indexName + ".hash-fun", this.H);
			Console.WriteLine ("*** Creating index of sequences");
			this.Seq = this.SeqBuilder (seq, 1 << numbits);
			var name = String.Format ("{0}.seq", indexName);
			using (var Output = new BinaryWriter(File.Create(name))) {
				// Console.WriteLine ("Saving index of sequences. Type: {0}, Name: {1}", this.Seq.GetType (), name);
				// seq.Save (Output);
				RankSelectSeqGenericIO.Save (Output, this.Seq);
			}
		}
				
		public abstract int ComputeHash (T u);

		public override IResult Search (T q, double radius)
		{
			return this.FilterByRadius (this.KNNSearch (q, 1024), radius);
		}
		
		public virtual HashSet<int> GetCandidates (T q)
		{
			int hash = this.ComputeHash (q);
			HashSet<int > Q = new HashSet<int> ();
			IRankSelect L = this.Seq.Unravel (hash);
			var len = L.Count1;
			for (int i = 1; i <= len; i++) {
				Q.Add (L.Select1 (i));
			}
			return Q;
		}

		public override IResult KNNSearch (T q, int K, IResult R)
		{
			var Q = this.GetCandidates (q);
			// K = -1;
			// Console.WriteLine ("q: {0}, K: {1}, len: {2}", q, K, len);
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
	}

	/// <summary>
	/// LSH for Binary hamming space
	/// </summary>
	public class HammingLSC : LSC<IList<byte>>
	{
		public override void Build (IEnumerable<string> args)
		{
			string name = null;
			string space = null;
			string spaceclass = null;
			int sampleSize = 0;
			OptionSet ops = new OptionSet() {
				{"indexname=", "Index output name", v => name = v},
				{"space=", "Space filename", v => space = v},
				{"spaceclass=", "Space class", v => spaceclass = v},
				{"samplesize=", "Number of samples", v => sampleSize = int.Parse(v)}
			};
			ops.Parse(args);
			if (name == null || space == null) {
				ops.WriteOptionDescriptions(Console.Out);
				throw new ArgumentNullException(String.Format("Build name: {0}, space: {1} can't be null", name, space));
			}
			if (sampleSize < 1) {
				ops.WriteOptionDescriptions(Console.Out);
				throw new ArgumentException("samplesize must be greater than zero");
			}
			this.Build(name, spaceclass, space, sampleSize);
		}
		
		protected override void LoadSpace ()
		{
			Console.WriteLine ("Loading space: {0}, class: {1}", this.spaceName, this.spaceClass);
			this.SetMainSpace ((Space<IList<byte>>)SpaceCache.Load (this.spaceClass, this.spaceName));
		}

		protected override int GetDimension ()
		{
			return (this.MainSpace[0].Count * 8);
		}
		
		/// <summary>
		/// Compute the LSH hashes
		/// </summary>
		public override int ComputeHash (IList<byte> u)
		{
			int hash = 0;
			for (int j = 0; j < this.H.Length; j++) {
				// j: position to sample
				// k: sample
				int k = H[j];
				// hash: the hash
				hash ^= ((u[k >> 3] >> (k & 7)) & 1) << (j & 31);
			}
			return hash;
		}
	}
}
