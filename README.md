# 🎮 Mini Jogo LEDs - Arduino Gaming Platform

Sistema de jogos interativos com matriz 4x4 de LEDs conectada ao Arduino, controlado por aplicação desktop Avalonia UI com **sistema de autenticação completo**.

## 🔐 Sistema de Autenticação

### 👥 **Dois Tipos de Usuários**

#### 🔧 **Administradores**
- **Código Fixo**: `ADMIN2024`
- **Acesso Completo**: Debug, configurações, geração de códigos
- **Sem Restrições**: Acesso a todas as funcionalidades
- **Conexão Manual**: Configuração total do Arduino

#### 🎮 **Clientes/Jogadores** 
- **Códigos Únicos**: Bilhetes de 6 caracteres (ex: `AB1234`)
- **Nome Obrigatório**: Identificação personalizada
- **Auto-Conexão**: Arduino conecta automaticamente
- **Interface Simplificada**: Foco apenas no jogo
- **Modo de Jogo**: Seleção prévia na tela de login

### 🎫 **Sistema de Códigos de Cliente**

#### 📋 **Características dos Códigos**
- **Formato**: 2 letras + 4 números (ex: `MX7391`)
- **Únicos**: Cada código pode ser usado apenas 1 vez
- **Imprevisíveis**: Geração criptograficamente segura
- **Curtos**: Fáceis de digitar e imprimir
- **Validação**: Sistema anti-fraude integrado

#### 🏷️ **Geração para Bilhetes**
1. Admin faz login com `ADMIN2024`
2. Clica em "📄 Gerar Códigos de Cliente"
3. Define quantidade (1-10.000 códigos)
4. Sistema gera arquivo `.txt` formatado para impressão
5. Códigos prontos para corte em bilhetes

#### 💳 **Exemplo de Bilhete**
```
╔══════════════════════╗
║  🎮 MINI JOGO LEDS   ║
║                      ║
║    Código: AB1234    ║
║                      ║
║  Valid: ___/___/___  ║
╚══════════════════════╝
```

## 🚀 Quick Start

### 🎯 **Para Clientes**
1. Digite seu **nome**
2. Digite o **código do bilhete**
3. Escolha o **jogo desejado** (com instruções)
4. Clique **"🚀 Entrar no Jogo"**
5. **Arduino conecta automaticamente**
6. **Jogue imediatamente!**

### 🔧 **Para Administradores**
1. Digite apenas: `ADMIN2024`
2. Acesso completo liberado
3. Configure Arduino manualmente
4. Gere códigos de cliente
5. Acesse debug e estatísticas

## 🎯 Jogos Disponíveis

| Jogo | Dificuldade | Descrição | Efeitos Visuais |
|------|-------------|-----------|-----------------|
| 🎯 **Pega-Luz** | ⭐⭐ | Pressione LEDs que acendem rapidamente | ✨ Explosão de acerto |
| 🧠 **Sequência Maluca** | ⭐⭐⭐ | Memorize e repita sequências crescentes | 🌟 Feedback de progresso |
| 🐱 **Gato e Rato** | ⭐⭐ | Persiga o LED piscante pela matriz | 🏃 Animação de movimento |
| ☄️ **Esquiva Meteoros** | ⭐⭐⭐ | Sobreviva aos meteoros que caem | 💥 Explosões dinâmicas |
| 🎸 **Guitar Hero** | ⭐⭐⭐⭐ | Toque as notas no ritmo certo | 🎵 Pulsos musicais |
| 🎲 **Roleta Russa** | ⭐⭐⭐⭐⭐ | 1/16 chance, multiplicadores até 256x | 💣 Explosão épica |
| ⚡ **Lightning Strike** | ⭐⭐⭐⭐⭐ | Memorize padrões em milissegundos | ⚡ Raios ultra-rápidos |
| 🎯 **Sniper Mode** | ⭐⭐⭐⭐⭐ | Mire em alvos que piscam por 0.1s | 🏆 Vitória impossível |

## ⌨️ Controles

