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
//   Original filename: natix/SimilaritySearch/Indexes/PivInvIndex.cs
// 
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NDesk.Options;
using natix.CompactDS;

namespace natix.SimilaritySearch
{
	public class PivInvIndex<T> : BaseIndex<T>
	{
		IDictionary<int,IRankSelect> InvIndex;
		Space<T> pivots;
		public string piv_name;
		public float ring_width;
		public int numbits_pivots;

		public PivInvIndex ()
		{
		}
		
		public override void Build (IEnumerable<string> args)
		{
			string name = null;
			string space = null;
			string spaceclass = null;
			string spacepivots = null;
			float ring = 1;
			OptionSet ops = new OptionSet() {
				{"indexname=", "Index output name", v => name = v},
				{"space=", "Space filename", v => space = v},
				{"spaceclass=", "Space class", v => spaceclass = v},
				{"pivots=", "Space pivots filename", v => spacepivots = v},
				{"ring-width|ring=", "Ring width", v => ring = float.Parse(v) }
			};
			ops.Parse(args);
			if (name == null || space == null || spaceclass == null || spacepivots == null) {
				Console.WriteLine("Index list-of-pivots options:");
				Console.WriteLine("name: {0}, space: {1}, spaceclass: {2}, spacepivots: {3}, ring: {4}",
					name,space, spaceclass, spacepivots, ring);
				ops.WriteOptionDescriptions(Console.Out);
				throw new ArgumentException("Some arguments were not given");
			}
			this.Build(name, space, spacepivots, spaceclass, ring);
		}

		public void Build (
			string indexname, string dbname,
			string pivname, string spaceClass,
			double ring_width)
		{
			this.spaceClass = spaceClass;
			this.spaceName = dbname;
			this.piv_name = pivname;
			this.LoadSpaces ();
			this.ring_width = (float)ring_width;
			this.numbits_pivots = (int)Math.Ceiling (Math.Log (this.pivots.Count, 2));
			var inv_index = new Dictionary<int, IList<int>> ();
			int size = this.MainSpace.Count;
			int percent = size / 100 + 1;
			for (int docid = 0; docid < size; docid++) {
				var obj = this.MainSpace[docid];
				if (docid % percent == 0) {
					Console.WriteLine ("--- docid: {0}, total: {1}, advance: {2:0.00}%", docid, size, docid * 100.0 / size);
				}
				for (short k = 0; k < this.pivots.Count; k++) {
					var piv = this.pivots[k];
					int key = this.GetKey (k, this.MainSpace.Dist (piv, obj));
					IList<int> L;
					if (!inv_index.TryGetValue (key, out L)) {
						L = new List<int> ();
						inv_index[key] = L;
					}
					L.Add (docid);
				}
			}
			this.InvIndex = new Dictionary<int, IRankSelect> ();
			int numkeys = inv_index.Count;
			percent = numkeys / 100 + 1;
			int keyid = 0;
			Console.WriteLine("Compressing lists: {0}", numkeys);
			foreach (var pair in inv_index) {
				if (keyid++ % percent == 0) {
					Console.WriteLine ("docid: {0}, total: {1}, advance: {2:0.00}%", keyid, numkeys, keyid * 100.0 / numkeys);
				}
				var sa = new SArray ();
				sa.Build (pair.Value);
				this.InvIndex[pair.Key] = sa;
			}
			Console.WriteLine("Saving index, num-lists {0}, num-objects: {1}, num-pivots: {2}", numkeys, size, this.pivots.Count);
			inv_index.Clear ();
			inv_index = null;
			this.Save (indexname);
		}
		
		int GetKey (short pivotId, double dist)
		{
			var cell = (int)(dist / this.ring_width);
			return pivotId | (cell << this.numbits_pivots);
		}
		
		public void Save (string name)
		{
			using (var Output = new BinaryWriter (File.Create(name + ".invindex"))) {
				Output.Write (this.InvIndex.Count);
				foreach (var p in this.InvIndex) {
					Output.Write (p.Key);
					p.Value.Save (Output);
				}
			}
			Dirty.SaveIndexXml (name, this);
		}
		
		void LoadSpaces ()
		{
			this.pivots = (Space<T>)SpaceCache.Load (this.spaceClass, this.piv_name);
			this.SetMainSpace((Space<T>)SpaceCache.Load (this.spaceClass, this.spaceName));
		}
		
		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			this.LoadSpaces ();
			this.InvIndex = new Dictionary<int, IRankSelect> ();
			using (var Input = new BinaryReader (File.OpenRead (name + ".invindex"))) {
				int len = Input.ReadInt32 ();
				for (int i = 0; i < len; i++) {
					int key = Input.ReadInt32 ();
					var sa = new SArray ();
					sa.Load (Input);
					this.InvIndex[key] = sa;
				}
			}
		}
		
		public override IResult KNNSearch (T q, int K, IResult res)
		{
			throw new NotImplementedException ();
		}
		
		public override IResult Search (T q, double radius)
		{
			throw new NotImplementedException ();
		}
	}
}

