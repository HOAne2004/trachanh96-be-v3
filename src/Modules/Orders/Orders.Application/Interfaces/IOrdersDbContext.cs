using Microsoft.EntityFrameworkCore;
using Orders.Domain.Entities; 

namespace Orders.Application.Interfaces;

public interface IOrdersDbContext
{
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
}