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
//   Original filename: natix/natix/SimilaritySearch/Indexes/KnrJaccard.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Mono.Options;
using natix.SortingSearching;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// The knr index for sequential jaccard
	/// </summary>
	public class KnrJaccard<T> : Knr<T>, Index<T> 
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public KnrJaccard () : base()
		{
		}
		 
		/// <summary>
		/// Wraps the Knr sequence
		/// </summary>
		protected override IList<UInt16> KnrWrap (IList<UInt16> a)
		{
			Sorting.Sort<UInt16> (a);
			return a;
		}
		/// <summary>
		/// Knr distance
		/// </summary>
		public override double KnrDist (IList<UInt16> a, IList<UInt16> b)
		{
			// a & b are already sorted
			// union
			int U = a.Count + b.Count;
			// intersection
			int I = 0;
			for (int ia = 0, ib = 0; ia < a.Count && ib < b.Count;) {
				if (a[ia] == b[ib]) {
					U--;
					I++;
					ia++;
					ib++;
				} else if (a[ia] < b[ib]) {
					ia++;
				} else {
					ib++;
				}
			}
			return 1.0 - ((double)I) / U;
		}
		
		/// <summary>
		///  Finalize the load (<see cref="natix.IndexLoader.Load"/>)
		/// </summary>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
				
		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			base.FinalizeLoad (name, config);
			
			var fs = new StreamWriter (name + ".ascii");
			var data = this.GetListOfKnrSeq ();
			for (int docid = 0; docid < data.Count; docid++) {
				IList<UInt16> v = data[docid];
				for (int k = 0; k < v.Count; k++) {
					fs.Write ("{0} ", v[k]);
				}
				fs.WriteLine ();
			}
			fs.Close ();
		}

	}
}