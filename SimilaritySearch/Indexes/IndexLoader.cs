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
//   Original filename: natix/SimilaritySearch/Indexes/IndexLoader.cs
// 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using natix;
using natix.SimilaritySearch;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Helping class to load and create indexes from xml saved files
	/// </summary>
	/// <remarks>
	/// The xml files contains all the necessary information to 
	/// recover and load the index. Loading issues are delegated to 
	/// index implementations.
	/// 
	/// We can create indexes specifing an indexclass and spaceclass. The spaceclass
	/// can be one of the defined in SpaceCache.cs
	/// 
	/// Indexclass should be one of the specified here
	/// 
	///</remarks>
	public class IndexLoader
	{
		/// <summary>
		/// Returns the object Type of the index, this should be done before the generic creation.
		/// </summary>
		/// <remarks>
		/// Returns the object Type of the index, this should be done before the generic creation.
		/// The type will be signed using the space type.
		/// </remarks>
		/// <example>
		/// // Example of a delegate:
		/// () =&gt; typeof(Bkt&lt;&gt;)
		/// </example>
		public static Dictionary<string, Func<Type> > IndexFactory = new Dictionary<string, Func<Type>>() {
			{"bkt", () => typeof(Bkt<>)},
			{"sequential", () => typeof(Sequential<>)},
			// {"knrinvindex", () => typeof(KnrInvIndex<>)},
			{"knrinvindexjaccard", () => typeof(KnrInvIndexJaccard<>)},
			{"knrjaccard", () => typeof(KnrJaccard<>)},
			{"knrdice", () => typeof(KnrDice<>)},
			{"knrhamming", () => typeof(KnrHamming<>)},
			{"knrshortjaccard", () => typeof(KnrShortJaccard<>)},
			{"ppindex", () => typeof(KnrPermPrefix<>)},
			{"knrinvindexprefixes", () => typeof(KnrInvIndexSetPrefixes<>)},
			{"sortedppindex", () => typeof(KnrSortedPermPrefix<>)},
			{"knrlcs", () => typeof(KnrLCS<>)},
			{"knrinvindexlcs", () => typeof(KnrInvIndexSetLCS<>)},
			{"knrlevenshtein", () => typeof(KnrLevenshtein<>)},
			{"knrinvindexlevenshtein", () => typeof(KnrInvIndexSetLevenshtein<>)},
			{"knrsortedlevenshtein", () => typeof(KnrSortedLevenshtein<>)},
			{"knrfootrule", () => typeof(KnrSpearmanFootrule<>)},
			{"knrspearmanrho", () => typeof(KnrSpearmanRho<>)},
			//{"knrsharingandorder", () => typeof(natix.KnrSharingAndOrder<>)},
			{"knrcosine", () => typeof(KnrCosine<>)},
			{"perms", () => typeof(Perms<>)},
			{"perms8", () => typeof(Perms8<>)},
			{"movperms", () => typeof(MovPerms<>)},
			{"binperms", () => typeof(BinPerms<>)},
			{"binpermstwobit", () => typeof(BinPermsTwoBit<>)},
			{"binperms2bit", () => typeof(BinPermsTwoBit<>)},
			{"lshhamming", () => typeof(HammingLSH)},
			{"lschamming", () => typeof(HammingLSC)},
			{"mlschamming", () => typeof(HammingMLSC)},
			{"pivinvindex", () => typeof(PivInvIndex<>) },
			{"lcirnn", () => typeof(LC_IRNN<>)},
			{"lcprnn", () => typeof(LC_PRNN<>)},
			{"lcpfixedm", () => typeof(LC_PFixedM<>)},
			//{"lcirnncutlen", () => typeof(LC_IRNN_CUT_LEN<>)}, // comment out after review the algorithm
			//{"lcirnncutrad", () => typeof(LC_IRNN_CUT_RAD<>)}, // comment out after review the algorithm
			{"lcrnn", () => typeof(LC_RNN<>)},
			// {"lcknn", () => typeof(LC_KNN<>)},
			{"lcfixedm", () => typeof(LC_FixedM<>)},
			{"lcparallelsearch", () => typeof(LC_ParallelSearch<>)},
			{"polyindexlc", () => typeof(PolyIndexLC<>)},
			// {"permpolyindexlc", () => typeof(PermPolyIndexLC<>)}, // comment out after review the algorithm
		};
		
		/// <summary>
		/// Creates an empty index using the given indexclass and spaceclass
		/// </summary>
		/// <remarks>
		/// indexclass will be make a generic implementation using the spaceclass and the space.GenericType
		/// </remarks>
		/// <param name="indexclass">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="spaceclass">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="Index"/>
		/// </returns>
		public static Index Create (string indexclass, string spaceclass, IDictionary<string, object> config)
		{
			if (indexclass == null || spaceclass == null) {
				throw new ArgumentException ("IndexLoader.NewIndex 'indexclass' and 'spaceclass' can't be null");
			}
			Type indexType = null;
			try {
				indexType = IndexFactory[indexclass.ToLower ()] ();
			} catch (KeyNotFoundException e) {
				StackTrace st = new StackTrace (true);
				for (int i = 0; i < st.FrameCount; i++)
				{
					// Note that high up the call stack, there is only
					// one stack frame.
					StackFrame sf = st.GetFrame (i);
					Console.WriteLine ();
					Console.WriteLine ("High up the call stack, Method: {0}",
                    sf.GetMethod ());
					
					Console.WriteLine ("High up the call stack, Line Number: {0}",
						sf.GetFileLineNumber ());
				}
				Console.WriteLine ("Unknown index class: '{0}' (space: '{1}'), candidates:", indexclass, spaceclass);
				foreach (string s in IndexLoader.IndexFactory.Keys) {
					Console.WriteLine ("Index class: {0}", s);
				}
				throw e;
			}
			Space sp = SpaceCache.Create (spaceclass, config);
			Index idx;
			if (indexType.IsGenericType) {
				idx = (Index)Activator.CreateInstance (indexType.MakeGenericType (sp.GenericType));
			} else {
				idx = (Index)Activator.CreateInstance (indexType);
			}
			return idx;
		}
		
		public static Index Load (string name, string indexClass, string spaceClass, IDictionary<string, object> config)
		{
			Type indexType;
			Console.WriteLine ("Loading index: {0}", name);
			Console.WriteLine ("Loading indexClass: {0}", indexClass);
			indexType = Type.GetType (indexClass);
			if (indexType == null) {
				indexType = Create (indexClass, spaceClass, null).GetType ();
			}
			// Index idx = (Index)Dirty.DeserializeXML (name, indexType);
			Index idx = Dirty.LoadIndexXml (name, indexType);
			idx.FinalizeLoad (name, config);
			return idx;
		}
		
		
		public static void GetSpaceIndexClassFromFile (string filename,
			out string spaceClass, out string indexClass)
		{
			spaceClass = null;
			indexClass = null;
			XmlTextReader reader = new XmlTextReader (filename);
			while (!reader.EOF) {
				reader.Read ();
				if (reader.Name == "IndexType" && indexClass == null) {
					reader.Read ();
					indexClass = reader.Value.Trim ();
				}
				if (reader.Name == "spaceClass" && spaceClass == null) {
					reader.Read ();
					spaceClass = reader.Value.Trim ();
				}
			}
			reader.Close ();			
		}
		
		/// <summary>
		/// Load an index from xml file. It automatically calls FinalizeLoad
		/// </summary>

		public static Index Load (string name, string indexClass = null, IDictionary<string, object> config = null)
		{
			string spaceClass = null;
			string _indexClass = null;
			GetSpaceIndexClassFromFile (name, out spaceClass, out _indexClass);
			if (indexClass == null) {
				indexClass = _indexClass;
			}
			// string indexClass = null;
			if (spaceClass == null) {
				throw new ArgumentException ("Expecting spaceClass tag, invalid index file '{0}'", name);
			}
			if (indexClass == null) {
				throw new ArgumentException ("Expecting IndexType tag, invalid index file '{0}'", name);
			}
			return Load (name, indexClass, spaceClass, config);
		}
	}
}
