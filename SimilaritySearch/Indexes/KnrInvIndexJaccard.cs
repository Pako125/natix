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
//   Original filename: natix/natix/SimilaritySearch/Indexes/KnrInvIndexJaccard.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Mono.Options;

using natix.Sets;
using natix.SortingSearching;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Jaccard's inverted index
	/// </summary>
	public class KnrInvIndexJaccard<T> : KnrInvIndexBase<T>
	{
		ITThresholdAlgorithm TThreshold = new NTTArray8(-1, true);
		// ITThresholdAlgorithm TThreshold = new LargeStepTThreshold( new DoublingSearch<int>() );
		// ITThresholdAlgorithm TThreshold = new MergeTThreshold();
		// ITThresholdAlgorithm TThreshold = new MergeAndSkipTThreshold( new DoublingSearch(8, 8) );
		// ITThresholdAlgorithm TThreshold = new MergeAndSkipTThreshold (new DoublingSearch ());
		// ITThresholdAlgorithm TThreshold = new MergeAndSkipTThreshold (new BinarySearch() );
		// ITThresholdAlgorithm TThreshold = new MergeAndSkipTThreshold (new SequentialSearch ());
		// ITThresholdAlgorithm TThreshold = new SimpleIntersectionTThreshold( new InOrderTreeIntersection( new BinarySearch() ) );
		// ITThresholdAlgorithm TThreshold = new SimpleIntersectionTThreshold (new BaezaYatesIntersection (new BinarySearch ()));
		// ITThresholdAlgorithm TThreshold = new SimpleIntersectionTThreshold (new BarbayRandomized(new DoublingSearch()));
		// ITThresholdAlgorithm TThreshold = new SimpleIntersectionTThreshold (new BarbaySequential (new DoublingSearch ()));
		// ITThresholdAlgorithm TThreshold = new SimpleIntersectionTThreshold (new SvS (new DoublingSearch ()));

		/// <summary>
		/// Constructor
		/// </summary>
		public KnrInvIndexJaccard () : base()
		{
		}

		public void SetTThresholdAlgorithm (ITThresholdAlgorithm ttalg)
		{
			this.TThreshold = ttalg;
		}
		
		public override void Configure (IEnumerable<string> args)
		{
			var tta = "array8";
			OptionSet op = new OptionSet () {
				{
					"tta|t-threshold-algorithm=",
					"T-Threshold algorithm (large-step|merge|merge-skip|array8|hash-table|intersection)",
					v => tta = v
				}
			};
			op.Parse (args);
			base.Configure (args);
			
			switch (tta) {
			case "large-step":
				this.TThreshold = new LargeStepTThreshold (new DoublingSearch<int> ());
				break;
			case "merge":
				this.TThreshold = new MergeTThreshold ();
				break;
			case "merge-skip":
				this.TThreshold = new MergeAndSkipTThreshold (new BinarySearch<int> ());
				break;
			case "array8":
				this.TThreshold = new NTTArray8 (-1, false);
				break;
			case "hash-table":
				this.TThreshold = new NTTHashTable (false);
				break;
			case "intersection":
				var I = new BaezaYatesIntersection (new BinarySearch<int> ());
				this.TThreshold = new SimpleIntersectionTThreshold (I);
				break;
			default:
				throw new ArgumentException (String.Format ("Unknown t-threshold-algorithm '{0}'", tta));
			}
		}

		protected override IResult GetCandidates (T q, IList<ushort> qseq, int k)
		{
			int expectedSize;
			var posting = this.GetPostingLists (qseq, out expectedSize);
			IList<int> docs;
			IList<short> card;
			int threshold = Math.Max (1, posting.Count - this.ThresholdError);
			this.TThreshold.SearchTThreshold (posting, threshold, out docs, out card);
			int docsI = docs.Count;
			// Console.WriteLine ("===> threshold-results: qseq.Count: {0}, docs.Count: {1}, this.Maxcand: {2}, card: {3}", qseq.Count, docsI, this.Maxcand, card);
			IResult cand = new Result (Math.Abs (this.Maxcand), false);
			if (card == null) {
				foreach (var _d in docs) {
					cand.Push (_d, 0);
				}
			} else {
				double jacc;
				int I, U;
				for (int i = 0; i < docsI; i++) {
					I = card [i];
					// U = this.GetKnrSeqLength (docs[i]) + qseq.Count - I;
					U = qseq.Count - I + this.KnrSeqLength [docs [i]];
					jacc = 1.0 - ((double)I) / U;
					// Console.WriteLine ("xxxxxxxxxxxxx->>>> docs[i] = {0}, qseq.Count: {1}, I: {2}, jacc: {3}, AsShownByFun: {4}", docs[i], qseq.Count, I, jacc, this.GetKnrSeqLength (docs[i]));
					cand.Push (docs [i], jacc);
				}
			}
			return cand;
		}
	}
}