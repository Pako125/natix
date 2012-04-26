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
//   Original filename: natix/SimilaritySearch/Indexes/BaseIndex.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
// using System.Linq;


namespace natix.SimilaritySearch
{
	/// <summary>
	/// The basic methods for an Index
	/// </summary>
	public abstract class BaseIndex<T> : Index<T>
	{
		/// <summary>
		/// Space class
		/// </summary>
		public string spaceClass;
		/// <summary>
		/// database name
		/// </summary>
		public string spaceName;
		/// <summary>
		/// space
		/// </summary>
		Space<T> space;
		
		/// <summary>
		/// Constructor
		/// </summary>
		public BaseIndex ()
		{
		}
		
		/// <summary>
		/// Returns the main space
		/// </summary>
		public virtual Space<T> MainSpace {
			get {
				return this.space;
			}
		}
		
		/// <summary>
		/// Gets the main space (not generic).
		/// </summary>
		public virtual Space MainSpaceWithoutType {
			get {
				return (Space)this.space;
			}
		}
		
		/// <summary>
		/// Sets the main space.
		/// </summary>
		public void SetMainSpace (Space<T> sp)
		{
			this.space = sp;
		}
		/// <summary>
		/// Search by range
		/// </summary>
		public abstract IResult Search (T q, double radius);
		
		/// <summary>
		/// Search by KNN
		/// </summary>
		public virtual IResult KNNSearch (T q, int K)
		{
			return this.KNNSearch (q, K, this.MainSpace.CreateResult (Math.Abs (K), false));
		}
		
		/// <summary>
		/// Perform a KNN search.
		/// </summary>
		public abstract IResult KNNSearch(T q, int K, IResult res);
		/// <summary>
		/// Finalize the laod of an index
		/// </summary>
		public virtual void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			// do nothing
		}
		
		/// <summary>
		/// Configure a loaded index
		/// </summary>
		public virtual void Configure (IEnumerable<string> args)
		{
			// do nothing
		}

		/// <summary>
		/// Build the index
		/// </summary>
		public virtual void Build (IEnumerable<string> args)
		{
			
		}

		/// <summary>
		/// The current search cost object for the index
		/// </summary>
		public virtual SearchCost Cost {
			get {
				var numdists = this.MainSpace.NumberDistances;
				return new SearchCost (numdists, 0);
			}
		}
		
		/// <summary>
		/// A generic IndexType implementation
		/// </summary>
		public virtual string IndexType {
			get {
				return this.GetType ().ToString ();
			}
			set {}
		}
		
		/// <summary>
		/// Search by range parsing an string representing the object
		/// </summary>
		public IResult ParseSearch (string query, double radius)
		{
			return this.Search (this.MainSpace.Parse (query, true), radius);
		}
		
		/// <summary>
		/// Search for KNN parsing an string representing the object
		/// </summary>
		public IResult ParseKNNSearch (string query, int K)
		{
			return this.KNNSearch (this.MainSpace.Parse (query, true), K);
		}

		/// <summary>
		/// Filter a result by radius
		/// </summary>
		public IResult FilterByRadius (IResult C, double radius)
		{
			var R = new Result (C.Count);
			foreach (var c in C) {
				if (c.dist <= radius) {
					R.Push (c.docid, c.dist);
				} else {
					break;
				}
			}
			return R;
		}
		
		/// <summary>
		/// Filter KNN candidates with real distances
		/// </summary>
		public IResult FilterKNNByRealDistances (T q, int K, bool ceiling, IResult C, int maxcand)
		{
			var R = new Result (K, ceiling);
			int i = 0;
			foreach (var c in C) {
				if (i < maxcand) {
					R.Push (c.docid, this.space.Dist (q, this.space[c.docid]));
				} else {
					break;
				}
				i++;
			}
			return R;
		}
		
		/// <summary>
		/// Filter KNN candidates with real distances
		/// </summary>
		public IResult FilterRadiusByRealDistances (T q, double radius, IResult C, int maxcand)
		{
			var R = new Result (C.Count);
			double d;
			int i = 0;
			foreach (var c in C) {
				if (i < maxcand) {
					d = this.space.Dist (q, this.space[c.docid]);
					if (d <= radius) {
						R.Push (c.docid, d);
					}
				} else {
					break;
				}
				i++;
			}
			return R;
		}

	}
}
