using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUI
{

    public class PriorityQueue<TPriority, TValue>
    {
        private SortedDictionary<TPriority, Queue<TValue>> dict;

        public PriorityQueue()
        {
            dict = new SortedDictionary<TPriority, Queue<TValue>>();
        }

        public void Enqueue(TPriority priority, TValue value)
        {
            if (!dict.ContainsKey(priority))
            {
                dict[priority] = new Queue<TValue>();
            }
            dict[priority].Enqueue(value);
        }

        public TValue Dequeue()
        {
            if (dict.Count == 0)
            {
                throw new InvalidOperationException("The priority queue is empty");
            }
            var first = dict.First();
            var value = first.Value.Dequeue();
            if (first.Value.Count == 0)
            {
                dict.Remove(first.Key);
            }
            return value;
        }

        public TValue Peek()
        {
            if (dict.Count == 0)
            {
                throw new InvalidOperationException("The priority queue is empty");
            }
            return dict.First().Value.Peek();
        }

        public bool IsEmpty()
        {
            return dict.Count == 0;
        }
    }

}
