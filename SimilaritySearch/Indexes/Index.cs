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
//   Original filename: natix/SimilaritySearch/Indexes/Index.cs
// 
using System;
using System.Collections.Generic;
//using System.Xml;
//using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Simple implementation of two values to measure an index performance. Internal and external cost
	/// </summary>
	/// <remarks>
	/// We can distinguish over distance computations and computations need to the index.
	/// </remarks>
	[Serializable]
	public struct SearchCost
	{
		/// <summary>
		/// Internal cost
		/// </summary>
		public int Internal;
		/// <summary>
		/// External cost
		/// </summary>
		public int External;
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="Internal">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="External">
		/// A <see cref="System.Int32"/>
		/// </param>
		public SearchCost (int Internal, int External)
		{
			this.Internal = Internal;
			this.External = External;
		}
	}
	
	/// <summary>
	/// An index without access to the individual objects, useful load,save, build and configure tasks 
	/// </summary>
	public interface Index
	{
		/// <summary>
		///  The index type, used by XML serialization
		/// </summary>
		string IndexType {
			get;
			set;
		}
		/// <summary>
		/// Called by IndexLoader to finalize the index loading.
		/// </summary>
		/// <param name="name">
		/// The filename of index
		/// </param>
		/// <param name="config">A possibly null dictionary of config values string to object map</param>
		void FinalizeLoad(string name, IDictionary<string, object> config);
		/// <summary>
		///  Configure index's options (Used by command line)
		/// </summary>
		/// <remarks>
		/// Used by (command line) user interfaces to set special options to the index
		/// The options should be given in command line like syntax strings (--keyword value) items
		/// 
		/// The particular index API to make the same task 
		/// </remarks>
		/// <param name="options">
		/// The options arguments
		/// </param>
		void Configure(IEnumerable<string> options);
		/// <summary>
		/// Build interface for (command line) user interfaces
		/// </summary>
		/// <remarks>
		/// Build interface accepting options in string format. It accepts command line like syntax strings (--keyword value).
		/// The options are dependent of the index class.
		/// 
		/// An index dependent Build method should be present, but it can be only be usefult from API.
		/// </remarks>
		/// <param name="options">
		/// Arguments
		/// </param>
		void Build(IEnumerable<string> options);
		/// <summary>
		/// Access to the accumulated performed work of the index
		/// </summary>
		SearchCost Cost {
			get;
		}
		/// <summary>
		/// Queries by range a query given in string format
		/// </summary>
		/// <param name="s">
		/// The string representing the query
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="radius">
		/// The radius to search
		/// A <see cref="System.Double"/>
		/// </param>
		/// <returns>
		/// The result set
		/// A <see cref="Result"/>
		/// </returns>
		IResult ParseSearch(string s, double radius);
		/// <summary>
		/// Search K Nearest Neighbors using a string representing the query
		/// </summary>
		/// <param name="s">
		/// String representing the query
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="k">
		/// How many objects we should retrieve
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// The result set
		/// A <see cref="Result"/>
		/// </returns>
		IResult ParseKNNSearch(string s, int k);
		
		/// <summary>
		/// Gets space (not generic).
		/// </summary>

		Space MainSpaceWithoutType {
			get;
		}
	}
	
	/// <summary>
	/// Interface to index using the underlying object type
	/// </summary>
	public interface Index<T> : Index
	{
		/// <summary>
		/// Access to the main space (the indexed space)
		/// </summary>
		Space<T> MainSpace {
			get;
		}
		/// <summary>
		/// Search by range
		/// </summary>
		/// <param name="q">
		/// Object to search
		/// </param>
		/// <param name="radius">
		/// Search's radius
		/// A <see cref="System.Double"/>
		/// </param>
		/// <returns>
		/// The result set
		/// A <see cref="Result"/>
		/// </returns>
		IResult Search(T q, double radius);
		/// <summary>
		/// Searching K Nearest Neighbors
		/// </summary>
		/// <param name="q">
		/// The query object
		/// </param>
		/// <param name="k">
		/// The number of nearest neighbors to retrieve
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// The result set
		/// A <see cref="Result"/>
		/// </returns>
		IResult KNNSearch (T q, int k);
		/// <summary>
		/// Searches for KNN but using res as *suggested* ouput, it could returns another res object, so it must be updated (if necessary)
		/// </summary>
		IResult KNNSearch (T q, int k, IResult res);
	}	
}
