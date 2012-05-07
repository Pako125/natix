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
//   Original filename: natix/SimilaritySearch/Spaces/AudioSpace.cs
// 
using System;
using System.IO;
using System.Collections.Generic;
using natix.CompactDS;
using natix.SortingSearching;

namespace  natix.SimilaritySearch
{
	public class BinQGram : ListGenerator<byte>
	{
		int StartIndex;
		int Len;
		IList<byte> Data;
		
		public BinQGram (IList<byte> Data, int startIndex, int Len)
		{
			this.StartIndex = startIndex;
			this.Data = Data;
			this.Len = Len;
		}
		
		public override int Count {
			get {
				return this.Len;
			}
		}
		
		public override byte GetItem (int index)
		{
			return this.Data[this.StartIndex + index];
		}
		
		public override void SetItem (int index, byte u)
		{
			throw new NotSupportedException ();
		}
	}
	
	public class AudioSpace : Space< IList<byte> >
	{
		public ListIDiff ListOfLengths;
		IList< byte > Data;
		public int SymbolSize;
		public int Q;
		string listname;
		int numdist = 0;
		IList<string> NameList;
		
		public string Name {
			get {
				return this.listname;
			}
			set {
				this.listname = value;
				if (!File.Exists (this.listname + ".header")) {
					this.Build (this.listname);
				}
				this.Load (this.listname);
			}
		}
				
		public IList<byte> Parse (string name, bool isquery)
		{
			if (name.StartsWith ("obj")) {
				var A = name.Split (' ');
				var id = A [1];
				var u = this [int.Parse (id)];
				if (A.Length == 2) {
					return u;
				}
				int num_bits = u.Count * 8;
				var num_flips = float.Parse (A [2]) * num_bits;
				var L = new byte[u.Count];
				for (int i = 0; i < u.Count; ++i) {
					L [i] = u [i];
				}
				var rand = new Random ();
				for (int i = 0; i < num_flips; ++i) {
					var pos = rand.Next (0, num_bits);
					if (BitAccess.GetBit (L, pos)) {
						BitAccess.ResetBit (L, pos);
					} else {
						BitAccess.SetBit (L, pos);
					}
				}
				return L;
			}
			var res = BinaryHammingSpace.ParseAndLoadFromFile (name, !isquery);
			return new BinQGram (res, 0, this.Q);
		}

		public IResult CreateResult (int K, bool ceiling)
		{
			return new Result (K, ceiling);
		}
		
		public string SpaceType {
			get {
				return "audio-space";
			}
			set {
				throw new NotImplementedException ();
			}
		}
		
		public Type GenericType {
			get {
				return typeof(IList<byte>);
			}
		}
		
		public int NumberDistances {
			get {
				return this.numdist;
			}
		}
		
		public double Dist (IList<byte> a, IList<byte> b)
		{
			this.numdist++;
			/* if (this.numdist % 2048 == 0) {
				Console.WriteLine ("numdist: {0}, a.Count: {1}, b.Count: {2}", this.numdist, a.Count, b.Count);
			}
			*/
			return BinaryHammingSpace.DistMinHamming (a, b, this.SymbolSize);
		}

		public AudioSpace () : this(30,1)
		{
		}

		public AudioSpace (int qsize, int symsize)
		{
			this.SymbolSize = symsize;
			this.Q = qsize;
		}
		
		public void Build (string listname)
		{
			this.Build (listname, this.Q, this.SymbolSize);
		}

		public void Build (string listname, int qsize, int symsize)
		{
			this.Q = qsize;
			this.SymbolSize = symsize;
			int linenum = 0;
			this.ListOfLengths = new ListIDiff ();
			using (var Output = new BinaryWriter (File.Create (listname + ".bin"))) {
				foreach (var filename in File.ReadAllLines (listname)) {
					linenum++;
					Console.WriteLine ("**** Loading line-number: {0}, file: {1}", linenum, filename);
					var data = BinaryHammingSpace.ParseAndLoadFromFile (filename, false);
					PrimitiveIO<byte>.WriteVector (Output, data);
					if (data.Count % this.Q > 0) {
						// padding
					}
					this.ListOfLengths.Add (data.Count);
				}
			}
			using (var Output = new BinaryWriter (File.Create (listname + ".lens"))) {
				this.ListOfLengths.Save (Output);
			}
			using (var Output = new StreamWriter (listname + ".header")) {
				Output.WriteLine ("Q: {0}", this.Q);
				Output.WriteLine ("SymbolSize: {0}", this.SymbolSize);
				Output.WriteLine ("NumFiles: {0}", linenum);
			}
		}
		
