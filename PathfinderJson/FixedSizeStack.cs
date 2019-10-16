using System;
using System.Collections.Generic;
using System.Text;

namespace PathfinderJson
{
    // source: http://ntsblog.homedev.com.au/index.php/2010/05/06/c-stack-with-maximum-limit/

    public class FixedSizeStack<T>
    {
        #region Fields/Constructor

        private int _limit;
        private LinkedList<T> _list;

        public FixedSizeStack(int maxSize)
        {
            _limit = maxSize;
            _list = new LinkedList<T>();
        }

        #endregion

        #region Public Stack Implementation

        public void Push(T value)
        {
            if (_list.Count == _limit)
            {
                _list.RemoveLast();
            }
            _list.AddFirst(value);
        }

        public T Pop()
        {
            if (_list.Count > 0)
            {
                T value = _list.First.Value;
                _list.RemoveFirst();
                return value;
            }
            else
            {
                throw new InvalidOperationException("The Stack is empty");
            }
        }

        public T Peek()
        {
            if (_list.Count > 0)
            {
                T value = _list.First.Value;
                return value;
            }
            else
            {
                throw new InvalidOperationException("The Stack is empty");
            }

        }

        public void Clear()
        {
            _list.Clear();
        }

        public int Count
        {
            get { return _list.Count; }
        }

        public int Limit
        {
            get { return _limit; }
        }

        /// <summary>
        /// Checks if the top object on the stack matches the value passed in
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsTop(T value)
        {
            bool result = false;
            if (this.Count > 0)
            {
                result = Peek().Equals(value);
            }
            return result;
        }

        public bool Contains(T value)
        {
            bool result = false;
            if (this.Count > 0)
            {
                result = _list.Contains(value);
            }
            return result;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public void CopyTo(T[] array, int index)
        {
            _list.CopyTo(array, index);
        }

        public T[] ToArray()
        {
            T[] array = new T[_list.Count];
            _list.CopyTo(array, 0);
            return array;
        }

        #endregion

    }
}
