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
//   Original filename: natix/SimilaritySearch/Spaces/BinaryHammingSpace.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using natix.CompactDS;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Identifiers to binary distances
	/// </summary>
	public enum BinaryDistance
	{
		/// <summary>
		/// Minimum Hamming
		/// </summary>
		MinHamming,
		/// <summary>
		/// Minimum Jaccard
		/// </summary>
		MinJaccard,
		/// <summary>
		/// Minimum enabled bits after OR (not tested for metric properties)
		/// </summary>
		MinEnabled,
		/// <summary>
		/// External distance
		/// </summary>
		External
	}
	/// <summary>
	/// Hamming space for bit strings
	/// </summary>
	public class BinaryHammingSpace : Space< IList<byte> >
	{
		static string[] ascii_nibbles = new string[] { 
			"0000", "0001", "0010", "0011", 
			"0100", "0101", "0110", "0111", 
			"1000", "1001", "1010", "1011",	
			"1100", "1101", "1110", "1111"};
		string name;
		List<string> names;
		IList< IList<byte> > pool;
		int numdist;
		int fixed_dim;
		int fixed_len;
		/// <summary>
		///  Symbol's length in bytes
		/// </summary>
		/// <remarks>
		/// The length in bytes of each symbol. For general data this should be 1, for audio MBSES this should be 3. 
		/// </remarks>
		public int symlen;
		/// <summary>
		/// Real distance to be executed. This can be overloaded from outside.
		/// </summary>
		public Distance< IList<byte> > RealDist;
		Func<int, string> get_item_name; 

		/// <summary>
		///  The generic type of this space (byte[]), used by reflection capabilities
		/// </summary>
		public Type GenericType {
			get { return typeof(IList<byte>); }
		}

		/// <summary>
		///  Binary distance enum. Use BinDist instead.
		/// </summary>
		public BinaryDistance bindist;
		
		/// <summary>
		/// Get/Set the bindist
		/// </summary>
		/// <remarks>Get the bindist.
		///  The Set operation choose the apropiated distance
		/// </remarks>
		public BinaryDistance BinDist {
			get { return this.bindist; }
			set {
				this.bindist = value;
				switch (value) {
				case BinaryDistance.MinEnabled:
					this.RealDist = new Distance< IList<byte> > (this.DistMinEnabled);
					break;
				case BinaryDistance.MinHamming:
					this.RealDist = new Distance< IList<byte> > (this.DistMinHamming);
					break;
				case BinaryDistance.MinJaccard:
					this.RealDist = new Distance< IList<byte> > (this.DistMinJaccard);
					break;
				default:
					throw new ArgumentException ("Unknown Binary Distance " + value.ToString());
				}
			}
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		public BinaryHammingSpace ()
		{
			this.names = new List<string> ();
			this.pool = new List<IList<byte>> ();
			this.symlen = 1;
			this.BinDist = BinaryDistance.MinHamming;
			// default
			this.get_item_name = (docid) => this.names[docid];
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="symlen">
		/// Length of the symbol
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="fixed_len">
		/// Fixed length for space if fixed_len > 0
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="fixed_dim">
		/// Fixed dimension for the space if fixed_dim > 0
		/// A <see cref="System.Int32"/>
		/// </param>
		public BinaryHammingSpace (int symlen, int fixed_len, int fixed_dim) : this()
		{
			this.symlen = symlen;
			this.numdist = 0;
			if (fixed_len > 0) {
				// changing the default
				this.get_item_name = (docid) => docid.ToString();
			}
			this.fixed_dim = fixed_dim;
			this.fixed_len = fixed_len;
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="symlen">
		/// Symbol's length
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="fixed_len">
		/// Fixed length for space if fixed_len > 0
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="fixed_dim">
		/// Fixed dimension for bit vectors if fixed_dim > 0
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="b">
		/// The binary distance
		/// A <see cref="BinaryDistance"/>
		/// </param>
		public BinaryHammingSpace (int symlen, int fixed_len, int fixed_dim, BinaryDistance b) : this(symlen, fixed_len, fixed_dim)
		{
			this.BinDist = b;
		}
	
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="symlen">
		/// Symbol's length
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="fixed_len">
		/// Fixed space's length if fixed_len > 0
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="fixed_dim">
		/// Fixed space's dimension if fixed_dim > 0
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="b">
		/// The distance function
		/// A <see cref="BinaryDistance"/>
		/// </param>
		/// <param name="name">
		/// File to load the database
		/// </param>
		public BinaryHammingSpace (int symlen, int fixed_len, int fixed_dim, BinaryDistance b, string name) : this(symlen, fixed_len, fixed_dim, b)
		{
			this.Name = name;
		}
		
		/// <summary>
		/// Get/Set the database name.
		/// </summary>
		public string Name
		{
			set {
				this.name = value;
				Console.WriteLine("Loading '{0}' BinaryHammingSpace database", value);
				if (this.name == null || this.name.Length == 0)  return;
				if (value.EndsWith(".list")) {
					this.symlen = 3;
					// TODO: Subclass for audio fingerprints, or a better configuration file
					this.ReadFromList(value);
				} else {
					this.ReadFromBinaryFile(value);
				}
				Console.WriteLine("Done. {0} loaded.", value);
			}
			get {
				return this.name;
			}
		}
		
		public IResult CreateResult (int K, bool ceiling)
		{
			//if (this.fixed_len > 0 && this.fixed_dim < 256) {
			//	return new ResultTies (K, ceiling);
			//} else {
			return new Result(K, ceiling);
			//}
		}
		/// <summary>
		/// Read the database from a listing file (one filename per line)
		/// </summary>
		/// <param name="filename">
		/// The file's name
		/// </param>
		public void ReadFromList (string filename)
		{
			Console.WriteLine ("****** Reading database from list of files");
			StreamReader r = new StreamReader (filename);
			while (!r.EndOfStream) {
				string s = r.ReadLine ().Trim ();
				if (s.Length == 0) {
					continue;
				}
				this.names.Add (s);
				this.pool.Add (this.Parse (s, false));
			}
			Console.WriteLine ("done reading");
			r.Close ();
		}
		
		/// <summary>
		/// Reads the database from a single bundle binary file
		/// </summary>
		/// <param name="filename">
		/// The file to be loaded
		/// A <see cref="System.String"/>
		/// </param>
		public void ReadFromBinaryFile (string filename)
		{
			Console.WriteLine ("***** Reading database from binary file: '{0}'", filename);
			string[] header = File.ReadAllText (filename).Split (':');
			this.fixed_dim = int.Parse (header[1]);
			this.fixed_len = int.Parse (header[2]);
			string binfile = header[3];
			//Console.WriteLine ("POOL: {0}", this.pool);
			this.BinDist = (BinaryDistance)Enum.Parse (typeof(BinaryDistance), header[4]);
			BinaryReader b = new BinaryReader (new BufferedStream (new FileStream (binfile, FileMode.Open), 1 << 20));
			//Console.WriteLine ("===> len: {0}, dim: {1}", fixed_len, fixed_dim);
			for (int i = 0; i < this.fixed_len; i++) {
				this.pool.Add (b.ReadBytes (this.fixed_dim));
			}
			b.Close ();
		}

		/// <summary>
		/// Add some object to the space
		/// </summary>
		/// <param name="name">
		/// The name to be stored with this object
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="b">
		/// Object to store
		/// A <see cref="System.Byte[]"/>
		/// </param>
		public void Add (string name, IList<byte> b)
		{
			this.names.Add (name);
			this.pool.Add (b);
		}

		/// <summary>
		/// Saves an object into the space. No name is stored.
		/// </summary>
		public void Add (IList<byte> b)
		{
			this.pool.Add (b);
		}
		
		/// <summary>
		/// Indexer to retrieve an object
		/// </summary>
		/// <param name="docid">
		/// Identifier of the object
		/// A <see cref="System.Int32"/>
		/// </param>
		public IList<byte> this[int docid]
		{
			get { return this.pool[docid]; }
			set { this.pool[docid] = value; }
		}
		
		public void SubSpace (string name, IList<int> sample)
		{	
			if (this.fixed_len <= 0) {
				if (!name.EndsWith (".list")) {
					throw new ArgumentException ("A subspace without fixed length should have a .list extension");
				}
				Console.WriteLine ("Dumping binary fingerprints to directory");
				string namedir = name + ".pool";
				if (!Directory.Exists (namedir)) {
					Directory.CreateDirectory (namedir);
				}
				StreamWriter w = new StreamWriter (name);
				foreach (int docid in sample) {
					string fname = Path.Combine (namedir, Path.GetFileName (this.GetItemName (docid)) + ".bin");
					Console.WriteLine ("Saving {0}", fname);
					w.WriteLine (fname);
					using (var wf = File.Create (fname)) {
						foreach (byte bv in this[docid]) {
							wf.WriteByte (bv);
						}
					}
				}
				w.Close ();
			} else {
				Console.WriteLine ("Dumping binary arrays to a single binary file");
				string h = String.Format (@"{0}:{1}:{2}:{3}:{4}", this.SpaceType, this.fixed_dim, sample.Count, name + ".bin", this.BinDist);
				File.WriteAllText (name, h);
				BinaryWriter b = new BinaryWriter (new BufferedStream (new FileStream (name + ".bin", FileMode.Create), 1 << 20));
				foreach (int docid in sample) {
					foreach (byte bv in this[docid]) {
						b.Write (bv);
					}
				}
				b.Close ();
			}
		}
		/// <summary>
		/// Saves a subspace
		/// </summary>
		/// <remarks>
		/// Saves the subspace in filename 'name' of size samplesize. The sample can be taken randomly or sequentialy (starting in 0)
		/// Setting samplesize to a negative value means to copy the entire database.
		/// </remarks>
		/// <param name="name">
		/// The file's name to store the subspace
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="samplesize">
		/// The size of the sample (negatives means the entire size)
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="random">
		/// If true the sample should be uniformly sampled, else sequential sample will be used.
		/// A <see cref="System.Boolean"/>
		/// </param>
		public void SubSpace (string name, int samplesize, bool random)
		{
			if (!random && samplesize < 0) {
				samplesize = this.Count;
			}
			samplesize = Math.Min (samplesize, this.Count);
			Console.WriteLine ("******* samplesize: {0}, len: {1}", samplesize, this.Count);
			Console.WriteLine ("BinaryHammingSpace: There're several meanings of subspace.");
			// IList<int> sample = Perms<IList<byte>>.GetRandomSample (this, samplesize, random);
			if (random) {
				var sample = RandomSets.GetRandomSubSet (samplesize, this.Count);
				this.SubSpace (name, sample);
			} else {
				var sample = RandomSets.GetExpandedRange (samplesize);
				this.SubSpace (name, sample);
			}
		}
		
		/// <summary>
		/// Returns a string representation of a single byte
		/// </summary>
		/// <param name="b">
		/// A <see cref="System.Byte"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public static string ToAsciiString (byte b)
		{
			return ascii_nibbles[(b & 0xF0) >> 4] + ascii_nibbles[b & 0x0F];
		}
		/// <summary>
		/// Returns the string representation of an UInt16
		/// </summary>
		/// <param name="b">
		/// A <see cref="UInt16"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public static string ToAsciiString (UInt16 b)
		{
			return ToAsciiString((byte)(b >> 8)) + ToAsciiString((byte)(b & 0xFF));
		}
		/// <summary>
		/// Returns the string representation of an UInt32
		/// </summary>
		/// <param name="b">
		/// A <see cref="UInt32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public static string ToAsciiString (UInt32 b)
		{
			return ToAsciiString((UInt16)(b >> 16)) + ToAsciiString((UInt16)(b & 0xFFFF));
		}

		/// <summary>
		/// Converts an UInt64 to binary (ascii format)
		/// </summary>
		public static string ToAsciiString (UInt64 b)
		{
			return ToAsciiString((UInt32)(b >> 32)) + ToAsciiString((UInt32)(b & 0xFFFFFFFF));
		}
		/// <summary>
		/// Converts an object to a readeable representation in ascii '0' and '1'
		/// </summary>
		public static string ToAsciiString (IList<byte> b)
		{
			StringWriter s = new StringWriter ();
			for (int i = 0; i < b.Count; i++) {
				s.Write (ToAsciiString (b[i]));
			}
			string _s = s.ToString ();
			s.Close ();
			return _s;
		}
		public static string ToAsciiString (int d)
		{
			return BinaryHammingSpace.ToAsciiString((uint)d);
		}

		/// <summary>
		/// Converts an object to Ascii '0' and '1' using an object id
		/// </summary>
		/// <param name="docid">
		/// The object identifier
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// The '0' and '1' string
		/// A <see cref="System.String"/>
		/// </returns>
		public string ObjectToAsciiString (int docid)
		{
			return BinaryHammingSpace.ToAsciiString (this [docid]);
		}
		
		public static IList<byte> ParseFromString (string data)
		{
			List<byte > res = new List<byte> ();
			int ishift = 0;
			int buffer = 0;
			foreach (char r in data) {
				switch (r) {
				case '1':
					buffer |= 1 << ishift;
					break;
				case '0':
					break;
				default:
					continue;
				}
				if (ishift == 7) {
					ishift = 0;
					res.Add ((byte)buffer);
					buffer = 0;
				} else {
					ishift++;
				}
			}
			return res;
		}
		
		public static IList<byte> ParseAndLoadFromFile (string name, bool save_binary_cache)
		{
			string bin;
			if (name.EndsWith (".bin") || name.EndsWith (".binafb")) {
				Console.WriteLine ("Loading audio fingerprint {0}", name);
				return File.ReadAllBytes (name);
			} else {
				bin = name + ".bin";
			}
			if (File.Exists (bin)) {
				Console.WriteLine ("Loading binary version {0}.bin", name);
				return File.ReadAllBytes (bin);
			}
			Console.WriteLine ("Loading audio fingerprint {0}", name);
			var res = ParseFromString (File.ReadAllText (name));
			if (save_binary_cache) {
				Console.WriteLine ("Writing binary version {0}.bin of {1} bytes", name, res.Count);
				using (var binfile = new BinaryWriter(File.Create(bin))) {
					PrimitiveIO<byte>.WriteVector (binfile, res);
				}
			}
			return res;
		}
		
		/// <summary>
		/// Converts 'name' into an object
		/// </summary>
		public IList<byte> Parse (string name, bool isquery)
		{
			//Console.WriteLine ("Parsing '{0}', isquery: {1}", name, isquery);
			if (name.StartsWith ("obj")) {
				return this[int.Parse (name.Split (' ')[1])];
			}
			var res = ParseAndLoadFromFile (name, !isquery);
			return res;
		}
		/// <summary>
		/// The current length of the space
		/// </summary>
		public int Count {
			get { return this.pool.Count; }
		}
		
		/// <summary>
		/// Returns the name of the object. For listing spaces, uses the specified name 
		/// </summary>
		/// <param name="docid">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// The name of the object id
		/// A <see cref="System.String"/>
		/// </returns>
		public string GetItemName (int docid)
		{
			return this.get_item_name (docid);
		}
		/// <summary>
		///  The number of computed distances. This property is deprecated
		/// </summary>
		public int NumberDistances {
			get { return this.numdist; }
		}
		/// <summary>
		/// The name of the space type. Used to save and load spaces
		/// </summary>
		public string SpaceType {
			get { return this.GetType ().FullName; }
			set { }
		}
		/// <summary>
		/// Wrap the distance to the given BinDist distance.
		/// </summary>
		/// <param name="a">
		/// An object
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <param name="b">
		/// An object
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <returns>
		/// The distance from a to b
		/// A <see cref="System.Double"/>
		/// </returns>
		public double Dist (IList<byte> a, IList<byte> b)
		{
			this.numdist++;
			return this.RealDist (a, b);
		}
		/// <summary>
		/// The minimum hamming distance
		/// </summary>
		/// <param name="a">
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <param name="b">
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Double"/>
		/// </returns>
		
		public double DistMinHamming (IList<byte> a, IList<byte> b)
		{
			return DistMinHamming (a, b, this.symlen);
		}
		
		public static double DistMinHamming (IList<byte> a, IList<byte> b, int symlen)
		{
			int min = int.MaxValue;
			if (a.Count < b.Count) {
				IList<byte> w = a;
				a = b;
				b = w;
			}
			int bL = b.Count;
			int aL = a.Count - bL;
			int d;
			//Console.WriteLine ("aL: {0} bL: {1}, symlen: {2}", aL, bL, this.symlen);
			for (int askip = 0; askip <= aL; askip += symlen) {
				d = 0;
				for (int bskip = 0, abskip = askip; bskip < bL; bskip++,abskip++) {
					// Console.WriteLine ("a:{0}, b:{1}, A: {2}, B: {3}", askip, bskip, a[askip], b[bskip]);
					d += Bits.PopCount8[a[abskip] ^ b[bskip]];
				}
				if (min > d) {
					min = d;
				}
			}
			return min;
		}
		
		/// <summary>
		/// The minimum enabled bits (metric/distance?)
		/// </summary>
		/// <param name="a">
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <param name="b">
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Double"/>
		/// </returns>
		public double DistMinEnabled (IList<byte> a, IList<byte> b)
		{
			int min = int.MaxValue;
			if (a.Count < b.Count) {
				IList<byte> w = a;
				a = b;
				b = w;
			}
			int bL = b.Count;
			int aL = a.Count - bL;
			int d;
			for (int askip = 0; askip <= aL; askip += this.symlen) {
				d = 0;
				for (int bskip = 0, abskip = askip; bskip < bL; bskip++,abskip++) {
					//Console.WriteLine ("a:{0}, b:{1}, A: {2}, B: {3}", askip, bskip, a[askip], b[bskip]);
					d += Bits.PopCount8[a[abskip] | b[bskip]];
				}
				if (min > d) {
					min = d;
				}
			}
			return min;
		}
		/// <summary>
		///  The minimum jaccard distance for bit-strings
		/// </summary>
		/// <param name="a">
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <param name="b">
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Double"/>
		/// </returns>
		public double DistMinJaccard (IList<byte> a, IList<byte> b)
		{
			double min = double.MaxValue;
			if (a.Count < b.Count) {
				IList<byte> w = a;
				a = b;
				b = w;
			}
			int bL = b.Count;
			int aL = a.Count - bL;
			for (int askip = 0; askip <= aL; askip += this.symlen) {
				float d = 0;
				for (int bskip = 0, abskip = askip; bskip < bL; bskip++,abskip++) {
					//Console.WriteLine ("a:{0}, b:{1}, A: {2}, B: {3}", askip, bskip, a[askip], b[bskip]);
					float I = Bits.PopCount8[a[abskip] & b[bskip]];
					float U = Bits.PopCount8[a[abskip] | b[bskip]];
					d += I / U;
				}
				if (min > d) {
					min = d;
				}
			}
			return min;
		}
	}
}
