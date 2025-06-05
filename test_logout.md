# Teste de Logout e Sistema de Áudio v2.0 - Verificação Completa

## Objetivo
Verificar se os bugs de logout foram corrigidos no sistema e testar a integração do sistema de áudio premium v2.0.

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
- **🎵 Som de shutdown deve tocar** (swoosh descendente)

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
- **🎵 Som de game over deve tocar** (progressão descendente)

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
- **🎵 Som de erro deve tocar** (beep 200Hz com envelope)

### 4. Múltiplos Logouts Sequenciais
**Passos:**
1. Fazer login → logout → login → logout (repetir 3x)

**Resultado Esperado:**
- Cada logout deve funcionar corretamente
- Não deve haver janelas duplicadas
- Aplicação deve permanecer estável
- **🎵 Sons de sistema devem tocar em cada transição**

## Logs de Debug
Verificar no console os seguintes logs:
- `[APP] ShowLoginWindow iniciado`
- `[APP] OnMainWindowClosed chamado`
- `[APP] LoginWindow mostrada com sucesso`
- `[LOGOUT] MainWindow fechado`
- **🎵 AudioService logs de reprodução de sons**

## Teste de Áudio v2.0 Durante Logout

### 5. Verificação de Sons Durante Fluxo de Logout
**Passos:**
1. Iniciar aplicação → **Deve tocar startup.wav** (power-up energético)
2. Login válido → **Deve tocar login_success.wav** (arpejo C4-E4-G4-C5)
3. Cliques em botões → **Deve tocar button_click.wav** (beep retrô vibrato)
4. Logout → **Deve tocar shutdown.wav** (swoosh descendente)

**Resultado Esperado:**
- Todos os 4 sons devem reproduzir nos momentos corretos
- Qualidade de áudio 44.1kHz premium
- Sem latência perceptível
- Sons com harmônicos e envelopes ADSR audíveis

### 6. Teste de Performance de Áudio
**Passos:**
1. Fazer login/logout múltiplas vezes rapidamente
2. Verificar se áudio não causa travamentos
3. Monitorar uso de memória (~15MB para 42 sons)

**Resultado Esperado:**
- Áudio não deve interferir na performance do logout
- Sistema deve manter responsividade
- Cache de áudio deve funcionar eficientemente

## Status dos Fixes
- ✅ Prevenção de múltiplas janelas de login
- ✅ Controle de shutdown explícito
- ✅ Race condition protection
- ✅ Window lifecycle management
- ✅ Session dialog management
- ✅ Comprehensive logging
- **✅ Sistema de áudio v2.0 integrado**
- **✅ 42 sons premium com qualidade CD**
- **✅ Feedback sonoro para todas as ações**
- **✅ Performance otimizada para áudio**

## Comandos para Teste
```bash
# Gerar arquivos de áudio v2.0 primeiro
cd miniJogo
python3 generate_audio_files.py

# Executar aplicação com áudio
dotnet build
dotnet run

# Verificar arquivos de áudio gerados
find Assets/Audio -name "*.wav" | wc -l
# Esperado: 42 arquivos
```

## Notas
- Se a aplicação fechar inesperadamente, verificar logs de debug
- Testar em diferentes ordens de operação
- Verificar comportamento após múltiplas sessões
- **🎵 Verificar se áudio reproduz em Linux usando `aplay`**
- **🎵 Monitorar logs de AudioService para debugging**
- **🎵 Testar volume e qualidade dos sons premium**
- **🎵 Confirmar que todos os 42 arquivos foram gerados corretamente**

## Checklist de Áudio v2.0
- [ ] Startup sound toca na inicialização
- [ ] Login success sound toca no login válido
- [ ] Button click sounds tocam em todos os botões
- [ ] Shutdown sound toca no logout
- [ ] Arquivos gerados com 44.1kHz, 16-bit, mono
- [ ] Performance mantida com sistema de áudio ativo
- [ ] Sem travamentos durante reprodução de áudio
- [ ] Cache de áudio funcionando eficientemente