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
//   Original filename: natix/SimilaritySearch/Containers/ListContainerFactory.cs
// 
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace natix.SimilaritySearch
{
	
	/// <summary>
	/// Represents a list of lists inside a container (simulate a large array and pointers)
	/// </summary>
	public class ListContainerFactory<T>
	{
		static Func<int,IListContainer<T>> FixedSizeFactory =
			(int Len) => new ListContainerFixedSize<T> (Len);
		
		static Func<IList<T>, int, IListContainer<T>> FixedSizeFactoryWithInitialValues =
			(IList<T> container, int len) => new ListContainerFixedSize<T>(container, len);

		static Func<IListContainer<T>> VariableSizeFactory =
			() => new ListContainerWithSize<T>();
		
		static Func<int,IListContainer<T>> VariableSizeFactoryWithExpectedContainerSize =
			(int size) => new ListContainerWithSize<T> (size);
		
		static Func<IList<T>,IList<int>,IListContainer<T>> VariableSizeFactoryWithInitialValues =
			(IList<T> container, IList<int> startingpos) => new ListContainerWithSize<T> (container, startingpos);
		
		// public static Func<string, IList<T>> VariableSizeFactoryFromFile = (string filename) => ReadFromFile(filename);
		
		public static IListContainer<T> GetFixedSizeListContainer (int fixedSize)
		{
			return FixedSizeFactory (fixedSize);
		}
		
		public static IListContainer<T> GetFixedSizeListContainer (IList<T> container, int fixedSize)
		{
			return FixedSizeFactoryWithInitialValues (container, fixedSize);
		}

		public static IListContainer<T> GetVariableSizeListContainer ()
		{
			return VariableSizeFactory ();
		}

		public static IListContainer<T> GetVariableSizeListContainer (int size)
		{
			return VariableSizeFactoryWithExpectedContainerSize (size);
		}

		public static IListContainer<T> GetVariableSizeListContainer (IList<T> container, IList<int> offsets)
		{
			return VariableSizeFactoryWithInitialValues (container, offsets);
		}
	}
}

