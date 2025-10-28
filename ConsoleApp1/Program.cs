using System.Collections;
using System.Runtime.InteropServices;

var collection = new CustomCollection<int>();
        
// Добавляем элементы
collection.Add(10);
collection.Add(20);
collection.Add(30);
collection.Add(40);
collection.Add(50); // Произойдет расширение памяти

Console.WriteLine($"Collection count: {collection.Count}");
Console.WriteLine($"Collection capacity: {collection.Capacity}");

Console.WriteLine("\nIterating with foreach:");
foreach (var item in collection)
{
    Console.WriteLine($"Item: {item}");
}

Console.WriteLine($"\nElement at index 2: {collection[2]}");
collection[2] = 300;
Console.WriteLine($"Modified element at index 2: {collection[2]}");
        

public class CustomCollection<T> : IEnumerable<T>, IDisposable
    where T : unmanaged
{
    private IntPtr _memory;
    private bool _disposed;

    public CustomCollection(int capacity = 4)
    {
        Capacity = capacity;
        Count = 0;
        _memory = Marshal.AllocHGlobal(Capacity * Marshal.SizeOf<T>());
        
        // Сообщаем GC о выделенной неуправляемой памяти
        GC.AddMemoryPressure(Capacity * Marshal.SizeOf<T>());
    }

    public int Count { get; private set; }

    public int Capacity { get; private set; }

    // Добавление элемента с расширением памяти
    public void Add(T item)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CustomCollection<T>));
            
        if (Count >= Capacity)
        {
            // Расширяем память в 2 раза
            var newCapacity = Capacity * 2;
            var newMemory = Marshal.AllocHGlobal(newCapacity * Marshal.SizeOf<T>());
            
            // Копируем старые данные в новую память
            for (var i = 0; i < Count; i++)
            {
                var value = GetElement(_memory, i);
                SetElement(newMemory, i, value);
            }
            
            // Освобождаем старую память и обновляем давление GC
            Marshal.FreeHGlobal(_memory);
            GC.RemoveMemoryPressure(Capacity * Marshal.SizeOf<T>());
            
            _memory = newMemory;
            Capacity = newCapacity;
            
            // Сообщаем GC о новой выделенной памяти
            GC.AddMemoryPressure(Capacity * Marshal.SizeOf<T>());
        }
        
        // Записываем новый элемент
        SetElement(_memory, Count, item);
        Count++;
    }

    // Индексатор
    public T this[int index]
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CustomCollection<T>));
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException("Index is out of range");
            return GetElement(_memory, index);
        }
        set
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CustomCollection<T>));
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException("Index is out of range");
            SetElement(_memory, index, value);
        }
    }

    // Вспомогательный метод для получения элемента
    private T GetElement(IntPtr memory, int index)
    {
        return Marshal.PtrToStructure<T>(memory + index * Marshal.SizeOf<T>());
    }

    // Вспомогательный метод для установки элемента
    private void SetElement(IntPtr memory, int index, T value)
    {
        Marshal.StructureToPtr(value, memory + index * Marshal.SizeOf<T>(), false);
    }

    // Реализация IEnumerable<T>
    public IEnumerator<T> GetEnumerator()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CustomCollection<T>));
        return new CustomEnumerator(this);
    }

    // Реализация IEnumerable
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    // Финализатор - будет вызван GC если Dispose не был вызван
    ~CustomCollection()
    {
        Dispose(false);
    }

    // Освобождение памяти (можно вызывать вручную, но не обязательно)
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this); // Отменяем вызов финализатора
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (_memory != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_memory);
            _memory = IntPtr.Zero;
                
            // Снимаем давление с GC
            GC.RemoveMemoryPressure(Capacity * Marshal.SizeOf<T>());
        }
        _disposed = true;
    }

    // Внутренний класс-перечислитель с работой через память
    private class CustomEnumerator(CustomCollection<T> collection) : IEnumerator<T>
    {
        private int _currentIndex = -1;
        private T _currentItem;

        public T Current
        {
            get
            {
                if (_currentIndex < 0 || _currentIndex >= collection.Count)
                    throw new InvalidOperationException("Enumerator is not started or finished");
                return _currentItem;
            }
        }
        
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (collection._disposed)
                throw new ObjectDisposedException(nameof(CustomCollection<T>));
                
            _currentIndex++;
            
            if (_currentIndex < collection.Count)
            {
                // Читаем элемент напрямую из памяти
                _currentItem = collection.GetElement(collection._memory, _currentIndex);
                return true;
            }
            
            _currentItem = default(T);
            return false;
        }

        public void Reset()
        {
            _currentIndex = -1;
            _currentItem = default(T);
        }

        public void Dispose()
        {
        }
        
    }
}