```
Matriz 4x4:    Teclas:
🔴🔴🔴🔴      0 1 2 3
🟡🟡🟡🟡  →   4 5 6 7
🟢🟢🟢🟢      8 9 A B
🔵🔵🔵🔵      C D E F

F1: Iniciar   | F2: Parar    | F3: Reset   | F4: Rankings
F5: 🌈 Arco-íris | F6: ⏹️ Parar FX | F7: 💚 Matrix | F8: 💓 Pulso
F9: 🎆 Fogos   | F10: ✨ Demo Completa | F11: 🖥️ Tela Cheia (secreto)
```

## 🔧 Hardware Setup

### Componentes
- Arduino Uno R3
- 16x LEDs (4 cores: vermelho, amarelo, verde, azul)
- 16x Resistores 220Ω
- Protoboard e jumpers

### Conexões
```
LEDs 0-3   (VERMELHOS): Pinos 2-5
LEDs 4-7   (AMARELOS):  Pinos 6-9
LEDs 8-11  (VERDES):    Pinos 10-13
LEDs 12-15 (AZUIS):     Pinos A0-A3
```

Cada LED: Pino → Resistor 220Ω → LED → GND

## 💻 Software

### Requisitos
- .NET 9.0
- Arduino IDE 2.x
- Windows 10/11

### Instalação
```bash
git clone [repo-url]
cd miniJogo
dotnet restore
dotnet run
```

### Estrutura
```
miniJogo/
├── arduino_led_games.ino     # Código Arduino (8 jogos)
├── MainWindow.axaml.cs       # Interface principal
├── Views/LoginWindow.axaml   # Sistema de autenticação
├── Models/Auth/              # Modelos de usuário
├── Services/               
│   ├── AuthService.cs        # Autenticação e códigos
│   ├── ArduinoService.cs     # Comunicação serial
│   └── ScoreService.cs       # Sistema de pontuação
├── client_codes.json         # Códigos válidos
└── used_codes.json          # Códigos já utilizados
```

## 🎫 Gerenciamento de Códigos

### 📊 **Arquivos do Sistema**
- `client_codes.json`: Lista de todos os códigos válidos
- `used_codes.json`: Códigos já utilizados (não podem ser reutilizados)
- `bilhetes_jogo_YYYYMMDD_HHMMSS.txt`: Arquivo de impressão gerado

### 🔄 **Fluxo de Geração**
1. **Admin acessa** ferramenta de geração
2. **Define quantidade** de códigos necessários
3. **Sistema gera** códigos únicos e seguros
4. **Salva automaticamente** nos arquivos JSON
5. **Cria arquivo** formatado para impressão
6. **Pronto para** corte em bilhetes

### 📈 **Estatísticas em Tempo Real**
- Total de códigos gerados
- Códigos utilizados
- Códigos disponíveis
- Taxa de utilização

## 🛡️ Segurança

### 🔐 **Proteções Implementadas**
- **Código único**: Cada bilhete pode ser usado apenas 1 vez
- **Validação criptográfica**: Códigos imprevisíveis
- **Armazenamento seguro**: JSON local encriptado
- **Auditoria completa**: Log de todos os acessos
- **Separação de privilégios**: Admin vs Cliente

### 🚫 **Restrições para Clientes**
- ❌ Sem acesso ao console debug
- ❌ Sem acesso às configurações
- ❌ Sem geração de códigos
- ❌ Conexão manual do Arduino
- ✅ Interface simplificada e limpa

## 📊 Sistema de Pontuação

- **Salvamento Automático**: Todas as partidas são salvas
- **Rankings**: Top 10 global e por jogo
- **Estatísticas**: Tempo de jogo, melhor pontuação, média
- **Exportação**: CSV/TXT para análise

## 🛠️ Troubleshooting

| Problema | Solução |
|----------|---------|
| Código inválido | Verifique digitação, código pode ter sido usado |
| Arduino não conecta | Admin: verificar porta COM. Cliente: automático |
| LEDs não acendem | Confira conexões e resistores 220Ω |
| Login falha | Admin: `ADMIN2024`. Cliente: nome + código válido |
| Auto-conexão falha | Reconecte Arduino USB, aguarde 5 segundos |

## 🎮 Interface por Tipo de Usuário

