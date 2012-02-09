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
//   Original filename: natix/natix/SimilaritySearch/Containers/CompressedOrderedLists.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using natix.CompactDS;
using natix.SortingSearching;

namespace natix.SimilaritySearch
{
	public class CompressedOrderedList : ListContainerBase<int>
	{
		IList<int> bitoffsets;
		IList<int> lenoffsets;
		BitStream32 bstream;
		IIEncoder32 encoder;
		// static INumericManager<T> num = (INumericManager<T>)NumericManager.Get (typeof(T));
		
		public CompressedOrderedList (string basename, int length)
		{
			Console.WriteLine("===> Opening compressed integer ordered list {0}", basename);
			this.bitoffsets = PrimitiveIO<int>.ReadAllFromFile (basename + ".bitoffsets");
			this.lenoffsets = PrimitiveIO<int>.ReadAllFromFile (basename + ".lenoffsets");
			var s = PrimitiveIO<uint>.ReadAllFromFile (basename);
			this.bstream = new BitStream32 (s);
			this.encoder = new EliasDelta ();
		}
		
		public override int GetLengthAtIndex (int i)
		{
			if (i == 0) {
				return this.lenoffsets[0];
			} else {
				return this.lenoffsets[i] - this.lenoffsets[i-1];
			}
		}
		
		public override int Count {
			get {
				return this.bitoffsets.Count;
			}
		}

		public override IList<int> GetList (int StartIndex, int _Len)
		{
			// Console.WriteLine ("GetList: Start: {0} Len: {1}", StartIndex, Len);
			int L = this.GetLengthAtIndex (StartIndex);
			var V = new int[L];
			if (_Len != L) {
				throw new ArgumentOutOfRangeException (
					"The length of the requested sequence must match with the stored sequence");
			}
			BitStreamCtx ctx = new BitStreamCtx ();
			if (StartIndex == 0) {
				ctx.Seek (0);
			} else {
				ctx.Seek (this.bitoffsets[StartIndex - 1]);
			}
			for (int i = 0; i < L; i++) {
				V[i] = this.encoder.Decode (this.bstream, ctx);
			}
			return V;
		}
		
		public override void Add (IList<int> item)
		{
			throw new NotSupportedException ("Read only ListContainer");
		}
	}
}

