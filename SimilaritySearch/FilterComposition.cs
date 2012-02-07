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
//   Original filename: natix/natix/SimilaritySearch/FilterComposition.cs
// 
using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mono.Options;


namespace natix.SimilaritySearch
{
	/// <summary>
	/// Iteration Filter Composition
	/// </summary>
	public class FilterComposition<T> : List< ResultFilter<T> >
	{
		/// <summary>
		/// Composition
		/// </summary>
		public FilterComposition ()
		{
		}
		
		/// <summary>
		/// Configure the filter from command line style arguments
		/// </summary>
		/// <param name="args">
		/// An iterable collection of strings in command line style 
		/// </param>
		public void Configure (IEnumerable<string> args)
		{
			OptionSet ops = new OptionSet() {
				{"filterfromfile=", "Loads a list binary permutations indexes from file to be used as filters", v => this.LoadExternalFilterList(v) }
			};
			ops.Parse(args);
		}
		
		/// <summary>
		/// Perform filtering (reorder results)
		/// </summary>
		/// <param name="index">
		/// The caller index
		/// </param>
		/// <param name="q">
		/// Query object
		/// </param>
		/// <param name="qenc">
		/// Query object encoded in the original mapping (caller of the filter).
		/// </param>
		/// <param name="R">
		/// A <see cref="Result"/>
		/// </param>
		/// <returns>
		/// A <see cref="Result"/>
		/// </returns>
		/// 
		public IResult Filter (Index<T> index, T q, object qenc, IResult R)
		{
			foreach (var F in this) {
				R = F (index, q, qenc, R);
			}
			return R;
		}
		
		/// <summary>
		/// Load a list of indexes from a file
		/// </summary>
		/// <remarks>
		/// One index file per line, followed by parameters to be send to Configure, every argument is
		/// separated by a single colon (:)
		/// Example
		/// binperms:Index.BinPerms.np=128:--maxcand:1000
		/// The first argument is the indexfile, the second is the type of the index (currently only binperms is supported),
		/// the last ones are arguments to the Configure method of the index.
		///
		/// Lines starting with # will be ignored
		/// </remarks>
		/// <param name="filename">
		/// A <see cref="System.String"/>
		/// </param>
		public void LoadExternalFilterList (string filename)
		{
			Console.WriteLine ("=== Loading filters from {0}", filename);
			// throw new ArgumentException ("just testing");
			List<string> args = new List<string> ();
			Regex re = new Regex (@"([^\s:]+)");
			foreach (string l in File.ReadAllLines (filename)) {
				var line = l.Trim ();
				if (line == "" || line.StartsWith ("#") || line.StartsWith("//")) {
					continue;
				}
				Console.WriteLine ("=== Loading filter '{0}'", line);
				args.Clear ();
				foreach (Match s in re.Matches(line)) {
					args.Add (s.Value);
				}
				var itype = args[0].ToLower ();
				var iarg = args[1];
				args.RemoveAt (0);
				args.RemoveAt (0);
				
				if (itype == "function") {
					switch (iarg) {
					case "cut":
						this.Add ((cindex, q, qenc, r) => this.FunctionCut (q, qenc, r, args));
						break;
					case "distline":
						this.Add ((cindex, q, qenc, r) => this.FunctionDistLine (q, qenc, r, args));
						break;
					default:
						throw new NotImplementedException (String.Format ("Filter function for {0} {1} is not implemented", itype, iarg));
					}
					continue;
				}

				var findex = (Index<T>)IndexLoader.Load (iarg);
				
				findex.Configure (args);
				switch (itype) {
				case "binperms":
					this.Add ((cindex, q, qenc, r) => this.ResortByBinPermsExternalIndex ((BinPerms<T>)findex, q, qenc, r));
					break;
				case "perms":
					this.Add ((cindex, q, qenc, r) => this.ResortByPermsExternalIndex ((Perms<T>)findex, q, qenc, r));
					break;
				case "knr":
					this.Add ((cindex, q, qenc, r) => this.ResortByKnrExternalIndex ((Index<T>)findex, q, qenc, r));
					break;
				default:
					throw new NotImplementedException(String.Format ("Filter Index for {0} is not implemented", itype));
				}
			}
		}

		IResult FunctionDistLine (T q, object qseq, IResult R, IEnumerable<string> args)
		{
			double prod = 1;
			double sum = 0;

			OptionSet ops = new OptionSet() {
				{"prod=", "a : a * dist + b", v => prod = double.Parse(v) },
				{"sum=", "b : a * dist + b", v => sum = double.Parse(v) },
			};
			ops.Parse(args);

			var RR = new Result(R.Count, R.Ceiling);
			foreach (var p in R) {
				RR.Push(p.docid, prod * p.dist + sum);
			}
			return RR;
		}

		IResult FunctionCut (T q, object qseq, IResult R, IEnumerable<string> args)
		{
			int start = 0;
			int count = R.Count;
			bool ceiling = R.Ceiling;
			double rangeconstraint = R.Last.dist;
			
			OptionSet ops = new OptionSet() {
				{"start=", "Starting index", v => start = int.Parse(v) },
				{"count=", "Number of results to include (excepting for ceiling results or availability)", v => count = int.Parse(v) },
				{"ceiling", "Set ceiling result to true, the default is taken from previous result", v => ceiling = true },
				{"noceiling", "Set ceiling result to false, the default is taken from previous result", v => ceiling = false },
				{"range-constraint=", "Accept only results with ranges smaller or equal to the given constraint (default all results are accepted)", v => rangeconstraint = double.Parse(v)}
			};
			ops.Parse(args);
			IResult RR = new Result(count, ceiling);
			int i = 0;
			foreach (var p in R) {
				i++;
				if (i >= start && p.dist <= rangeconstraint) {
					RR.Push(p.docid, p.dist);
				}
			}
			return RR;
		}
		
		IResult ResortByBinPermsExternalIndex (BinPerms<T> findex, T q, object qseq, IResult R)
		{
			var qbin = findex.Encode (q);
			var sp = findex.GetIndexHamming ().MainSpace;
			// we are hopping that the best result is really the best result (just for testing)	
			var RR = new Result (Math.Abs (findex.Maxcand), false);
			foreach (ResultPair p in R) {
				RR.Push (p.docid, p.dist + sp.Dist (qbin, sp[p.docid]));
				// RR.Push (p.docid, sp.Dist (qbin, sp[p.docid]));
			}
			return RR;
		}
		
		IResult ResortByPermsExternalIndex (Perms<T> findex, T q, object qseq, IResult R)
		{
			var qperm = findex.GetInverse (q);
			var sp = findex.GetInvPermsVectorSpace ();
			// we are hopping that the best result is really the best result (just for testing)	
			var RR = new Result (Math.Abs (findex.Maxcand), false);
			foreach (ResultPair p in R) {
				RR.Push (p.docid, p.dist + sp.Dist (qperm, sp[p.docid]));
				//RR.Push (p.docid, sp.Dist (qperm, sp[p.docid]));
			}
			return RR;
		}
		
		IResult ResortByKnrExternalIndex (Index<T> _findex, T q, object qseq, IResult R)
		{
			var findex = (Knr<T>)_findex;
			var qknr = findex.GetKnr (q, true);
			// we are hopping that the best result is really the best result (just for testing)	
			var RR = new Result (Math.Abs (findex.Maxcand), false);
			foreach (ResultPair p in R) {
				RR.Push (p.docid, p.dist + findex.KnrDist (qknr, findex.GetSingleKnrSeq(p.docid)));
				//RR.Push (p.docid, sp.Dist (qperm, sp[p.docid]));
			}
			return RR;
		}
	}
}
