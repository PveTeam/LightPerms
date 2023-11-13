using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NLog;
using Torch;
namespace heh.Utils;

/// <summary>
/// Simple class that manages saving <see cref="Persistent{T}.Data"/> to disk using XML serialization.
/// Will automatically save on changes in the data class.
/// </summary>
/// <typeparam name="TViewModel">Data class type</typeparam>
public sealed class ProperPersistent<TViewModel> : IDisposable where TViewModel : class, INotifyPropertyChanged, new()
{
    private static readonly XmlSerializer Serializer = new(typeof(TViewModel));
    private static readonly ILogger Log = LogManager.GetLogger($"ProperPersistent_{typeof(TViewModel)}");

    private readonly ChangeListener _listener;
    private Timer? _saveConfigTimer;
    
    public TViewModel Data { get; }
    public string Path { get; set; }
    
    public ProperPersistent(string path, TViewModel? defaultViewModel = default)
    {
        Path = path;
        if (File.Exists(path))
        {
            try
            {
                using var stream = File.OpenRead(path);
                Data = (TViewModel) Serializer.Deserialize(stream);
            }
            catch (Exception e)
            {
                Log.Error(e);
                Data = defaultViewModel ?? new TViewModel();
            }
        }
        else
        {
            Data = defaultViewModel ?? new TViewModel();
            Save();
        }

        _listener = ChangeListener.Create(Data)!;
        _listener.PropertyChanged += ListenerOnPropertyChanged;
    }
    private void ListenerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        SaveAsync();
    }

    private void SaveAsync()
    {
        _saveConfigTimer ??= new(_ => Save());
        _saveConfigTimer.Change(1000, -1);
    }
    public void Dispose()
    {
        _listener.Dispose();
        _saveConfigTimer?.Dispose();
        _saveConfigTimer = null;
    }
#region Backwards compatibility
    public void Save(string? newPath = null)
    {
        if (newPath is not null)
            Path = newPath;
        
        using var stream = File.Create(Path);
        using var writer = new XmlTextWriter(stream, Encoding.UTF8)
        {
            Formatting = Formatting.Indented
        };
        
        Serializer.Serialize(writer, Data);
    }

    public static ProperPersistent<TViewModel> Load(string path, bool saveIfNew = true) => new(path);
#endregion
}
