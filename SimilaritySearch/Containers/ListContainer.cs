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
//   Original filename: natix/SimilaritySearch/Containers/ListContainer.cs
// 
using System;
using System.Collections;
using System.Collections.Generic;

namespace natix.SimilaritySearch
{
	
	/// <summary>
	/// Represents a list of lists inside a container (simulate a large array and pointers)
	/// </summary>
	public class ListContainer<T> : ListContainerBase<T>
	{
		/// <summary>
		/// The container.
		/// </summary>
		public IList<T> Container;
		
		/// <summary>
		/// Initializes a new instance
		/// </summary>
		public ListContainer (IList<T> C)
		{
			this.Container = C;
		}
		
		/// <summary>
		/// Add the list "L" to the container
		/// </summary>
		public override void Add (IList<T> L)
		{
			for (int i = 0; i < L.Count; i++) {
				this.Container.Add (L [i]);
			}
		}
		
		/// <summary>
		/// Get a list of length "len" starting at position i
		/// </summary>
		public override IList<T> GetList (int i, int Len)
		{
			return new ListShiftIndex<T> (this.Container, i, Len);
		}
		
		/// <summary>
		/// Gets the length of the i-th list
		/// </summary>
		public override int GetLengthAtIndex (int StartIndex)
		{
			return this.Count - StartIndex;
		}
		
		/// <summary>
		/// Returns the number of lists
		/// </summary>
		public override int Count {
			get {
				return this.Container.Count;
			}
		}
		
		/// <summary>
		/// Gets the i-th list
		/// </summary>
		public IList<T> this[int i] {
			get {
				return this.GetList (i);
			}
		}
	}
	
}

