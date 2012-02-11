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
//   Original filename: natix/SimilaritySearch/Spaces/SpaceCache.cs
// 
using System;
using System.Collections.Generic;
using System.IO;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Works as cache for loaded spaces and a loader of spaces.
	/// </summary>
	public class SpaceCache
	{
		static Dictionary<string, Space > cache = new Dictionary<string, Space >();
		/// <summary>
		/// Known spaces, this dictionary is used as name resolver for spaces. To manage spaces, just manage the dictionary entries
		/// </summary>
		public static Dictionary<string, Func<IDictionary<string,object>,Space>> SpaceFactory =
		new Dictionary<string, Func< IDictionary<string,object>, Space>>() {
			{"documents", (cfg) => new DocumentSpace() },
			{"document", (cfg) => new DocumentSpace() },
			{"vspace-float", (cfg) => new VectorSpace<Single>() },
			{"vspace-single", (cfg) => new VectorSpace<Single>() },
			{"vspace-double", (cfg) => new VectorSpace<Double>() },
			{"vspace-int32", (cfg) => new VectorSpace<Int32>() },
			{"vspace-int16", (cfg) => new VectorSpace<Int16>() },
			{"vspace-int8", (cfg) => new VectorSpace<SByte>() },
			{"vspace-uint32", (cfg) => new VectorSpace<UInt32>() },
			{"vspace-uint16", (cfg) => new VectorSpace<UInt16>() },
			{"vspace-uint8", (cfg) => new VectorSpace<Byte>() },
			{"sequence-float", (cfg) => new SequenceSpace<Single>() },
			{"sequence-single", (cfg) => new SequenceSpace<Single>() },
			{"sequence-double", (cfg) => new SequenceSpace<Double>() },
			{"sequence-int32", (cfg) => new SequenceSpace<Int32>() },
			{"sequence-int16", (cfg) => new SequenceSpace<Int16>() },
			{"sequence-int8", (cfg) => new SequenceSpace<SByte>() },
			{"sequence-uint32", (cfg) => new SequenceSpace<UInt32>() },
			{"sequence-uint16", (cfg) => new SequenceSpace<UInt16>() },
			{"sequence-uint8", (cfg) => new SequenceSpace<Byte>() },
			// {"sequence-string", () => new SequenceSpace<String>() },
			
			{"string-levenshtein", (cfg) => new StringSpace(StringDistance.Levenshtein) },
			{"string-edit", (cfg) => new StringSpace(StringDistance.Levenshtein) },
			{"string-hamming", (cfg) => new StringSpace(StringDistance.Hamming) },
			{"audio-space", (cfg) => new AudioSpace() },
			{"mbses", (cfg) => new BinaryHammingSpace() },
			{"binary", (cfg) => new BinaryHammingSpace() },
			{"binary-hamming", (cfg) => new BinaryHammingSpace() }
		};
		
		/// <summary>
		/// Save an space in the cache
		/// </summary>
		public static string Save (Space sp)
		{
			var fullPath = Path.GetFullPath (sp.Name);
			cache[fullPath] = sp;
			return fullPath;
		}
		
		/// <summary>
		/// Create an empty space from an space's class name
		/// </summary>
		
		public static Space Create (string spaceclass)
		{
			return Create (spaceclass, null);
		}
		public static Space Create (string spaceclass, IDictionary<string,object> config)
		{
			if (spaceclass == null) {
				throw new ArgumentNullException ("Space.Create spaceclass can't be null");
			}
			Func<IDictionary<string,object>,Space> sp;
			try {
				sp = SpaceFactory[spaceclass.ToLower ()];
			} catch (KeyNotFoundException e) {
				Console.WriteLine ("Available spaces:");
				foreach (string s in SpaceFactory.Keys) {
					Console.WriteLine ("Space's class: {0}", s);
				}
				throw e;
			}
			return sp (config);
		}
		
		/// <summary>
		/// Load an space using the given space's class name and the database name
		/// </summary>
		public static Space Load (string spaceclass, string dbname)
		{
			return Load (spaceclass, dbname, null, true);
		}
		public static Space Load (string spaceclass, string dbname, IDictionary<string, object> config)
		{
			return Load (spaceclass, dbname, config, true);
		}
		
		public static Space Load (string spaceclass, string dbname, IDictionary<string, object> config, bool saveCache)
		{
			var fullPath = Path.GetFullPath (dbname);
			if (cache.ContainsKey (fullPath)) {
				return cache [fullPath];
			}
			Space sp = Create (spaceclass, config);
			sp.Name = dbname;
			if (saveCache) {
				Save (sp);
			}
			return sp;
		}
		
		public static void RemoveFromCache (string dbname)
		{
			cache.Remove (Path.GetFullPath(dbname));
		}
		
		public static void RemoveAllFromCache ()
		{
			cache.Clear ();
		}
	}
}
