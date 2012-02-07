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
//   Original filename: natix/natix/SimilaritySearch/Indexes/MovPerms.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Mono.Options;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Mov perms. It works like permutants but it's a continuos version of BinPerms. The main role of this
	/// index is to show the limits behaviors of the Brief permutations.
	/// </summary>
	public class MovPerms<T> : Perms<T>
	{
		/// <summary>
		/// Use absolute movements instead relative ones
		/// </summary>
		public bool absmovs;
		
		/// <summary>
		/// Constructor
		/// </summary>
		public MovPerms () : base()
		{
		}
		
		/// <summary>
		/// The API Build method
		/// </summary>
		/// <param name="name">
		/// Output index file <see cref="System.String"/>
		/// </param>
		/// <param name="spaceClass">
		/// The name of the space's class <see cref="natix.SpaceCache.Load"/>
		/// </param>
		/// <param name="spaceName">
		/// The database's name <see cref="System.String"/>
		/// </param>
		/// <param name="spacePerms">
		/// The database of permutants <see cref="System.String"/>
		/// </param>
		/// <param name="maxcand">
		/// The maximum number of candidates <see cref="System.Int32"/>
		/// </param>
		/// <param name="absmovs">
		/// Permute center <see cref="System.Boolean"/>
		/// </param>
		/// <param name="idxperms">
		/// Perms Index to get the permutations. If it's null we compute the permutations
		/// </param>
		public void Build (string name, string spaceClass, string spaceName, string spacePerms, int maxcand, bool absmovs, Perms<T> idxperms)
		{
			this.spaceClass = spaceClass;
			this.spaceName = Dirty.ComputeRelativePath(name, spaceName);
			this.spacePerms = Dirty.ComputeRelativePath(name, spacePerms);
			this.SetMainSpace ((Space<T>)SpaceCache.Load (spaceClass, spaceName));
			this.SetRefsSpace ((Space<T>)SpaceCache.Load (spaceClass, spacePerms, null, false));
			this.Maxcand = maxcand;
			this.absmovs = absmovs;
			if (idxperms == null) {
				base.Build (name, spaceClass, spaceName, spacePerms, maxcand);
			} else {
				BinaryWriter bw = new BinaryWriter (new BufferedStream (new FileStream (name + ".data", FileMode.Create, FileAccess.Write), 1 << 20));
				for (int docid = 0; docid < this.MainSpace.Count; docid++) {
					IList<Int16> inv = idxperms.GetComputedInverse (docid);
					this.SaveInverse (bw, inv);
				}
				bw.Close ();
				Dirty.SaveIndexXml (name, this);
			}
		}
	 
		/// <summary>
		/// (Command line) user interface Build method. See API Build to get the options (in lowercase).
		/// </summary>
		/// <param name="args">
		/// </param>
		public override void Build (IEnumerable<string> args)
		{
			string name = null;
			string space = null;
			string spaceclass = null;
			string spaceperms = null;
			int maxcand = 1024;
			bool absmovs = false;
			string fromperms = null;
			OptionSet ops = new OptionSet() {
				{"indexname=", "Index output name", v => name = v},
				{"space=", "Space filename", v => space = v},
				{"spaceclass=", "Space class", v => spaceclass = v},
				{"spaceperms|perms=", "Spaceperms filename", v => spaceperms = v},
				{"maxcand=", "Default Maxcand", v => maxcand = int.Parse(v) },
				{"absmovs|absolutemovements", "Use absolute movements (default false)", v => absmovs = true},
				{"fromperms=", "Read the permutations from a Perm index", v => fromperms = v}
			};
			ops.Parse(args);
			if (fromperms == null) {
				if (name == null || space == null || spaceclass == null ) {
					Console.WriteLine("Index MovPerms options:");
					ops.WriteOptionDescriptions(Console.Out);
					throw new ArgumentException("Some arguments were not given");
				}
				if (spaceperms == null) {
					Console.WriteLine("Notice: spaceperms were not given, using space as spaceperms");
					spaceperms = space;
				}
				this.Build(name, spaceclass, space, spaceperms, maxcand, absmovs, null);
			} else {
				if (space != null || spaceperms != null) {
					Console.WriteLine("Building options for MovPerms");
					ops.WriteOptionDescriptions(Console.Out);					
					throw new ArgumentException(String.Format("space and spaceperms will be taken from perms {0} index", fromperms));	
				}
				if (name == null) {
					Console.WriteLine("Building options for MovPerms");
					Console.WriteLine("Checking for null> indexname: '{0}'", name);
					ops.WriteOptionDescriptions(Console.Out);
					throw new ArgumentException("Some null parameters building 'fromperms'");
				}
				var perms = (Perms<T>)IndexLoader.Load(fromperms);
				if (spaceclass != perms.spaceClass) {
					Console.WriteLine("Building options for MovPerms");
					ops.WriteOptionDescriptions(Console.Out);
					throw new ArgumentException("spaceclass != perms.spaceClass");
				}
				var pathSpace = Dirty.CombineRelativePath(name, perms.spaceName);
				var pathPerms = Dirty.CombineRelativePath(name, perms.spacePerms);
				this.Build (name, perms.spaceClass, pathSpace, pathPerms, maxcand, absmovs, perms);
			}
		}

	
		/// <summary>
		/// Encode (testing version)
		/// </summary>
		/// <param name="inv">
		/// A <see cref="Int16[]"/>
		/// </param>
		/// <returns>
		/// A <see cref="Int16[]"/>
		/// </returns>
		public IList<Int16> Encode (IList<Int16> inv)
		{
			//return inv;
			int len = inv.Count;
			var res = new Int16[len];
			if (this.absmovs) {
				for (int i = 0; i < len; i++) {
					res[i] = (Int16)(inv[i] - i);
				}
			} else {
				for (int i = 0; i < len; i++) {
					res[i] = (Int16)Math.Abs(inv[i] - i);
				}				
			}
			return res;
		}

		/// <summary>
		/// Save inverse
		/// </summary>
		/// <param name="bw">
		/// A <see cref="BinaryWriter"/>
		/// </param>
		/// <param name="inv">
		/// A <see cref="Int16[]"/>
		/// </param>
		public override void SaveInverse (BinaryWriter bw, IList<Int16> inv)
		{
			IList<Int16> enc = this.Encode (inv);
			base.SaveInverse (bw, enc);
		}
		
		/// <summary>
		///  Finalize the load
		/// </summary>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		public override void FinalizeLoad (string name, IDictionary<string,object> config)
		{
			base.FinalizeLoad (name, config);
		}
		
		/// <summary>
		/// KNN Search
		/// </summary>
		/// <param name="q">
		/// Query object
		/// </param>
		/// <param name="k">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="Result"/>
		/// </returns>
		public override IResult KNNSearch (T q, int k)
		{
			IList<Int16> qinv = this.Encode (this.GetInverse (q));
			/*for (int i = 0; i < qinv.Length; i++) {
				Console.WriteLine ("({0},{1}) ", i, qinv[i]);
			}
			Console.WriteLine ("<TheEnd>");
			*/
			var cand = this.invperms.CreateResult (Math.Abs (this.Maxcand), false);
			for (int docid = 0; docid < this.invperms.Count; docid++) {
				cand.Push (docid, this.invperms.Dist (this.invperms[docid], qinv));
			}
			if (this.Maxcand < 0) {
				return cand;
			}
			var res = this.MainSpace.CreateResult (k, false);
			foreach (ResultPair p in cand) {
				res.Push(p.docid, this.MainSpace.Dist(q, this.MainSpace[p.docid]));
			}
	        return res;
		}

	}
}
