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
//   Original filename: natix/SimilaritySearch/Containers/InternalList.cs
// 
//using System;
//using System.Collections;
//using System.Collections.Generic;
//
//namespace natix.SimilaritySearch
//{
//	/// <summary>
//	/// InternalList is simulation of a pointer array implemented with a struct.
//	/// </summary>
//	public struct InternalList<T> : IList<T>
//	{
//		int StartPos;
//		int Length;
//		IList<T> ParentList;
//		
//		/// <summary>
//		/// Advances the start position.
//		/// </summary>
//		public void AdvanceStartPosition (int pos)
//		{
//			this.StartPos += pos;
//			this.Length -= pos;
//		}
//		
//		/// <summary>
//		/// Initializes a new instance
//		/// </summary>
//		public InternalList (int StartPos, int Length, IList<T> ParentList)
//		{
//			this.StartPos = StartPos;
//			this.ParentList = ParentList;
//			this.Length = Length;
//		}
//		
//		/// <summary>
//		/// Locates the first occurrence "item"
//		/// </summary>
//		public int IndexOf (T item)
//		{
//			int pos = -1;
//			int hitem = item.GetHashCode ();
//			for (int i = StartPos, EndPos = StartPos + this.Length; i < EndPos; i++) {
//				if (hitem == this.ParentList [i].GetHashCode ()) {
//					return i - StartPos;
//				}
//			}
//			return pos;
//		}
//		
//		/// <summary>
//		/// Returns true if this list contains "a"
//		/// </summary>
//		public bool Contains (T a)
//		{
//			return this.IndexOf (a) >= 0;
//		}
//		
//		/// <summary>
//		/// Insert "item" into the list at position "a"
//		/// </summary>
//		public void Insert (int a, T item)
//		{
//			throw new NotSupportedException ();
//		}
//		
//		/// <summary>
//		/// Add the specified item.
//		/// </summary>
//		public void Add (T item)
//		{
//			throw new NotSupportedException ();
//		}
//		
//		/// <summary>
//		/// Clear this instance.
//		/// </summary>
//		public void Clear ()
//		{
//			throw new NotSupportedException ();
//		}
//		
//		/// <summary>
//		/// Remove the first occurrence of "a"
//		/// </summary>
//		public void Remove (object a)
//		{
//			throw new NotSupportedException ();
//		}
//		
//		/// <summary>
//		/// Remove the first occurrence of "a"
//		/// </summary>
//		public bool Remove (T a)
//		{
//			throw new NotSupportedException ();
//		}
//		
//		/// <summary>
//		/// Removes the item at index "a"
//		/// </summary>
//		public void RemoveAt (int a)
//		{
//			throw new NotSupportedException ();
//		}
//
//		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
//		{
//			int End = this.StartPos + this.Length;
//			for (int i = this.StartPos; i < End; i++) {
//				yield return this.ParentList[i];
//			}
//		}
//		
//		/// <summary>
//		/// Gets the enumerator.
//		/// </summary>
//		public IEnumerator GetEnumerator ()
//		{
//			int End = this.StartPos + this.Length;
//			for (int i = this.StartPos; i < End; i++) {
//				yield return this.ParentList[i];
//			}
//		}
//		
//		/// <summary>
//		/// Copies items to the given array "A"
//		/// </summary>
//		public void CopyTo (T[] A, int arrayIndex)
//		{
//			foreach (var item in ((IEnumerable<T>)this)) {
//				A [arrayIndex] = item;
//				arrayIndex++;
//			}
//		}
//		
//		/// <summary>
//		/// Gets the number of items in the list
//		/// </summary>
//		public int Count {
//			get {
//				return this.Length;
//			}
//		}
//		
//		/// <summary>
//		/// Predicate 
//		/// </summary>
//
//		public bool IsReadOnly {
//			get {
//				return false;
//			}
//		}
//		
//		/// <summary>
//		/// Gets or sets the a-th item of the list
//		/// </summary>
//		public T this[int a]
//		{
//			get {
//				return this.ParentList[a + this.StartPos];
//			}
//			set {
//				this.ParentList[a + this.StartPos] = value;
//			}
//		}
//	}
//	
//	/// <summary>
//	/// InternalList is simulation of a pointer array implemented with a class
//	/// </summary>
//	public class InternalListClass<T> : IList<T>
//	{
//		int StartPos;
//		int Length;
//		IList<T> ParentList;
//		
//		/// <summary>
//		/// Advances the start position.
//		/// </summary>
//		public void AdvanceStartPosition (int pos)
//		{
//			this.StartPos += pos;
//			this.Length -= pos;
//		}
//		
//		/// <summary>
//		/// Initializes a new instance.
//		/// </summary>
//		public InternalListClass (int StartPos, int Length, IList<T> ParentList)
//		{
//			this.StartPos = StartPos;
//			this.ParentList = ParentList;
//			this.Length = Length;
//		}
//		
//		/// <summary>
//		/// Indexs the of first occurrence of "item"
//		/// </summary>
//		public int IndexOf (T item)
//		{
//			int pos = -1;
//			int hitem = item.GetHashCode ();
//			for (int i = StartPos, EndPos = StartPos + this.Length; i < EndPos; i++) {
//				if (hitem == this.ParentList [i].GetHashCode ()) {
//					return i - StartPos;
//				}
//			}
//			return pos;
//		}
//		
//		/// <summary>
//		/// True if the list contains the specified a, false otherwise
//		/// </summary>
//		public bool Contains (T a)
//		{
//			return this.IndexOf (a) >= 0;
//		}
//		
//		/// <summary>
//		/// Inserts "item" into the list at position "a".
//		/// </summary>
//		public void Insert (int a, T item)
//		{
//			throw new NotSupportedException ();
//		}
//		
//		/// <summary>
//		/// Add the specified item.
//		/// </summary>
//		public void Add (T item)
//		{
//			throw new NotSupportedException ();
//		}
//		
//		/// <summary>
//		/// Clear this instance.
//		/// </summary>
//		public void Clear ()
//		{
//			throw new NotSupportedException ();
//		}
//		
//		/// <summary>
//		/// Remove the specified a.
//		/// </summary>
//		public void Remove (object a)
//		{
//			throw new NotSupportedException ();
//		}
//		
//		/// <summary>
//		/// Remove the specified a.
//		/// </summary>
//		public bool Remove (T a)
//		{
//			throw new NotSupportedException ();
//		}
//		
//		/// <summary>
//		/// Removes the item at position "a".
//		/// </summary>
//		public void RemoveAt (int a)
//		{
//			throw new NotSupportedException ();
//		}
//
//		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
//		{
//			int End = this.StartPos + this.Length;
//			for (int i = this.StartPos; i < End; i++) {
//				yield return this.ParentList[i];
//			}
//		}
//		
//		/// <summary>
//		/// Gets the enumerator.
//		/// </summary>
//		/// <returns>
//		/// The enumerator.
//		/// </returns>
//		public IEnumerator GetEnumerator ()
//		{
//			int End = this.StartPos + this.Length;
//			for (int i = this.StartPos; i < End; i++) {
//				yield return this.ParentList[i];
//			}
//		}
//		
//		/// <summary>
//		/// Copies to.
//		/// </summary>
//
//		public void CopyTo (T[] A, int arrayIndex)
//		{
//			foreach (var item in ((IEnumerable<T>)this)) {
//				A [arrayIndex] = item;
//				arrayIndex++;
//			}
//		}
//
//		/// <summary>
//		/// Gets the count.
//		/// </summary>
//
//		public int Count {
//			get { return this.Length; }
//		}
//
//		/// <summary>
//		/// Gets a value indicating whether this instance is read only.
//		/// </summary>
//		/// <value>
//		/// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
//		/// </value>
//		public bool IsReadOnly {
//			get { return false; }
//		}
//		
//		/// <summary>
//		/// Gets or sets the a-th item
//		/// </summary>
//		public virtual T this[int a] {
//			get { return this.ParentList[a + this.StartPos]; }
//			set { this.ParentList[a + this.StartPos] = value; }
//		}
//	}
//}
