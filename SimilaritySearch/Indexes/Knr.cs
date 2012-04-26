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
//   Original filename: natix/SimilaritySearch/Indexes/Knr.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NDesk.Options;
using natix.SortingSearching;
using System.Threading.Tasks;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// The K Nearest Permutants index base class
	/// </summary>
	public abstract class Knr<T>: BaseIndex<T>
	{
		// TODO: Save the whole set of distances (pivots) and feed perm, pivots and knr based indexes from it.
		/// <summary>
		/// Index for perms
		/// </summary>
		protected Index<T> IndexRefs;
		/// <summary>
		/// File name of the index perms
		/// </summary>
		public string IndexRefsName;
		/// <summary>
		/// The array to store the knr sequences
		/// </summary>
		protected SequenceSpace<UInt16> SeqSpace;
		/// <summary>
		/// Maximum Knr distance to be useful.
		/// </summary>
		/// <remarks>
		/// Useful to limit sequential implementations of indexes not overloading
		/// the generic KNNSearch.
		/// </remarks>
		protected float MaxKnrDist = float.MaxValue;
		/// <summary>
		/// Accesor to the internal sequences
		/// </summary>
		public virtual IList<IList<UInt16>> GetListOfKnrSeq ()
		{
			return this.SeqSpace.seqs;
		}

		public virtual void LoadListOfKnrSeq (string name)
		{
			if (this.SeqSpace == null) {
				this.SeqSpace = (SequenceSpace<UInt16>)SpaceCache.Load ("sequence-uint16", name + ".seqspace", null, false);
			}
		}
		
		public IList<UInt16> GetSingleKnrSeq (int i)
		{
			return this.SeqSpace[i];
		}
		
		public SequenceSpace<UInt16> GetSeqSpace ()
		{
			return this.SeqSpace;
		}
		
		FilterComposition<T> _OrderingFunctions = new FilterComposition<T> ();
		
		/// <summary>
		/// A list of ordering functions (candidate filter's composition)
		/// </summary>
		public FilterComposition<T> GetOrderingFunctions()
		{
			return this._OrderingFunctions;
		}
		
		/// <summary>
		/// The suggested number of candidates for the method
		/// </summary>
		public int Maxcand {
			get;
			set;
		}
		
		/// <summary>
		///  The bound to be used as knr.
		/// </summary>
		/// <remarks>
		/// Negatives numbers are given for exact k elements.
		/// Positive numbers perform a ceiling knr operation (search by range at the knn radius)
		/// </remarks>
		public int KnrBound {
			get;
			set;
		}
		/// <summary>
		/// The KnrBound as in building time
		/// </summary>
		public int KnrBoundBuild {
			get;
			set;
		}
	
		/// <summary>
		/// Constructor
		/// </summary>
		public Knr ()
		{
			this.SetMainSpace (null);
			this.SeqSpace = null;
			this.Maxcand = 1000;
			this.KnrBound = 0;
			this.KnrBoundBuild = 0;
		}
		
		protected void LoadSpaceAndRefs (string basePath, string spaceClass, string spaceName, string indexRefs, bool computeRelativePaths)
		{
			if (computeRelativePaths) {
				spaceName = Dirty.ComputeRelativePath (basePath, spaceName);
				indexRefs = Dirty.ComputeRelativePath (basePath, indexRefs);
			}
			var pathSpace = Dirty.CombineRelativePath (basePath, spaceName);
			var pathRefs = Dirty.CombineRelativePath (basePath, indexRefs);
			Console.WriteLine ("pathSpace: {0}, pathRefs: {1}", pathSpace, pathRefs);
			this.spaceClass = spaceClass;
			this.spaceName = spaceName;
			this.IndexRefsName = indexRefs;
			this.SetMainSpace((Space<T>)SpaceCache.Load (spaceClass, pathSpace));
			this.IndexRefs = (Index<T>)IndexLoader.Load (pathRefs);
		}

		/// <summary>
		/// API Build
		/// </summary>
		/// <param name="name">
		/// The filename to save the index
		/// </param>
		/// <param name="spaceClass">
		/// The space's class name
		/// </param>
		/// <param name="spaceName">
		/// The database's name
		/// </param>
		/// <param name="indexpermsname">
		/// The index for permutants (filename)
		/// </param>
		/// <param name="maxcand">
		/// The default number of candidates to be verified 
		/// </param>
		/// <param name="knrbound">
		/// The knrbound
		/// </param>
		/// <param name="_GetKnr">
		/// Function to retrieve the knr representation of the database,
		/// prototype Func(System.Int32, IList<UInt16>), null to compute the knr
		/// </param>
		public virtual void Build (string name, string spaceClass, string spaceName, string indexpermsname, int maxcand, int knrbound, Func<int, IList<UInt16> > _GetKnr, bool parallel_build)
		{
			this.LoadSpaceAndRefs (name, spaceClass, spaceName, indexpermsname, true);
			this.Maxcand = maxcand;
			this.KnrBound = knrbound;
			this.KnrBoundBuild = knrbound;
			var sname = name + ".seqspace";
			var head = String.Format ("--sequences {0}.seqs --size {1} --distance None --usedisk False", sname, this.MainSpace.Count);
			File.WriteAllText (sname, head);
			using (var w = new StreamWriter (File.Create (sname + ".seqs"))) {
				IList<UInt16>[] seqs;
				Console.WriteLine ("Starting construction of {0}, parallel: {1}", name, parallel_build);
				if (parallel_build) {
					seqs = this.ParallelBuild (_GetKnr, indexpermsname);
				} else {
					seqs = this.SequentialBuild (_GetKnr, indexpermsname);
				}
				foreach (var seq in seqs) {
					for (int i = 0; i < seq.Count; ++i) {
						if (i + 1 == seq.Count) {
							w.Write ("{0}", seq [i]);
						} else {
							w.Write ("{0} ", seq [i]);
						}
					}
					w.WriteLine ();
				}
			}
			Dirty.SaveIndexXml (name, this);
		}

		protected virtual IList<UInt16>[] ParallelBuild (Func<int, IList<UInt16> > _GetKnr, string nick)
		{
			var len = this.MainSpace.Count;
			var seqs = new IList<UInt16> [len];
			int pc = len / 1000 + 1;
			int I = 0;
			Action<int> compute_knr = delegate (int docid) {
				seqs [docid] = _GetKnr (docid);
				++I; // don't care about race conditions
				//Console.WriteLine ("===> I: {0}", I);
				if ((I % pc) == 0) {
					Console.WriteLine ("Parallel-Build Knr docid {0} of {1} ({2}), advance {3:0.00}%. Timestamp: {4} ",
					                   I, len, nick, I * 100.0 / len, DateTime.Now.ToString ());
				}
			};
			Parallel.For (0, len, compute_knr);
			return seqs;
		}

		protected virtual IList<UInt16>[] SequentialBuild (Func<int, IList<UInt16> > _GetKnr, string nick)
		{
			var len = this.MainSpace.Count;
			var seqs = new IList<UInt16> [len];
			int pc = len / 1000 + 1;
			for (int docid = 0; docid < len; docid++) {
				seqs [docid] = _GetKnr (docid);
				if ((docid % pc) == 0) {
					Console.WriteLine ("Sequential-Build Knr docid {0} of {1} ({2}), advance {3:0.00}%. Timestamp: {4} ",
					                   docid, len, nick, docid * 100.0 / len, DateTime.Now.ToString ());
				}
			}
			return seqs;
		}
		
		/// <summary>
		/// (Command line) user interface. Command line like arguments (--kwarg value)
		/// </summary>
		/// <param name="args">
		/// </param>
		public override void Build (IEnumerable<string> args)
		{
			string name = null;
			string space = null;
			string spaceclass = null;
			string indexrefs = null;
			string fromperms = null;
			string fromknr = null;
			bool parallel_build = true;
			int numrefs = -1;
			
			OptionSet ops = new OptionSet () {
				{"indexname|index=", "Index output name", v => name = v},
				{"space=", "Space filename", v => space = v},
				{"spaceclass=", "Space class", v => spaceclass = v},
				{"indexperms|indexrefs=", "SpaceRefs filename", v => indexrefs = v},
				{"fromperms=", "Permutants index to be used as previously computed perms index", v => fromperms = v},
				{"fromknr=", "Permutants index to be used as previously computed index", v => fromknr = v},
				{"knrbound=", "Knr bound (negative means knn, positive means ceiling radius in knn)", v => this.KnrBound = int.Parse (v) },
				{"maxcand=", "Default Maxcand", v => this.Maxcand = int.Parse (v) },
				{"sequentialbuild", "Sequential build (default parallel)", (v) => parallel_build = false },	
				{"numrefs|numperms=", "Number of references (only if indexrefs is null) ", (v) => numrefs = int.Parse (v)}
			};
			ops.Parse (args);
			if (fromknr != null && fromperms != null) {
				// THESE METHODS ARE MUTUALLY EXCLUSIVE
				Console.WriteLine ("Building options for Knr {0}", this);
				ops.WriteOptionDescriptions (Console.Out);
				throw new ArgumentException ("fromknr and fromperms are mutually exclusive options");
			}
			if (fromperms == null && fromknr == null) {
				// CONSTRUCTION BASE - THIS COMPUTES DISTANCES
				if (name == null || space == null || spaceclass == null || this.KnrBound == 0) {
					Console.WriteLine ("Building options for Knr {0}", this);
					Console.WriteLine ("indexname: '{0}', space: '{1}', spaceclass: '{2}', indexrefs: '{3}', knrbound: {4}",
					                  name, space, spaceclass, indexrefs, this.KnrBound);
					ops.WriteOptionDescriptions (Console.Out);
					throw new ArgumentException ("Some mandatory parameters are null");
				}
				if (indexrefs == null) {
					if (numrefs <= Math.Abs (this.KnrBound)) {
						Console.WriteLine ("IMPORNTANT If *indexrefs* is not given, then *numrefs* > knrbound should be given to create the proper structures");
						Console.WriteLine ("Building options for Knr {0}", this);
						Console.WriteLine ("indexname: '{0}', space: '{1}', spaceclass: '{2}', indexrefs: '{3}', knrbound: {4}",
						                   name, space, spaceclass, indexrefs, this.KnrBound);
						ops.WriteOptionDescriptions (Console.Out);
						throw new ArgumentException ("Some mandatory parameters are null");
					}
					var subspace = String.Format ("{0}.subspace.{1}", name, numrefs);
					indexrefs = subspace + ".index";
					Commands.SubSpace (subspace, spaceclass, space, numrefs, true, true);
					var cmd = String.Format ("--build --indexname {0} --indexclass sequential --space {1} --spaceclass {2} --force", indexrefs, subspace, spaceclass);
					Commands.Build (cmd);				
				}
				this.Build (name, spaceclass, space, indexrefs, this.Maxcand, this.KnrBound, docid => this.GetKnr (this.MainSpace [docid]), parallel_build);
			} else if (fromperms != null) {
				// CONSTRUCTION FROM AN ALREADY COMPUTED PERMS INDEX
				if (this.KnrBound >= 0) {
					Console.WriteLine ("Building options for Knr {0}", this);
					ops.WriteOptionDescriptions (Console.Out);					
					throw new ArgumentException ("KnrBound should be negative (knn search for knr)");	
				}
				if (space != null) {
					Console.WriteLine ("Building options for Knr {0}", this);
					ops.WriteOptionDescriptions (Console.Out);					
					throw new ArgumentException (String.Format ("space will be taken from perms {0} index", fromperms));	
				}
				if (name == null || indexrefs == null) {
					Console.WriteLine ("Building options for Knr {0}", this);
					Console.WriteLine ("Checking for null> indexname: '{0}', indexperms: '{1}'", name, indexrefs);
					ops.WriteOptionDescriptions (Console.Out);
					throw new ArgumentException ("Some null parameters building 'fromperms'");
				}
				var iperms = (Index<T>)IndexLoader.Load (indexrefs);
				var perms = (Perms<T>)IndexLoader.Load (fromperms);
				if (Path.GetFileName (iperms.MainSpace.Name) != Path.GetFileName (perms.spacePerms) || spaceclass != perms.spaceClass) {
					Console.WriteLine ("Building options for Knr {0}", this);
					Console.WriteLine ("=== iperms.MainSpace.Name '{0}', perms.spaceName '{1}'", iperms.MainSpace.Name, perms.spacePerms);
					Console.WriteLine ("=== spaceclass '{0}',  perms.spaceClass '{1}'", spaceclass, perms.spaceClass);
					ops.WriteOptionDescriptions (Console.Out);
					throw new ArgumentException ("indexperms.MainSpace.Name != perms.spaceName or spaceclass != perms.spaceClass");
				}
				string pathSpace = Dirty.CombineRelativePath (fromperms, perms.spaceName);
				this.Build (name, perms.spaceClass, pathSpace, indexrefs, this.Maxcand, this.KnrBound, docid => this.GetKnrFromPerms (docid, perms), parallel_build);
			} else if (fromknr != null) {
				// BUILD FROM AN ALREADY CONSTRUCTED KNR Method
				Console.WriteLine ("Please notice that only Knr based indexes with KnrWrap void methods should be used");
				Console.WriteLine ("(e.g. PPIndex), unless you know that the KnrWrap is not destroying the");
				Console.WriteLine ("data for your index");
				if (space != null) {
					Console.WriteLine ("Building options for Knr {0}", this);
					ops.WriteOptionDescriptions (Console.Out);					
					throw new ArgumentException (String.Format ("space will be taken from perms {0} index", fromperms));	
				}
				if (name == null || indexrefs != null /*|| this.KnrBound != 0*/) {
					Console.WriteLine ("Building options for Knr {0}, fromknr", this);
					Console.WriteLine ("*** indexname can't be null: '{0}'", name);
					Console.WriteLine ("*** indexperms should be null: '{0}'", indexrefs);
					ops.WriteOptionDescriptions (Console.Out);
					throw new ArgumentException ("Some null parameters building 'fromknr'");
				}
				var permsknr = (Knr<T>)IndexLoader.Load (fromknr);
				if (spaceclass != permsknr.spaceClass) {
					Console.WriteLine ("Building options for Knr {0}, fromknr", this);
					Console.WriteLine ("=== spaceclass '{0}',  permsknr.spaceClass '{1}'", spaceclass, permsknr.spaceClass);
					ops.WriteOptionDescriptions (Console.Out);
					throw new ArgumentException ("spaceclass != permsknr.spaceClass");
				}
				Func<int, IList<UInt16> > getknrfun;
				if (this.KnrBound == 0) {
					this.KnrBound = permsknr.KnrBound;
				}
				if (this.KnrBound < 0) {
					getknrfun = (docid => this.KnrWrap (this.TakeFirst (permsknr.SeqSpace [docid], Math.Abs (this.KnrBound))));	
				} else {
					getknrfun = (docid => this.KnrWrap (permsknr.SeqSpace [docid]));
				}
				string pathSpace = Dirty.CombineRelativePath (fromknr, permsknr.spaceName);
				string pathIndexRefs = Dirty.CombineRelativePath (fromknr, permsknr.IndexRefsName);
				this.Build (name, permsknr.spaceClass, pathSpace, pathIndexRefs, this.Maxcand, this.KnrBound, getknrfun, parallel_build);
			} else {
				Console.WriteLine ("Building options for Knr {0}, fromknr", this);
				ops.WriteOptionDescriptions (Console.Out);
				throw new ArgumentException ("Not enought options for building knr index");
			}
		}
		
		IList<UInt16> TakeFirst (IList<UInt16> s, int count)
		{
			UInt16[] r = new UInt16[count];
			for (int i = 0; i < count; i++) {
				r[i] = s[i];
			}
			return r;
		}
		/// <summary>
		/// GetKnr from perms
		/// </summary>
		IList<UInt16> GetKnrFromPerms (int docid, Perms<T> perms)
		{
			IList<Int16> directperm = perms.GetInverseRaw (perms.GetComputedInverse (docid));
			int k = (int)(-this.KnrBound);
			UInt16[] KnrSeq = new UInt16[k];
			for (int i = 0; i < k; i++) {
				KnrSeq[i] = (UInt16)directperm[i];
			}
			// Console.WriteLine ("KnrSeq.Len {0}", KnrSeq.Length);
			return this.KnrWrap (KnrSeq);
		}
		/// <summary>
		/// Accumulated search cost
		/// </summary>
		public override SearchCost Cost {
			get { return new SearchCost (this.IndexRefs.Cost.Internal, this.MainSpace.NumberDistances); }
		}
		/// <summary>
		/// Knr distance (internal distance)
		/// </summary>
		public virtual double KnrDist (IList<UInt16> a, IList<UInt16> b)
		{
			throw new NotImplementedException ("This is an abstract method");
		}
		/// <summary>
		/// Wrapping function to the knr implementation
		/// </summary>
		public virtual IList<UInt16> KnrWrap (IList<UInt16> a)
		{
			throw new NotImplementedException ("This is an abstract method");
		}
		
		/// <summary>
		/// Compute the knr set
		/// </summary>
		public IList<UInt16> GetKnr (T obj)
		{
			return this.GetKnr (obj, true);
		}
		
		/// <summary>
		/// Compute the knr set
		/// </summary>
		public IList<UInt16> GetKnr (T obj, bool wrap)
		{
			int iknr = 0;
			//int numdists = this.indexperms.MainSpace.NumberDistances;
			//long t = DateTime.Now.Ticks;
			var bound = Math.Abs (this.KnrBound);
			var r = this.IndexRefs.MainSpace.CreateResult (bound, this.KnrBound > 0);
			r = this.IndexRefs.KNNSearch (obj, bound, r);
			int iknrmax = Math.Min (ushort.MaxValue, r.Count);
			UInt16[] knrset = new UInt16[iknrmax];
			foreach (ResultPair p in r) {
				knrset[iknr] = (UInt16)p.docid;
				iknr++;
				if (iknr >= iknrmax) {
					break;
				}
			}
			if (wrap) {
				return this.KnrWrap (knrset);
			} else {
				return knrset;
			}
		}
		
		/// <summary>
		/// Configure the index
		/// </summary>
		/// <param name="args">
		/// </param>
		public override void Configure(IEnumerable<string> args)
		{
			OptionSet ops = new OptionSet() {
				{"knrbound=", "The knr-bound. Negative means knn on perms, positive means ceiling knr", v => this.KnrBound = int.Parse(v) },
				{"maxcand|cand=", "The number of candidates to be verified in the original distance", v => this.Maxcand = int.Parse(v) },
				{"maxknrdist=", "Maximum KNR distance to be allowed by the index (mostly for sequential indexes)", v => this.MaxKnrDist = float.Parse(v)}
			};
			var unused = ops.Parse(args);
			this.GetOrderingFunctions().Configure(unused);
		}
		
		/// <summary>
		/// Dump the knr as ascii
		/// </summary>
		public void DumpKnrAscii ()
		{
			foreach (IList<UInt16> d in this.SeqSpace.seqs) {
				foreach (UInt16 e in d) {
					Console.Write ("{0} ", e);
				}
				Console.WriteLine ();
			}
		}
		
		/// <summary>
		/// Finalize the load of an index (<see cref="natix.IndexLoader.Load"/>)
		/// </summary>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			this.LoadSpaceAndRefs (name, this.spaceClass, this.spaceName, this.IndexRefsName, false);
			Console.WriteLine ("Load Knr index {0}", name);
			this.LoadListOfKnrSeq (name);
			Console.WriteLine ("Loaded");
		}

		/// <summary>
		/// Search range
		/// </summary>
		/// <param name="q">
		/// Query object
		/// </param>
		/// <param name="radius">
		/// A <see cref="System.Double"/>
		/// </param>
		/// <returns>
		/// A <see cref="Result"/>
		/// </returns>
		public override IResult Search (T q, double radius)
		{
			return this.FilterByRadius(this.KNNSearch (q, Math.Abs(this.Maxcand)), radius);
		}
		
		/// <summary>
		/// KNN Search
		/// </summary>
		public override IResult KNNSearch (T q, int k, IResult R)
		{
			int maxcand = Math.Abs (this.Maxcand);
			IResult res = new Result (maxcand, false);
			IList<UInt16> qseq = this.GetKnr (q);

			for (int docid = 0, sL = this.SeqSpace.Count; docid < sL; docid++) {
				IList<UInt16> oseq = this.SeqSpace [docid];
				double d = this.KnrDist (oseq, qseq);
				if (/*docid < maxcand &&*/ d <= this.MaxKnrDist) {
					res.Push (docid, d);
				}
			}
			// possible induction of a new order in the candidates
			res = this.GetOrderingFunctions ().Filter (this, q, qseq, res);
			if (this.Maxcand < 0) {
				return res;
			}
			// The final review
			foreach (ResultPair p in res) {
				double d = this.MainSpace.Dist (q, this.MainSpace [p.docid]);
				R.Push (p.docid, d);
			}
			return R;
		}
		
		/// <summary>
		/// Creates a permutation of the space using the Knr distance
		/// In theory it must increase the locality of reference.
		/// </summary>
		public virtual void ComputeClusterPermutation (string permOut, bool sortKnrSeq)
		{
			int[] permutation = new int[this.MainSpace.Count];
			for (int i = 0; i < permutation.Length; i++) {
				permutation[i] = i;
			}
			Console.WriteLine ("Begin sort by KnrDist");
			Chronos chronos = new Chronos ();
			chronos.Begin ();
			var W = this.GetListOfKnrSeq ();
			IList<IList<ushort>> G;
			if (sortKnrSeq) {
				var seq_list = new ushort[W.Count][];
				for (int i = 0; i < seq_list.Length; i++) {
					var g = W[i];
					var l = seq_list[i] = new ushort[g.Count];
					for (int j = 0; j < l.Length; j++) {
						l[j] = g[j];
					}
					Array.Sort<ushort> (l);
				}
				G = seq_list;
			} else {
				G = W;
			}
			Sorting.Sort<int> (permutation, (int a, int b) =>
				SequenceSpace<ushort>.LexicographicCompare (G[a], G[b]));
			Comparison<int> cmp_sort = (int a, int b) => SequenceSpace<ushort>.LexicographicCompare (G[a], G[b]);
			Sorting.LocalInsertionSort<int, int> (permutation, null, 0, permutation.Length, cmp_sort);
			// Sorting.Sort<int> (permutation, cmp_sort);
			// Sorting.SortSeq (permutation, null, 0, permutation.Length);
			chronos.End ();
			Console.WriteLine ("End sort.");
			chronos.PrintStats ();
			// Console.WriteLine ("Starting permutation of the space");
			// this.MainSpace.SubSpace (spaceOut, permutation);
			Console.WriteLine ("Writing permutation to {0}", permOut);
			using (var wperm = new BinaryWriter(File.Create(permOut))) {
				PrimitiveIO<int>.WriteVector( wperm, permutation );
			}
		}
	}
}