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
//   Original filename: natix/natix/SimilaritySearch/Containers/DiskListContainer.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// This file is for read-only containers. It must be replaced for the new a memory mapped file in .NET 4.
	/// It will work for implementations of INumericManager
	/// </summary>
	public class DiskListContainer<T> : ListContainerBase<T>
	{
		IList<int> offsets;
		// BinaryReader infile;
		Stream infile;
		INumeric<T> num;
		long Tsize;
		
		public DiskListContainer (string filename, IList<int> offsets)
		{
			// this.infile = new BinaryReader (File.OpenRead (filename));
			// this.infile = new BufferedStream (File.OpenRead (filename), 1 << 20);
			this.infile = File.OpenRead (filename);
			// this.infile = new FileStream (filename, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 22);
			this.offsets = offsets;
			this.num = (INumeric<T>)Numeric.Get (typeof(T));
			this.Tsize = this.num.SizeOf ();
		}
		
		~DiskListContainer ()
		{
			this.infile.Close ();
		}
		
		public override int GetLengthAtIndex (int i)
		{
			if (i == 0) {
				return this.offsets[0];
			} else {
				// Console.WriteLine ("-----------------------= i: {0}, offsets.Count: {1}", i, offsets.Count);
				return this.offsets[i] - this.offsets[i-1];
			}
		}
		
		public override int Count {
			get {
				return this.offsets.Count;
			}
		}

		/*public override IList<T> GetList (int StartIndex, int Len)
		{
			// Console.WriteLine ("GetList: Start: {0} Len: {1}", StartIndex, Len);
			int D = this.GetLengthAtIndex (StartIndex);
			var V = new T[D];
			var offsetItems = 0;
			if (StartIndex > 0) {
				offsetItems = this.offsets[StartIndex - 1];
			}
			
			this.infile.BaseStream.Seek (offsetItems * this.Tsize, SeekOrigin.Begin);
			for (int i = 0; i < Len; i++) {
				V[i] = this.num.ReadBinary(this.infile);
				// this.num.ReadBinaryVector (V, this.infile, Len);
			}
			return V;
		}*/

		public override IList<T> GetList (int StartIndex, int Len)
		{
			// Console.WriteLine ("GetList: Start: {0} Len: {1}", StartIndex, Len);
			int L = this.GetLengthAtIndex (StartIndex);
			var V = new T[L];
			var offsetItems = 0;
			if (StartIndex > 0) {
				offsetItems = this.offsets[StartIndex - 1];
			}
			lock (this.infile) {
				this.infile.Seek (offsetItems * this.Tsize, SeekOrigin.Begin);
				this.num.ReadBinaryVector (V, this.infile, Len);
			}
			return V;
		}
		
		public override void Add (IList<T> item)
		{
			throw new NotSupportedException ("Read only ListContainer");
		}
	}
}

