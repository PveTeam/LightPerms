using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
namespace heh.Utils;

public class ChildChangeListener : ChangeListener
{
        #region *** Members ***
    protected static readonly Type InotifyType = typeof(INotifyPropertyChanged);

    private readonly INotifyPropertyChanged _value;
    private readonly Type _type;
    private readonly Dictionary<string?, ChangeListener?> _childListeners = new();
        #endregion


        #region *** Constructors ***
    public ChildChangeListener(INotifyPropertyChanged instance)
    {
        _value = instance ?? throw new ArgumentNullException(nameof(instance));
        _type = _value.GetType();

        Subscribe();
    }

    public ChildChangeListener(INotifyPropertyChanged instance, string? propertyName)
        : this(instance)
    {
        PropertyName = propertyName;
    }
        #endregion


        #region *** Private Methods ***
    private void Subscribe()
    {
        _value.PropertyChanged += value_PropertyChanged;

        var query =
            from property
                in _type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            where InotifyType.IsAssignableFrom(property.PropertyType)
            select property;

        foreach (var property in query)
        {
            // Declare property as known "Child", then register it
            _childListeners.Add(property.Name, null);
            ResetChildListener(property.Name);
        }
    }


    /// <summary>
    /// Resets known (must exist in children collection) child event handlers
    /// </summary>
    /// <param name="propertyName">Name of known child property</param>
    private void ResetChildListener(string? propertyName)
    {
        if (propertyName is null || !_childListeners.TryGetValue(propertyName, out var childListener))
            return;
            
        // Unsubscribe if existing
        if (childListener != null)
        {
            childListener.PropertyChanged -= child_PropertyChanged;

            // Should unsubscribe all events
            childListener.Dispose();
            _childListeners.Remove(propertyName);
        }

        var property = _type.GetProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"Was unable to get '{propertyName}' property information from Type '{_type.Name}'");

        var newValue = property.GetValue(_value, null);

        if (newValue is not null)
            _childListeners[propertyName] = Create(newValue, propertyName);

        if (_childListeners[propertyName] != null)
            _childListeners[propertyName]!.PropertyChanged += child_PropertyChanged;
    }
        #endregion


        #region *** Event Handler ***
    private void child_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        RaisePropertyChanged(e.PropertyName);
    }

    private void value_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // First, reset child on change, if required...
        ResetChildListener(e.PropertyName);

        // ...then, notify about it
        RaisePropertyChanged(e.PropertyName);
    }

    protected override void RaisePropertyChanged(string propertyName)
    {
        // Special Formatting
        base.RaisePropertyChanged($"{PropertyName}{(PropertyName != null ? "." : null)}{propertyName}");
    }
        #endregion


        #region *** Overrides ***
    /// <summary>
    /// Release all child handlers and self handler
    /// </summary>
    protected override void Unsubscribe()
    {
        _value.PropertyChanged -= value_PropertyChanged;

        foreach (var kv in _childListeners)
        {
            kv.Value?.Dispose();
        }

        _childListeners.Clear();

        Debug.WriteLine("ChildChangeListener '{0}' unsubscribed", PropertyName);
    }
        #endregion
}