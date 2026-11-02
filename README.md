# fcg-catalog-api

Serviço de catálogo da plataforma FIAP Cloud Games (FCG). Cuida dos jogos, das promoções, dos pedidos e da biblioteca de cada usuário. É ele quem inicia uma compra e quem, no fim, coloca o jogo na biblioteca (ou não, se o pagamento for recusado).

Faz parte da Fase 2 do Tech Challenge, em que o monólito da Fase 1 foi dividido em quatro microsserviços. A visão geral da arquitetura, os fluxos de evento e as instruções para subir tudo juntos estão no [fcg-orchestration](https://github.com/PedroRCampelo/fcg-orchestration). Este README cobre só o que é específico deste serviço.

## O que o serviço faz

- Mantém o catálogo: um administrador cadastra, atualiza e remove jogos. A remoção é **lógica** (`Ativo = false`): o jogo some do catálogo e não pode mais ser comprado, mas continua nas bibliotecas de quem já o tem, e o título permanece reservado.
- Cadastra e lista promoções por jogo (percentual, data de início e de fim).
- Inicia a compra de um jogo: abre um pedido `Pendente` e publica o `OrderPlacedEvent`. O jogo **não** entra na biblioteca nesse momento.
- Consome o `PaymentProcessedEvent`: pagamento aprovado conclui o pedido e coloca o jogo na biblioteca; recusado marca o pedido como `Recusado`.
- Lista os pedidos e a biblioteca do usuário autenticado.

O preço do pedido é o preço do jogo no momento da compra. As promoções são cadastradas e listadas, mas **não** alteram o valor cobrado (veja as decisões de projeto no fim).

Banco: PostgreSQL, database `fcg_catalog`. As migrações do EF Core são aplicadas sozinhas na subida.

## Eventos

| Direção | Evento | Por onde trafega | Quando |
|---|---|---|---|
| Publica | `OrderPlacedEvent` (`OrderId`, `UserId`, `GameId`, `Titulo`, `Price`) | exchange `FCG.Contracts:OrderPlacedEvent`, fila `payments.order-placed` | Ao abrir um pedido |
| Consome | `PaymentProcessedEvent` (`OrderId`, `UserId`, `GameId`, `Titulo`, `Status`) | fila `catalog.payment-processed` | Quando o Payments decide sobre um pedido |

O `OrderId` é o próprio id do pedido. O pedido e o `OrderPlacedEvent` são gravados na mesma transação (outbox transacional), e na subida o Catalog declara a fila do Payments, então um pedido aberto com o Payments fora do ar fica esperando na fila.

**Idempotência pelo `OrderId`.** O RabbitMQ entrega pelo menos uma vez, então a mesma mensagem pode chegar duas vezes. Se o pedido já não estiver `Pendente`, a mensagem é descartada sem efeito, com um log de aviso explicando o motivo. Um evento para um pedido que não existe, ou com usuário ou jogo diferentes dos do pedido, é inválido: não é repetido e vai direto para a fila `catalog.payment-processed_error`. Como garantia a mais, o banco tem um índice único em `(UsuarioId, JogoId)` na biblioteca e um índice único parcial que impede dois pedidos pendentes do mesmo jogo para o mesmo usuário.

## Endpoints

Rodando com `dotnet run`, o serviço escuta na porta 5102; dentro do container, na 8080. O Swagger está em `/swagger`. "Autenticado" significa qualquer usuário com um JWT válido emitido pelo fcg-users-api.

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| GET | `/jogos` | Autenticado | Lista os jogos ativos. |
| GET | `/jogos/{id}` | Autenticado | Consulta um jogo ativo. 404 se não existir ou estiver inativo. |
| POST | `/jogos` | Administrador | Cadastra um jogo (`titulo`, `descricao`, `preco`). 201, 400 ou 409 (título repetido). |
| PUT | `/jogos/{id}` | Administrador | Atualiza título, descrição e preço. 200, 400, 404 ou 409. |
| DELETE | `/jogos/{id}` | Administrador | Exclusão lógica. 204 ou 404. Repetir a chamada também responde 204. |
| GET | `/promocoes` | Autenticado | Lista as promoções. |
| POST | `/promocoes` | Administrador | Cadastra uma promoção (`jogoId`, `percentualDesconto`, `dataInicio`, `dataFim`). 201, 400 ou 404. |
| POST | `/biblioteca/{jogoId}` | Autenticado | Inicia a compra. Responde **202** com o pedido `Pendente`. 404 se o jogo não existir ou estiver inativo, 409 se o usuário já tem o jogo ou já tem um pedido pendente para ele. |
| GET | `/biblioteca` | Autenticado | Jogos adquiridos pelo usuário. |
| GET | `/pedidos` | Autenticado | Pedidos do usuário, com o status (`Pendente`, `Concluido` ou `Recusado`). |
| GET | `/health` | Anônimo | Saúde completa, inclui a conexão com o RabbitMQ. Serve de readiness. |
| GET | `/health/live` | Anônimo | Só confirma que o processo responde. Serve de liveness. |

A resposta 202 significa "pedido recebido": o resultado do pagamento chega depois, de forma assíncrona. Consulte `GET /pedidos` para ver o desfecho. Datas de promoção sem fuso (`Kind` indefinido) são tratadas como UTC.

## Variáveis de ambiente

No .NET, `__` (dois sublinhados) separa os níveis da configuração: `RabbitMq__Host` é a chave `RabbitMq:Host` do `appsettings.json`.

| Variável | Descrição | Obrigatória | Padrão |
|---|---|---|---|
| `ConnectionStrings__Default` | Connection string do PostgreSQL (database `fcg_catalog`) | Sim¹ | Só em Development: `Host=localhost;Port=5435;Database=fcg_catalog;Username=fcg;Password=fcg_password` |
| `JWT__SIGNINGKEY` | Chave com que o token é validado, mínimo de 32 bytes. **Tem que ser a mesma do fcg-users-api** | Sim | Nenhum, de propósito. Sem ela o serviço não sobe |
| `Jwt__Issuer` | Emissor esperado no token | Não | `fcg-users-api` |
| `Jwt__Audience` | Audiência esperada no token | Não | `fcg` |
| `RabbitMq__Host` | Host do RabbitMQ | Não | `localhost` |
| `RabbitMq__Port` | Porta AMQP | Não | `5672` |
| `RabbitMq__VirtualHost` | Virtual host | Não | `/` |
| `RabbitMq__Username` | Usuário do RabbitMQ | Sim¹ | Só em Development: `fcg` |
| `RabbitMq__Password` | Senha do RabbitMQ | Sim¹ | Só em Development: `fcg_password` |
| `RabbitMq__Queues__PaymentsOrderPlaced` | Fila do Payments para o `OrderPlacedEvent` (o Catalog a declara). Igual à do Payments | Não | `payments.order-placed` |
| `RabbitMq__Queues__CatalogPaymentProcessed` | Fila deste serviço para o `PaymentProcessedEvent`. Igual à declarada pelo Payments | Não | `catalog.payment-processed` |
| `ASPNETCORE_ENVIRONMENT` | Em `Development` o serviço lê o `appsettings.Development.json` | Não | `Development` no `dotnet run`, `Production` na imagem |

¹ Obrigatória em qualquer ambiente que não seja o Development. Rodando com `dotnet run`, o `appsettings.Development.json` já preenche com os valores do PostgreSQL e do RabbitMQ do compose.

Se faltar algo obrigatório, o serviço escreve o motivo no log e encerra com código de saída 1.

## Como rodar isolado

Pré-requisitos: .NET 8 SDK e Docker.

O Catalog precisa de um PostgreSQL e de um RabbitMQ. O jeito mais simples é subir só os dois com o compose do orquestrador (o [fcg-orchestration](https://github.com/PedroRCampelo/fcg-orchestration) deve estar clonado ao lado deste repositório):

```bash
cd ../fcg-orchestration
cp .env.example .env              # só na primeira vez; preencha JWT__SIGNINGKEY (openssl rand -hex 32)
docker compose up -d rabbitmq postgres
cd ../fcg-catalog-api
```

Depois, com o .NET:

```bash
export JWT__SIGNINGKEY=<a mesma chave usada pelo fcg-users-api>
dotnet run --project src/FCG.Catalog.Api
```

O serviço sobe em http://localhost:5102/swagger. Quase todas as rotas pedem token, e quem emite o token é o fcg-users-api. Para testar o Catalog sozinho, suba também o Users com a **mesma** `JWT__SIGNINGKEY` (instruções no README dele) e faça login para obter o JWT. Se as chaves forem diferentes, o Catalog responde 401.

Ou com a imagem Docker, sem precisar do SDK:

```bash
docker build -t fcg-catalog-api:local .
docker run --rm -p 5102:8080 \
  -e JWT__SIGNINGKEY="<a mesma chave do users-api>" \
  -e ConnectionStrings__Default="Host=host.docker.internal;Port=5435;Database=fcg_catalog;Username=fcg;Password=fcg_password" \
  -e RabbitMq__Host=host.docker.internal \
  -e RabbitMq__Username=fcg -e RabbitMq__Password=fcg_password \
  fcg-catalog-api:local
```

`host.docker.internal` é o nome que o Docker Desktop dá à máquina hospedeira, onde o compose publicou o PostgreSQL (porta 5435) e o RabbitMQ (5672).

Sem o Payments no ar, o Catalog aceita compras normalmente, mas os pedidos ficam `Pendente`: o `OrderPlacedEvent` espera na fila `payments.order-placed` e é processado assim que o Payments subir.

Para rodar os testes (não precisam de banco nem de broker):

```bash
dotnet test
```

## Estrutura do código

```
src/
  FCG.Catalog.Domain           entidades (Jogo, Promocao, Pedido, JogoAdquirido) e regras de negócio
  FCG.Catalog.Application      serviços, DTOs, interfaces e os contratos de evento (Contracts/)
  FCG.Catalog.Infrastructure   EF Core, migrações, MassTransit (publicação e consumidor) e validação do JWT
  FCG.Catalog.Api              endpoints, middleware de exceções, Swagger e Program.cs
tests/
  FCG.Catalog.Tests            testes de unidade e de consumo/publicação com o harness do MassTransit
k8s/                           Deployment, Service, ConfigMap e Secret deste serviço
Dockerfile                     build multi-stage, compila para arm64 e amd64
```

## Decisões que afetam este serviço

As justificativas completas estão na seção [Decisões de projeto](https://github.com/PedroRCampelo/fcg-orchestration#decisões-de-projeto) do fcg-orchestration. As que mais pesam aqui:

- **Preço sem desconto no `OrderPlacedEvent`:** o evento leva o preço cheio do jogo. A especificação não pediu que a promoção alterasse o valor cobrado, então ela ainda não é aplicada.
- **Idempotência pelo `OrderId`:** descrita acima.
- **Outbox transacional** na publicação do `OrderPlacedEvent`.
- **Contrato de evento duplicado:** a pasta `Contracts/` é uma cópia idêntica à dos outros repositórios que usam o mesmo evento. O `PaymentStatus` tem valores fixos (`Approved = 1`, `Rejected = 2`) e vai como texto no JSON.
- **Nomes de fila repetidos** no `k8s/configmap.yaml`, iguais aos dos outros serviços.
- **EF Core 9 sobre .NET 8**, por exigência do MassTransit.EntityFrameworkCore 8.5.
