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
//   Original filename: natix/natix/SimilaritySearch/Indexes/LSH.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NDesk.Options;
using natix.CompactDS;

namespace natix.SimilaritySearch
{
	public enum LSHQueryExpansionPolicy 
	{
		None,
		OnEmptyBlockSingle,
		OnEmptyBlockFull,
		Always
	}
	
	/// <summary>
	/// Abstract class for locality sensitive hashing
	/// </summary>
	public abstract class LSH<T> : BaseIndex<T>
	{
		/// <summary>
		/// Matrix. One vector per LSH function 
		/// </summary>
		protected UInt16[] SamplingIndexes;
	
		/// <summary>
		/// Buckets / Hash tables to save doc references
		/// </summary>
		protected IDictionary< int, IRankSelect > InvIndex;
		//public LSHQueryExpansionPolicy ExpandPolicy = LSHQueryExpansionPolicy.None;
		// public bool ExpandQuery = false;
		
		/// <summary>
		/// Constructor
		/// </summary>
		public LSH () : base()
		{
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
			// this.space = (Space<byte[]>)SpaceCache.Load (this.spaceClass, this.spaceName);
			// TODO: Save and load without recompute LSH, right now we always compute the hashes
			this.SamplingIndexes = (ushort[])Dirty.DeserializeBinary (indexName + ".samples.bin");
			//for (int docid = 0; docid < this.MainSpace.Count; docid++) {
			//	this.Insert (docid);
			//}
			this.InvIndex = new Dictionary<int, IRankSelect> ();
			var Keys = new SArray ();
			var name = String.Format ("{0}.lsh", indexName);
			using (var Input = new BinaryReader (File.OpenRead (name))) {
				Keys.Load (Input);
			}
			this.InvIndex = new Dictionary<int, IRankSelect> ();
			using (var Input = new BinaryReader (File.OpenRead (name + ".invindex"))) {
				int numlists = Input.ReadInt32 ();
				for (int j = 0; j < numlists; j++) {
					var sa = new SArray ();
					sa.Load (Input);
					this.InvIndex[Keys.Select1(j+1)] = sa;
				}
			}
		}

		/// <summary>
		///  The dimension of the indexed space
		/// </summary>
		protected abstract int GetDimension();

		public virtual void Build (string indexName, string spaceClass, string spaceName, int sampleSize)
		{
			this.spaceClass = spaceClass;
			this.spaceName = spaceName;
			this.LoadSpace ();
			int dim = this.GetDimension ();
			Random rand = new Random ();
			this.SamplingIndexes = new UInt16[sampleSize];
			for (int sIndex = 0; sIndex < sampleSize; sIndex++) {
				this.SamplingIndexes[sIndex] = (ushort)(rand.Next () % dim);
			}
			int Slen = this.MainSpace.Count;
			int pc = Slen / 100 + 1;
			var IIdx = new Dictionary<int, IList<int> >();
			for (int docid = 0; docid < Slen; docid++) {
				if (docid % pc == 0) {
					Console.WriteLine("Advance: {0:0.00}%, docid: {1}, total: {2}", docid * 100.0 / Slen, docid, Slen);
				}
				// Console.WriteLine(docid);
				this.Insert (docid, IIdx);
			}
			
			Dirty.SaveIndexXml (indexName, this);
			Dirty.SerializeBinary (indexName + ".samples.bin", this.SamplingIndexes);
			
			// saves the sizes of each inverted list
			// var L = new ListIntegersDiffSet ();
			// the vocabulary (1 if key is set, 0 if not)
			// var V = new SortedListDiffSet();
			var Keys = new List<int>(IIdx.Keys);
			Keys.Sort();
			var name = String.Format("{0}.lsh.invindex", indexName);
			using (var OutputSortedLists = new BinaryWriter(File.Create(name))) {
				OutputSortedLists.Write((int) Keys.Count);
				foreach (var key in Keys) {
					var sa = new SArray();
					sa.Build(IIdx[key]);
					sa.Save(OutputSortedLists);
					// L.Add(II[key].Count);
					// V.Add(key);						
				}
			}
			name = String.Format("{0}.lsh", indexName);
			using (var Output = new BinaryWriter(File.Create(name))) {
				// L.Save(Output);
				// V.Save(Output);
				var sa = new SArray();
				sa.Build (Keys);
				sa.Save (Output);
			}
		}

