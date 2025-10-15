# Gamification.Domain - TDD Exercise (Badges by Mission)

Aluno: Bruno Luis da Cruz 
Curso: Ciência da Computação
Disciplina: Programação Orientada a Objeto

Este repositório contém uma implementação de domínio para concessão de badges (insígnias) por missão,
seguindo o enunciado da atividade de TDD (C# / .NET 9).

## Estrutura
```
/src/Gamification.Domain
/tests/Gamification.Domain.Tests
```

## Como rodar os testes
Recomendado: .NET 9 SDK instalado.

```bash
# a partir da raiz do pacote
cd src/Gamification.Domain
dotnet build
cd ../../tests/Gamification.Domain.Tests
dotnet test
```

## Decisões de design
- Domínio isolado (ports/adapters): IAwardsReadStore e IAwardsWriteStore para permitir fakes/mocks.
- BonusPolicy é pura e testada isoladamente.
- Idempotência por `requestId` + chave natural (studentId, themeId, missionId, badgeSlug).
- Atomicidade modelada por `SaveAwardAtomic` — o store deve garantir transação; se exceção lançada, o serviço propaga `AtomicPersistenceException`.

## Limitações
- Não há persistência real (banco) — fakes usados nos testes.
- Alguns detalhes (ex.: múltiplas badges por missão) podem ser estendidos conforme necessidade.