### 👨‍💼 **Interface Admin**
```
┌─ PAINEL ADMINISTRATIVO ─────────────────┐
│ ✅ Console Debug         ✅ Configurações │
│ ✅ Conexão Manual        ✅ Gerar Códigos │
│ ✅ Todas as Estatísticas ✅ Modo Completo │
└─────────────────────────────────────────┘
```

### 👤 **Interface Cliente**
```
┌─ MODO JOGO SIMPLIFICADO ────────────────┐
│ 🎮 Seleção de Jogo      🏆 Rankings      │
│ ⚡ Conexão Automática   📊 Sua Pontuação │  
│ 🎯 Foco no Jogo         ❌ Sem Debug     │
└─────────────────────────────────────────┘
```

## ✨ Sistema de Efeitos Visuais

### 🎭 **Animações Automáticas**
- **🚀 Inicialização**: Sequência épica de 4 segundos com espiral crescente
- **🔗 Conexão**: Explosão de alegria + ondas concêntricas  
- **⚡ Início de Jogo**: Countdown visual 3-2-1-GO com números formados
- **🎯 Acertos**: Explosões do centro para fora baseadas em precisão
- **🆙 Level Up**: Ondas de energia + estrela de vitória
- **💥 Game Over**: Implosão dramática + flash vermelho
- **🏆 Vitórias**: Fogos de artifício + chuva de estrelas

### 🎪 **Efeitos Especiais (F5-F10)**
- **F5 - 🌈 Arco-íris**: Ondas coloridas contínuas por linha
- **F6 - ⏹️ Stop**: Para todos os efeitos visuais
- **F7 - 💚 Matrix Rain**: Chuva digital estilo Matrix
- **F8 - 💓 Pulso Universal**: Todos os LEDs pulsam sincronizados
- **F9 - 🎆 Fogos**: Múltiplas explosões sequenciais
- **F10 - ✨ Demo Completa**: Apresentação de 10 segundos

## 💰 Modelo de Negócio

### 🎫 **Sistema de Bilhetes**
- **Geração em Lote**: 10, 50, 100, 500, 1000+ códigos
- **Impressão Otimizada**: Formato pronto para corte
- **Controle Total**: Rastreamento de uso em tempo real
- **Segurança Anti-Fraude**: Códigos únicos e imprevisíveis

### 📈 **Monetização**
- **Pay-per-Play**: Cada código = 1 sessão de jogo
- **Diferentes Valores**: Jogos premium (Roleta, Lightning, Sniper)
- **Controle de Estoque**: Saber quantos bilhetes restam
- **Relatórios**: Estatísticas de vendas e uso

## 🏆 Recursos Premium

### 🎲 **Jogos de Alta Dificuldade**
- **Roleta Russa**: 6.25% chance + explosão visual épica
- **Lightning Strike**: Padrões impossíveis + animações ultra-rápidas
- **Sniper Mode**: 0.000000095% chance + celebração legendária

### 🎬 **Experiência Cinematográfica**
- **20+ animações** diferentes para situações específicas
- **Timing perfeito** sincronizado com eventos do jogo  
- **Feedback visual** que recompensa habilidade e precisão
- **Efeitos épicos** para vitórias raras

## 🔌 Protocolo de Comunicação

### PC → Arduino
```
START_GAME:[1-8]    # Iniciar jogo
STOP_GAME           # Parar jogo
KEY_PRESS:[0-15]    # Tecla pressionada
KEY_RELEASE:[0-15]  # Tecla solta
INIT                # Inicializar sistema
```

### Arduino → PC
```
READY                           # Arduino pronto
GAME_EVENT:[tipo]:[dados]       # Eventos do jogo
LED_ON:[index]                  # Acender LED
LED_OFF:[index]                 # Apagar LED
SCORE:[pontos]                  # Pontuação atual
```

## 📄 Licença

MIT License - Open Source

## 🤝 Contribuições

Issues e PRs são bem-vindos! Áreas para melhoria:
- Novos jogos e mecânicas
- Sistema de achievements online
- Modo multiplayer
- Integração com pagamentos
- Dashboard web de administração

---

**Versão 2.1.0** | **Sistema de Auth Completo** | **Suporte**: Issues no GitHub | **Compatibilidade**: .NET 9.0 + Arduino IDE 2.x