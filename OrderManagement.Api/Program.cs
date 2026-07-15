using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Events;
using OrderManagement.Domain.Orders;
using OrderManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// --- Composition root: this is the ONE place that knows about every layer.
// Domain knows about nothing. Application knows about Domain only.
// Infrastructure implements Domain's interfaces. Api wires it all together.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")
        ?? "Server=localhost\\SQLEXPRESS;Database=OrderManagement;Trusted_Connection=True;TrustServerCertificate=True;"));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<OrderService>();

// Registers IDomainEventDispatcher plus every IDomainEventHandler<T> found in
// the Application assembly (see OrderManagement.Application/Orders/EventHandlers).
builder.Services.AddDomainEventHandling(typeof(OrderService).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
