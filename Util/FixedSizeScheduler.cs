// 
//  Copyright 2012  sadit
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace natix
{
	public class FixedSizeScheduler : TaskScheduler
	{
		// maybe later we will use all operations on the queue
		ConcurrentQueue<Task> queue;
		int counter = 0;
		
		public FixedSizeScheduler ()
		{
			this.queue = new ConcurrentQueue<Task> ();
		}
		
		protected override bool TryExecuteTaskInline (Task task, bool taskWasPreviouslyQueued)
		{
			return false;
		}
		
		protected override IEnumerable<Task> GetScheduledTasks ()
		{
			return this.queue;
		}
		
		protected override void QueueTask (Task task)
		{
			Console.WriteLine ("INSIDE FIXED SIZE SCHEDULER count: {0}", this.counter);
			ThreadStart doIt = delegate() {
				TryExecuteTask (task);
				lock (this.queue) {
					--this.counter;
					Monitor.Pulse (this.queue);
				}				
			};
			//lock (this) {
				++this.counter;
			//}
			if (this.counter > Environment.ProcessorCount) {
				lock (this.queue) {
					var t = new Thread (doIt);
					t.IsBackground = true;
					t.Start ();
				}
			} else {
				var t = new Thread (doIt);
				t.IsBackground = true;
				t.Start ();
			}
		}
	}
}

