using System;

public class Heap<T> where T : IHeapItem<T>
{
    T[] items;
    int currentItemCount;

    /// <summary>
    /// Current count of items in the collection
    /// </summary>
    public int Count { get => this.currentItemCount; }

    public Heap(int maxHeapSize)
    {
        this.items = new T[maxHeapSize];
    }

    /// <summary>
    /// Add an item to the collection
    /// </summary>
    public void Add(T item)
    {
        item.HeapIndex = currentItemCount;
        items[currentItemCount] = item;
        
        this.SortUp(item);
        this.currentItemCount++;
    }

    /// <summary>
    /// Remove the first item in the collection and reorganise remaining items
    /// </summary>
    /// <returns>The first item in the collection</returns>
    public T Pop()
    {
        var firstItem = items[0];
        currentItemCount--;

        items[0] = items[currentItemCount];
        items[0].HeapIndex = 0;

        this.SortDown(items[0]);

        return firstItem;
    }

    /// <summary>
    /// Resort items after an item is updated
    /// </summary>
    /// <param name="item">The item which has changed</param>
    public void UpdateItem(T item)
    {
        this.SortUp(item);
    }

    /// <summary>
    /// Returns true if an item is contained in this collection
    /// </summary>
    /// <param name="item">The item to verify</param>
    public bool Contains(T item)
    {
        return Equals(items[item.HeapIndex], item);
    }

    /// <summary>
    /// Sort heap items by traversing up the tree from
    /// </summary>
    /// <param name="item">The item to sort</param>
    private void SortUp(T item)
    {
        int parentIndex = (item.HeapIndex - 1) / 2;
        
        while (true)
        {
            var parentItem = items[parentIndex];

            // If the value of the parent is greater than the current item, swap
            if (item.CompareTo(parentItem) > 0)
                this.Swap(item, parentItem);
            // Otherwise item is in the right position, return
            else
                return;

            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }

    /// <summary>
    /// Sort heap items by traversing down the tree
    /// </summary>
    /// <param name="item">The item to sort</param>
    private void SortDown(T item)
    {
        while (true)
        {
            int leftChildIndex = (item.HeapIndex * 2) + 1;
            int rightChildIndex = (item.HeapIndex * 2) + 2;

            // If left child index is within the bounds of the heap
            if (leftChildIndex < currentItemCount)
            {
                // Set the check index to left
                int swapIndex = leftChildIndex;
                // If right child index is within the bounds of the heap, and the value of the right child is less than the value of the left child
                // Set the check index to right
                if (rightChildIndex < currentItemCount && items[leftChildIndex].CompareTo(items[rightChildIndex]) < 0)
                    swapIndex = rightChildIndex;

                // If the value of the child is less than the value of the current item, swap the items
                if (item.CompareTo(items[swapIndex]) < 0)
                    this.Swap(item, items[swapIndex]);
                // Otherwise item is in right place, return
                else
                    return;
            }
            // Otherwise we are at the bottom of the tree, return
            else
                return;
        }
    }

    /// <summary>
    /// Swap two items and set their new HeapIndex values
    /// </summary>
    /// <param name="itemA">Item 1 to swap</param>
    /// <param name="itemB">Item 2 to swap</param>
    private void Swap(T itemA, T itemB)
    {
        items[itemA.HeapIndex] = itemB;
        items[itemB.HeapIndex] = itemA;

        var itemAIndex = itemA.HeapIndex;
        itemA.HeapIndex = itemB.HeapIndex;
        itemB.HeapIndex = itemAIndex;
    }
}

public interface IHeapItem<T> : IComparable<T>
{
    int HeapIndex { get; set; }
}
