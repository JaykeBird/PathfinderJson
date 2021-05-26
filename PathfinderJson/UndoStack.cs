using System;
using System.Collections.Generic;
using System.Text;

namespace PathfinderJson
{
    public class UndoStack<T>
    {
        private uint undoSize = 30;
        private List<T> undoList = new List<T>();
        private List<T> redoList = new List<T>();

        public int UndoCount { get => undoList.Count; }
        public int RedoCount { get => redoList.Count; }

        public uint UndoSizeLimit
        {
            get
            {
                return undoSize;
            }
            set
            {
                uint oldSize = undoSize;
                undoSize = value;
                if (undoSize < oldSize)
                {
                    while (undoList.Count > undoSize)
                    {
                        undoList.RemoveAt(0);
                    }
                }
            }
        }

        public bool CanUndo
        {
            get
            {
                if (SkipCurrentStateOnUndo && !isUndoing) return undoList.Count > 1;
                else return undoList.Count > 0;
            }
        }
        public bool CanRedo
        {
            get
            {
                if (SkipCurrentStateOnUndo && !isRedoing) return redoList.Count > 1;
                else return redoList.Count > 0;
            }
        }

        bool isUndoing = false;
        bool isRedoing = false;

        public void StoreState(T state)
        {
            undoList.Add(state);

            while (undoList.Count > undoSize)
            {
                undoList.RemoveAt(0);
            }

            redoList.Clear();
            isUndoing = false;
            isRedoing = false;
        }

        public T PeekUndo()
        {
            if (undoList.Count > 0)
            {
                return undoList[^1];
            }
            else
            {
                throw new InvalidOperationException("There are no available undo states.");
            }
        }

        public T PeekRedo()
        {
            if (redoList.Count > 0)
            {
                return redoList[^1];
            }
            else
            {
                throw new InvalidOperationException("There are no available redo states.");
            }
        }

        public bool SkipCurrentStateOnUndo { get; set; } = true;

        public T Undo()
        {
            return Undo(true);
        }

        public T Undo(bool skipCurrentState)
        {
            if (undoList.Count > 0)
            {
                if ((SkipCurrentStateOnUndo || skipCurrentState) && !isUndoing)
                {
                    T pval = undoList[^1];
                    redoList.Add(pval);
                    undoList.RemoveAt(undoList.Count - 1);
                    if (undoList.Count > 0)
                    {
                        T value = undoList[^1];
                        redoList.Add(value);
                        undoList.RemoveAt(undoList.Count - 1);
                        isUndoing = true;
                        isRedoing = false;
                        return value;
                    }
                    else
                    {
                        throw new InvalidOperationException("There are no available undo states.");
                    }
                }
                else
                {
                    T value = undoList[^1];
                    redoList.Add(value);
                    undoList.RemoveAt(undoList.Count - 1);
                    isUndoing = true;
                    isRedoing = false;
                    return value;
                }
            }
            else
            {
                throw new InvalidOperationException("There are no available undo states.");
            }
        }

        public T Redo()
        {
            return Redo(true);
        }

        public T Redo(bool skipCurrentState)
        {
            if (redoList.Count > 0)
            {
                if ((SkipCurrentStateOnUndo || skipCurrentState) && !isRedoing)
                {
                    T pval = redoList[^1];
                    undoList.Add(pval);
                    redoList.RemoveAt(redoList.Count - 1);
                    if (redoList.Count > 0)
                    {
                        T value = redoList[^1];
                        undoList.Add(value);
                        redoList.RemoveAt(redoList.Count - 1);
                        isRedoing = true;
                        isUndoing = false;
                        return value;
                    }
                    else
                    {
                        throw new InvalidOperationException("There are no available redo states.");
                    }
                }
                else
                {
                    T value = redoList[^1];
                    undoList.Add(value);
                    redoList.RemoveAt(redoList.Count - 1);
                    isRedoing = true;
                    isUndoing = false;
                    return value;
                }
            }
            else
            {
                throw new InvalidOperationException("There are no available redo states.");
            }
        }

    }
}
