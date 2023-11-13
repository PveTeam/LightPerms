using System.IO;
using NLog;
using PetaPoco;
using PetaPoco.Core.Inflection;
using PetaPoco.Providers;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;

namespace heh;

public interface IDbManager : IManager
{
    IDatabase Create(string name);
}
public class DbManager : Manager, IDbManager
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
    
#pragma warning disable CS0618
    public static readonly IDbManager Static = new DbManager(TorchBase.Instance);
#pragma warning restore CS0618

    public DbManager(ITorchBase torchInstance) : base(torchInstance)
    {
    }
    public IDatabase Create(string name)
    {
        return DatabaseConfiguration.Build()
            .UsingProvider<SQLiteDatabaseProvider>()
            .UsingExceptionThrown((_, args) => Log.Error(args.Exception))
            .WithAutoSelect()
            .UsingConnectionString($"Data Source={Path.Combine(Torch.Config.InstancePath, $"{name}.db")};Version=3;")
            .UsingDefaultMapper<ConventionMapper>(mapper =>
            {
                string UnFuckIt(IInflector inflector, string s) => inflector.Underscore(s).ToLower();
                mapper.InflectColumnName = UnFuckIt;
                mapper.InflectTableName = UnFuckIt;
            })
            .Create();
    }
}
