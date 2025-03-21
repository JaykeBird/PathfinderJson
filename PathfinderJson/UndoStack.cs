﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace PathfinderJson
{
    public class UndoStack<T>
    {
        /// <summary>
        /// Create a new undo stack with the default size of 30 undo items (can be changed later).
        /// </summary>
        public UndoStack() { }

        /// <summary>
        /// Create a new undo stack with a size pre-defined.
        /// </summary>
        /// <param name="size">The size of the undo stack to set.</param>
        public UndoStack(uint size)
        {
            undoSize = size;
        }

        private uint undoSize = 30;
        private List<T> undoList = new List<T>();
        private List<T> redoList = new List<T>();

        /// <summary>
        /// Count the number of items in the undo stack. Note that this may not 100% match how many undo actions are possible.
        /// </summary>
        public int UndoCount { get => undoList.Count; }

        /// <summary>
        /// Count the number of items in the redo stack. Note that this may not 100% match how many redo actions are possible.
        /// </summary>
        public int RedoCount { get => redoList.Count; }

        /// <summary>
        /// Get or set the size limit of the undo stack. If more than this number of actions are stored for undoing, the oldest actions are deleted out.
        /// </summary>
        /// <remarks>
        /// If you set the size limit to a lower number than previously, the undo stack will go ahead and delete any undo items that are beyond the new limit.
        /// </remarks>
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

        /// <summary>
        /// Get if the undo action is currently possible.
        /// </summary>
        public bool CanUndo
        {
            get
            {
                return SkipCurrentStateOnUndo && !isUndoing ? undoList.Count > 1 : undoList.Count > 0;
            }
        }

        /// <summary>
        /// Get if the redo action is currently possible.
        /// </summary>
        public bool CanRedo
        {
            get
            {
                return SkipCurrentStateOnUndo && !isRedoing ? redoList.Count > 1 : redoList.Count > 0;
            }
        }

        bool isUndoing = false;
        bool isRedoing = false;

        /// <summary>
        /// Store the current state of the item. Stored states can be undone back to later. Storing a state will clear the redo stack and set it as it is now.
        /// </summary>
        /// <param name="state"></param>
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

        public T? TryPeekUndo()
        {
            if (undoList.Count > 0)
            {
                return undoList[^1];
            }
            else
            {
                return default;
            }
        }

        public T? TryPeekRedo()
        {
            if (redoList.Count > 0)
            {
                return redoList[^1];
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Clear the undo and redo list. This removes all earlier and later states, only leaving what is current.
        /// </summary>
        public void Clear()
        {
            undoList.Clear();
            redoList.Clear();
        }

        public bool SkipCurrentStateOnUndo { get; set; } = true;

        /// <summary>
        /// Get the undo state of item <typeparamref name="T"/> prior to the current state.
        /// </summary>
        /// <returns>A copy of the object in the state prior to the current state.</returns>
        /// <remarks>Use <see cref="CanUndo"/> to check if there are undo states available.</remarks>
        /// <exception cref="InvalidOperationException">There are no undo states available.</exception>
        public T Undo()
        {
            return Undo(true);
        }

        /// <summary>
        /// Get the undo state of item <typeparamref name="T"/> prior to the current state.
        /// </summary>
        /// <param name="skipCurrentState">set if the current state should be skipped when getting an undo state; if false, the current state may be returned</param>
        /// <returns>A copy of the object in the state prior to the current state.</returns>
        /// <remarks>Use <see cref="CanUndo"/> to check if there are undo states available.</remarks>
        /// <exception cref="InvalidOperationException">There are no undo states available.</exception>
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

        public ReadOnlyCollection<T> UndoItems
        {
            get { return undoList.AsReadOnly(); }
        }

        public ReadOnlyCollection<T> RedoItems
        {
            get { return redoList.AsReadOnly(); }
        }
    }
}