		public void SubSpace (string name, IList<int> permutation)
		{
			using (var Output = new StreamWriter (File.Create (name))) {
				Output.WriteLine (this.listname);
			}
			using (var Output = new BinaryWriter (File.Create (name + ".bin"))) {
				for (int i = 0; i < permutation.Count; i++) {
					PrimitiveIO<byte>.WriteVector (Output, this[permutation[i]]);
				}
			}
			var lens = new ListIDiff ();
			lens.Add (permutation.Count * this.Q);
			using (var Output = new BinaryWriter (File.Create (name + ".lens"))) {
				lens.Save (Output);
			}
			using (var Output = new StreamWriter (name + ".header")) {
				Output.WriteLine ("Q: {0}", this.Q);
				Output.WriteLine ("SymbolSize: {0}", this.SymbolSize);
				Output.WriteLine ("NumFiles: {0}", 1);
			}
		}

		public void SubSpace (string name, int samplesize, bool random)
		{
			if (!random && samplesize < 0) {
				samplesize = this.Count;
			}
			samplesize = Math.Min (samplesize, this.Count);
			Console.WriteLine ("******* samplesize: {0}, len: {1}", samplesize, this.Count);
			Console.WriteLine ("BinaryHammingSpace: There're several meanings of subspace.");
			IList<int> sample;// = Perms<IList<byte>>.GetRandomSample (this, samplesize, random);
			if (random) {
				sample = RandomSets.GetRandomSubSet (samplesize, this.Count);
			} else {
				sample = RandomSets.GetExpandedRange (samplesize);
			}
			Sorting.Sort<int> (sample);
			this.SubSpace (name, sample);
		}
	
		public void SubSpace (string name, int samplesize, int QRef)
		{
			int q = this.Q;
			this.Q = QRef;
			this.SubSpace (name, samplesize, true);
			this.Q = q;
		}
		
		public void Load (string listname)
		{
			using (var Input = new StreamReader (File.OpenRead (listname + ".header"))) {
				var S = Input.ReadLine ().Split (' ');
				this.Q = int.Parse (S[S.Length - 1]);
				S = Input.ReadLine ().Split (' ');
				this.SymbolSize = int.Parse (S[S.Length - 1]);
			}
			this.ListOfLengths = new ListIDiff ();
			Console.WriteLine ("Loading file lengths");
			using (var Input = new BinaryReader (File.OpenRead (listname + ".lens"))) {
				this.ListOfLengths.Load (Input);
			}
			var dset = this.ListOfLengths.dset;
			this.Data = new byte[dset.Count - dset.Count1];
			Console.WriteLine ("Loading binary data {0}.bin", listname);
			using (var Input = new BinaryReader (File.OpenRead (listname + ".bin"))) {
				PrimitiveIO<byte>.ReadFromFile (Input, this.Data.Count, this.Data);
			}
			Console.WriteLine ("Reading filenames");
			this.NameList = File.ReadAllLines (this.listname);
			Console.WriteLine ("Q: {0}, SymbolSize: {1}, Data.Count: {2}, N: {3}",
				this.Q, this.SymbolSize, this.Data.Count, this.Count);
		}

		public IList<byte> this[int docid] {
			get { return this.GetQGram (docid); }
		}

		public IList<byte> GetQGram (int docid)
		{
			// int startIndex = docid * this.Q;
			//int startIndex = docid * this.SymbolSize;
			//return new ListGen<byte> (delegate(int i) {
			//	return this.Data[startIndex + i];
			//}, this.Q);
			return new BinQGram (this.Data, docid * this.SymbolSize, this.Q);
		}
		
		public IList<byte> GetAudio (int audioId)
		{
			var dset = this.ListOfLengths.dset;
			var startPos = 0;
			if (audioId > 0) {
				startPos = dset.Select1 (audioId);
			}
			var len = this.ListOfLengths [audioId];
			startPos -= audioId;
			return new BinQGram (this.Data, startPos, len);
		}
		
		public int Count {
			get {
				// return this.Data.Count / this.Q;
				return (this.Data.Count - Q) / this.SymbolSize;
			}
		}
		
		public string GetNameFromDocId (int docId)
		{
			return this.NameList[docId];
		}
		
		public int GetDocIdFromBlockId (int blockId)
		{
			blockId *= this.SymbolSize;
			int rank1 = this.ListOfLengths.dset.Rank1 (blockId);
			return this.ListOfLengths.dset.Rank1 (blockId + rank1);
		}
	}
}

