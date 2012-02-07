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
//   Original filename: natix/natix/SimilaritySearch/Indexes/Bkt.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
//using System.Collections.Specialized;
//using System.Collections.ObjectModel;
using NDesk.Options;

using natix.SortingSearching;

namespace natix.SimilaritySearch
{
	// TODO: Implement BktNode.children with IndexedSortedArray discarding nodes in knn (right now it's sequential over children)
	class BktNode<T>
	{
		IndexedSortedArray<int, BktNode<T> > children;
			
		int docid;
		
		public BktNode () // for Load
		{
			this.children = null;
		}
		public BktNode (int newdocid) : this()
		{
			this.docid = newdocid;
		}
		
		public int Count
		{
			get {
				return this.children == null ? 0 : this.children.Count;
			}
		}
		public override string ToString ()
		{
			if (this.children == null) {
				return "(null)";
			} else {
				StringWriter s = new StringWriter ();
				foreach (int i in this.children.Keys) {
					//s.Write ("({0},{1}),", i, this.children[i]);
					s.Write ("({0}),", i);
				}
				s.Write("<TheEnd>");
				return s.ToString();
			}
		}

		public void Load (Bkt<T> tree, BinaryReader b)
		{
			this.docid = b.ReadInt32 ();
			int n = b.ReadInt32 ();
			if (n > 0) {
				this.children = new IndexedSortedArray<int, BktNode<T>> (tree.cmpDiscDist);
				for (int i = 0; i < n; i++) {
					int slot = b.ReadInt32 ();
					var node = new BktNode<T> ();
					node.Load (tree, b);
					// don't worry, they can't be duplicates
					this.children.Add (slot, node);
				}
			}
		}

		public void Save (BinaryWriter b)
		{
			b.Write (this.docid);
			int n = 0;
			if (this.children != null) {
				n = this.children.Count;
			}
			b.Write (n);
			if (n > 0) {
				for (int i = 0; i < this.children.Count; i++) {
					b.Write (this.children.Keys[i]);
					this.children.Values[i].Save (b);
				}
			}
		}

		public void Insert (Bkt<T> tree, int newdocid)
		{
			int slot = (int)(tree.MainSpace.Dist (tree.MainSpace[this.docid], tree.MainSpace[newdocid]) / tree.RingWidth);
			if (this.children == null) {
				this.children = new IndexedSortedArray< int, BktNode<T> > (tree.cmpDiscDist);
			}
			int index = this.children.IndexOf (slot);
			//Console.WriteLine ("=== Index {0}, Slot {1}", index, slot);
			if (index < 0) {
				// doesn't exists
				this.children.Add (slot, new BktNode<T> (newdocid));
			} else {
				this.children.Values[index].Insert (tree, newdocid);
			}
		}

		public void Search (T q, double radius, Bkt<T> tree, IResult r)
		{
			double d = tree.MainSpace.Dist (tree.MainSpace[this.docid], q);
			if (d <= radius) {
				r.Push (this.docid, d);
			}
			double scale = tree.AggressivePrunningAlpha / tree.RingWidth;
			if (this.children != null) {
				int slot = (int)(d * scale);
				int radslot = (int)(radius * scale);
				int lowerBound, upperBound;
				this.children.GetIndexRange (slot - radslot, slot + radslot, radslot * 2, out lowerBound, out upperBound);
				//Console.WriteLine ("lowerBound {0}, upperBound {1}", lowerBound, upperBound);
				while (lowerBound <= upperBound) {
					this.children.Values[lowerBound].Search (q, radius, tree, r);
					lowerBound++;
				}
			}
		}
		
		public void KNNSearch (T q, int k, Bkt<T> tree, IResult r)
		{
			double d = tree.MainSpace.Dist (tree.MainSpace[this.docid], q);
			// Result objects knows how to handle bigger radius, and |r| > k
			double scale = tree.AggressivePrunningAlpha / tree.RingWidth;
			r.Push (this.docid, d);
			if (this.children != null) {
				int discreteDist = (int)(d * scale);
				/*
				 * int centerIndex = this.children.CloserIndexOf (discreteDist);
				 * double maxrad = Math.Floor (r.GetRadius () * scale);
				*/
				var _k = this.children.Keys;
				var _v = this.children.Values;
				for (int i = 0, sC = _k.Count; i < sC; i++) {
					// if (Math.Abs (_k[i] - discreteDist) <= Math.Floor (r.GetRadius () * scale)) {
					var radmax = Math.Floor (r.CoveringRadius * scale);
					if (Math.Abs (discreteDist - _k[i]) <= radmax) {
						_v[i].KNNSearch (q, k, tree, r);
					} 
				}
			}
		}
	}
	
