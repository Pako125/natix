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
//   Original filename: natix/natix/SimilaritySearch/Indexes/Pivots.cs
// 
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using NDesk.Options;

namespace natix.SimilaritySearch
{

	/// <summary>
	/// Maps to pivot's vector space, it can be used to permutation and KNR indexes
	/// </summary>
	public class Pivots<T, GType> : BaseIndex<T>
	{
		/// <summary>
		/// The pivot space
		/// </summary>
		protected Space<T> pivots = null;
				
		/// <summary>
		/// The cost of the index (accumulated distances)
		/// </summary>
		public override SearchCost Cost {
			get { return new SearchCost (this.pivots.NumberDistances, this.MainSpace.NumberDistances); }
		}
		/// <summary>
		/// the name of the pivots database
		/// </summary>
		public string spacePivots = null;
		
		/// <summary>
		/// The mapped space
		/// </summary>
		public string mappedSpace = null;
		
		/// <summary>
		/// Distances to pivots
		/// </summary>
		protected VectorSpace<GType> pivdists;
	
		/// <summary>
		/// Mapped space, distances to pivots
		/// </summary>
		public VectorSpace<GType> PivotDistances
		{
			get { return this.pivdists; }
		}
		/// <summary>
		/// Constructor
		/// </summary>
		public Pivots ()
		{
		}
		
		/// <summary>
		/// Build index pivots
		/// </summary>
		public void Build (string name, string _spaceClass, string _spaceName, string _spacePivots)
		{
			this.spaceClass = _spaceClass;
			this.spaceName = _spaceName;
			this.spacePivots = _spacePivots;
			this.pivots = (Space<T>)SpaceCache.Load (spaceClass, spaceName);
			this.SetMainSpace ((Space<T>)SpaceCache.Load (spaceClass, spaceName));
			this.mappedSpace = name + ".vspace";
			int sL = this.MainSpace.Count;
			double[] V = new double[this.pivots.Count];
			//dim len pnorm dosqrt ctype dommap dbnames dbvectors 
			string header = String.Format ("{0} {1} -1 False f False None {2}",
				this.pivots.Count, this.MainSpace.Count, this.mappedSpace + ".vec");
			File.WriteAllText (this.mappedSpace, header);
			var f = new FileStream (this.mappedSpace + ".vec", FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20);
			var w = new StreamWriter (f, Encoding.ASCII);
			for (int i = 0; i < sL; i++) {
				this.ComputePivots (this.MainSpace[i], V);
				this.SaveVector (w, i, V);
			}
			w.Close ();
			Dirty.SaveIndexXml (name, this);
		}
		
		/// <summary>
		/// Save vector
		/// </summary>
		public virtual void SaveVector (StreamWriter f, int docid, double[] V)
		{
			for (int i = 0; i < V.Length; i++) {
				f.Write (V[i].ToString ());
				if ((i + 1) < V.Length) {
					f.Write (" ");
				} else {
					f.WriteLine ();
				}
			}
		}
		
		/// <summary>
		/// Compute pivots
		/// </summary>
		public void ComputePivots (T u, double[] V)
		{
			int pL = this.pivots.Count;
			for (int i = 0; i < pL; i++) {
				V[i] = this.pivots.Dist (u, this.pivots[i]);
			}
		}

		/// <summary>
		/// Compute pivots
		/// </summary>
		public void ComputePivots (T u, GType[] V)
		{
			int pL = this.pivots.Count;
			for (int i = 0; i < pL; i++) {
				V[i] = this.pivdists.Numeric.FromDouble (this.pivots.Dist (u, this.pivots[i]));
			}
		}

		/// <summary>
		/// Encode a query into a vector of distances to pivots
		/// </summary>
		public GType[] Encode (T u)
		{
			GType[] g = new GType[this.pivots.Count];
			this.ComputePivots (u, g);
			return g;
		}
		
		/// <summary>
		/// Build a pivot index
		/// </summary>
		public override void Build (IEnumerable<string> args)
		{
			string _spaceClass = null;
			string _spaceName = null;
			string _spacePivots = null;
			string _name = null;

			OptionSet op = new OptionSet() {
				{"spaceclass=", "The space class", v => _spaceClass = v},
				{"spacename|space=", "The database name", v => _spaceName = v},
				{"spacepivots|pivots=", "The pivots database", v => _spacePivots = v},
				{"index|indexname=", "The output index name", v => _name = v}
			};
			op.Parse(args);
			this.Build (_name, _spaceClass, _spaceName, _spacePivots);
		}
		
		/// <summary>
		/// Finalize the index's load
		/// </summary>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			this.SetMainSpace ((Space<T>)SpaceCache.Load (this.spaceClass, this.spaceName));
			this.pivots = (Space<T>)SpaceCache.Load (this.spaceClass, this.spacePivots);
			this.pivdists = new VectorSpace<GType> ();
			this.pivdists.Name = this.spacePivots;
		}
		
		/// <summary>
		/// Radius search
		/// </summary>
		public override IResult Search (T q, double radius)
		{
			List<int> C = new List<int> ();
			int sL = this.MainSpace.Count;
			var qenc = this.Encode (q);
			double dist;
			for (int i = 0; i < sL; i++) {
				dist = this.pivdists.Dist (qenc, this.pivdists[i]);
				if (dist <= radius) {
					C.Add (i);
				}
			}
			var R = this.MainSpace.CreateResult (sL, false);
			for (int i = 0; i < C.Count; i++) {
				dist = this.MainSpace.Dist (q, this.MainSpace[C[i]]);
				if (dist <= radius) {
					R.Push (i, dist);
				}
			}
			return R;
		}


		/// <summary>
		/// Searching by KNN
		/// </summary>
		public override IResult KNNSearch (T q, int K, IResult R)
		{
			int sL = this.MainSpace.Count;
			var C = this.MainSpace.CreateResult (sL, false);
			var qenc = this.Encode (q);
			double dist;
			for (int i = 0; i < sL; i++) {
				dist = this.pivdists.Dist (qenc, this.pivdists[i]);
				if (dist <= C.CoveringRadius) {
					C.Push (i, dist);
				}
			}
			foreach(ResultPair p in C) {
				dist = this.MainSpace.Dist (q, this.MainSpace[p.docid]);
				R.Push (p.docid, dist);
			}
			return R;
		}
	}
}
