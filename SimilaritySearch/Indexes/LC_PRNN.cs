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
//   Original filename: natix/natix/SimilaritySearch/Indexes/LC_PIRNN.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NDesk.Options;
using natix.CompactDS;
using System.Threading.Tasks;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// LC with a fixed number of centers
	/// </summary>
	/// <exception cref='ArgumentNullException'>
	/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
	/// </exception>
	public class LC_PRNN<T> : LC_RNN<T>
	{
		public LC_PRNN () : base()
		{
		}
		
		public override void BuildInternal (BitStream32 IsCenter, IList<IList<int>> invindex, string output_name)
		{
			int len = this.MainSpace.Count;
			int pc = len / 100 + 1;
			int count = 0;
			Action<int> classify_object = delegate (int docid) {
				if (IsCenter [docid]) {
					return;
				}		
				int nn_center;
				double nn_dist;
				this.BuildSearchNN (docid, out nn_center, out nn_dist);
				lock (invindex) {
					++count;
					invindex [nn_center].Add (docid);
					if (this.COV [nn_center] < nn_dist) {
						this.COV [nn_center] = (float)nn_dist;
					}
					if (count % pc == 0) {
						Console.WriteLine ("count {0} of {1}, advance: {2:0.00}%, date-time: {3}", count, len, count * 100.0 / len, DateTime.Now);
					}
					// Console.WriteLine ("** count {0} of {1}, advance {2:0.00}%, advance: {3}", count, len, count * 100.0 / len, output_name);
				}
			};
			classify_object (0);
			var pops = new ParallelOptions ();
			pops.MaxDegreeOfParallelism = -1;
			//pops.MaxDegreeOfParallelism = 1;
			Parallel.For (1, this.MainSpace.Count, classify_object);
		}
	}
}
