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
//   Original filename: natix/natix/SimilaritySearch/Indexes/LC_IRNN.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NDesk.Options;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// LC with a fixed number of centers
	/// </summary>
	/// <exception cref='ArgumentNullException'>
	/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
	/// </exception>
	public class LC_IRNN<T> : LC_RNN<T>
	{
		Index<T> build_index = null;
			
		public LC_IRNN () : base()
		{	
		}
		
		/// <summary>
		/// Build the index 
		/// </summary>
		public override void Build (string output_name, string db_name, string space_class, int sample_size)
		{
			this.RemoveCenterIndex (output_name);
			base.Build (output_name, db_name, space_class, sample_size);
			this.RemoveCenterIndex (output_name);
		}
		
		void RemoveCenterIndex (string output_name)
		{
			File.Delete (output_name + ".CENTERS.IDX.xml");
			File.Delete (output_name + ".CENTERS.IDX.xml.lc");
			File.Delete (output_name + ".CENTERS.IDX.xml.lc.bin");
			File.Delete (output_name + ".CENTERS.DB");
		}
		
		static int MIN_NUM_CENTERS = 1000;
		
		protected override void BuildInternalIndex (string output_name)
		{
			var ssname = output_name + ".CENTERS.DB";
			if (File.Exists (ssname)) {
				File.Delete (ssname);
			}
			this.MainSpace.SubSpace (ssname, this.CENTERS);
			double ratio = this.CENTERS.Count * 1.0 / this.MainSpace.Count;
			ratio = Math.Max (ratio, 0.01);
			var child_num_centers = (int)Math.Ceiling (this.CENTERS.Count * ratio);
			//var method = "lcrnn";
			var method = "lcfixedm";
			/*if (child_num_centers > 15000) {
					method = "lcrnn";
			}*/
			var cmd = String.Format (
					" --build --force --indexclass {0} " +
					" --spaceclass {1} --space {2}.CENTERS.DB " + 
					" --index {2}.CENTERS.IDX.xml --numcenters {3}",
					method, this.spaceClass, output_name, child_num_centers);
			Commands.Build (cmd);
			this.build_index = (Index<T>)IndexLoader.Load (output_name + ".CENTERS.IDX.xml");			
		}
		
		/// <summary>
		/// SearchNN, only used at preprocessing time
		/// </summary>
		public override void BuildSearchNN (int docid, out int nn_center, out double nn_dist)
		{
			var num_centers = this.CENTERS.Count;
			if (num_centers < MIN_NUM_CENTERS) {
				base.BuildSearchNN (docid, out nn_center, out nn_dist);
				return;
			}
			var R = this.build_index.KNNSearch (this.MainSpace [docid], 1);
			var p = R.First;
			nn_center = p.docid;
			nn_dist = p.dist;
		}
		
	}
}
