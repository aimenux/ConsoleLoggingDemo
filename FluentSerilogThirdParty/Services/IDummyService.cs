using System.Threading.Tasks;

namespace FluentSerilogThirdParty.Services;

public interface IDummyService
{
    Task DoNothingAsync();
}