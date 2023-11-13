using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using NLog;
namespace heh.Utils;

public abstract class ChangeListener : INotifyPropertyChanged, IDisposable
{
        #region *** Members ***
    protected static readonly ILogger Log = LogManager.GetCurrentClassLogger();
    protected string? PropertyName;
        #endregion


        #region *** Abstract Members ***
    protected abstract void Unsubscribe();
        #endregion


        #region *** INotifyPropertyChanged Members and Invoker ***
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void RaisePropertyChanged(string propertyName)
    {
        var temp = PropertyChanged;
        temp?.Invoke(this, new(propertyName));
    }
        #endregion


        #region *** Disposable Pattern ***

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Unsubscribe();
        }
    }

    ~ChangeListener()
    {
        Dispose(false);
    }

        #endregion


        #region *** Factory ***
    public static ChangeListener? Create(object value, string? propertyName = null)
    {
        switch (value)
        {
            case INotifyCollectionChanged collectionChanged and IEnumerable:
                return new CollectionChangeListener(collectionChanged, propertyName);
            case INotifyPropertyChanged propertyChanged:
                return new ChildChangeListener(propertyChanged, propertyName);
            default:
                Log.Warn("changes in {0} type cannot be watched", value.GetType().FullName);
                return null;
        }

    }
        #endregion
}