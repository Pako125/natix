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
//   Original filename: natix/SimilaritySearch/Containers/ListContainerFixedSize.cs
// 
using System;
using System.Collections;
using System.Collections.Generic;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// List container fixed size.
	/// </summary>
	public class ListContainerFixedSize<T> : ListContainerBase<T>
	{
		/// <summary>
		/// The container.
		/// </summary>
		public IList<T> Container;
		/// <summary>
		/// The length
		/// </summary>
		public readonly int FixedLength;
		
		/// <summary>
		/// Gets the length of the list at the specified index
		/// </summary>

		public override int GetLengthAtIndex (int StartIndex)
		{
			return this.FixedLength;
		}
		
		/// <summary>
		/// Initializes a new instance
		/// </summary>
		public ListContainerFixedSize (int FixedLength)
		{
			this.Container = new List<T> ();
			this.FixedLength = FixedLength;
		}
		
		/// <summary>
		/// Initializes a new instance
		/// </summary>
		public ListContainerFixedSize (IList<T> Container, int FixedLength)
		{
			this.Container = Container;
			this.FixedLength = FixedLength;
		}
		
		/// <summary>
		/// Add the items of S to the container
		/// </summary>

		public override void Add (IList<T> S)
		{
			for (int i = 0; i < this.FixedLength; i++) {
				this.Container.Add (S [i]);
			}
		}
		
		/// <summary>
		/// Gets the number of lists
		/// </summary>
		public override int Count {
			get {
				return this.Container.Count / this.FixedLength;
			}
		}
		
		/// <summary>
		/// Gets the IndexPos-th list (specifies a length)
		/// </summary>
		public override IList<T> GetList (int IndexPos, int Len)
		{
			int S = IndexPos * this.FixedLength;
			return new ListShiftIndex<T> (this.Container, S, Len);
		}
				
		/// <summary>
		/// Get the IndexPos-th list (using the default size)
		/// </summary>
		public IList<T> this[int IndexPos]
		{
			get {
				return this.GetList (IndexPos);
			}
		}
	}

}

