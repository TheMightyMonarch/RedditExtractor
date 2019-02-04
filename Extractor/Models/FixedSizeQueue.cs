using System.Collections.Generic;

namespace Extractor.Models
{
    // A fun wrapper class pulled partially off of StackOverflow
    public class FixedSizeQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private short _size { get; set; }

        public int Count
        {
            get { return _queue.Count; }
        }

        public FixedSizeQueue(short size)
        {
            _size = size;
        }

        public void Enqueue(T value)
        {
            _queue.Enqueue(value);

            while (_queue.Count > _size)
            {
                _queue.Dequeue();
            }
        }

        public void Clear()
        {
            _queue.Clear();
        }

        public T[] ToArray()
        {
            return _queue.ToArray();
        }
    }
}
