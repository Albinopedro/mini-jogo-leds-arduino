# Teste de Logout - Verificação de Bugs

## Objetivo
Verificar se os bugs de logout foram corrigidos no sistema.

## Cenários de Teste

### 1. Logout Normal como Cliente
**Passos:**
1. Iniciar aplicação
2. Fazer login como cliente com código válido
3. Selecionar um jogo
4. Clicar no botão "Encerrar Sessão"
5. Confirmar logout

**Resultado Esperado:**
- Aplicação deve retornar à tela de login
- Apenas UMA janela de login deve aparecer
- Aplicação NÃO deve fechar completamente

### 2. Fim de Sessão Automático
**Passos:**
1. Fazer login como cliente
2. Jogar até esgotar todas as chances (3 erros)
3. Aguardar diálogo de fim de sessão
4. Clicar "Voltar ao Login"

**Resultado Esperado:**
- Diálogo de sessão completa deve aparecer
- Ao clicar "Voltar ao Login", deve retornar à tela de login
- Aplicação NÃO deve fechar

### 3. Timeout no Jogo Gato e Rato
**Passos:**
1. Fazer login como cliente
2. Selecionar jogo "Gato e Rato"
3. Aguardar timeout (não jogar)
4. Verificar se sessão é encerrada automaticamente

**Resultado Esperado:**
- Diálogo de timeout deve aparecer
- Sessão deve ser finalizada automaticamente
- Retorno à tela de login

### 4. Múltiplos Logouts Sequenciais
**Passos:**
1. Fazer login → logout → login → logout (repetir 3x)

**Resultado Esperado:**
- Cada logout deve funcionar corretamente
- Não deve haver janelas duplicadas
- Aplicação deve permanecer estável

## Logs de Debug
Verificar no console os seguintes logs:
- `[APP] ShowLoginWindow iniciado`
- `[APP] OnMainWindowClosed chamado`
- `[APP] LoginWindow mostrada com sucesso`
- `[LOGOUT] MainWindow fechado`

## Status dos Fixes
- ✅ Prevenção de múltiplas janelas de login
- ✅ Controle de shutdown explícito
- ✅ Race condition protection
- ✅ Window lifecycle management
- ✅ Session dialog management
- ✅ Comprehensive logging

## Comandos para Teste
```bash
cd miniJogo
dotnet run
```

## Notas
- Se a aplicação fechar inesperadamente, verificar logs de debug
- Testar em diferentes ordens de operação
- Verificar comportamento após múltiplas sessões