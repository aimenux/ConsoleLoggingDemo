using System.Threading.Tasks;

namespace SerilogJsonConfig.Services;

public interface IDummyService
{
    Task DoNothingAsync();
}