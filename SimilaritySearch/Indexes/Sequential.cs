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
//   Original filename: natix/SimilaritySearch/Indexes/Sequential.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO.Compression;
using System.Reflection;
using NDesk.Options;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// The sequential index
	/// </summary>
	public class Sequential<T> : BaseIndex<T>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public Sequential ()
		{
		}
		
		/// <summary>
		/// API build command
		/// </summary>
		public virtual void Build (string name, string spaceClass, string spaceName)
		{
			this.spaceClass = spaceClass;
			this.spaceName = Dirty.ComputeRelativePath (name, spaceName);
			var S = SpaceCache.Load (spaceClass, spaceName);
			this.SetMainSpace ((Space<T>)S);
			Dirty.SaveIndexXml (name, this);
		}

		/// <summary>
		/// (Command line) user interface
		/// </summary>
		/// <param name="args">
		/// Command line syntax (--kwarg value)
		/// </param>
		public override void Build (IEnumerable<string> args)
		{
			string name = null;
			string space = null;
			string spaceclass = null;
			
			OptionSet ops = new OptionSet() {
				{"indexname=", "Output index name", v => name = v},
				{"space=", "Space filename", v => space = v},
				{"spaceclass=", "Space class", v => spaceclass = v}
			};
			ops.Parse(args);
			if (name == null || space == null || spaceclass == null) {
				Console.WriteLine("Sequential index options: ");
				ops.WriteOptionDescriptions(Console.Out);
				throw new ArgumentException("Some mandatory options were not given");
			}
			this.Build(name, spaceclass, space);
		}
		
		/// <summary>
		/// Finalize the index's load <see cref="natix.IndexLoader.Load"/>
		/// </summary>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		public override void FinalizeLoad (string name, IDictionary<string, object> config)
		{
			var pathSpace = Dirty.CombineRelativePath (name, this.spaceName);
			this.SetMainSpace ((Space<T>)SpaceCache.Load (this.spaceClass, pathSpace));
		}
		
		/// <summary>
		/// Search by range
		/// </summary>
		/// <param name="q">
		/// Query object 
		/// </param>
		/// <param name="radius">
		/// Radius <see cref="System.Double"/>
		/// </param>
		/// <returns>
		/// The result set <see cref="Result"/>
		/// </returns>
		public override IResult Search (T q, double radius)
		{
			int L = this.MainSpace.Count;
			var r = new Result (L);
			for (int docid = 0; docid < L; docid++) {
				double d = this.MainSpace.Dist (q, this.MainSpace[docid]);
				if (d <= radius) {
					r.Push (docid, d);
				}
			}
			return r;
		}
		
		/// <summary>
		/// KNN Search
		/// </summary>
		/// <param name="q">
		/// Query object 
		/// </param>
		/// <param name="k">
		/// The number of nearest neighbors <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// The result set <see cref="Result"/>
		/// </returns>
		public override IResult KNNSearch (T q, int k, IResult R)
		{
			int L = this.MainSpace.Count;
			for (int docid = 0; docid < L; docid++) {
				double d = this.MainSpace.Dist (q, this.MainSpace[docid]);
				R.Push (docid, d);
			}
			return R;
		}

	}
}
