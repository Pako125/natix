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
//   Original filename: natix/SimilaritySearch/Indexes/KnrSharingAndOrder.cs
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
	/// Uses the sharing and the order as closeness predictor,
	/// without any kind of penalisation.
	/// </summary>
	public class KnrSharingAndOrder<T> : KnrSpearmanFootrule<T>
	{
		struct _tupleshort
		{
			public ushort data;
			public ushort pos;
			public _tupleshort (ushort data, ushort pos)
			{
				this.data = data;
				this.pos = pos;
			}
		}
		
		/// <summary>
		/// Initializes a new instance 
		/// </summary>
		public KnrSharingAndOrder () : base()
		{
		}
		
		/// <summary>
		///  Knr footrule 
		/// </summary>
		public override double KnrDist (IList<ushort> a, IList<ushort> b)
		{
			// a & b are already sorted
			// this can be incredible innefficient, but we want to see the recall
			List<_tupleshort> A = new List<_tupleshort> ();
			List<_tupleshort> B = new List<_tupleshort> ();
			for (ushort ia = 0, ib = 0; ia < a.Count && ib < b.Count;) {
				if (a[ia] == b[ib]) {
					A.Add (new _tupleshort (ia, a[ia + 1]));
					B.Add (new _tupleshort (ia, b[ib + 1]));
					ia += 2;
					ib += 2;
				} else if (a[ia] < b[ib]) {
					ia += 2;
				} else {
					ib += 2;
				}
			}
			
			Sorting.Sort<_tupleshort> (A, (u, v) => u.pos.CompareTo (v.pos));
			Sorting.Sort<_tupleshort> (B, (u, v) => u.pos.CompareTo (v.pos));
			
			int ordercounter = 1;
			for (int ia = 0, ib = 0; ia < A.Count && ib < A.Count;) {
				if (A[ia].data == B[ib].data) {
					ordercounter++;
					ia++;
					ib++;
				} else if (A[ia].pos < B[ib].pos) {
					ia++;
				} else {
					ib++;
				}
			}
			return - A.Count * ordercounter;
		}
	}
}