		public abstract int ComputeHash (T u, out int sizehash);
		
		public void Insert (int docid, IDictionary<int, IList<int>> IIdx)
		{
			int sizehash;
			var hash = this.ComputeHash (this.MainSpace[docid], out sizehash);
			IList<int> bucket;
			// Console.WriteLine ("HASH-{0}: {1}", i, hash);
			if (!IIdx.TryGetValue (hash, out bucket)) {
				bucket = new List<int> ();
				IIdx[hash] = bucket;
			}
			bucket.Add (docid);
		}
		
		public override IResult Search (T q, double radius)
		{
			return this.FilterByRadius (this.KNNSearch (q, 1024), radius);
		}
		
		//ITThresholdAlgorithm ttsearch = new SimpleIntersectionTThreshold(new BaezaYatesIntersection(new BinarySearch<int>()));
		public override IResult KNNSearch (T q, int K, IResult R)
		{
			int sizehash;
			int hash = this.ComputeHash (q, out sizehash);
			IRankSelect L;
			HashSet<int > Q = new HashSet<int> ();
			if (this.InvIndex.TryGetValue (hash, out L)) {
				for (int i = 1; i <= L.Count1; i++) {
					Q.Add (L.Select1 (i));
				}
			}
			// expanding query to distance 1
			/*if (this.ExpandPolicy != LSHQueryExpansionPolicy.None) {
				if (this.ExpandPolicy == LSHQueryExpansionPolicy.Always || Q.Count == 0) {
					for (int m = 0; m < sizehash; m++) {
						if (this.InvIndex.TryGetValue (hash ^ (1 << m), out L)) {
							//Console.WriteLine ("expanding query m: {0}, sizehash: {1}, hash: {2}, mod-hash: {3}",
							//	m, sizehash,
							//	BinaryHammingSpace.ToAsciiString (hash), BinaryHammingSpace.ToAsciiString (hash ^ (1<<m)));
							for (int i = 1; i <= L.Count1; i++) {
								Q.Add (L.Select1 (i));
							}
							if (this.ExpandPolicy == LSHQueryExpansionPolicy.OnEmptyBlockSingle && Q.Count > 0) {
								break;
							}
						}
					}
				}
			}*/
			// Console.WriteLine ("******* Q.Count: {0}, L.Count1: {1}", Q.Count, L.Count1);
			foreach (var docId in Q) {
				double dist;
				if (K < 0) {
					dist = -1;
				} else {
					dist = this.MainSpace.Dist (this.MainSpace [docId], q);
				}
				R.Push (docId, dist);
				/*Console.WriteLine ("docid: {0}, dist: {1}, q: {2}, cr: {3}, r-len: {4}",
					docId, dist, q, R.CoveringRadius, R.Count);
				Console.WriteLine ("smallest-r: {0}, K-r: {1}", R.First, R.K);
				*/
			}
			return R;
		}
	}
	
	/// <summary>
	/// LSH for Binary hamming space
	/// </summary>
	public class HammingLSH : LSH<IList<byte>>
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
				{"samplesize=", "How many samples per hashing function (for hamming spaces we use bits, then 8 multiplier is a good option)", v => sampleSize = int.Parse(v)}
			};
			ops.Parse(args);
			if (name == null || space == null) {
				ops.WriteOptionDescriptions(Console.Out);
				throw new ArgumentNullException(String.Format("Build name: {0}, space: {1} can't be null", name, space));
			}
			if (sampleSize == 0) {
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
			//return (((BinaryHammingSpace)this.MainSpace)[0].Count * 8);
			return (this.MainSpace[0].Count * 8);
		}
		
		/// <summary>
		/// Compute the LSH hashes
		/// </summary>
		public override int ComputeHash (IList<byte> u, out int sizehash)
		{
			int hash = 0;
			var H = this.SamplingIndexes;
			for (int j = 0; j < H.Length; j++) {
				// j: position to sample
				// k: sample
				int k = H[j];
				// hash: the hash
				hash ^= ((u[k >> 3] >> (k & 7)) & 1) << (j & 31);
			}
			sizehash = H.Length;
			return hash;
		}
	}
}
