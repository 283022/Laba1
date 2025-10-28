using System.Collections;

var check = new CustomCollection<double>(4);
check.AddElement(0,1);
check.AddElement(1,2);
check.AddElement(2,3); 
check.AddElement(3,4);

IEnumerator check2 = check.GetEnumerator();
var s = check2.Current;

var check3 = check.GetEnumerator();
var s2 = check3.Current;

//комментарий
foreach (var item in check)
{
    Console.WriteLine(item.GetType());
}


public class CustomCollection<T>(int size) : IEnumerable<T>
{
    private readonly T[] _array = new T[size];

    public void AddElement(int index,T value)
    {
        _array[index] = value;
    }

    
    public IEnumerator<T> GetEnumerator()
    {
        return new CustomEnumerator<T>(_array);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class CustomEnumerator<T>: IEnumerator<T>
{
    private int _currentindex = -1;
    private readonly T[] _array;

    public CustomEnumerator(T[] array)
    {
        _array = new T[array.Length];
        Array.Copy(array, _array, array.Length);
    }

    public T Current
    {
        get
        {
            if (_currentindex < 0 || _currentindex >= _array.Length)
                throw new InvalidOperationException();
            return _array[_currentindex];
        }
    } 
    object? IEnumerator.Current => Current;
    
    public bool MoveNext()
    {
        _currentindex++;
        return _currentindex < _array.Length;
    }

    public void Reset()
    {
        _currentindex = -1;
    }

    public void Dispose(){}
}