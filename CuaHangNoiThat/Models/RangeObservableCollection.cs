using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CuaHangNoiThat.Models;

public sealed class RangeObservableCollection<T> : ObservableCollection<T>
{
    public void ReplaceRange(IEnumerable<T> items)
    {
        var replacement = items.ToArray();
        CheckReentrancy();
        Items.Clear();
        foreach (var item in replacement)
            Items.Add(item);

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
