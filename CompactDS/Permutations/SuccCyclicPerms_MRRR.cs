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
//   Original filename: natix/natix/CompactDS/Permutations/SuccCyclicPerms_MRRR.cs
// 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace natix.CompactDS
{
	public class SuccCyclicPerms_MRRR : CyclicPerms_MRRR
	{
		public SuccCyclicPerms_MRRR () : base()
		{
		}
		
		public SuccCyclicPerms_MRRR (IList<int> perm, int t) : base (perm, t)
		{
		}
		
		protected override void FinishBuild (object arg)
		{
			int maxvalue = this.PERM.Count - 1;
			this.PERM = this.BuildSuccList (this.PERM, maxvalue);
			this.BACK = this.BuildSuccList (this.BACK, maxvalue);
		}
		
		protected virtual IList<int> BuildSuccList (IList<int> list, int maxvalue)
		{
			var L = new ListIFS ((int)Math.Ceiling (Math.Log (maxvalue + 1, 2)));
			foreach (var u in list) {
				L.Add (u);
			}
			return L;
		}
	}
}

