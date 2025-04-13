using DataAccess.Models;
using System.Threading.Tasks;

namespace DataAccess.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<bool> ProcessPaymentAsync(Order order);
    }
}
