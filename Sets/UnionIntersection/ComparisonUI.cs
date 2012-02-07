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
//   Original filename: natix/natix/Sets/UnionIntersection/ComparisonUI.cs
// 
using System;
using System.Collections.Generic;

namespace natix.Sets
{
	public class ComparisonUI : IUnionIntersection
	{
		IIntersectionAlgorithm ialg;
		
		public ComparisonUI (IIntersectionAlgorithm alg)
		{
			this.ialg = alg;
		}
		
		public IList<int> ComputeUI (IList<IList<IList<int>>> sets)
		{
			var L = new IList<int>[sets.Count];
			int i = 0;
			foreach (var alist in sets) {
				L [i] = this.Union (alist);
				i++;
			}
			var u = this.ialg.Intersection (L);
			var uL = u as IList<int>;
			if (uL != null) {
				return uL;
			}
			return new List<int> (u);
		}
		
		IList<int> Union (IList<IList<int>> disjoint_sets)
		{
			HashSet<int > S = new HashSet<int> ();
			foreach (var list in disjoint_sets) {
				foreach (var item in list) {
					S.Add (item);
				}
			}
			var L = new int[S.Count];
			int i = 0;
			foreach (var item in S) {
				L [i] = item;
				i++;
			}
			Array.Sort (L);
			return L;
		}
	}
}
