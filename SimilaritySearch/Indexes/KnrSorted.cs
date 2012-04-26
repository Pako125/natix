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
//   Original filename: natix/SimilaritySearch/Indexes/KnrSorted.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NDesk.Options;
using natix.SortingSearching;

namespace natix.SimilaritySearch
{
	
	/// <summary>
	/// Knr sequential sorted ppindex
	/// </summary>
	public class KnrSortedPermPrefix<T> : KnrPermPrefix<T>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public KnrSortedPermPrefix() : base()
		{
		}
		/// <summary>
		/// Wrap to sorted ppindex
		/// </summary>
		public override IList<UInt16> KnrWrap (IList<UInt16> a)
		{
			Sorting.Sort<UInt16> (a);
			return a;
		}
	}

	/// <summary>
	/// Knr sequential sorted KnrLevenshtein
	/// </summary>
	public class KnrSortedLevenshtein<T> : KnrPermPrefix<T>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public KnrSortedLevenshtein() : base()
		{
		}
		/// <summary>
		/// Wrap to sorted ppindex
		/// </summary>
		/// <param name="a">
		/// A <see cref="UInt16[]"/>
		/// </param>
		/// <returns>
		/// A <see cref="UInt16[]"/>
		/// </returns>
		public override IList<UInt16> KnrWrap (IList<UInt16> a)
		{
			Sorting.Sort<UInt16> (a);
			return a;
		}
		/// <summary>
		/// A levenshtein distance of Knr Sequences
		/// </summary>
		public override double KnrDist (IList<ushort> a, IList<ushort> b)
		{
			return SequenceSpace<ushort>.Levenshtein(a, b, 1, 1, 1);
		}
	}
}