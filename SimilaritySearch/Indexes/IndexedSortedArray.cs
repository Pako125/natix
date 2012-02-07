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
//   Original filename: natix/natix/SimilaritySearch/Indexes/IndexedSortedArray.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using natix.SortingSearching;

namespace natix.SimilaritySearch
{	
	/// <summary>
	/// Augmentable interface
	/// </summary>
	public interface IAugmentable<TKey, TValue>
	{
		/// <summary>
		/// Do an augmentation
		/// </summary>
		/// <param name="k">
		/// </param>
		/// <param name="v">
		/// </param>
		/// <param name="arg">
		/// A <see cref="System.Object"/>
		/// </param>
		void Augment(TKey k, TValue v, object arg);
	}

	/// <summary>
	/// Augmentable indexed sorted array
	/// </summary>
	public class AugmentableIndexedSortedArray<TKey, TValue>: List< KeyValuePair<TKey, TValue> >
		where TValue : IAugmentable<TKey, TValue>
	{
		/// <summary>
		/// Comparison function
		/// </summary>
		public IComparer<TKey> Comp;
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cmp">
		/// A <see cref="IComparer"/>
		/// </param>
		public AugmentableIndexedSortedArray (IComparer<TKey> cmp) : base()
		{
			this.Comp = cmp;
		}
	
		/// <summary>
		/// Add an item or augment it if already exists
		/// </summary>
		/// <param name="key">
		/// </param>
		/// <param name="val">
		/// </param>
		/// <param name="arg">
		/// </param>
		/// <returns>
		/// Index of the accessed key
		/// </returns>
		public int AddOrAugment (TKey key, TValue val, object arg)
		{
			int upperBound = this.Count;
			if (upperBound == 0) {
				this.Add (new KeyValuePair<TKey, TValue> (key, val));
				//this.Sort ();
				return 0;
			}
			// starting from the end
						/*
			int lowerBound = upperBound - 1;

			for (int i = 2; lowerBound > 0; i++) {
				int lower = lowerBound - (1 << i);
				if (lower < 0) {
					lower = 0;
				}
				int c = this.Comp.Compare(this[lower].Key, key);
				if (c == 0) {
					this[lower].Value.Augment (key, val);
					return;
				}
				lowerBound = lower;
				if (c < 0) {
					break;
				}
			}
			*/
			int lowerBound = 0;
			int mid;
			do {
				mid = (lowerBound >> 1) + (upperBound >> 1);
				int c = this.Comp.Compare (this[mid].Key, key);
				if (c == 0) {
					this[mid].Value.Augment (key, val, arg);
					return mid;
				}
				if (c < 0) {
					lowerBound = mid + 1;
				} else {
					upperBound = mid;
				}
			} while (lowerBound < upperBound);
			this.Insert (lowerBound, new KeyValuePair<TKey, TValue>(key, val));
			return lowerBound;
		}
		/// <summary>
		/// String representation
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public override string ToString ()
		{
			StringWriter s = new StringWriter ();
			for (int i = 0; i < this.Count; i++) {
				s.Write ("({0},{1}), ", this[i].Key, this[i].Value);
			}
			s.WriteLine ("<TheEnd>");
			return s.ToString ();
		}
	}
		
	/// <summary>
	/// Indexed sorted array
	/// </summary>
	public class IndexedSortedArray<TKey, TValue>
	{
		/// <summary>
		/// Keys
		/// </summary>
		public List< TKey > Keys;
		/// <summary>
		/// Values
		/// </summary>
		public List< TValue > Values;
		/// <summary>
		/// Comparer
		/// </summary>
		public IComparer<TKey> Comp;
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cmp">
		/// Comparison function
		/// </param>
		public IndexedSortedArray (Comparison<TKey> cmp) : base()
		{
			this.Comp = new ComparerFromComparison<TKey> (cmp);
			this.Keys = new List<TKey> ();
			this.Values = new List<TValue> ();
		}
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cmp">
		/// Comparer
		/// </param>
		public IndexedSortedArray (IComparer<TKey> cmp) : base()
		{
			this.Comp = cmp;
			this.Keys = new List<TKey> ();
			this.Values = new List<TValue> ();
		}

