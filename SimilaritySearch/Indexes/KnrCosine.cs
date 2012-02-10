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
//   Original filename: natix/SimilaritySearch/Indexes/KnrCosine.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Linq;
using System.Threading;
using NDesk.Options;

namespace natix.SimilaritySearch
{
	public interface KnrCosineBase
	{
		DocumentSpace GetDocumentSpace();
	}
	
	/// <summary>
	/// The K Nearest Permutants index base class
	/// </summary>
	public class KnrCosine<T> : BaseIndex<T>, KnrCosineBase
	{
		/// <summary>
		/// Index for perms
		/// </summary>
		protected Index<T> indexperms;
		/// <summary>
		/// File name of the index perms
		/// </summary>
		protected string indexpermsname;
		/// <summary>
		/// The representation as document space
		/// </summary>
		protected DocumentSpace docspace;
		/// <summary>
		/// The suggested number of candidates for the method
		/// </summary>
		public int Maxcand {
			get;
			set;
		}
		
		public DocumentSpace GetDocumentSpace ()
		{
			return this.docspace;
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
		/// The Index for permutations. It must be already indexed
		/// </summary>
		public string IndexPermsName {
			get { return this.indexpermsname; }
			set {
				this.indexpermsname = value;
				this.indexperms = (Index<T>) IndexLoader.Load (this.indexpermsname);
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public KnrCosine ()
		{
			this.SetMainSpace(null);
			this.Maxcand = 1000;
			this.KnrBound = 0;
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
		public virtual void Build (string name, string spaceClass, string spaceName, string indexpermsname, int maxcand, int knrbound)
		{
			this.spaceClass = spaceClass;
			this.spaceName = Dirty.ComputeRelativePath(name, spaceName);
			this.SetMainSpace ((Space<T>)SpaceCache.Load (this.spaceClass, spaceName));
			this.IndexPermsName = indexpermsname;
			this.Maxcand = maxcand;
			this.KnrBound = knrbound;
			string docname = name + ".docspace";
			var wvecs = new StreamWriter (docname + ".docvecs");
			var wnames = new StreamWriter (docname + ".dbnames");
			//Directory.CreateDirectory(docname);
			Int32 sL = this.MainSpace.Count;
			for (int docid = 0; docid < sL; docid++) {
				IList<UInt32> knrseq;
				IList<Single> knrdist;
				this.GetKnr (this.MainSpace[docid], out knrseq, out knrdist);
				for (int iknr = 0; iknr < knrseq.Count; iknr++) {
					wvecs.Write ("{0} {1};", knrseq[iknr], knrdist[iknr]);
				}
				wvecs.WriteLine ();
				wnames.WriteLine (docid.ToString ());
				if ((docid % 1000) == 0) {
					Console.WriteLine ("Knr index docid {0}, advance {1:0.00}%", docid, docid * 100.0 / sL);
				}
			}
			wvecs.Close ();
			wnames.Close ();
			File.WriteAllText (docname, String.Format ("--numdocs {0} --vecdocs {1}.docvecs --dbnames {1}.dbnames", sL, Path.GetFileName(docname)));
			Dirty.SaveIndexXml (name, this);
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
			string indexperms = null;
			OptionSet ops = new OptionSet() {
				{"indexname=", "Index output name", v => name = v},
				{"space=", "Space filename", v => space = v},
				{"spaceclass=", "Space class", v => spaceclass = v},
				{"indexperms=", "Spaceperms filename", v => indexperms = v},
				{"knrbound=", "Knr bound (negative means knn, positive means ceiling radius in knn)", v => this.KnrBound = int.Parse(v) },
				{"maxcand=", "Default Maxcand", v => this.Maxcand = int.Parse(v) },
			};
			ops.Parse(args);
			if (name  == null || space == null || spaceclass == null || indexperms  == null || this.KnrBound == 0) {
				Console.WriteLine("Building options for Knr {0}", this);
				Console.WriteLine("indexname: '{0}', space: '{1}', spaceclass: '{2}', indexperms: '{3}', knrbound: {4}",
					name, space, spaceclass, indexperms, this.KnrBound);
				ops.WriteOptionDescriptions(Console.Out);
				throw new ArgumentException("Some mandatory parameters are null");
			}
			this.Build(name, spaceclass, space, indexperms, this.Maxcand, this.KnrBound );
		}
		
		/// <summary>
		/// Accumulated search cost
		/// </summary>
		public override SearchCost Cost {
			get { return new SearchCost (this.indexperms.Cost.Internal, this.MainSpace.NumberDistances); }
		}
		
		/// <summary>
		/// Knr distance (internal distance)
		/// </summary>
		protected virtual double KnrDist (IList<UInt16> a, IList<UInt16> b)
		{
			throw new NotImplementedException ("This is an abstract method");
		}
		/// <summary>
		/// Wrapping function to the knr implementation
		/// </summary>
		protected virtual IList<UInt16> KnrWrap (IList<UInt16> a)
		{
			throw new NotImplementedException ("This is an abstract method");
		}
		
		/// <summary>
		/// Get the nearest references and the nearest distances
		/// </summary>
		public void GetKnr (T obj, out IList<UInt32> knrseq, out IList<Single> knrdist)
		{
			int iknr = 0;
			//int numdists = this.indexperms.MainSpace.NumberDistances;
			//long t = DateTime.Now.Ticks;
			int bound = Math.Abs (this.KnrBound);
			var r = this.indexperms.MainSpace.CreateResult (bound, this.KnrBound > 0);
			r = this.indexperms.KNNSearch (obj, bound, r);
			int iknrmax = Math.Min (ushort.MaxValue, r.Count);
			// Console.WriteLine ("*****XXXXX> iknrmax: {0}, r.Count: {1}", iknrmax, r.Count);
			knrseq = new UInt32[iknrmax];
			knrdist = new Single[iknrmax];
			foreach (ResultPair p in r) {
				knrseq [iknr] = (UInt32)p.docid;
				knrdist [iknr] = (Single)(1.0 / (p.dist + 0.0001));
				iknr++;
				if (iknr >= iknrmax) {
					break;
				}
			}
		}
		/// <summary>
		/// Configure the index
		/// </summary>
		public override void Configure(IEnumerable<string> args)
		{
			OptionSet ops = new OptionSet() {
				{"knrbound=", "The knr-bound. Negative means knn on perms, positive means ceiling knr", v => this.KnrBound = int.Parse(v) },
				{"maxcand|cand=", "The number of candidates to be verified in the original distance", v => this.Maxcand = int.Parse(v) }
			};
			ops.Parse(args);
		}
		
		/// <summary>
		/// Finalize the load of an index (<see cref="natix.IndexLoader.Load"/>)
		/// </summary>
		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			Console.WriteLine ("Loading space {0}", this.spaceName);
			var pathSpace = Dirty.CombineRelativePath (name, this.spaceName);
			this.SetMainSpace ((Space<T>)SpaceCache.Load (this.spaceClass, pathSpace));
			string docname = name + ".docspace";
			Console.WriteLine ("Loading index {0}", docname);
			this.docspace = (DocumentSpace) SpaceCache.Load("document", docname);
			Console.WriteLine ("Loaded");
			// this.indexperms = IndexLoader<T>.Load(name + ".indexperms");
		}

		/// <summary>
		/// Search range
		/// </summary>

		public override IResult Search (T q, double radius)
		{
			int M = Math.Abs (this.Maxcand);
			var C = this.KNNSearch (q, M);
			var R = this.MainSpace.CreateResult (M, this.Maxcand > 0);
			foreach (ResultPair p in C) {
				if (p.dist <= radius) {
					R.Push (p.docid, p.dist);
				} else {
					break;
				}
			}
			return R;
		}
		
		/// <summary>
		/// KNN Search
		/// </summary>
		public override IResult KNNSearch (T q, int k, IResult R)
		{
			// Console.WriteLine ("******* KNNSearch ******** XXXXXXX");
			var res = this.docspace.CreateResult (Math.Abs (this.Maxcand), false);
			IList<UInt32> knrseq;
			IList<Single> knrdist;
			this.GetKnr (q, out knrseq, out knrdist);
			Tdoc qdoc = this.docspace.BuildTdocFromVectors (knrseq, knrdist);
			for (int docid = 0, sL = this.docspace.Count; docid < sL; docid++) {
				Tdoc odoc = this.docspace [docid];
				double d = this.docspace.Dist (odoc, qdoc);
				res.Push (docid, d);
			}
			if (this.Maxcand < 0) {
				return res;
			}
			foreach (ResultPair p in res) {
				double d = this.MainSpace.Dist (q, this.MainSpace [p.docid]);
				R.Push (p.docid, d);
			}
			return R;
		}
	}
}

