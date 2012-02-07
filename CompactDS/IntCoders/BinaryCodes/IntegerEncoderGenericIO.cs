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
//   Original filename: natix/natix/CompactDS/IntCoders/BinaryCodes/IntegerEncoderGenericIO.cs
// 
using System;
using System.IO;
using System.Collections.Generic;

namespace natix.CompactDS
{
	/// <summary>
	/// Save/Load IIntegerEncoder objects
	/// </summary>
	public class IntegerEncoderGenericIO
	{
		public static List<Type> Catalog = new List<Type>() {
			typeof(BinarySearchCoding),
			typeof(DoublingSearchCoding),
			typeof(UltimateSearchCoding),
			typeof(BinaryCoding),
			typeof(BlockCoding),
			typeof(EliasDelta),
			typeof(EliasGamma),
			typeof(HuffmanCoding),
			typeof(RiceCoding),
			typeof(UnaryCoding),
			typeof(ZeroCoding),
			typeof(EliasGamma32)
		};
		
		/// <summary>
		/// Saves "coder" to the binary file "Output"
		/// </summary>
		public static void Save (BinaryWriter Output, IIntegerEncoder coder)
		{
			var type = coder.GetType ();
			byte idType = 255;
			for (byte i = 0; i < Catalog.Count; i++) {
				if (type == Catalog [i]) {
					idType = i;
					break;
				}
			}
			if (idType == 255) {
				var s = String.Format ("Type {0} is not a recognized IIntegerEncoder, please add it to " +
					"IntegerEncoderGenericIO.Catalog", type);
				throw new ArgumentException (s);
			}
			Output.Write (idType);
			coder.Save (Output);
		}

		/// <summary>
		/// Loads a "coder" from the binary file "Input"
		/// </summary>

		public static IIntegerEncoder Load (BinaryReader Input)
		{
			byte idType = Input.ReadByte ();
			var type = Catalog[idType];
			if (type == null) {
				var s = String.Format ("IntegerEncoderGenericIO.Catalog returned null using idType: {0}," +
					"is it deprecated?", idType);
				throw new ArgumentNullException (s);
			}
			var coder = (IIntegerEncoder)Activator.CreateInstance (type);
			coder.Load (Input);
			return coder;
		}
		
	}
}