	/// <summary>
	/// A Burkhard-Keller Tree implementation. Exact index.
	/// </summary>
	[Serializable]
	public class Bkt<T> : BaseIndex<T>
	{
		BktNode<T> root;
		int numitems;
		IComparer<int> _cmpDiscDist;
		/// <summary>
		/// Distance comparison discretized distance (Internal function, but public because I don't know how to access from BktNode)
		/// </summary>
		public IComparer<int> cmpDiscDist {
			get { return this._cmpDiscDist; }
		}
		/// <summary>
		/// Probabilistic search constant (0,1]
		/// </summary>
		public double AggressivePrunningAlpha = 1.0;
		/// <summary>
		///  The step or ring width to discretize distances
		/// </summary>
		public double RingWidth {
			get;
			set;
		}
		/// <summary>
		///  The current accumulated cost of the index
		/// </summary>
		public override SearchCost Cost {
			get { return new SearchCost (this.MainSpace.NumberDistances, 0); } 
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public Bkt ()
		{
			this.root = null;
			this.RingWidth = 1;
			this.numitems = 0;
			this._cmpDiscDist = new ComparerFromComparison<int> ((a, b) => a - b);
		}
		
		/// <summary>
		/// API Build method
		/// </summary>
		/// <param name="name">
		/// The name of the index to be saved
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="spaceClass">
		/// The name of the space's class
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="spaceName">
		/// The database name
		/// A <see cref="System.String"/>
		/// </param>
		public virtual void Build (string name, string spaceClass, string spaceName)
		{
			this.spaceClass = spaceClass;
			this.spaceName = spaceName;
			this.SetMainSpace( (Space<T>)SpaceCache.Load (this.spaceClass, this.spaceName) );
			Console.WriteLine ("======> sp-class: {0}, sp-Name: {1}", this.spaceClass, this.spaceName);

			for (int i = 0, sL = this.MainSpace.Count; i < sL; i++) {
				this.Insert (i);
				if ((i % 1000) == 0) {
					Console.WriteLine ("Bkt inserting docid {0}, advance {1:0.00}%", i, i * 100.0 / sL);
				}
			}
			//Dirty.SerializeBinary (name + ".data", this.root);
			BinaryWriter b = new BinaryWriter (File.Create(name + ".raw"));
			this.root.Save (b);
			b.Close ();
			Dirty.SaveIndexXml (name, this);
		}

		/// <summary>
		/// (Command line) user interface to Build method <see cref="natix.Index.Build"/>
		/// </summary>
		/// <param name="args">
		/// Arguments in command line syntax (--keyword value)
		/// </param>
		public override void Build (IEnumerable<string> args)
		{
			string name = null;
			string space = null;
			string spaceclass = null;
			OptionSet ops = new OptionSet() {
				{"indexname=", "Index output name", v => name = v},
				{"spaceclass=", "Space class", v => spaceclass = v},
				{"space=", "Space filename", v => space = v},
				{"ringwidth|rwidth|rw=", "Ring width", v => this.RingWidth = double.Parse(v) },
				{"alpha=", "alpha constant for aggressive prunning (0 to 1)", v => this.AggressivePrunningAlpha = double.Parse(v) }
			};
			ops.Parse(args);
			if (name == null || space == null || spaceclass == null) {
				Console.WriteLine("Bkt options: ");
				ops.WriteOptionDescriptions(Console.Out);
				throw new ArgumentException("Some mandatory arguments were not given");
			}
			this.Build(name, spaceclass, space);
		}
		
		/// <summary>
		/// Configure the searches
		/// </summary>
		/// <param name="args">
		/// </param>
		public override void Configure(IEnumerable<string> args)
		{
			OptionSet ops = new OptionSet() {
				{"alpha=", "alpha constant for aggressive prunning (0 to 1)", v => this.AggressivePrunningAlpha = double.Parse(v) }
			};
			ops.Parse(args);
		}

		/// <summary>
		/// Finalize to load the index. <see cref="natix.IndexLoader.Load"/>
		/// </summary>
		/// <param name="name"> 
		/// </param>
		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			this.SetMainSpace ((Space<T>)SpaceCache.Load (this.spaceClass, this.spaceName));
			BinaryReader b = new BinaryReader (File.OpenRead (name + ".raw"));
			this.root = new BktNode<T> ();
			this.root.Load (this, b);
			b.Close ();
		}

		/// <summary>
		/// Search by range
		/// </summary>
		/// <param name="q">
		/// The query object
		/// </param>
		/// <param name="radius">
		/// Radius of the search <see cref="System.Double"/>
		/// </param>
		/// <returns>
		/// A <see cref="Result"/>
		/// </returns>
		public override IResult Search (T q, double radius)
		{
			var r = new Result (this.MainSpace.Count);
			if (this.root == null) {
				return r;
			}
			this.root.Search (q, radius, this, r);
			return r;
		}
		// TODO: The optimal IResult should be given by the mainspace, because is the only knowning the dist. dist.
		/// <summary>
		///  KNN Search
		/// </summary>
		/// <param name="q">
		/// The query object
		/// </param>
		/// <param name="k">
		/// Number of nearest neighbors
		/// <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// The result set
		/// <see cref="Result"/>
		/// </returns>
		public override IResult KNNSearch (T q, int k, IResult r)
		{
			if (this.root == null) {
				return r;
			}
			this.root.KNNSearch (q, k, this, r);
			return r;
		}
		
		/// <summary>
		/// Insert some object into the BKT
		/// </summary>
		/// <param name="newdocid">
		/// A <see cref="System.Int32"/>
		/// </param>
		public void Insert (int newdocid)
		{
			this.numitems++;
			if (this.root == null) {
				this.root = new BktNode<T> (newdocid);
			} else {
				this.root.Insert (this, newdocid);
			}
		}
	}
}
