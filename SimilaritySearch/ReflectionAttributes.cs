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
//   Original filename: natix/SimilaritySearch/ReflectionAttributes.cs
// 
using System;

namespace natix.SimilaritySearch
{
	/// <summary>
	/// Index attribute for reflection capabilities. It should mark index classes
	/// </summary>
	public class IndexAttribute : Attribute
	{
		public string shortName;
		public IndexAttribute (string shortName)
		{
			this.shortName = shortName;
		}
	}
	
	/// <summary>
	/// Space attribute for reflection capabilities. It should mark spaces classes
	/// </summary>
	public class SpaceAttribute : Attribute
	{
		public string shortName;
		public SpaceAttribute (string shortName)
		{
			this.shortName = shortName;
		}
	}
	
}
