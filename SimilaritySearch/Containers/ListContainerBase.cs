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
//   Original filename: natix/natix/SimilaritySearch/Containers/ListContainerBase.cs
// 
using System;
using System.Collections;
using System.Collections.Generic;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// InternalList is simulation of a pointer array implemented with an struct.
	/// </summary>
	public abstract class ListContainerBase<T> : IListContainer<T>
	{
		// public abstract int Alloc(int Len, T initObj);
		/// <summary>
		/// Gets the length of the StartIndex-th list
		/// </summary>
		public abstract int GetLengthAtIndex(int StartIndex);
		/// <summary>
		/// Returns the number of lists
		/// </summary>
		public abstract int Count {
			get;
		}
		/// <summary>
		/// Gets the list starting at position i, with length "Len"
		/// </summary>
		public abstract IList<T> GetList (int i, int Len);
		/// <summary>
		/// Gets the i-th list.
		/// </summary>
		public virtual IList<T> GetList (int i)
		{
			return this.GetList (i, this.GetLengthAtIndex (i));
		}

		/*
		public IList<T> this[int StartIndex] {
			get { return this.GetList (StartIndex); }
			set {
				var C = this;
				var D = C[StartIndex];
				for (int i = 0; i < D.Count; i++) {
					D[i] = value[i];
				}
			}
		}*/
		
		IList<T> IList<IList<T>>.this [int StartIndex] {
			get {
				//Console.WriteLine ("ListContainerBase.this[{0}]", StartIndex);
				return ((IListContainer<T>)this).GetList (StartIndex);
			}
			set {
				var C = (IListContainer<T>)this;
				var D = C [StartIndex];
				for (int i = 0; i < D.Count; i++) {
					D [i] = value [i];
				}
			}
		}
		
		/// <summary>
		/// Returns the first position equal to item
		/// </summary>
		public virtual int IndexOf (IList<T> item)
		{
			int hitem = item.GetHashCode ();
			int i = 0;
			foreach (var e in this) {
				if (hitem == e.GetHashCode ()) {
					return i;
				}
				i++;
			}
			return -1;
		}
		/// <summary>
		/// Tests if the container contains the specified a.
		/// </summary>
		public bool Contains (IList<T> a)
		{
			return this.IndexOf (a) >= 0;
		}
		
		/// <summary>
		/// Insert the list "item" at position "a"
		/// </summary>
		public virtual void Insert (int a, IList<T> item)
		{
			throw new NotSupportedException ();
		}
		
		/// <param name='item'>
		/// Add the list "item" to the end of the container
		/// </param>

		public abstract void Add (IList<T> item);
		
		/// <summary>
		/// Clear this instance.
		/// </summary>
		public virtual void Clear ()
		{
			throw new NotSupportedException ();
		}
		
		/// <summary>
		/// Remove the specified a
		/// </summary>
		public virtual void Remove (object a)
		{
			throw new NotSupportedException ();
		}
		
		/// <summary>
		/// Remove the specified a.
		/// </summary>
		public virtual bool Remove (IList<T> a)
		{
			throw new NotSupportedException ();
		}
		
		/// <summary>
		/// Removes at a.
		/// </summary>
		public virtual void RemoveAt (int a)
		{
			throw new NotSupportedException ();
		}

		IEnumerator<IList<T>> IEnumerable<IList<T>>.GetEnumerator ()
		{
			var X = (IList<IList<T>>)this;
			for (int i = 0, L = X.Count; i < L; i++) {
				yield return X[i];
			}
		}
		
		/// <summary>
		/// Gets the enumerator.
		/// </summary>
		public virtual IEnumerator GetEnumerator ()
		{
			foreach (var e in ((IEnumerable<IList<T>>)this)) {
				yield return e;
			}
		}
		
		/// <summary>
		/// Copies to.
		/// </summary>
		public void CopyTo (IList<T>[] A, int arrayIndex)
		{
			foreach (var item in ((IEnumerable<IList<T>>)this)) {
				A[arrayIndex] = item;
				arrayIndex++;
			}
		}
		
		/*public int Count {
			get {
				return this.Length;
			}
		}*/
		
		/// <summary>
		/// Gets a value indicating whether this instance is read only.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
		/// </value>
		public bool IsReadOnly {
			get {
				return false;
			}
		}
		
		/*
		public T this[int a]
		{
			get {
				return this.ParentList[a + this.StartPos];
			}
			set {
				this.ParentList[a + this.StartPos] = value;
			}
		}*/
	}
}