		/// <summary>
		/// Returns the index of key, -1 if it's not present
		/// </summary>
		/// <param name="key">
		/// </param>
		/// <returns>
		/// Key index
		/// </returns>
		public int IndexOf (TKey key)
		{
			if (this.Count > 0) {
				int index = this.Bisect (key);
				//if (index == this.Count || index < 0) {
				if (index == this.Count) {
					// not found
					return -1;
				}
				if (this.Comp.Compare (key, this.Keys[index]) == 0) {
					return index;
				}
			}
			return -1;
		}

		/// <summary>
		/// Get the closer index to key
		/// </summary>
		/// <param name="key">
		/// </param>
		/// <returns>
		/// Valid index
		/// </returns>
		public int CloserIndexOf (TKey key)
		{
			int bounds = this.Bisect (key);
			if (bounds == this.Count) {
				bounds = this.Count - 1;
			}
			return bounds;
		}
		
		/// <summary>
		/// Get a range for a range of keys
		/// </summary>
		/// <param name="startRange">
		/// </param>
		/// <param name="endRange">
		/// </param>
		/// <param name="lowerBound">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="upperBound">
		/// A <see cref="System.Int32"/>
		/// </param>
		public void GetIndexRange (TKey startRange, TKey endRange, out int lowerBound, out int upperBound)
		{
			this.GetIndexRange (startRange, endRange, this.Keys.Count, out lowerBound, out upperBound);
		}

		/// <summary>
		/// Gets the bounds for a range of keys
		/// </summary>
		/// <param name="startRange">
		/// </param>
		/// <param name="endRange">
		/// </param>
		/// <param name="maxrange">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="lowerBound">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="upperBound">
		/// A <see cref="System.Int32"/>
		/// </param>
		public void GetIndexRange (TKey startRange, TKey endRange, int maxrange, out int lowerBound, out int upperBound)
		{
			lowerBound = this.Bisect (startRange);
			upperBound = this.Bisect (lowerBound, maxrange, endRange);
		}

		/// <summary>
		/// Count the number of items
		/// </summary>
		public int Count {
			get { return this.Keys.Count; }
		}
		
		/// <summary>
		/// Insert a key into the structure
		/// </summary>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="key">
		/// </param>
		/// <param name="val">
		/// </param>
		public void InsertAtIndex (int index, TKey key, TValue val)
		{
			this.Keys.Insert (index, key);
			this.Values.Insert (index, val);
		}
		
		/// <summary>
		/// Performs bisection into the array searching by key
		/// </summary>
		/// <param name="key">
		/// </param>
		/// <returns>
		/// Index position
		/// </returns>
		public int Bisect (TKey key)
		{
			return this.Bisect (0, this.Keys.Count, key);
		}

		/// <summary>
		/// Bisection with given bounds for key
		/// </summary>
		/// <param name="lowerBound">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="countItems">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="key">
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public int Bisect (int lowerBound, int countItems, TKey key)
		{
			int mid;
			int upperBound = lowerBound + countItems - 1;
			//Console.WriteLine ("==== lowerBound: {0}, upperBound: {1}", lowerBound, upperBound);
			do {
				mid = (lowerBound >> 1) + (upperBound >> 1);
				int c = this.Comp.Compare (this.Keys[mid], key);
				if (c == 0) {
					return mid;
				}
				if (c < 0) {
					lowerBound = mid + 1;
				} else {
					upperBound = mid;
				}
			} while (lowerBound < upperBound);
			return lowerBound;
		}

		/// <summary>
		///  Add a pair into the sorted array
		/// </summary>
		/// <param name="key">
		/// </param>
		/// <param name="val">
		/// </param>
		public void Add (TKey key, TValue val)
		{
			int index = 0;
			if (this.Count > 0) {
				index = this.Bisect (key);
			}
			//Console.WriteLine ("Key: {0}, Index: {1}, Count: {2}", key, index, this.Count);
			this.InsertAtIndex (index, key, val);
			//Console.WriteLine (this);
		}
		/// <summary>
		///  A string representation of the instance
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public override string ToString ()
		{
			StringWriter s = new StringWriter ();
			for (int i = 0; i < this.Keys.Count; i++) {
				s.Write ("({0},{1}), ", this.Keys[i], this.Values[i]);
			}
			s.WriteLine ("<TheEnd>");
			return s.ToString ();
		}
	}
}
