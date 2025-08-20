using Microsoft.EntityFrameworkCore;
using ProvaPub.Repository;
using ProvaPub.Services;
using ProvaPub.Services.Payments;
using ProvaPub.Services.Time;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<TestDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ctx")));

// RandomService deve ser Scoped (mesmo ciclo de vida do DbContext)
builder.Services.AddScoped<RandomService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IDateTimeProvider, UtcDateTimeProvider>();

// Estratégias de pagamento (Strategy)
builder.Services.AddScoped<IPaymentProcessor, PixPaymentProcessor>();
builder.Services.AddScoped<IPaymentProcessor, CreditCardPaymentProcessor>();
builder.Services.AddScoped<IPaymentProcessor, PaypalPaymentProcessor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
