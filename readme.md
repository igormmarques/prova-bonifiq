# Prova BonifiQ — API + Testes

Este repositório contém a API **ProvaPub** (.NET 6) e a suíte de **testes automatizados** com xUnit. 
Organizamos o código em **src/** (aplicação) e **tests/** (testes), adicionamos **paginação correta**, **injeção de dependência**, **persistência de números únicos**, **refatoração de pagamentos (OCP)** e **testes abrangentes** para o método `CustomerService.CanPurchase`.

---

## 📁 Estrutura

```
prova-bonifiq/
├── .github/workflows/ci.yml        # Pipeline GitHub Actions (build+tests+coverage)
├── src/ProvaPub/                   # Projeto da API
│   ├── Controllers/
│   ├── Models/
│   ├── Repository/
│   ├── Services/
│   ├── Migrations/
│   ├── appsettings.json
│   └── ProvaPub.csproj
├── tests/ProvaPub.Tests/           # Projeto de testes (xUnit + EF InMemory)
│   ├── CustomerServiceTests.cs
│   └── ProvaPub.Tests.csproj
└── ProvaPub.sln
```

---

## ✅ O que implementamos

### Parte 1 — Random único e realmente aleatório
- `RandomService` agora usa **`RandomNumberGenerator`** (criptograficamente seguro) para gerar números imprevisíveis.
- Persistência em `RandomNumber` com **índice único** para garantir **unicidade no banco**.
- `Parte1Controller` retorna `ActionResult<int>` e trata exceções amigavelmente.

### Parte 2 — Paginação e redução de duplicidade
- Criado um **DTO genérico `PagedResult<T>`** para evitar repetição (`CustomerList`/`ProductList`).
- `CustomerService` e `ProductService` agora paginam com `OrderBy + Skip + Take` e retornam `PagedResult<T>`.
- Controllers passaram a usar **Injeção de Dependências (DI)**, sem `new Service(...)`.

### Parte 3 — Open/Closed para pagamentos
- Introduzido **Strategy** para pagamentos: `IPaymentProcessor` com `PixPaymentProcessor`, `CreditCardPaymentProcessor`, `PaypalPaymentProcessor`.
- `OrderService.PayOrder` apenas seleciona a estratégia por `paymentMethod` (sem ifs encadeados no método).
- `OrderDate` salvo **em UTC** no banco; o Controller devolve **UTC-3** na resposta.
- Campos de pagamento adicionados ao `Order` para testes e consistência (`PaymentMethod`, `PaymentProvider`, `PaymentStatus`, `PaymentTransactionId`).

### Parte 4 — Regra de compra + Testes
- `CustomerService.CanPurchase` validado com as regras:
  - Cliente precisa existir;
  - 1 compra por mês: bloqueia se existe pedido com `OrderDate >= UtcNow.AddMonths(-1)`;
  - Primeira compra **≤ 100,00**; compras subsequentes sem teto;
  - Permitido apenas em **dias úteis** e **horário comercial** (08h–18h) — usando `IDateTimeProvider` para testabilidade.
- **19 testes** com EF **InMemory**, incluindo bordas de horário, janelas de 29/31 dias, cliente inexistente e parâmetros inválidos.

---

## ▶️ Rodando o projeto

Pré-requisitos: **.NET 6 SDK** e **SQL Server LocalDB** (ou ajuste a connection string).

1) Restaurar e compilar
```bash
dotnet restore
dotnet build
```

2) Aplicar migrations (instale as ferramentas do EF se necessário)
```bash
dotnet tool install --global dotnet-ef
dotnet ef database update --project src/ProvaPub
```

3) Subir a API
```bash
dotnet run --project src/ProvaPub
```
Acesse o Swagger em `https://localhost:xxxxx/swagger`.

---

## 🧪 Testes e Cobertura

Rodar os testes (com EF InMemory):
```bash
dotnet test tests/ProvaPub.Tests
```

Gerar **cobertura** (Coverlet via collector):
```bash
dotnet test tests/ProvaPub.Tests --collect:"XPlat Code Coverage"
```
O arquivo Cobertura ficará em:
```
tests/ProvaPub.Tests/TestResults/<runId>/coverage.cobertura.xml
```

Gerar relatório HTML (opcional):
```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:tests/ProvaPub.Tests/TestResults/**/coverage.cobertura.xml -targetdir:coveragereport
# abrir coveragereport/index.html
```

---

## 🤖 CI — GitHub Actions

A pipeline **ci.yml** roda em `push`/`PR` (main/master), compila, executa testes e publica artefatos (TRX e cobertura).

---

## 🔒 .gitignore

Ignoramos `.vs/`, `bin/`, `obj/`, `TestResults/` e artefatos temporários para evitar conflitos de lock no Windows.
