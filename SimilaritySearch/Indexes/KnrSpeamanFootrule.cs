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
//   Original filename: natix/SimilaritySearch/Indexes/KnrSpeamanFootrule.cs
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
	/// Sequential spearman footrule 
	/// </summary>
	public class KnrSpearmanFootrule<T> : Knr<T>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public KnrSpearmanFootrule () : base()
		{
		}

		/// <summary>
		/// Knr wrapper for footrule
		/// </summary>
		protected override IList<UInt16> KnrWrap (IList<UInt16> a)
		{
			int aL = a.Count;
			UInt16[] idx = new UInt16[aL];
			for (ushort i = 0; i < aL; i++) {
				idx[i] = i;
			}
			Sorting.Sort<UInt16, UInt16> (a, idx);
			UInt16[] v = new UInt16[aL * 2];
			for (int i = 0, ii = 0; i < aL; i++,ii++) {
				v[ii] = a[i];
				ii++;
				v[ii] = idx[i];
			}
			return v;
		}

		/// <summary>
		/// Knr footrule
		/// </summary>
		public override double KnrDist (IList<UInt16> a, IList<UInt16> b)
		{
			// a & b are already sorted
			// union
			// penalization
			int P = (a.Count + b.Count) << 1;
			double d = 0;
			//Console.WriteLine ("====> a.Len {0}, b.Len {1}", a.Length, b.Length);
			for (int ia = 0, ib = 0; ia < a.Count && ib < b.Count;) {
				//Console.Write ("Ini> ia: {0}, ib: {1}, ", ia, ib);
				if (a [ia] == b [ib]) {
					double m = Math.Abs (a [ia + 1] - b [ib + 1]);
					d += m;
					ia += 2;
					ib += 2;
				} else if (a [ia] < b [ib]) {
					ia += 2;
					d += P;
				} else {
					ib += 2;
					d += P;
				}
				//Console.WriteLine ("Fin> ia: {0}, ib: {1}", ia, ib);
			}
			return d;
		}		
	}
}