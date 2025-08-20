using Microsoft.EntityFrameworkCore;
using ProvaPub.Models;
using ProvaPub.Repository;
using ProvaPub.Services;
using ProvaPub.Services.Time;
using Xunit;

namespace ProvaPub.Tests
{
    public class CustomerServiceTests
    {
        private class FakeClock : IDateTimeProvider
        {
            public DateTime UtcNow { get; set; }
        }

        private static Order MakePaidOrder(int customerId, DateTime orderDateUtc, decimal value = 50m, int? id = null)
            => new Order
            {
                Id = id ?? 0,
                CustomerId = customerId,
                Value = value,
                OrderDate = orderDateUtc,
                PaymentMethod = "test",
                PaymentProvider = "TestProvider",
                PaymentStatus = "APPROVED",
                PaymentTransactionId = Guid.NewGuid().ToString("N")
            };

        private TestDbContext CreateDb(out FakeClock clock, DateTime? nowUtc = null)
        {
            clock = new FakeClock { UtcNow = nowUtc ?? new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc) }; // quarta 10:00 UTC
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;
            var ctx = new TestDbContext(options);

            // Seed básico de clientes (usamos 1 e 2; novos testes usam 3..7 e 1000)
            ctx.Customers.AddRange(
                new Customer { Id = 1, Name = "Alice" },
                new Customer { Id = 2, Name = "Bob" }
            );
            ctx.SaveChanges();

            return ctx;
        }

        [Fact(DisplayName = "OK: Cliente existente, horário comercial, já comprou antes, compra de 200 é permitida")]
        public async Task CanPurchase_ReturnsTrue_WhenAllRulesPass()
        {
            var ctx = CreateDb(out var clock, new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc)); // quarta, 10h (comercial)
            ctx.Orders.Add(MakePaidOrder(1, clock.UtcNow.AddMonths(-2)));
            ctx.SaveChanges();

            var service = new CustomerService(ctx, clock);

            var allowed = await service.CanPurchase(1, 200m);

