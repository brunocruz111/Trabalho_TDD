using System.Threading.Tasks;

namespace Gamification.Domain.Ports
{
    public interface IAwardsUnitOfWork
    {
        Task CompleteAsync();
    }
}