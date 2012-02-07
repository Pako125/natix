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
//   Original filename: natix/natix/SimilaritySearch/Spaces/Space.cs
// 
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Should be used as a distance prototype for methods and classes accepting external distance's functions
	/// </summary>
	public delegate double Distance<T>(T a, T b);
	/// <summary>
	/// Exposes the basic methods to provide the space functionality.
	/// </summary>
	/// <remarks> There exists two versions of an space
	/// The complete version. It must know things about the underlying object datatypes, and can access to single items
	/// Restricted version. It should be used for tasks that doesn't need to know things about the object types.
	/// </remarks>
	public interface Space
	{
		/// <summary>
		///  Get and Set (and Load) the name of the space
		/// </summary>
		/// <remarks>
		/// Used to load and save spaces by indexes. When we use it to set the space's name it should load the database
		/// </remarks>
		string Name {
			get;
			set;
		}
		/// <summary>
		/// The number of objects in the space, useful for iterating over the space using the indexer facilities.
		/// For simplicity and the randomness nature of the spaces, this methods should be prefeared instead any
		/// IEnumerable implementation.
		/// 
		/// This should be thread-safe (specially important for spaces with delete capabilities)
		/// </summary>
		int Count {
			get;
		}
		/// <summary>
		///  The number of distances computed at the accessing time, this is an monotonic function.
		/// This can be non-safe for multithread environments, it's useful for experimental tests.
		/// </summary>
		int NumberDistances {
			get;
		}
		/// <summary>
		/// Get the generic type (underlying datatype) for this space. The object's type.
		/// </summary>
		Type GenericType {
			get;
		}
		/// <summary>
		/// The name of the type, useful for reflection (loading and saving in xml files)
		/// </summary>
		string SpaceType {
			get;
			set;
		}
		/// <summary>
		/// Saves a subspace of the current space.
		/// </summary>
		/// <param name="name">
		/// The output file <see cref="System.String"/>
		/// </param>
		/// <param name="samplesize">
		/// The size of the sampling (negatives means the entire database) <see cref="System.Int32"/>
		/// </param>
		/// <param name="random">
		/// If random is true then an uniform sampling is performed, else a sequential sampling (starting in the first item)
		///  <see cref="System.Boolean"/>
		/// </param>
		void SubSpace(string name, int samplesize, bool random);
		/// <summary>
		/// Saves a permutation or a subsample of the space
		/// </summary>
		void SubSpace (string name, IList<int> permutation);
		//Result verify (Result Input, Result Output);
		/// <summary>
		/// Result creation. The space *should* know the distance distribution better than any other piece
		/// </summary>		
		IResult CreateResult (int K, bool ceiling);
	}
	
	/// <summary>
	/// The Space's interface knowning about the indexed datatype
	/// </summary>
	public interface Space<T> : Space
	{
		/// <summary>
		/// Returns the object numerated with docid
		/// </summary>
		/// <param name="docid">
		/// The object's id to be retrieved
		/// A <see cref="System.Int32"/>
		/// </param>
		T this[int docid] {
			get;
		}
		/// <summary>
		/// The distance function
		/// </summary>
		/// <param name="a">
		/// An object to be measured, relative to b
		/// </param>
		/// <param name="b">
		/// An object to be measured, relative to a
		/// </param>
		/// <returns>
		/// The distance measure
		/// A <see cref="System.Double"/>
		/// </returns>
	    double Dist(T a, T b);
		/// <summary>
		/// Parses an string to the space's object type.
		/// </summary>
		/// <param name="s">
		/// The string representing the object
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="isquery">
		/// Tell the method if the string to be parsed is a query.
		/// A <see cref="System.Boolean"/>
		/// </param>
		/// <returns>
		/// The parsed object
		/// </returns>
		T Parse(string s, bool isquery);
	}	
}
