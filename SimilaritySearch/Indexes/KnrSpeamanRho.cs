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
//   Original filename: natix/SimilaritySearch/Indexes/KnrSpeamanRho.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NDesk.Options;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Spearman Rho for Knr
	/// </summary>
	public class KnrSpearmanRho<T> : KnrSpearmanFootrule<T>
	{
		/// <summary>
		/// Constructor for Spearman Rho
		/// </summary>
		public KnrSpearmanRho () : base()
		{
		}
		
		/// <summary>
		/// Knr Spearman Rho
		/// </summary>
		public override double KnrDist (IList<ushort> a, IList<ushort> b)
		{
			// a & b are already sorted
			// union
			// penalization
			int P = (a.Count + b.Count);
			P *= P;
			double d = 0;
			//Console.WriteLine ("====> a.Len {0}, b.Len {1}", a.Length, b.Length);
			int ia = 0, ib = 0;
			while ( ia < a.Count && ib < b.Count ) {
				//Console.Write ("Ini> ia: {0}, ib: {1}, ", ia, ib);
				if (a [ia] == b [ib]) {
					double m = a [ia + 1] - b [ib + 1];
					d += m * m;
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
			d += ((a.Count - ia) >> 1) * P;
			d += ((b.Count - ib) >> 1) * P;
			return d;
		}
	}
}