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
//   Original filename: natix/SimilaritySearch/Containers/ListContainerWithSize.cs
// 
using System;
using System.Collections;
using System.Collections.Generic;

namespace natix.SimilaritySearch
{
	// TODO: Container for IList in secondary memory
	// TODO: Use framework 4 for mmap like function
	// TODO: Replace most of the array based code to IList
	/// <summary>
	/// Simulate pointers in an array
	/// </summary>
	public class ListContainerWithSize<T> : ListContainerBase<T>
	{
		/// <summary>
		/// The container.
		/// </summary>
		public IList<T> Container;
		/// <summary>
		/// The starting position.
		/// </summary>
		public IList<int> StartingPosition;
		
		/// <summary>
		/// Initializes a new instance 
		/// </summary>
		public ListContainerWithSize ()
		{
			this.Container = new List<T> ();
			this.StartingPosition = new List<int> ();
		}
		
		/// <summary>
		/// Initializes a new instance 
		/// </summary>
		public ListContainerWithSize (int expectedSize)
		{
			this.Container = new List<T> (expectedSize);
			this.StartingPosition = new List<int> (expectedSize);
		}
		
		/// <summary>
		/// Initializes a new instance 
		/// </summary>
		public ListContainerWithSize (IList<T> Container, IList<int> StartingPosition)
		{
			this.Container = Container;
			this.StartingPosition = StartingPosition;
		}
		
		/// <summary>
		/// Gets the count.
		/// </summary>
		public override int Count {
			get {
				return this.StartingPosition.Count;
			}
		}
		
		/// <summary>
		/// Alloc space for Len items inside the list. Return the new index
		/// </summary>
		public override void Add (IList<T> InitObject)
		{
			int C = this.StartingPosition.Count;
			int K = InitObject.Count;
			if (C == 0) {
				this.StartingPosition.Add (K);
			} else {
				this.StartingPosition.Add (this.StartingPosition [C - 1] + K);
			}

			for (int i = 0; i < K; i++) {
				this.Container.Add (InitObject [i]);
			}
		}
		
		/// <summary>
		/// Gets IndexPos-th list (with a given length)
		/// </summary>
		public override IList<T> GetList (int IndexPos, int Length)
		{
			int S = 0;
			if (IndexPos > 0) {
				S = this.StartingPosition [IndexPos - 1];
			}
			// return new InternalList<T> (S, Length, this.Container);
			return new ListShiftIndex<T> (this.Container, S, Length);
		}
		
		/// <summary>
		/// Gets length of the i-th list
		/// </summary>
		public override int GetLengthAtIndex (int i)
		{
			if (i == 0) {
				return this.StartingPosition [0];
			} else {
				return this.StartingPosition [i] - this.StartingPosition [i - 1];
			}
		}
		
		/// <summary>
		/// Gets the i-th list.
		/// </summary>
		public override IList<T> GetList (int i)
		{
			int S;
			int E;
			//Console.WriteLine ("==GetList IndexPos: {0}, Offsets: {1}", IndexPos, this.StartingPosition.Count);
			if (i == 0) {
				S = 0;
				E = this.StartingPosition [i];
			} else {
				S = this.StartingPosition [i - 1];
				E = this.StartingPosition [i];
			}
			// return new InternalList<T> (S, E - S, this.Container);
			return new ListShiftIndex<T> (this.Container, S, E - S);
		}
		
		/// <summary>
		/// Gets the i-the list
		/// </summary>
		public IList<T> this[int i]
		{
			get {
				return this.GetList (i);
			}
		}
	}
}

