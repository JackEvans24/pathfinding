using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Heap<T> where T : IHeapItem<T>
{
    T[] items;
    int currentItemCount;

    public int Count { get => this.currentItemCount; }

    public Heap(int maxHeapSize)
    {
        this.items = new T[maxHeapSize];
    }

    public void Add(T item)
    {
        item.HeapIndex = currentItemCount;
        items[currentItemCount] = item;
        
        this.SortUp(item);
        this.currentItemCount++;
    }

    public T Pop()
    {
        var firstItem = items[0];
        currentItemCount--;

        items[0] = items[currentItemCount];
        items[0].HeapIndex = 0;

        this.SortDown(items[0]);

        return firstItem;
    }

    public void UpdateItem(T item)
    {
        this.SortUp(item);
    }

    public bool Contains(T item)
    {
        return Equals(items[item.HeapIndex], item);
    }

    private void SortUp(T item)
    {
        int parentIndex = (item.HeapIndex - 1) / 2;
        
        while (true)
        {
            var parentItem = items[parentIndex];

            if (item.CompareTo(parentItem) > 0)
                this.Swap(item, parentItem);
            else
                break;

            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }

    private void SortDown(T item)
    {
        while (true)
        {
            int leftChildIndex = (item.HeapIndex * 2) + 1;
            int rightChildIndex = (item.HeapIndex * 2) + 2;
            int swapIndex = 0;

            if (leftChildIndex < currentItemCount)
            {
                swapIndex = leftChildIndex;

                if (rightChildIndex < currentItemCount)
                {
                    if (items[leftChildIndex].CompareTo(items[rightChildIndex]) < 0)
                        swapIndex = rightChildIndex;
                }

                if (item.CompareTo(items[swapIndex]) < 0)
                    this.Swap(item, items[swapIndex]);
                else
                    return;
            }
            else
            {
                return;
            }
        }
    }

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