            Assert.True(allowed);
        }

        [Fact(DisplayName = "NOK: Primeira compra não pode exceder 100,00")]
        public async Task CanPurchase_ReturnsFalse_WhenFirstPurchaseExceeds100()
        {
            var ctx = CreateDb(out var clock);
            var service = new CustomerService(ctx, clock);

            var allowed = await service.CanPurchase(2, 150m);

            Assert.False(allowed);
        }

        [Fact(DisplayName = "NOK: Já possui compra no último mês")]
        public async Task CanPurchase_ReturnsFalse_WhenOrderInLastMonth()
        {
            var ctx = CreateDb(out var clock);
            ctx.Orders.Add(MakePaidOrder(1, clock.UtcNow.AddDays(-5), 80m));
            ctx.SaveChanges();

            var service = new CustomerService(ctx, clock);

            var allowed = await service.CanPurchase(1, 60m);

            Assert.False(allowed);
        }

        [Fact(DisplayName = "NOK: Fora do horário comercial (7h)")]
        public async Task CanPurchase_ReturnsFalse_OutsideBusinessHours()
        {
            var ctx = CreateDb(out var clock, new DateTime(2025, 1, 15, 7, 0, 0, DateTimeKind.Utc)); // 07:00 => fora
            var service = new CustomerService(ctx, clock);

            var allowed = await service.CanPurchase(1, 50m);

            Assert.False(allowed);
        }

        [Fact(DisplayName = "NOK: Fim de semana (sábado)")]
        public async Task CanPurchase_ReturnsFalse_OnWeekend()
        {
            // 2025-01-18 é sábado
            var ctx = CreateDb(out var clock, new DateTime(2025, 1, 18, 10, 0, 0, DateTimeKind.Utc));
            var service = new CustomerService(ctx, clock);

            var allowed = await service.CanPurchase(1, 50m);

            Assert.False(allowed);
        }

        [Fact(DisplayName = "Exceção: Cliente inexistente")]
        public async Task CanPurchase_Throws_WhenCustomerDoesNotExist()
        {
            var ctx = CreateDb(out var clock);
            var service = new CustomerService(ctx, clock);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CanPurchase(999, 10m));
        }

        [Theory(DisplayName = "Exceção: Parâmetros inválidos")]
        [InlineData(0, 10)]
        [InlineData(-1, 10)]
        [InlineData(1, 0)]
        [InlineData(1, -5)]
        public async Task CanPurchase_Throws_OnInvalidParams(int customerId, decimal value)
        {
            var ctx = CreateDb(out var clock);
            var service = new CustomerService(ctx, clock);

            await Assert.ThrowsAnyAsync<ArgumentOutOfRangeException>(() => service.CanPurchase(customerId, value));
        }

        [Fact(DisplayName = "Borda: 18h é permitido, 19h não")]
        public async Task CanPurchase_BorderHours()
        {
            // 18h → permitido
            var ctx1 = CreateDb(out var clock1, new DateTime(2025, 1, 15, 18, 0, 0, DateTimeKind.Utc));
            var s1 = new CustomerService(ctx1, clock1);
            var okAt18 = await s1.CanPurchase(1, 50m);
            Assert.True(okAt18);

            // 19h → negado
            var ctx2 = CreateDb(out var clock2, new DateTime(2025, 1, 15, 19, 0, 0, DateTimeKind.Utc));
            var s2 = new CustomerService(ctx2, clock2);
            var okAt19 = await s2.CanPurchase(1, 50m);
            Assert.False(okAt19);
        }

        [Fact(DisplayName = "Primeira compra de exatamente 100 deve ser permitida")]
        public async Task FirstPurchase_Exactly100_Allows()
        {
            var ctx = CreateDb(out var clock);
            // Cliente novo (3)
            ctx.Customers.Add(new Customer { Id = 3, Name = "Carol" });
            ctx.SaveChanges();

            var service = new CustomerService(ctx, clock);
            var ok = await service.CanPurchase(3, 100m);

            Assert.True(ok);
        }

        [Fact(DisplayName = "Primeira compra de 101 deve ser negada")]
        public async Task FirstPurchase_Exactly101_Denies()
        {
            var ctx = CreateDb(out var clock);
            ctx.Customers.Add(new Customer { Id = 4, Name = "Dan" });
            ctx.SaveChanges();

            var service = new CustomerService(ctx, clock);
            var ok = await service.CanPurchase(4, 101m);

            Assert.False(ok);
        }

        [Fact(DisplayName = "Cliente recorrente: valor mínimo positivo é permitido")]
        public async Task ExistingCustomer_TinyValue_Allows()
        {
            var ctx = CreateDb(out var clock);
            ctx.Customers.Add(new Customer { Id = 5, Name = "Eva" });
            ctx.Orders.Add(MakePaidOrder(5, clock.UtcNow.AddMonths(-2), 20m));
            ctx.SaveChanges();

            var service = new CustomerService(ctx, clock);
            var ok = await service.CanPurchase(5, 0.01m);

            Assert.True(ok);
        }

        [Fact(DisplayName = "Cliente recorrente: valor muito alto é permitido (sem regra de teto)")]
        public async Task ExistingCustomer_HugeValue_Allows()
        {
            var ctx = CreateDb(out var clock);
            ctx.Customers.Add(new Customer { Id = 6, Name = "Frank" });
            ctx.Orders.Add(MakePaidOrder(6, clock.UtcNow.AddMonths(-3), 80m));
            ctx.SaveChanges();

            var service = new CustomerService(ctx, clock);
            var ok = await service.CanPurchase(6, 1_000_000m);

            Assert.True(ok);
        }

        [Fact(DisplayName = "Janela: pedido há 29 dias deve bloquear")]
        public async Task Window_29Days_Denies()
        {
            var baseNow = new DateTime(2025, 2, 15, 10, 0, 0, DateTimeKind.Utc);
            var ctx = CreateDb(out var clock, baseNow);

            ctx.Customers.Add(new Customer { Id = 7, Name = "Gabe" });
            ctx.Orders.Add(MakePaidOrder(7, baseNow.AddDays(-29)));
            ctx.SaveChanges();

            var service = new CustomerService(ctx, clock);
            var ok = await service.CanPurchase(7, 50m);

            Assert.False(ok);
        }

        [Fact(DisplayName = "Janela: pedido exatamente agora.AddMonths(-1) (limite) deve bloquear")]
        public async Task Window_ExactlyBaseDate_Denies()
        {
            var baseNow = new DateTime(2025, 3, 10, 10, 0, 0, DateTimeKind.Utc);
            var ctx = CreateDb(out var clock, baseNow);

            ctx.Customers.Add(new Customer { Id = 1000, Name = "Hero" });
            var baseDate = baseNow.AddMonths(-1);
            ctx.Orders.Add(MakePaidOrder(1000, baseDate));
            ctx.SaveChanges();

            var service = new CustomerService(ctx, clock);
            var ok = await service.CanPurchase(1000, 50m);

            Assert.False(ok);
        }

        [Fact(DisplayName = "Janela: pedido há 31 dias deve permitir")]
        public async Task Window_31Days_Allows()
        {
            // Segunda-feira (dia útil) às 10:00 UTC
            var baseNow = new DateTime(2025, 2, 17, 10, 0, 0, DateTimeKind.Utc);
            var ctx = CreateDb(out var clock, baseNow);

            ctx.Customers.Add(new Customer { Id = 1001, Name = "Ivy" });

            // Regra do serviço: bloqueia pedidos com OrderDate >= baseDate (now.AddMonths(-1))
            // Portanto, para PERMITIR, o pedido deve ser ESTRITAMENTE anterior a baseDate.
            var baseDate = baseNow.AddMonths(-1); // 2025-01-17 10:00 UTC
            ctx.Orders.Add(MakePaidOrder(1001, baseDate.AddSeconds(-1))); // 2025-01-17 09:59:59 UTC
            ctx.SaveChanges();

            var service = new CustomerService(ctx, clock);
            var ok = await service.CanPurchase(1001, 50m);

            Assert.True(ok);
        }

        [Fact(DisplayName = "Mesmo serviço respeita mudanças no relógio injetado em runtime")]
        public async Task ClockChange_RespectedByService()
        {
            var ctx = CreateDb(out var clock, new DateTime(2025, 1, 15, 6, 0, 0, DateTimeKind.Utc)); // 06h → fora
            ctx.Customers.Add(new Customer { Id = 1002, Name = "Jade" });
            ctx.SaveChanges();

            var service = new CustomerService(ctx, clock);

            // 06h → deve ser false
            var t1 = await service.CanPurchase(1002, 50m);
            Assert.False(t1);

            // muda o relógio para 10h → true
            clock.UtcNow = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);
            var t2 = await service.CanPurchase(1002, 50m);
            Assert.True(t2);

            // muda para 19h → false
            clock.UtcNow = new DateTime(2025, 1, 15, 19, 0, 0, DateTimeKind.Utc);
            var t3 = await service.CanPurchase(1002, 50m);
            Assert.False(t3);
        }
    }
}
