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
//   Original filename: natix/SimilaritySearch/Indexes/KnrPermPrefix.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NDesk.Options;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// The knr class for sequential ppindex
	/// </summary>
	public class KnrPermPrefix<T> : Knr<T>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public KnrPermPrefix () : base()
		{}


		/// <summary>
		/// The wrapper function to the knr sequence
		/// </summary>
		public override IList<UInt16> KnrWrap (IList<UInt16> a)
		{
			return a;
		}
		/// <summary>
		/// Knr distance
		/// </summary>
		public override double KnrDist (IList<UInt16> a, IList<UInt16> b)
		{
			int i, min = Math.Min (a.Count, b.Count);
			for (i = 0; i < min && a[i] == b[i]; i++) {
				//empty
			}
			return -i;
			// return 1.0 / (i + 0.01);
		}
	}
}