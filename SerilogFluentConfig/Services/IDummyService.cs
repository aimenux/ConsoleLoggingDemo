using System.Threading.Tasks;

namespace SerilogFluentConfig.Services;

public interface IDummyService
{
    Task DoNothingAsync();
}