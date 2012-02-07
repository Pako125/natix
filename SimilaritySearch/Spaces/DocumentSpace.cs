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
//   Original filename: natix/natix/SimilaritySearch/Spaces/DocumentSpace.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mono.Options;
using natix.SortingSearching;
// Adapted from SISAP library src/spaces/documents/objdocuments.h to Natix library.

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Represents a vocabulary entry
	/// </summary>
	public struct Tvoc
	{
		/// <summary>
		/// Key identifier
		/// </summary>
		public UInt32 keyid;
		/// <summary>
		/// Weight for this key
		/// </summary>
		public float weight;
		/// <summary>
		///  Constructor
		/// </summary>
		/// <param name="k">
		/// A <see cref="UInt32"/>
		/// </param>
		/// <param name="w">
		/// A <see cref="System.Single"/>
		/// </param>
		public Tvoc (UInt32 k, float w)
		{
			this.keyid = k;
			this.weight = w;
		}
	}
	
	/// <summary>
	///  A document vector representation
	/// </summary>
	public class Tdoc
	{
		/// <summary>
		/// The document vector
		/// </summary>
		public Tvoc[] doc;
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="d">
		/// A <see cref="Tvoc[]"/>
		/// </param>
		public Tdoc (Tvoc[] d)
		{
			this.doc = d;
		}
		/// <summary>
		/// Save this document's vector
		/// </summary>
		public void Save (BinaryWriter b)
		{
			b.Write ((int)this.doc.Length);
			foreach (Tvoc v in this.doc) {
				b.Write (v.keyid);
				b.Write (v.weight);
			}
		}
		
		
		/// <summary>
		/// Load a document's vector from stream
		/// </summary>
		/// <param name="b">
		/// A <see cref="BinaryReader"/>
		/// </param>
		/// <returns>
		/// A <see cref="Tdoc"/>
		/// </returns>
		public static Tdoc Load (BinaryReader b)
		{
			int len = b.ReadInt32 ();
			// Console.WriteLine ("***** BIG LOAD? {0}", len);
			Tdoc res = new Tdoc (new Tvoc[len]);
			for (int i = 0; i < len; i++) {
				res.doc[i] = new Tvoc (b.ReadUInt32 (), b.ReadSingle ());
			}
			return res;
		}
	}
	
	/// <summary>
	/// The document's space
	/// </summary>
	public class DocumentSpace : Space<Tdoc>
	{
		string name;
		List<string> names;
		List<Tdoc> docs;
		int numdist;
		
		/// <summary>
		/// Returns the generic types
		/// </summary>
		public Type GenericType {
			get { return typeof(Tdoc); }
		}
	
		/// <summary>
		/// Constructor
		/// </summary>
		public DocumentSpace ()
		{
			this.names = new List<string> ();
			this.docs = new List<Tdoc> ();
			this.numdist = 0;
		}
		
		/// <summary>
		/// The space's class name
		/// </summary>
		public string SpaceType {
			get { return this.GetType ().FullName; }
			set { }
		}
		
		/// <summary>
		/// The length of the space
		/// </summary>
		public int Count {
			get { return this.docs.Count; }
		}
		
		/// <summary>
		/// Indexer to retrieve an object by id
		/// </summary>
		/// <param name="docid">
		/// A <see cref="System.Int32"/>
		/// </param>
		public Tdoc this[int docid]
		{
			get { return this.docs[docid]; }
		}

		/// <summary>
		/// Returns the name of the object pointed by id
		/// </summary>
		/// <param name="docid">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public string GetItemName (int docid)
		{
			return this.names[docid];
		}
		
		/// <summary>
		/// Get/Set database name
		/// </summary>
		/// <remarks>The set operation loads the database</remarks>
		public string Name {
			get { return this.name; }
			set {
				this.name = value;
				// to support the sisap format
				bool isfile = File.Exists (this.name);
				if (!isfile && Directory.Exists (this.name)) {
					if (!File.Exists (this.name + ".docspace")) {
						this.LoadFromDirectory (this.name);
						// saving in natix format
						this.SubSpace (this.name + ".docspace", -1, false);
					} else {
						this.LoadFromDocSpace (this.name + ".docspace");
					}
				} else {
					this.LoadFromDocSpace (this.name);
				}
			}
		}
		
		public IResult CreateResult (int K, bool ceiling)
		{
			return new Result (K, ceiling);
		}
		
		
		void LoadFromDirectory (string inputdir)
		{
			bool isbin = (inputdir.EndsWith (".bin"));
			string[] files = Directory.GetFiles (inputdir);
			// Array.Sort (files);
			Sorting.Sort<string> (files);
			int numdocs = files.Length;
			int pc = 1 + numdocs / 100;
			int i = 0;
			foreach (string filename in files) {
				if ((i % pc) == 0) {
					Console.WriteLine ("Loading document: {0}, name: {1}, advance: {2:0.00}%", i, filename, i * 100.0 / numdocs);
				}
				if (isbin) {
					BinaryReader br = new BinaryReader (new FileStream (filename, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 12));
					this.names.Add (filename);
					this.docs.Add (Tdoc.Load (br));
					br.Close ();
				} else {
					this.names.Add (filename);
					this.docs.Add (this.ParseFromFile (filename, false));
				}
				i++;
			}
		}

		void LoadFromDocSpace (string docspacename)
		{
			var args = Commands.TokenizeLine (File.ReadAllLines (docspacename)[0]);
			int numdocs = 0;
			string vecdocs = null;
			string dbnames = null;
			var prefix = Path.GetDirectoryName (docspacename);
			OptionSet ops = new OptionSet() {
				{"numdocs=", "Number of documents", (v) => numdocs = int.Parse(v)},
				{"vecdocs=", "vector documents", (v) => vecdocs = Path.Combine(prefix, v)},
				{"dbnames=", "Filenames list", (v) => dbnames = Path.Combine(prefix, v)}
			};
			ops.Parse(args);
			if (dbnames == null) {
				throw new ArgumentNullException("dbnames can't be null");
			}
			if (vecdocs == null) {
				throw new ArgumentOutOfRangeException("vecdocs can't be null");
			}
			if (numdocs <= 0) {
				throw new ArgumentException("numdocs should be bigger than 0");
			}
			string[] filenames = File.ReadAllLines(dbnames);
			if (filenames.Length < numdocs) {
				throw new ArgumentException("numdocs can't be bigger than filenames.Length");
			}
			if (vecdocs.EndsWith(".bin")) {
				this.LoadVecDocsBinary(vecdocs, numdocs, filenames);
			} else if (File.Exists(vecdocs + ".bin")) {
				this.LoadVecDocsBinary(vecdocs + ".bin", numdocs, filenames);
			} else {
				this.LoadVecDocsAscii(vecdocs, numdocs, filenames);
			}
		}

		void LoadVecDocsBinary (string binname, int numdocs, string[] filenames)
		{
			BinaryReader bread = new BinaryReader (File.OpenRead(binname));
			int pc = 1 + numdocs / 100;
			for (int i = 0; i < numdocs; i++) {
				if ((i % pc) == 0) {
					Console.WriteLine ("Loading document: {0}, name: {1}, advance: {2:0.00}%", i, filenames[i], i * 100.0 / numdocs);
				}
				this.names.Add (filenames[i]);
				this.docs.Add (Tdoc.Load (bread));
			}
			bread.Close ();	
		}
		
		void LoadVecDocsAscii (string asciiname, int numdocs, string[] filenames)
		{
			BinaryWriter bwrite = new BinaryWriter (File.Create(asciiname + ".bin"));
			TextReader reader = new StreamReader(File.OpenRead(asciiname));
			int pc = 1 + numdocs / 100;
			for (int i = 0; i < numdocs; i++) {
				if ((i % pc) == 0) {
					Console.WriteLine ("Loading/Writing document: {0}, name: {1}, advance: {2:0.00}%", i, filenames[i], i * 100.0 / numdocs);
				}
				this.names.Add (filenames[i]);
				var docvec = this.ParseFromString(reader.ReadLine(), false);
				this.docs.Add (docvec);
				docvec.Save(bwrite);
			}
			bwrite.Close ();	
			reader.Close();
		}

		/// <summary>
		/// Saves a subspace of the space
		/// </summary>
		public void SubSpace (string name, int samplesize, bool random)
		{
			/*  {"numdocs=", "Number of documents", (v) => numdocs = int.Parse(v)},
				{"vecdocs=", "vector documents", (v) => vecdocs = v},
				{"dbnames=", "Filenames list", (v) => dbnames = v}
			 */
			// IList<int> sample = natix.Perms<Tdoc>.GetRandomSample (this, samplesize, random);
			IList<int> sample;
			if (random) {
				sample = RandomSets.GetRandomSubSet (samplesize, this.Count);
			} else {
				sample = RandomSets.GetExpandedRange (samplesize);
			}
			Console.WriteLine ("Documents SubSpace {0}, size: {1}, random: {2}", name, sample.Count, random);
			this.SubSpace (name, sample);
		}
		
		public void SubSpace (string name, IList<int> permutation)
		{
			string dbnames = name + ".dbnames";
			string vecsname = name + ".docvecs.bin";
			var vecs = new BinaryWriter (File.Create (vecsname));
			var names = new StreamWriter (dbnames);
			for (int docid = 0; docid < permutation.Count; docid++) {
				string n = Path.GetFileName (this.GetItemName (docid));
				n = Path.Combine (name, n);
				names.WriteLine (n);
				this[docid].Save (vecs);
			}
			var sheader = String.Format ("--numdocs {0} --vecdocs {1} --dbnames {2}",
				permutation.Count,
				Path.GetFileName (vecsname),
				Path.GetFileName(dbnames));
			File.WriteAllText (name, sheader);
			vecs.Close();
			names.Close();
			
		}

		/// <summary>
		/// Returns the number of accumulated distances
		/// </summary>
		public int NumberDistances {
			get { return this.numdist; }
		}
		
		/// <summary>
		/// Parse an string into the document's vector
		/// </summary>
		/// <param name="s">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="isquery">
		/// A <see cref="System.Boolean"/>
		/// </param>
		/// <returns>
		/// A <see cref="Tdoc"/>
		/// </returns>
		public Tdoc Parse (string s, bool isquery)
		{
			return this.ParseFromFile (s, isquery);
		}
		
		/// <summary>
		/// Load vectors from ascii string
		/// </summary>
		public Tdoc ParseFromString (string data, bool isquery)
		{
			List<Tvoc> v = new List<Tvoc> ();
			var matches = Regex.Matches (data, @"[\d\.eE\-\+]+");
			var mcount = matches.Count;
			for (int i = 0; i < mcount; i += 2) {
				var k = uint.Parse(matches[i].Value);
				var d = float.Parse(matches[i+1].Value);
				v.Add(new Tvoc(k, d));
			}
			return new Tdoc (v.ToArray ());
		}
		
		/// <summary>
		/// Parse document vector from file
		/// </summary>
		public Tdoc ParseFromFile (string name, bool isquery)
		{
			return this.ParseFromString (File.ReadAllText (name), isquery);
		}

		/// <summary>
		/// Builds the Tdoc from two vectors
		/// </summary>
		/// <param name="keywords">
		/// A <see cref="UInt32[]"/>
		/// </param>
		/// <param name="weigths">
		/// A <see cref="Single[]"/>
		/// </param>
		/// <returns>
		/// A <see cref="Tdoc"/>
		/// </returns>
		public Tdoc BuildTdocFromVectors (IList<UInt32> keywords, IList<Single> weigths)
		{
			List<Tvoc> v = new List<Tvoc> ();
			for (int i = 0; i < keywords.Count; i++) {
				UInt32 keyid = keywords[i];
				float d = weigths[i];
				v.Add (new Tvoc (keyid, d));
			}
			return new Tdoc (v.ToArray ());	
		}
		/// <summary>
		/// The distance function (angle between vectors)
		/// </summary>
		/// <param name="v1">
		/// A <see cref="Tdoc"/>
		/// </param>
		/// <param name="v2">
		/// A <see cref="Tdoc"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Double"/>
		/// </returns>
		public double Dist (Tdoc v1, Tdoc v2)
		{
			this.numdist++;
			Tvoc[] w1 = v1.doc;
			int n1 = v1.doc.Length;
			Tvoc[] w2 = v2.doc;
			int n2 = v2.doc.Length;
			double sum, norm1, norm2;
			norm1 = norm2 = sum = 0.0;
			for (int i = 0; i < n1; i++) {
				norm1 += w1[i].weight * w1[i].weight;
			}
			for (int i = 0; i < n2; i++) {
				norm2 += w2[i].weight * w2[i].weight;
			}
			for (int i = 0,j = 0; (i < n1) && (j < n2);) {
				if (w1[i].keyid == w2[j].keyid) {
					// match
					sum += w1[i].weight * w2[j].weight;
					i++;
					j++;
				} else if (w1[i].keyid < w2[j].keyid) {
					i++;
				} else {
					j++;
				}
			}
			// free(w1); free(w2);
			// printf ("internal product: %f\n",sum/(sqrt(norm1)*sqrt(norm2)));
			// printf ("distance: %f\n",acos(sum/(sqrt(norm1)*sqrt(norm2))));
			double M = sum/(Math.Sqrt(norm1)*Math.Sqrt(norm2));
			//M=max(-1.0,min(1.0,M));
			M=Math.Min(1.0, M);
			return Math.Acos(M);
		}
	}
}
