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
//   Original filename: natix/SimilaritySearch/Spaces/VectorSpace.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Vector space
	/// </summary>
	public class VectorSpace<T> : Space<IList<T>> 
	{
		static INumeric<T> Num = (INumeric<T>)(natix.Numeric.Get (typeof(T)));
		/// <summary>
		/// The underlying storage for vectors 
		/// </summary>
		public IList< IList< T > > MatrixBackend;
		// public LargeMatrix<T> matrix;
		/// <summary>
		/// The underlying filename of the vectorspace
		/// </summary>
		protected string name;
		/// <summary>
		/// Dimension of the space
		/// </summary>
		public int Dimension;
		private int pnorm;
		/// <summary>
		/// Number of distances
		/// </summary>
		protected int numdist;
		/// <summary>
		/// If true perform the P root for Minkowski spaces 
		/// </summary>
		public bool DoSqrt;
		/// <summary>
		/// The real distance to be used
		/// </summary>
		public Distance< IList<T> > RealDist;

		/// <summary>
		///  The numeric manager handling the underlying numeric representation of the vector space.
		/// </summary>
		public INumeric<T> Numeric
		{
			get { return Num; }
		}
		/// <summary>
		/// Returns the generic type
		/// </summary>
		public Type GenericType {
			get { return typeof(IList<T>); }
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		public VectorSpace ()
		{
			this.DoSqrt = true;
		}
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dbvecs">
		/// Name of the ascii/binary vector file. If dbvecs ends with .bin or .data it's will be openned as binary 
		/// </param>
		/// <param name="len">
		/// The expected length of the space <see cref="System.Int32"/>
		/// </param>
		/// <param name="dim">
		/// The dimension <see cref="System.Int32"/>
		/// </param>
		/// <param name="pnorm">
		/// The P norm for Minkoski spaces <see cref="System.Int32"/>
		/// </param>
		/// <param name="dommap">
		/// Open the file emmulating the mmap system call
		/// </param>
		public VectorSpace (string dbvecs, int len, int dim, int pnorm, bool dommap) : base()
		{
			this.Dimension = dim;
			this.Initialize (dbvecs, len, dommap);
			this.PNorm = pnorm;
		}
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		public VectorSpace (string name):base()
		{
			this.Name = name;
		}
		
		/// <summary>
		/// Get/Set (and load) the database name
		/// </summary>
		public string Name {
			get { return this.name; }

			set {
				this.name = value;
				this.Load();
			}
		}
		
		public IResult CreateResult (int K, bool ceiling)
		{
			return new Result (K, ceiling);
		}
		/// <summary>
		/// The name of space's class
		/// </summary>
		public string SpaceType {
			get { return this.GetType().FullName; }
			set {  }
		}

		/// <summary>
		/// Set the P norm in Minkowski spaces
		/// </summary>
		public int PNorm {
			get { return this.pnorm; }
			set {
				this.pnorm = value;
				
				switch (value) {
				case 1:
					// this.RealDist = new Distance< IList<T> > (this.DistL1);
					this.RealDist = new Distance<IList<T>> (this.Numeric.DistL1);
					break;
				case 2:
					// this.RealDist = new Distance< IList<T> > (this.DistL2);
					this.RealDist = new Distance<IList<T>> (this.Numeric.DistL2);
					break;
				case -1:
					// this.RealDist = new Distance< IList<T> > (this.DistLInf);
					this.RealDist = new Distance<IList<T>> (this.Numeric.DistLInf);
					break;
				case -2:
					// this.RealDist = new Distance< IList<T> > (this.DistCos);
					this.RealDist = new Distance<IList<T>> (this.Numeric.DistCos);
					break;
				default:
					// this.RealDist = new Distance< IList<T> > (this.DistLP);
					this.RealDist = new Distance<IList<T>> ((a,b) => this.Numeric.DistLP(a,b, this.pnorm, this.DoSqrt));
					break;
				}
			}
		}
		
		/// <summary>
		///  The accumulated number of distances in the space
		/// </summary>
		public int NumberDistances {
			get {
				return this.numdist;
			}
		}
		

		/// <summary>
		/// Length of the space
		/// </summary>
		public int Count {
			get {
				return this.MatrixBackend.Count;
		    }
		}
		
		/// <summary>
		/// Enumerator for the space
		/// </summary>
		/// <returns>
		/// A <see cref="IEnumerator"/>
		/// </returns>
		public IEnumerator GetEnumerator ()
		{
			foreach (var e in this.MatrixBackend) {
				yield return e;
			}
		}
		
		/// <summary>
		/// Retrieves the object with identifier docid
		/// </summary>
		/// <param name="docid">
		/// A <see cref="System.Int32"/>
		/// </param>
		public IList<T> this[int docid]
		{
			get { return this.MatrixBackend[docid]; }
			//set { this.matrix[docid] = value; }
		}
		private void Load ()
		{
			StreamReader rn = new StreamReader (this.name);
			// dim size pnorm dosqrt ctype dommap dbnames dbvectors / Example: 112 112682 2 true f 0 None colors.ascii
			string desc = rn.ReadLine ().Trim ();
			string[] m = desc.Split (' ');
			rn.Close ();
			// read from this.name file
			this.Dimension = int.Parse (m[0]);
			int len = int.Parse (m[1]);
			this.PNorm = int.Parse (m[2]);
			this.DoSqrt = bool.Parse (m[3]);
			string ctype = m[4];
			bool dommap;
			try {
				dommap = bool.Parse (m[5]);
			} catch (FormatException) {
				dommap = (int.Parse (m[5]) == 1);
			}
			string dbnames = Path.Combine (Path.GetDirectoryName (this.name), m[6]);
			string dbvecs = Path.Combine (Path.GetDirectoryName (this.name), m[7]);
			Console.WriteLine ("** Reading vectors from file {0}", this.name);
			Console.WriteLine ("** Recommended type: {0}", ctype);
			Console.WriteLine ("** dbnames: {0}", dbnames);
			Console.WriteLine ("** dbvecs: {0}", dbvecs);
			Console.WriteLine ("** Recommended mmap: {0}", dommap);
			this.Initialize (dbvecs, len, dommap);
		}
		
	
		private void Initialize (string dbvecs, int len, bool dommap)
		{
			dbvecs = PrimitiveIO<T>.CreateBinaryFile (dbvecs, len, this.Dimension);
			if (dommap) {
				Console.WriteLine ("** Opening {0} with disk list container", dbvecs);
				var G = new ListGen<int> ((int i) => (1 + i) * this.Dimension, len);
				this.MatrixBackend = new DiskListContainer<T> (dbvecs, G);
			} else {
				Console.WriteLine ("** Opening {0} with standard fixed-size list container", dbvecs);
				//try {
				//	var L = PrimitiveIO<T>.ReadAllFromFile (dbvecs);
				//	this.MatrixBackend = ListContainerFactory<T>.GetFixedSizeListContainer (L, this.Dimension);
				//} catch (System.ArgumentOutOfRangeException) {
				Console.WriteLine ("The database is really large, we must use another strategy");
				Console.WriteLine ("to load it. Supposing a large memory.");
				Console.WriteLine ("number-of-vectors: {0}, dimension: {1}", len, this.Dimension);
				this.MatrixBackend = new IList<T>[len];
				int II = 0;
				using (var F = new BinaryReader (File.OpenRead (dbvecs))) {
					int pc = len / 100 + 1;
					for (int docid = 0; docid < len; docid++) {
						if (docid % pc == 0) {
							Console.Write ("docid: {0}, adv: {1:0.00}%, ", docid, docid * 100.0 / len);
							II++;
							if (II % 4 == 0) {
								Console.WriteLine ();
							}
						}
						var L = new T[this.Dimension];
						PrimitiveIO<T>.ReadFromFile (F, this.Dimension, L);
						this.MatrixBackend [docid] = L;
					}
				}
				Console.WriteLine ();
				//}
			}
			// this.matrix = new LargeMatrix<T> (dbvecs, len, dim, dommap);
		}
		
		/// <summary>
		/// Returns a vector from an string
		/// </summary>
		public IList<T> Parse (string s, bool isquery)
		{
			if (s.StartsWith ("obj")) {
				return this[int.Parse (s.Split (' ')[1])];
			}
			return PrimitiveIO<T>.ReadVectorFromString (s, new T[this.Dimension], this.Dimension);
		}
		
		/// <summary>
		/// Saves a subspace of the current space, in ascii format
		/// </summary>
		/// <param name="name">
		/// Output filename (of the header) <see cref="System.String"/>
		/// </param>
		/// <param name="samplesize">
		/// The size of the sampling <see cref="System.Int32"/>
		/// </param>
		/// <param name="random">
		/// If true a randomly choosed sampling will be performed <see cref="System.Boolean"/>
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
		
		/// <summary>
		/// Saves a subspace of the space
		/// </summary>
		public void SubSpace (string name, IList<int> sample)
		{
			string namevec = name + ".vec";
			StreamWriter w = new StreamWriter (name);
			//dim len pnorm ctype dommap dbnames dbvectors
			w.WriteLine ("{0} {1} {2} {3} f 0 None {4}", this.Dimension, sample.Count, this.pnorm, this.DoSqrt, Path.GetFileName (namevec));
			w.Close ();
			StreamWriter wvec = new StreamWriter (File.Create (namevec, 1 << 22));
			int dim = this.Dimension;
			int ishow = this.Count / 100 + 1;
			int docid = 0;
			foreach (int i in sample) {
				IList<T> V = this[i];
				for (int j = 0; j < dim; j++) {
					wvec.Write ("{0}", V[j]);
					if (j + 1 == dim) {
						wvec.WriteLine ("");
					} else {
						wvec.Write (" ");
					}
				}
				if (docid % ishow == 0) {
					Console.WriteLine ("Advance {0:0.00}%, docid: {1}", docid * 100.0 / this.Count, docid);
				}
				docid++;
			}
			wvec.Close ();
			
		}

		/// <summary>
		/// Distance wrapper for any P-norm
		/// </summary>
		public virtual double Dist (IList<T> a, IList<T> b)
		{
			this.numdist++;
			return this.RealDist (a, b);
		}
		
		/*
		/// <summary>
		/// Minkowski general distance
		/// </summary>
		public double DistLP(IList<T> a, IList<T> b) {
			double d = 0;
			for (int i = 0; i < a.Count; i++) {
				double m = Num.Sub(a[i],b[i]);
				d+=Math.Pow(Math.Abs(m), this.pnorm);
			}
			if (this.DoSqrt) {
				return Math.Pow(d, 1.0/this.pnorm);
			} else {
				return d;
			}
		}
		/// <summary>
		/// Specialization for L2
		/// </summary>
		public double DistL2 (IList<T> a, IList<T> b)
		{
			double d = 0;
			for (int i = 0; i < a.Count; i++) {
				double m = Num.Sub (a[i], b[i]);
				d += m * m;
			}
			if (this.DoSqrt) {
				return Math.Sqrt(d);
			} else {
				return d;
			}
		}
		
		/// <summary>
		/// Specialization for L1
		/// </summary>
		public double DistL1(IList<T> a, IList<T> b) {
			double d = 0;
			for (int i = 0; i < a.Count; i++) {
				d+=Math.Abs(Num.Sub(a[i],b[i]));
			} 
			return d;
		}
		
		/// <summary>
		/// Specialization for L-Infinity (P=-1)
		/// </summary>
		public double DistLInf(IList<T> a, IList<T> b) {
			double d = 0;
			for (int i = 0; i < a.Count; i++) {
				double m = Math.Abs(Num.Sub(a[i],b[i]));
				if (m > d) d = m;
			} 
			return d;
		}
		/// <summary>
		/// Angle between two vectors (computing the cosine between them)
		/// </summary>

		public double DistCos(IList<T> a, IList<T> b) {
			double sum,norm1,norm2;
			norm1=norm2=sum=0.0f;
			for(int i=0; i<a.Count; i++) {
		    	norm1+=Num.Prod(a[i], a[i]);
	 	   		norm2+=Num.Prod(b[i], b[i]);
	    		sum+=Num.Prod(a[i], b[i]);
			}
			double M = sum/(Math.Sqrt(norm1)*Math.Sqrt(norm2));
			M=Math.Max(-1.0f,Math.Min(1.0f,M));
			//M=min(1.0,M);
			//cerr << "COS::::" << M << endl;
			return Math.Acos(M);
		}*/
	}
}
