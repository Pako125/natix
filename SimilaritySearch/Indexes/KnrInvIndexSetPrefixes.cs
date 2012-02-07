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
//   Original filename: natix/natix/SimilaritySearch/Indexes/KnrInvIndexSetPrefixes.cs
// 

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using NDesk.Options;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Jaccard's inverted index
	/// </summary>
	public class KnrInvIndexSetPrefixes<T> : KnrInvIndexBase<T>
	{
		int MinPrefix;
		/// <summary>
		/// Constructor
		/// </summary>
		public KnrInvIndexSetPrefixes () : base()
		{
		}
		
		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			base.FinalizeLoad (name, config);
			this.LoadListOfKnrSeq (name); // fixit! implement this with a complete seq. index
			this.MinPrefix = 1;
		}
		
		public override void Configure (IEnumerable<string> args)
		{
			base.Configure (args);
			OptionSet op = new OptionSet () {
				{"minprefix", "Minimum shared prefix length to considere candidates", v => this.MinPrefix = int.Parse (v)},
			};
			op.Parse (args);
		}

		protected override IResult GetCandidates (T q, IList<ushort> qseq, int k)
		{
			var C = this.GetUnionLists (q, qseq);
			var R = new Result (Math.Abs (this.Maxcand), false);
			foreach (var docid in C) {
				// var dist = this.MainSpace.Dist (q, this.MainSpace [docid]);
				var useq = this.GetSingleKnrSeq (docid);
				var dist = SequenceSpace<ushort>.PrefixLength (qseq, useq);
				if (dist >= this.MinPrefix) {
					R.Push (docid, -dist);
				}
			}
			return R;
		}
	}
}