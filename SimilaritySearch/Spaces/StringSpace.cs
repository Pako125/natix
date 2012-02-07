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
//   Original filename: natix/natix/SimilaritySearch/Spaces/StringSpace.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Available string distances
	/// </summary>
	public enum StringDistance {
		/// <summary>
		/// Levenshtein distance (edit distance)
		/// </summary>
		Levenshtein,
		/// <summary>
		/// Hamming distance
		/// </summary>
		Hamming
		//,LCS
	}
	/// <summary>
	/// String space
	/// </summary>
	public class StringSpace : Space< IList<char> >
	{
		static Dictionary<string, StringSpace> cache = new Dictionary<string, StringSpace>();
		string name;
		IList< IList<char> > data;
		int numdist;
		Distance< IList< char > > dist;
		
		/// <summary>
		///  The generic type 
		/// </summary>
		public Type GenericType {
			get { return typeof(IList<char> ); }
		}
		/// <summary>
		/// Constructor
		/// </summary>
		public StringSpace ()
		{
			this.name = null;
			this.data = null;
			this.numdist = 0;
			this.dist = new Distance< IList<char> > (StringSpace.Levenshtein);
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sdist">
		/// A <see cref="StringDistance"/>
		/// </param>
		public StringSpace (StringDistance sdist) : this()
		{
			switch (sdist) {
			case StringDistance.Levenshtein:
				this.dist = new Distance< IList<char> > (StringSpace.Levenshtein);
				break;
			//case StringDistance.LCS:
			//	this.dist = new Distance<string> (this.LCS);
			//	break;
			case StringDistance.Hamming:
				this.dist = new Distance< IList<char> > (StringSpace.Hamming);
				break;
			}
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
			IList<int> sample; // = Perms<IList<char>>.GetRandomSample (this, samplesize, random);
			if (random) {
				sample = RandomSets.GetRandomSubSet (samplesize, this.Count);
			} else {
				sample = RandomSets.GetExpandedRange (samplesize);
			}
			this.SubSpace (name, sample);
		}
		
		public void SubSpace (string name, IList<int> sample)
		{
			StreamWriter w = new StreamWriter (name);
			foreach (int i in sample) {
				foreach (char c in this[i]) {
					w.Write (c);
				}
				w.WriteLine ();
			}
			w.Close ();
			
		}
		/// <summary>
		/// Retrieves the object associated to object id docid
		/// </summary>
		/// <param name="docid">
		/// A <see cref="System.Int32"/>
		/// </param>
		public IList<char> this[int docid]
		{
			get { return this.data[docid]; }
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
				if (StringSpace.cache.ContainsKey (this.name)) {
					Console.WriteLine ("Loading {0} from cache", this.name);
					StringSpace sp = StringSpace.cache[this.name];
					this.data = sp.data;
					this.numdist = sp.numdist;
				} else {
					StreamReader stream = new StreamReader (new BufferedStream (new FileStream (this.name, FileMode.Open)));
					this.data = ListContainerFactory<char>.GetVariableSizeListContainer ();
					// this.data = new List<IList<char>> ();
					for (int docid = 0; !stream.EndOfStream; docid++) {
						IList<char> s = this.Parse (stream.ReadLine (), false);
						this.data.Add (s);
					}
					stream.Close ();
					StringSpace.cache[name] = this;
				}
			}
		}
		
		public IResult CreateResult (int K, bool ceiling)
		{
			return new ResultTies (K, ceiling);
		}
		/// <summary>
		/// Parse an string into the object representation
		/// </summary>

		public IList<char> Parse (string s, bool isquery)
		{
			return s.Trim ().ToLower ().ToCharArray ();
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
			get { return (this.data == null) ? 0 : this.data.Count; }
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
		public double Dist (IList<char> a, IList<char> b)
		{
			this.numdist++;
			return this.dist (a, b);
		}
		
		/// <summary>
		/// Edit distance
		/// </summary>
		public static double Levenshtein (IList<char> a, IList<char> b)
		{
			return SequenceSpace<char>.Levenshtein(a, b, 1, 1, 1);
		}
		/// <summary>
		/// The hamming distance
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
		public static double Hamming (IList<char> a, IList<char> b)
		{
			int d = 0;
			for (int i = 0; i < a.Count; i++) {
				if (a[i] != b[i]) {
					d++;
				}
			}
			return (double)d;
		}
	}
}
