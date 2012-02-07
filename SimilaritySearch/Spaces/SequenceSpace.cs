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
//   Original filename: natix/natix/SimilaritySearch/Spaces/SequenceSpace.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO.Compression;
using System.Reflection;
using NDesk.Options;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Available string distances
	/// </summary>
	public enum SequenceDistance {
		/// <summary>
		/// Levenshtein distance (edit distance)
		/// </summary>
		Levenshtein,
		/// <summary>
		/// Hamming distance
		/// </summary>
		Hamming,
		/// <summary>
		/// Intersection cardinality as similarity
		/// </summary>
		Intersection,
		/// <summary>
		/// Jaccard's distance
		/// </summary>
		Jaccard,
		/// <summary>
		/// Dice's distance
		/// </summary>
		Dice,
		/// <summary>
		/// Prefix's distance
		/// </summary>
		PrefixLength
		//,LCS
		,None // for knrinvindex
	}
	
	/// <summary>
	/// String space
	/// </summary>
	public class SequenceSpace<T> : Space< IList<T> > where T : IComparable
	{
		static Dictionary<string, SequenceSpace<T>> cache = new Dictionary<string, SequenceSpace<T>>();
		string name;
		public IList< IList<T> > seqs;
		//public IListContainer<T> seqs;
		int numdist;
		Distance< IList<T> > dist;
		INumeric<T> num;
		
		/// <summary>
		///  The generic type (char[])
		/// </summary>
		public Type GenericType {
			get { return typeof(IList<T> ); }
		}
		/// <summary>
		/// Constructor
		/// </summary>
		public SequenceSpace ()
		{
			this.name = null;
			this.seqs = null;
			this.numdist = 0;
			this.num = (INumeric<T>)Numeric.Get (typeof(T));
			this.dist = new Distance< IList<T> > (Levenshtein);
		}
		
		/// <summary>
		/// Set distance by enum
		/// </summary>
		/// <param name="sdist">
		/// A <see cref="SequenceDistance"/>
		/// </param>
		public void SetDist (SequenceDistance sdist)
		{
			switch (sdist) {
			case SequenceDistance.Levenshtein:
				this.dist = new Distance<IList<T> > (SequenceSpace<T>.Levenshtein);
				break;
			//case StringDistance.LCS:
			//	this.dist = new Distance<string> (this.LCS);
			//	break;
			case SequenceDistance.Hamming:
				this.dist = new Distance<IList<T> > (SequenceSpace<T>.Hamming);
				break;
			case SequenceDistance.Intersection:
				this.dist = new Distance<IList<T> > (SequenceSpace<T>.Intersection);
				break;
			case SequenceDistance.Jaccard:
				this.dist = new Distance<IList<T> > (SequenceSpace<T>.Jaccard);
				break;
			case SequenceDistance.Dice:
				this.dist = new Distance<IList<T> > (SequenceSpace<T>.Dice);
				break;
			case SequenceDistance.PrefixLength:
				this.dist = new Distance<IList<T> > (SequenceSpace<T>.PrefixLength);
				break;
			}			
		}
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sdist">
		/// A <see cref="StringDistance"/>
		/// </param>
		public SequenceSpace (SequenceDistance sdist) : this()
		{
			this.SetDist (sdist);
		}
		
		/// <summary>
		/// Extracts and save an string subspace of the calling instance
		/// </summary>
		/// <param name="name">
		/// The output name <see cref="System.String"/>
		/// </param>
		/// <param name="samplesize">
		/// The length of the sampling <see cref="System.Int32"/>
		/// </param>
		/// <param name="random">
		/// If true it will be a random sampling <see cref="System.Boolean"/>
		/// </param>
		public void SubSpace (string name, int samplesize, bool random)
		{
			IList<int> sample; // = Perms<IList<T>>.GetRandomSample (this, samplesize, random);
			if (random) {
				sample = RandomSets.GetRandomSubSet (samplesize, this.Count);
			} else {
				sample = RandomSets.GetExpandedRange (samplesize);
			}
			this.SubSpace (name, sample);
		}
		
		public void SubSpace (string name, IList<int> sample)
		{
			File.WriteAllText (name, String.Format (
					"--sequences {0}.seqs --size {1} --distance None --usedisk False",
					name, sample.Count));
			using (StreamWriter w = new StreamWriter (name + ".seqs")) {
				foreach (int i in sample) {
					IList<T> a = this[i];
					for (int j = 0; j < a.Count; j++) {
						w.Write ("{0} ", a[j]);
					}
					w.WriteLine ();
				}
			}
		}
		/// <summary>
		/// Retrieves the object associated to object id docid
		/// </summary>
		/// <param name="docid">
		/// A <see cref="System.Int32"/>
		/// </param>
		public IList<T> this[int docid]
		{
			get { return this.seqs[docid]; }
		}
		/// <summary>
		/// The name of the space
		/// </summary>
		public string SpaceType {
			get { return this.GetType().FullName; }
			set {  }
		}

		/// <summary>
		/// Get/Set (and load) the database
		/// </summary>
		public string Name {
			get { return this.name; }
			set {
				this.name = value;
				if (SequenceSpace<T>.cache.ContainsKey (this.name)) {
					Console.WriteLine ("Loading {0} from cache", this.name);
					SequenceSpace<T> sp = SequenceSpace<T>.cache[this.name];
					this.seqs = sp.seqs;
					this.numdist = sp.numdist;
				} else {
					this.LoadFromFile (value);
					SequenceSpace<T>.cache[name] = this;
				}
			}
		}
		
		public IResult CreateResult (int K, bool ceiling)
		{
			// maybe check the alphabet
			return new ResultTies (K, ceiling);
		}
		
		public virtual int GetSeqLength (int docid)
		{
			// TODO: FIX THIS !!!!!
			//WARN: this must be specialized for different containers
			Console.WriteLine ("GetSeqLength> docid: {0}, seqs.Count: {1}", docid, this.seqs.Count);
			return this.seqs[docid].Count;
		}

		public virtual void LoadSequences (string seqFile, int size, bool usedisk)
		{
			string binFile = null;
			if (seqFile.EndsWith (".bin") || seqFile.EndsWith (".data")) {
				binFile = seqFile;
			} else {
				binFile = seqFile + ".bin";
			}
			if (!File.Exists (binFile)) {
				Console.WriteLine ("Creating binary sequence file {0}.bin", seqFile);
				binFile = PrimitiveIO<T>.CreateBinaryFile (seqFile);
			}
			// this.seqs = ListContainerFactory<T>.GetVariableSizeListContainer (size);
			Console.WriteLine ("Loading binary sequences file {0}", binFile);
			IList<int> seqsizes = PrimitiveIO<int>.ReadAllFromFile (binFile + ".sizes");
			for (int i = 1; i < seqsizes.Count; i++) {
				seqsizes[i] += seqsizes[i - 1];
			}
			if (usedisk) {
				this.seqs = new DiskListContainer<T>(binFile, seqsizes);
			} else {
				IList<T> xseqs = PrimitiveIO<T>.ReadAllFromFile(binFile);
				this.seqs = new ListContainerWithSize<T>(xseqs, seqsizes);				
			}
		}
		
		public void Load (string seqFile, string distName, int size, bool usedisk)
		{
			if (seqFile == null) {
				throw new ArgumentNullException ("Sequence's file can't be null");
			}
			if (size == 0) {
				throw new ArgumentException ("Database size can't be zero");
			}
			if (distName == null) {
				throw new ArgumentNullException ("Distance name can't be null");
			}
			this.SetDist ((SequenceDistance)Enum.Parse (typeof(SequenceDistance), distName));
			this.LoadSequences(seqFile, size, usedisk);
			if (this.seqs == null) {
				throw new ArgumentNullException("The sequences can't be null after LoadSequences");
			}
		}
		
		public void LoadFromFile (string name)
		{
			string seqFile = null;
			int size = 0;
			string distName = null;
			bool usedisk = true;
			OptionSet op = new OptionSet () {
				{"sequences|seq=", "File containing sequences", (v) => seqFile = v },
				{"size=", "The number of sequences", (v) => size = int.Parse(v) },
				{"distance|dist=", "The distance function", (v) => distName = v},
				{"mmap|usedisk=", "Use mmap like functions if true", (v) => usedisk = bool.Parse(v)},
			};
			op.Parse(Commands.TokenizeLine(File.ReadAllLines (name)[0]));
			var path = seqFile;
			if (!File.Exists(path)) {
				Console.WriteLine("WARNING: sequence file '{0}' was not found in cwd, trying relative to the space header", path);
				path = Dirty.CombineRelativePath(name, seqFile);
			}
			this.Load(path, distName, size, usedisk);
		}
		
		/// <summary>
		/// Parse an string into the object representation
		/// </summary>
		public IList<T> Parse (string s, bool isquery)
		{
			if (isquery && s.StartsWith ("obj")) {
				return this.seqs[int.Parse(s.Split(' ')[1])];
			}
			string[] v = s.Trim ().Split (' ');
			IList<T>  n = new T[v.Length];
			for (int i = 0; i < n.Count; i++) {
				n[i] = this.num.FromDouble (Double.Parse (v[i]));
			}
			return n;
		}
		
		/// <summary>
		/// Accumulated number of distances
		/// </summary>
		public int NumberDistances {
			get { return this.numdist; }
		}
		
		/// <summary>
		/// The length of the space
		/// </summary>
		public int Count {
			get { return (this.seqs == null) ? 0 : this.seqs.Count; }
		}
		
		/// <summary>
		/// Wrapper to the real string distance
		/// </summary>
		/// <param name="a">
		/// A <see cref="System.Char[]"/>
		/// </param>
		/// <param name="b">
		/// A <see cref="System.Char[]"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Double"/>
		/// </returns>
		public double Dist (IList<T>  a, IList<T>  b)
		{
			this.numdist++;
			return this.dist (a, b);
		}
		private static int minimum3 (int a, int b, int c)
		{
			if (a > b) {
				a = b;
			}
			if (a > c) {
				a = c;
			}
			return a;
		}
		
		/// <summary>
		/// Levenshtein distance for generic datatype. It has customizable costs
		/// </summary>
		/// <param name="a">
		/// The first sequence
		/// </param>
		/// <param name="b">
		/// Second sequence
		/// </param>
		/// <param name="inscost">
		/// The cost of a single insert
		/// </param>
		/// <param name="delcost">
		/// The cost of a deletion operation
		/// </param>
		/// <param name="repcost">
		/// The cost of a replace operation
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static int Levenshtein (IList<T> a, IList<T> b, byte inscost, byte delcost, byte repcost) 
		{
			int alength = a.Count;
			int blength = b.Count;
			if (alength <= 0) {
				return blength;
			}
			if (blength <= 0) {
				return alength;
			}
			blength++;
			int[] C = new int[blength];
			int A_ant = 0;
			for (int i = 0; i < blength; i++) {
				C[i] = i;
			}
			blength--;
			for (int i = 0; i < alength; i++) {
				A_ant = i + 1;
				int C_ant = C[0];
				int j = 0;
				for (j = 0; j < blength; j++) {
					int cost = repcost;
					if (a[i].CompareTo(b[j]) == 0) {
						cost = 0;
					}
					// adjusting the indices
					j++;
					C[j-1] = A_ant;
					A_ant = minimum3 (C[j] + delcost, A_ant + inscost, C_ant + cost);
					C_ant = C[j];
					// return to default values. Only to be clear
					j--;
					
				}
				C[j] = A_ant;
			}
			return A_ant;
		}
		/// <summary>
		/// Edit distance
		/// </summary>
		/// <param name="a">
		/// A <see cref="System.Char[]"/>
		/// </param>
		/// <param name="b">
		/// A <see cref="System.Char[]"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Double"/>
		/// </returns>
		public static double Levenshtein (IList<T> a, IList<T> b)
		{
			return Levenshtein (a, b, 1, 1, 1);
		}
		/// <summary>
		/// LCS over Levenshtein
		/// </summary>
		public static double LCS (IList<T> a, IList<T> b)
		{
			return Levenshtein (a, b, 1, 1, 2);
		}
		/// <summary>
		/// Hamming distance for Generic Datatype
		/// </summary>
		public static double Hamming (IList<T> a, IList<T> b)
		{
			int d = 0;
			for (int i = 0; i < a.Count; i++) {
				if (a[i].CompareTo (b[i]) != 0) {
					d++;
				}
			}
			return d;
		}
		/// <summary>
		/// lexicographic comparison, starting always at position 0 of every sequence
		/// </summary>
		public static int LexicographicCompare (IList<T> a, IList<T> b)
		{
			return LexicographicCompare (a, 0, b.Count, b, 0, a.Count);
		}
		/// <summary>
		/// Compare to arrays lexicographically, returns an integer representing something like a - b
		/// </summary>
		public static int LexicographicCompare (IList<T> a, int aStart, int aEnd, IList<T> b, int bStart, int bEnd)
		{
			int cmp = 0;
			for (int i = aStart, j = bStart; i < aEnd && j < bEnd; i++,j++) {
				cmp = a[i].CompareTo (b[j]);
				if (cmp != 0) {
					return cmp;
				}
			}
			return (aEnd-aStart) - (bEnd-bStart);
		}
		/// <summary>
		/// Jaccard's distance
		/// </summary>
		public static double Jaccard (IList<T> a, IList<T> b)
		{
			// a & b are already sorted
			// union
			int U = a.Count + b.Count;
			// intersection
			int I = 0;
			int cmp;
			for (int ia = 0, ib = 0; ia < a.Count && ib < b.Count;) {
				cmp = a[ia].CompareTo (b[ib]);
				if (cmp == 0) {
					U--;
					I++;
					ia++;
					ib++;
				} else if (cmp < 0) {
					ia++;
				} else {
					ib++;
				}
			}
			// Console.WriteLine ("I {0}, U {1}", I, U);
			return 1.0 - ((double)I) / U;
		}
		
		/// <summary>
		/// Hamming distance
		/// </summary>
		public static double Dice (IList<T> a, IList<T> b)
		{
			// a & b are already sorted
			// union
			// intersection
			int I = 0;
			int cmp;
			for (int ia = 0, ib = 0; ia < a.Count && ib < b.Count;) {
				cmp = a[ia].CompareTo (b[ib]);
				if (cmp == 0) {
					I++;
					ia++;
					ib++;
				} else if (cmp < 0) {
					ia++;
				} else {
					ib++;
				}
			}
			return -(I * 2.0) / (a.Count + b.Count);
		}
		/// <summary>
		/// Knr Intersection distance
		/// </summary>
		public static double Intersection (IList<T> a, IList<T> b)
		{
			// a & b are already sorted
			// union
			// intersection
			int I = 0;
			int cmp;
			for (int ia = 0, ib = 0; ia < a.Count && ib < b.Count;) {
				cmp = a[ia].CompareTo (b[ib]);
				if (cmp == 0) {
					I++;
					ia++;
					ib++;
				} else if (cmp < 0) {
					ia++;
				} else {
					ib++;
				}
			}
			return -I;
		}
		
		/// <summary>
		/// Knr prefix length distance
		/// </summary>
		public static double PrefixLength (IList<T> a, IList<T> b)
		{
			int i, min = Math.Min (a.Count, b.Count);
			for (i = 0; i < min && a[i].CompareTo (b[i]) == 0; i++) {
				//empty
			}
			return -i;
		}	
	}
}
