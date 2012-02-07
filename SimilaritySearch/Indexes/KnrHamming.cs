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
//   Original filename: natix/natix/SimilaritySearch/Indexes/KnrHamming.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NDesk.Options;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// The knr index for sequential hamming, note that is hamming over jaccard sequence!
	/// 	/// </summary>
	public class KnrHamming<T> : KnrJaccard<T>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public KnrHamming () : base()
		{
		}

		/// <summary>
		/// Knr Hamming distance
		/// </summary>
		public override double KnrDist (IList<UInt16> a, IList<UInt16> b)
		{
			// a & b are already sorted
			// union
			// intersection
			int I = 0;
			for (int ia = 0, ib = 0; ia < a.Count && ib < b.Count;) {
				if (a[ia] == b[ib]) {
					I++;
					ia++;
					ib++;
				} else if (a[ia] < b[ib]) {
					ia++;
				} else {
					ib++;
				}
			}
			return -I;
		}
	}
}