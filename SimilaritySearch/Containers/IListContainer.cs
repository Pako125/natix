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
//   Original filename: natix/SimilaritySearch/Containers/IListContainer.cs
// 
using System;
using System.Collections;
using System.Collections.Generic;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Represents a list of lists inside a container (simulate a large array and pointers)
	/// </summary>
	public interface IListContainer<T> : IList< IList<T> >
	{
		/// <summary>
		/// Get an IList object, simulating a pointer inside the container.
		/// The StartIndex meaning is implementation dependent
		/// </summary>
		IList<T> GetList (int StartIndex, int Length);
		/// <summary>
		/// Get an IList object, simulating a pointer inside the container. The StartIndex and the length are
		/// implementation dependent
		/// </summary>
		IList<T> GetList (int StartIndex);
		/// <summary>
		///  Returns the number of items associated to the given index
		/// </summary>
		int GetLengthAtIndex(int StartIndex);
//		/// <summary>
//		/// GetList alias, with Length dependent in the implementation
//		/// </summary>
//		IList<T> this[int StartIndex] {
//			get;
//		}
//		/// <summary>
//		/// Return the number of naturally referenciable items inside the container
//		/// </summary>
//		int Count {
//		get;
//		}
//		/// <summary>
//		/// Allocate space for an vector in list-container. Returns the new index to be used with GetList or this[int]
//		/// </summary>
//		// int Alloc (int Length, T initObj);
	}
}