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
//   Original filename: natix/SimilaritySearch/QueryStream.cs
// 
using System;
//using NUnit.Framework;
using System.Xml;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;


namespace natix.SimilaritySearch
{
	/// <summary>
	/// The pair to be searched
	/// </summary>
	public struct CommandQuery
	{
		/// <summary>
		/// Query type. QType &gt; 0: radius (double), QType &lt; 0: abs(KNN) (int)
		/// </summary>
		public double QType;
		/// <summary>
		/// Query object in string representation
		/// </summary>
		public string QRaw;

		/// <summary>
		/// Constructor
		/// </summary>
		public CommandQuery (string qraw, double qtype)
		{
			this.QType = qtype;
			this.QRaw = qraw;
		}	
	}
	
	/// <summary>
	/// Query Reader
	/// </summary>
	public class QueryStream : IEnumerable<CommandQuery>, IEnumerator<CommandQuery>
	{
		string qname;
		StreamReader qfile;
		CommandQuery current;
		bool closeinput = false;
		static char[] sep = new char[] {','};
		
		/// <summary>
		/// Constructor from file (use "-" to read from standard input)
		/// </summary>
		public QueryStream (string qname)
		{
			this.qname = qname;
			((IEnumerator)this).Reset ();
		}
			
		/// <summary>
		/// Destructor
		/// </summary>
		~QueryStream ()
		{
			if (closeinput) {
				qfile.Close ();
				closeinput = false;
			}
		}
		
		IEnumerator<CommandQuery> IEnumerable<CommandQuery>.GetEnumerator ()
		{
			return this;
		}
		
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return this;
		}
		
		CommandQuery IEnumerator<CommandQuery>.Current {
			get {
				return this.current;
			}
		}
		object IEnumerator.Current {
			get { return this.current; }
		}

		void IEnumerator.Reset ()
		{
			if (this.closeinput) {
				this.qfile.Close ();
			}
			if (qname == "-") {
				qfile = (StreamReader)Console.In;
				closeinput = false;
			} else {
				qfile = new StreamReader (new MemoryStream (File.ReadAllBytes (qname)));
				closeinput = true;
			}
		}
		
		bool IEnumerator.MoveNext ()
		{
			if (this.qfile == null || this.qfile.EndOfStream) {
				return false;
			} else {
				string line = this.qfile.ReadLine ().Trim ();
				if (line == "-0") {
					this.qfile.Close ();
					this.qfile = null;
					this.closeinput = false;
					return false;
				}
				string[] s = line.Split (sep, 2);
				this.current = new CommandQuery (s[1], double.Parse(s[0]));
				return true;
			}
		}
		
		void IDisposable.Dispose ()
		{
			if (this.closeinput) {
				this.qfile.Close ();
				this.closeinput = false;
			}
		}
	}
	

}
