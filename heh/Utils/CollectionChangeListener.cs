using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
namespace heh.Utils;

public class CollectionChangeListener : ChangeListener
{
        #region *** Members ***
    private readonly INotifyCollectionChanged _value;
    private readonly Dictionary<INotifyPropertyChanged, ChangeListener> _collectionListeners = new();
        #endregion


        #region *** Constructors ***
    public CollectionChangeListener(INotifyCollectionChanged collection, string? propertyName)
    {
        _value = collection;
        PropertyName = propertyName;

        if (_value.GetType().IsGenericType && !typeof(INotifyPropertyChanged).IsAssignableFrom(_value.GetType().GetGenericArguments()[0]))
            return;
        Subscribe();
    }
        #endregion


        #region *** Private Methods ***
    private void Subscribe()
    {
        _value.CollectionChanged += value_CollectionChanged;

        foreach (INotifyPropertyChanged item in (IEnumerable)_value)
        {
            ResetChildListener(item);
        }
    }

    private void ResetChildListener(INotifyPropertyChanged item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        RemoveItem(item);

        var listener = Create(item)!;

        listener.PropertyChanged += listener_PropertyChanged;
        _collectionListeners.Add(item, listener);
    }

    private void RemoveItem(INotifyPropertyChanged item)
    {
        // Remove old
        if (!_collectionListeners.ContainsKey(item))
            return;
        _collectionListeners[item].PropertyChanged -= listener_PropertyChanged;

        _collectionListeners[item].Dispose();
        _collectionListeners.Remove(item);
    }


    private void ClearCollection()
    {
        foreach (var key in _collectionListeners.Keys)
        {
            _collectionListeners[key].Dispose();
        }

        _collectionListeners.Clear();
    }
        #endregion


        #region *** Event handlers ***
    private void value_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            ClearCollection();
            return;
        }
            
        // Don't care about e.Action, if there are old items, Remove them...
        if (e.OldItems != null)
        {
            foreach (INotifyPropertyChanged item in e.OldItems)
                RemoveItem(item);
        }

        // ...add new items as well
        if (e.NewItems != null)
        {
            foreach (INotifyPropertyChanged item in e.NewItems)
                ResetChildListener(item);
        }
    }


    private void listener_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // ...then, notify about it
        RaisePropertyChanged($"{PropertyName}{(PropertyName != null ? "[]." : null)}{e.PropertyName}");
    }
        #endregion


        #region *** Overrides ***
    /// <summary>
    /// Releases all collection item handlers and self handler
    /// </summary>
    protected override void Unsubscribe()
    {
        ClearCollection();

        _value.CollectionChanged -= value_CollectionChanged;

        Debug.WriteLine("CollectionChangeListener unsubscribed");
    }
        #endregion
}