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
//   Original filename: natix/SimilaritySearch/Indexes/BinPerms.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NDesk.Options;
using natix.SimilaritySearch;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// The binary encoded permutations (BriefPermutations) index
	/// </summary>
	public class BinPerms<T> : Perms<T>, Index<T>
	{
		BinaryHammingSpace binperms;
		Index< IList<byte> > indexHamming;
		/// <summary>
		/// Indicates if the index will permute the center
		/// </summary>
		public bool permcenter;
		/// <summary>
		/// The name of the hamming index (external index)
		/// </summary>
		public string nameIndexHamming;
		/// <summary>
		/// The module
		/// </summary>
		public int Mod;
				
		/// <summary>
		/// Constructor
		/// </summary>
		public BinPerms () : base()
		{
		}
		
		/// <summary>
		/// The length of the dimension in bytes (vector's length in bytes of the bit string)
		/// </summary>
		/// <param name="invlen">
		/// The length of the inverse
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// The vector's length in bytes (binary string)
		/// A <see cref="System.Int32"/>
		/// </returns>
		public virtual int GetDimLengthInBytes (int invlen)
		{
			return invlen >> 3;
		}

		/// <summary>
		/// The API Build method for BinPerms 
		/// </summary>
		/// <param name="name">
		/// The output index's name
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="spaceClass">
		/// The name of the class of the space (A <see cref="natix.SpaceCache.Load"/>)
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="spaceName">
		/// The name of the database
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="spacePerms">
		/// The database of permutants
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="maxcand">
		/// The maximum number of candidates to be verified
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="mod">
		/// The modulus to encode
		/// A <see cref="System.Double"/>
		/// </param>
		/// <param name="permcenter">
		/// If true the center will be permuted
		/// A <see cref="System.Boolean"/>
		/// </param>
		/// <param name="idxperms">
		/// If idxperms is not null it will be used to skip inverse computations and distance computations
		/// </param> 
		public void Build (string name, string spaceClass, string spaceName, string spacePerms, int maxcand, double mod, bool permcenter, Perms<T> idxperms)
		{
			this.spaceClass = spaceClass;
			this.spaceName = Dirty.ComputeRelativePath (name, spaceName);
			this.spacePerms = Dirty.ComputeRelativePath (name, spacePerms);
			this.SetMainSpace ((Space<T>)SpaceCache.Load (spaceClass, spaceName));
			this.SetRefsSpace ((Space<T>)SpaceCache.Load (spaceClass, spacePerms, null, false));
			this.Maxcand = maxcand;
			this.binperms = new BinaryHammingSpace (1, this.MainSpace.Count, this.GetDimLengthInBytes (this.RefsSpace.Count), BinaryDistance.MinHamming);
			if (mod <= 0) {
				throw new ArgumentException (String.Format ("The Modulus {0} must be strictly positive", mod));
			}
			if (mod < 1) {
				this.Mod = (int)Math.Ceiling (mod * this.RefsSpace.Count);
			} else {
				this.Mod = (int)mod;
			}

			this.permcenter = permcenter;
			string binSpaceName = name + ".data";
			this.nameIndexHamming = binSpaceName + ".seq";
			if (idxperms == null) {
				base.Build (name, spaceClass, spaceName, spacePerms, maxcand);
			} else {
				for (int docid = 0; docid < this.MainSpace.Count; docid++) {
					IList<Int16> inv = idxperms.GetComputedInverse (docid);
					this.SaveInverse (null, inv);
				}
				Dirty.SaveIndexXml (name, this);
			}
			this.binperms.SubSpace (binSpaceName, -1, false);
			var seq = new Sequential<IList< byte >> ();
			seq.Build (this.nameIndexHamming, "binary-hamming", binSpaceName);
		}
	
		/// <summary>
		/// (Command line) user interface method. See API Build for options
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
			double mod = 0.5;
			bool permcenter = true;
			string fromperms = null;
			OptionSet ops = new OptionSet() {
				{"indexname=", "Index output name", v => name = v},
				{"space=", "Space filename", v => space = v},
				{"spaceclass=", "Space class", v => spaceclass = v},
				{"spaceperms|perms=", "Spaceperms filename", v => spaceperms = v},
				{"maxcand=", "Default Maxcand", v => maxcand = int.Parse(v) },
				{"module|mod=", "Module for binary encoding (default 0.5)", v => mod = double.Parse(v)},
				{"permcenter|pcenter|pc=", "Permute center (default true)", v => permcenter = bool.Parse(v)},
				{"fromperms=", "Read the permutations from a Perm index", v => fromperms = v}
			};
			ops.Parse(args);
			if (fromperms == null) {
				if (name == null || space == null || spaceclass == null ) {
					Console.WriteLine("Index BinPerms options:");
					ops.WriteOptionDescriptions(Console.Out);
					throw new ArgumentException("Some arguments were not given");
				}
				if (spaceperms == null) {
					Console.WriteLine("Notice: spaceperms were not given, using space as spaceperms");
					spaceperms = space;
				}
				this.Build(name, spaceclass, space, spaceperms, maxcand, mod, permcenter, null);
			} else {
				if (space != null || spaceperms != null) {
					Console.WriteLine("Building options for BinPerms");
					ops.WriteOptionDescriptions(Console.Out);					
					throw new ArgumentException(String.Format("space and spaceperms will be taken from perms {0} index", fromperms));	
				}
				if (name == null) {
					Console.WriteLine("Building options for BinPerms");
					Console.WriteLine("Checking for null> indexname: '{0}'", name);
					ops.WriteOptionDescriptions(Console.Out);
					throw new ArgumentException("Some null parameters building 'fromperms'");
				}
				var perms = (Perms<T>)IndexLoader.Load(fromperms);
				if (spaceclass != perms.spaceClass) {
					Console.WriteLine("Building options for BinPerms");
					ops.WriteOptionDescriptions(Console.Out);
					throw new ArgumentException("spaceclass != perms.spaceClass");
				}
				var pathSpace = Dirty.CombineRelativePath(fromperms, perms.spaceName);
				var pathPerms = Dirty.CombineRelativePath(fromperms, perms.spacePerms);
				this.Build (name, perms.spaceClass, pathSpace, pathPerms, maxcand, mod, permcenter, perms);
			}
		}
		
		/// <summary>
		///  Returns the internal index hamming
		/// </summary>
		/// <returns>
		/// Index for byte[] data
		/// </returns>
		public Index< IList<byte> > GetIndexHamming ()
		{
			return this.indexHamming;
		}
		
		public override void Configure (IEnumerable<string> args)
		{
			string hammingIndex = null;
			OptionSet ops = new OptionSet() {
				{"hammingindex|indexhamming=", "Name of the hamming index", v => hammingIndex = v }
			};
			base.Configure(ops.Parse(args));
			if (hammingIndex != null) {
				Console.WriteLine("*** Replacing hamming index by configure argument '{0}'", hammingIndex);
				this.nameIndexHamming = hammingIndex;
				this.indexHamming = (Index< IList<byte> >)IndexLoader.Load (this.nameIndexHamming);
			}
		}

		/// <summary>
		/// Performs encoding of an object
		/// </summary>
		public IList<byte> Encode (T u)
		{
			return this.Encode (this.GetInverse (u));
		}
		/// <summary>
		/// Encode an inverse permutation into an encoded bit-string
		/// </summary>
		/// <param name="inv">
		/// Inverse permutation
		/// A <see cref="Int16[]"/>
		/// </param>
		/// <returns>
		/// Bit-string/Brief permutation
		/// A <see cref="System.Byte[]"/>
		/// </returns>
		public virtual IList<byte> Encode (IList<Int16> inv)
		{
			int len = this.GetDimLengthInBytes(inv.Count);
			byte[] res = new byte[len];
			if (this.permcenter) {
				int M = inv.Count / 4; // same
				for (int i = 0, c = 0; i < len; i++) {
					int b = 0;
					for (int bit = 0; bit < 8; bit++,c++) {
						int C = c;
						if ((((int)(C / M)) % 3) != 0) {
							C += M;
						}
						// Console.WriteLine ("C: {0}, Mod: {1}", C, this.mod);
						if (Math.Abs (inv[c] - C) > this.Mod) {
							b |= (1 << bit);
						}
					}
					res[i] = (byte)b;
				}
			} else {
				for (int i = 0, c = 0; i < len; i++) {
					int b = 0;
					for (int bit = 0; bit < 8; bit++,c++) {
						if (Math.Abs (inv[c] - c) > this.Mod) {
							b |= (1 << bit);
						}
					}
					res[i] = (byte)b;
				}

			}
			return res;
		}
		
		/// <summary>
		/// Save the inverse
		/// </summary>
		/// <param name="bw">
		/// The output stream
		/// A <see cref="BinaryWriter"/>
		/// </param>
		/// <param name="inv">
		/// The inverse to be saved
		/// A <see cref="Int16[]"/>
		/// </param>
		public override void SaveInverse (BinaryWriter bw, IList<Int16> inv)
		{
			IList<byte> enc = this.Encode (inv);
			this.binperms.Add (enc);
		}
		
		/// <summary>
		/// Finalize the load of the index (See IndexLoader)
		/// </summary>
		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			var pathSpace = Dirty.CombineRelativePath (name, this.spaceName);
			var pathPerms = Dirty.CombineRelativePath (name, this.spacePerms);
			this.SetMainSpace ((Space<T>)SpaceCache.Load (this.spaceClass, pathSpace));
			this.SetRefsSpace ((Space<T>)SpaceCache.Load (this.spaceClass, pathPerms, null, false));
			int PL = this.RefsSpace.Count;
			int SL = this.MainSpace.Count;
			this.invperms = null;
			this.binperms = new BinaryHammingSpace (1, SL, this.GetDimLengthInBytes (PL), BinaryDistance.MinHamming, name + ".data");
			SpaceCache.Save (this.binperms);
			this.indexHamming = (Index< IList<byte> >)IndexLoader.Load (this.nameIndexHamming);
		}
		
		/// <summary>
		/// KNN Search in the index
		/// </summary>
		/// <param name="q">
		/// The query object
		/// </param>
		/// <param name="k">
		/// The number of nearest neighbors
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// The result set
		/// A <see cref="Result"/>
		/// </returns>
		public override IResult KNNSearch (T q, int k)
		{
			IList<byte> enc = this.Encode (q);
			//Console.WriteLine ("EncQuery: {0}", BinaryHammingSpace.ToAsciiString (enc));
			var cand = this.indexHamming.KNNSearch (enc, Math.Abs (this.Maxcand));
			//Result candseq = this.indexHammingSeq.KNNSearch (enc, 10);
			//Math.Abs (this.Maxcand));
			//Result cand = this.indexHamming.Search (enc, 60);
			/*Result cand = new Result (Math.Abs (this.Maxcand));
			for (int docid = 0, bL = this.binperms.Length; docid < bL; docid++) {
				cand.Push (docid, this.binperms.Dist(this.binperms[docid], enc));
			}*/
			if (this.Maxcand < 0) {
				return cand;
			}
			var res = this.MainSpace.CreateResult(k, true);
			foreach (ResultPair p in cand) {
				res.Push(p.docid, this.MainSpace.Dist(q, this.MainSpace[p.docid]));
			}
	        return res;
		}
	}
}
