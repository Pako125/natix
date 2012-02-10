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
//   Original filename: natix/SimilaritySearch/Indexes/Perms.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NDesk.Options;

using natix.SortingSearching;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// The index of permutations
	/// </summary>
	/// <remarks>
	/// The permutation index with a full representation of permutations.
	/// 
	/// This is an approximate index.
	/// </remarks>
	public class GenPerms<T, GType> : BaseIndex<T>
	{	
		/// <summary>
		/// The space for permutants
		/// </summary>
		Space<T> refs;
		public Space<T> RefsSpace {
			get {
				return this.refs;
			}
		}
		protected void SetRefsSpace (Space<T> p)
		{
			this.refs = p;
		}
		/// <summary>
		/// The space to take the permutants. It could be a subspace of space
		/// </summary>
		public string spacePerms;
		/// <summary>
		/// The inverses of the permutations
		/// </summary>
		protected VectorSpace<GType> invperms;
		/// <summary>
		/// Numeric manager for GType
		/// </summary>
		/// 
		protected INumeric<GType> Num = (INumeric<GType>)(Numeric.Get (typeof(GType)));
		FilterComposition<T> _OrderingFunctions = new FilterComposition<T> ();
		/// <summary>
		/// A list of ordering functions (candidate filter's composition)
		/// </summary>
		public FilterComposition<T> GetOrderingFunctions()
		{
			return this._OrderingFunctions;
		}

		// TODO: interface para espacios mapeados (e.g. perms, knr), para poder hacer consultas de manera distribuida
		// TODO: Indice knr con levenshtein, distancia soportando inversiones
		/// <summary>
		///  Get/Set the maximum number of candidates
		/// </summary>
		public int Maxcand {
			get;
			set;
		}
		/// <summary>
		/// Returns the internal vector space with the inverted permutations
		/// </summary>
		/// <returns>
		/// The inverted permutations
		/// </returns>
		public VectorSpace<GType> GetInvPermsVectorSpace ()
		{
			return this.invperms;
		}
		
		/// <summary>
		///  Get the computed inverse (stored in invperms)
		/// </summary>
		/// <param name="docid">
		/// The object id to retrieve the inverse
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// The inverse
		/// A <see cref="Int16[]"/>
		/// </returns>
		public IList<GType> GetComputedInverse (int docid)
		{
			return this.invperms[docid];
		}
	
		/// <summary>
		/// The current search cost object for the index
		/// </summary>
		public override SearchCost Cost {
			get { return new SearchCost (this.RefsSpace.NumberDistances, this.MainSpace.NumberDistances); } 
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		public GenPerms ()
		{
			this.SetMainSpace (null);
			this.SetRefsSpace (null);
		}
		
		/// <summary>
		/// API method to build a Perms Index
		/// </summary>
		/// <param name="name">
		/// The name of the index to be saved
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="spaceClass">
		/// The class name for the space. See SpaceCache
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="spaceName">
		/// The name of the database to be loaded
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="spacePerms">
		/// The name of the permutants database
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="maxcand">
		/// The default number of candidates to be verified
		/// A <see cref="System.Int32"/>
		/// </param>
		public void Build (string name, string spaceClass, string spaceName, string spacePerms, int maxcand)
		{
			this.spaceName = Dirty.ComputeRelativePath(name, spaceName);
			this.spacePerms = Dirty.ComputeRelativePath(name, spacePerms);
			this.spaceClass = spaceClass;
			this.SetMainSpace ((Space<T>)SpaceCache.Load (this.spaceClass, spaceName));
			this.SetRefsSpace ((Space<T>)SpaceCache.Load (this.spaceClass, spacePerms, null, false));
			//this.perms = GetPermutants (this.spaceperms, numperms, true);
			this.Maxcand = maxcand;
			this.invperms = null;
			var vecname = name + ".data";
			var headname = name + ".vspace";
			// dim len pnorm dosqrt ctype dommap dbnames dbvectors
			var header = String.Format ("{0} {1} 2 false h false null {2}",
				this.RefsSpace.Count, this.MainSpace.Count, vecname);
			File.WriteAllText (headname, header);
			BinaryWriter bw = new BinaryWriter (File.Create (name + ".data"));
			int onepercent = 1 + (this.MainSpace.Count / 100);
			for (int i = 0, sL = this.MainSpace.Count; i < sL; i++) {
				if ((i % onepercent) == 0) {
					Console.WriteLine ("Generating permutations for {0}, advance {1:0.00}%", i, i * 100.0 / sL);
				}
				IList<GType> perm = this.GetInverseBuild (i);
				this.SaveInverse (bw, perm);
			}
			Dirty.SaveIndexXml (name, this);
			//Dirty.SerializeBinary (name + ".data", this.root);
			bw.Close();
		}

		/// <summary>
		/// Compute the inverse for the Build method
		/// </summary>
		/// <param name="docid">
		/// The object id to compute the inverse
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// The inverse
		/// A <see cref="Int16[]"/>
		/// </returns>
		protected virtual IList<GType> GetInverseBuild (int docid)
		{
			return this.GetInverse (this.MainSpace[docid]);
		}
		
		/// <summary>
		/// (Command line) user interface Build method. Any API's Build argument in lower case.
		/// </summary>
		/// <remarks>
		/// Remember that the arguments must be given in a command line style (--kwarg value)
		/// </remarks>
		/// <param name="args">
		/// </param>
		
		public override void Build (IEnumerable<string> args)
		{
			string name = null;
			string space = null;
			string spaceclass = null;
			string spaceperms = null;
			int maxcand = 1024;
		
			OptionSet ops = new OptionSet() {
				{"indexname=", "Index output name", v => name = v},
				{"space=", "Space filename", v => space = v},
				{"spaceclass=", "Space class", v => spaceclass = v},
				{"spaceperms|perms=", "Spaceperms filename", v => spaceperms = v},
				{"maxcand=", "Default Maxcand", v => maxcand = int.Parse(v) }
			};
			ops.Parse(args);
			if (name == null || space == null || spaceclass == null || spaceperms == null) {
				Console.WriteLine("Index Perms options:");
				ops.WriteOptionDescriptions(Console.Out);
				throw new ArgumentException("Some arguments were not given");
			}
			this.Build(name, spaceclass, space, spaceperms, maxcand);
		}
		
		/// <summary>
		/// Configure the index for the following queries. Arguments should be given in command line style.
		/// </summary>
		public override void Configure (IEnumerable<string> args)
		{
			OptionSet ops = new OptionSet() {
				{"maxcand|cand=", "Maximum number of candidates to verify", v => this.Maxcand = int.Parse(v) }
			};
			var unused = ops.Parse(args);
			this.GetOrderingFunctions().Configure(unused);
		}
		
		/// <summary>
		///  Finalize the load of the index
		/// </summary>
		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			/*	Perms<T> o = (Perms<T>)Dirty.DeserializeXML (name, this.GetType ());
				this.space = o.space;
				this.spaceperms = o.spaceperms;
				this.perms = o.perms;
				this.Maxcand = o.Maxcand;
			*/
			var pathSpace = Dirty.CombineRelativePath(name, this.spaceName);
			var pathPerms = Dirty.CombineRelativePath(name, this.spacePerms);
			this.SetMainSpace( (Space<T>)SpaceCache.Load (this.spaceClass, pathSpace) );
			this.SetRefsSpace( (Space<T>)SpaceCache.Load (this.spaceClass, pathPerms, null, false) );
			int PL = this.RefsSpace.Count;
			int SL = this.MainSpace.Count;
			this.invperms = new VectorSpace<GType> (name + ".data", SL, PL, 2, false);
		}
		
		/// <summary>
		///  Save the inverse into bw stream.
		/// </summary>
		/// <param name="bw">
		/// Binary stream to store the data
		/// A <see cref="BinaryWriter"/>
		/// </param>
		/// <param name="inv">
		/// The permutation inverse
		/// A <see cref="Int16[]"/>
		/// </param>
		public virtual void SaveInverse (BinaryWriter bw, IList<GType> inv)
		{
			for (int j = 0; j < inv.Count; j++) {
				this.Num.WriteBinary (bw, inv[j]);
			}
		}
		
		/// <summary>
		/// Compute the distances from permutants to the object
		/// </summary>
		/// <param name="obj">
		/// The object
		/// </param>
		/// <returns>
		/// The computed distances
		/// A <see cref="Int16[]"/>
		/// </returns>
		public IList<GType> GetDistances (T obj)
		{
			
			double[] D = new double[this.RefsSpace.Count];
			GType[] I = new GType[D.Length];
			
			for (int i = 0; i < D.Length; i++) {
				D[i] = this.MainSpace.Dist (obj, this.RefsSpace[i]);
				I[i] = this.Num.FromInt(i);
			}
			
			Sorting.Sort<double, GType> (D, I);
			return I;
			
			/*
			Result r = new Result (this.RefsSpace.Count);
			for (int pindex = 0, pL = this.RefsSpace.Count; pindex < pL; pindex++) {
				r.Push (pindex, this.MainSpace.Dist (obj, this.RefsSpace[pindex]));
			}
			GType[] seq = new GType[this.RefsSpace.Count];
			int i = 0;
			foreach (ResultPair p in r) {
				seq[i] = this.Numeric.FromInt(p.docid);
				i++;
			}
			return seq;
			*/
		}
		/// <summary>
		///  Compute the inverse from an already computed permutation
		/// </summary>
		/// <param name="seq">
		/// The permutation
		/// A <see cref="Int16[]"/>
		/// </param>
		/// <returns>
		/// The inverse
		/// A <see cref="Int16[]"/>
		/// </returns>
		public IList<GType> GetInverseRaw (IList<GType> seq)
		{
			IList<GType> inv = new GType[seq.Count];
			var num = this.Num;
			for (int i = 0; i < inv.Count; i++) {
				inv[num.ToInt(seq[i])] = num.FromInt(i);
			}
			return inv;
		}
		
		/// <summary>
		/// Compute the inverse for an object
		/// </summary>
		/// <param name="obj">
		/// The object
		/// </param>
		/// <returns>
		/// The computed inverse
		/// A <see cref="Int16[]"/>
		/// </returns>
		public IList<GType> GetInverse (T obj)
		{
			return this.GetInverseRaw (this.GetDistances (obj));
		}
		
		/// <summary>
		/// Search by radius
		/// </summary>
		/// <param name="q">
		/// The query object
		/// </param>
		/// <param name="radius">
		/// The search radius
		/// A <see cref="System.Double"/>
		/// </param>
		/// <returns>
		/// The result set
		/// A <see cref="Result"/>
		/// </returns>
		public override IResult Search (T q, double radius)
		{
			return this.FilterByRadius (this.KNNSearch (q, Math.Abs (this.Maxcand)), radius);
		}

		/// <summary>
		/// KNN searches.
		/// </summary>
		/// <param name="q">
		/// Object to search
		/// </param>
		/// <param name="k">
		/// The number of nearest neighbors
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// The result set
		/// A <see cref="Result"/>
		/// </returns>
		public override IResult KNNSearch (T q, int k, IResult res)
		{
			IList<GType> qinv = this.GetInverse (q);
			var cand = this.invperms.CreateResult (Math.Abs (this.Maxcand), false);
			for (int docid = 0; docid < this.invperms.Count; docid++) {
				cand.Push (docid, this.invperms.Dist (this.invperms[docid], qinv));
			}
			cand = this._OrderingFunctions.Filter (this, q, qinv, cand);
			if (this.Maxcand < 0) {
				return cand;
			}
			foreach (ResultPair p in cand) {
				res.Push(p.docid, this.MainSpace.Dist(q, this.MainSpace[p.docid]));
			}
	        return res;
		}
	}
	
	/// <summary>
	///  The basic permutation class, using 16 bit signed integers
	/// </summary>
	public class Perms<T> : GenPerms<T, Int16>
	{
		public Perms () : base()
		{
		}
	}
	
	/// <summary>
	/// A simpler permutation class using byte integers
	/// </summary>
	public class Perms8<T> : GenPerms<T, byte>
	{
		public Perms8 () : base()
		{
		}
	}

}
