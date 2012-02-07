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
//   Original filename: natix/natix/SimilaritySearch/Spaces/PermSpace.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using natix.CompactDS;
using natix.SortingSearching;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// A Generic permuted space
	/// </summary>
	public class PermSpace<T> : Space<T>
	{
		Space<T> space;
		IPermutation perm;
		string name;
		
		public PermSpace ()
		{
		}
		
		public int Count {
			get {
				return this.space.Count;
			}
		}
		
		public IResult CreateResult (int K, bool ceiling)
		{
			return this.space.CreateResult (K, ceiling);
		}
		
		public Type GenericType {
			get {
				return typeof(T);
			}
		}
		
		public T Parse (string s, bool isquery)
		{
			return this.space.Parse (s, isquery);
		}
		
		public void SubSpace (string name, int samplesize, bool random)
		{
			throw new NotSupportedException ();
		}
		
		public void SubSpace (string name, System.Collections.Generic.IList<int> permutation)
		{
			throw new NotSupportedException ();
		}
		
		public double Dist (T a, T b)
		{
			return this.space.Dist (a, b);
		}
		
		public string SpaceType {
			get {
				return this.GetType ().FullName;
			}
			set { }
		}
		
		public int NumberDistances {
			get {
				return this.space.NumberDistances;
			}
		}
		
		public string Name {
			get {
				return this.name;
			}
			set {
				this.name = value;
				string spaceclass;
				string spacename;
				using (var Input = new BinaryReader(File.OpenRead(value))) {
					this.perm = PermutationGenericIO.Load (Input);
					spaceclass = Input.ReadString ();
					spacename = Input.ReadString ();
				}
				this.space = (Space<T>)SpaceCache.Load (spaceclass, spacename);
			}
		}
		
		public T this [int docid] {
			get {
				var p = this.perm [docid];
				return this.space [p];
			}
		}
		
		public IPermutation Perm {
			get {
				return this.perm;
			}
		}
		
		public static void Save (string outname, IPermutation P, string spaceclass, string spacename)
		{
			using (var Output = new BinaryWriter(File.Create(outname))) {
				PermutationGenericIO.Save (Output, P);
				Output.Write (spaceclass);
				Output.Write (spacename);
			}
		}
	}
}
