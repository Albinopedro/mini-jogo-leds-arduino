# 🎮 Mini Jogo LEDs - Arduino Gaming Platform

Sistema de jogos interativos com matriz 4x4 de LEDs conectada ao Arduino, controlado por aplicação desktop em Avalonia UI com sistema de autenticação e áudio integrado.

## 🚀 Quick Start

### 👤 **Para Jogadores**
1. Digite seu **nome** e **código de acesso** (ex: `AB1234`)
2. Escolha um dos **7 jogos disponíveis**
3. Clique **"🚀 Entrar no Jogo"**
4. Arduino conecta automaticamente e o jogo inicia!

### 🔧 **Para Administradores**
1. Digite apenas: `ADMIN2024`
2. Acesso total liberado (debug, configurações, geração de códigos)
3. Conecte Arduino manualmente na porta desejada

## 🎯 Jogos Disponíveis (7 Jogos)

| Jogo | Dificuldade | Descrição | Objetivo |
|------|-------------|-----------|----------|
| 🎯 **Pega-Luz** | ⭐⭐ | Pressione LEDs que acendem rapidamente | Máxima velocidade de reação |
| 🧠 **Sequência Maluca** | ⭐⭐⭐ | Memorize e repita sequências crescentes | Memorização perfeita |
| 🐱 **Gato e Rato** | ⭐⭐ | Persiga o LED piscante pela matriz | Capture o "rato" LED |
| 🌧️ **Esquiva Meteoros** | ⭐⭐⭐ | Desvie dos meteoros que caem | Sobreviva o máximo possível |
| 🎸 **Guitar Hero** | ⭐⭐⭐⭐ | Toque as notas no ritmo correto | Timing musical perfeito |
| ⚡ **Lightning Strike** | ⭐⭐⭐⭐⭐ | Memorize padrões ultra-rápidos | Reflexos sobre-humanos |
| 🎯 **Sniper Mode** | ⭐⭐⭐⭐⭐ | Mire em alvos que piscam brevemente | Precisão impossível |

## 🔐 Sistema de Autenticação

### 👥 **Tipos de Usuários**

**🔧 Administradores**
- Código: `ADMIN2024`
- Acesso total ao sistema
- Geração de códigos de cliente
- Debug e configurações avançadas
- Conexão manual do Arduino

**🎮 Clientes/Jogadores**
- Códigos únicos de 6 caracteres (ex: `AB1234`, `MX7391`)
- Nome obrigatório para identificação
- Interface simplificada focada no jogo
- Conexão automática do Arduino

### 🎫 **Sistema de Códigos**
- **Formato**: 2 letras + 4 números (ex: `UV7617`)
- **Únicos**: Cada código usado apenas 1 vez
- **Arquivo**: `client_codes.json` com 116+ códigos disponíveis
- **Controle**: `used_codes.json` registra códigos utilizados

## ⌨️ Controles

### 🎮 **Controles dos Jogos**
- **WASD ou Setas**: Movimento/navegação
- **Espaço**: Ação principal
- **Enter**: Confirmar
- **ESC**: Pausar/Voltar
- **F2**: Parar jogo
- **F3**: Reset do sistema

### 🎭 **Efeitos Visuais (F6-F10)**
- **F6**: Parar todos os efeitos
- **F7**: Matrix Rain (chuva digital)
- **F8**: Pulso Universal (todos LEDs)
- **F9**: Fogos de Artifício
- **F10**: Demo completa de efeitos

## 🔧 Hardware Setup

### 📦 **Componentes Necessários**
- Arduino Uno R3
- 16x LEDs (4 vermelhos, 4 amarelos, 4 verdes, 4 azuis)
- 16x Resistores 220Ω
- Protoboard e jumpers
- Cabo USB A-B

### 🔌 **Conexões Arduino**
```
Linha 0 (VERMELHOS): LEDs 0-3   → Pinos 2-5
Linha 1 (AMARELOS):  LEDs 4-7   → Pinos 6-9
Linha 2 (VERDES):    LEDs 8-11  → Pinos 10-13
Linha 3 (AZUIS):     LEDs 12-15 → Pinos A0-A3
```

### 🏗️ **Layout da Matriz 4x4**
```
┌─────────────────────────┐
│  🔴    🔴    🔴    🔴   │  ← Linha 0 (Pinos 2-5)
│  🟡    🟡    🟡    🟡   │  ← Linha 1 (Pinos 6-9)
│  🟢    🟢    🟢    🟢   │  ← Linha 2 (Pinos 10-13)
│  🔵    🔵    🔵    🔵   │  ← Linha 3 (Pinos A0-A3)
└─────────────────────────┘
```

## 💻 Instalação e Execução

### 📋 **Requisitos**
- Windows 10/11 x64
- .NET 9.0 Runtime
- Arduino IDE 2.x
- Porta USB disponível

### 🚀 **Passos de Instalação**
1. Clone o repositório
2. Instale dependências: `dotnet restore`
3. Upload do firmware: `arduino_led_games.ino` para o Arduino
4. Execute: `dotnet run`
5. Conecte o Arduino via USB

### 📁 **Estrutura do Projeto**
```
miniJogo/
├── arduino_led_games.ino     # Firmware Arduino (7 jogos)
├── MainWindow.axaml(.cs)     # Interface principal
├── Views/                    # Janelas da aplicação
│   ├── LoginWindow           # Sistema de login
│   ├── ScoresWindow          # Rankings e pontuações
│   ├── VictoryWindow         # Tela de vitória
│   └── RoundsCompletedWindow # Fim de jogo
├── Services/                 # Serviços da aplicação
│   ├── ArduinoService.cs     # Comunicação serial
│   ├── AuthService.cs        # Autenticação
│   ├── AudioService.cs       # Sistema de áudio
│   ├── ScoreService.cs       # Pontuações
│   └── GameService.cs        # Lógica dos jogos
├── Assets/Audio/             # Sons organizados em categorias
│   ├── Sistema/              # Interface e navegação
│   ├── Jogos/                # Eventos de jogo
│   ├── Específicos/          # Sons únicos por jogo
│   ├── Efeitos/              # Efeitos visuais
│   └── Ambiente/             # Música de fundo
├── Models/                   # Modelos de dados
├── client_codes.json         # Códigos válidos
└── used_codes.json          # Códigos já utilizados
```

## 🎵 Sistema de Áudio

### ✨ **Recursos de Áudio**
- **42+ sons únicos** distribuídos em 5 categorias
- **NAudio** para reprodução de alta qualidade
- **Feedback sonoro** para cada ação do jogo
- **Efeitos sincronizados** com animações visuais

### 🎼 **Categorias de Sons**
- **Sistema**: Login, cliques, navegação
- **Jogos**: Vitória, game over, level up
- **Específicos**: Sons únicos para cada jogo
- **Efeitos**: Matrix, fogos, pulsos
- **Ambiente**: Música de fundo

## 📊 Sistema de Pontuação

### 🏆 **Recursos**
- **Rankings globais** por jogo
- **Estatísticas detalhadas** (score, level, duração)
- **Histórico completo** de partidas
- **Performance virtualizada** para grandes volumes de dados
- **Exportação** de dados para análise

### 📈 **Métricas Coletadas**
- Nome do jogador
- Jogo específico
- Pontuação final
- Nível alcançado
- Duração da partida
- Timestamp da sessão

## 🔌 Comunicação Arduino

### 📤 **PC → Arduino**
```
START_GAME:[1-7]      # Iniciar jogo específico
STOP_GAME             # Parar jogo atual
KEY_PRESS:[0-15]      # Tecla pressionada
INIT                  # Inicializar sistema
EFFECT_[TIPO]         # Ativar efeito visual
```

### 📥 **Arduino → PC**
```
READY                 # Sistema pronto
GAME_EVENT:[dados]    # Eventos do jogo
LED_STATE:[estado]    # Estado dos LEDs
SCORE_UPDATE:[pontos] # Atualização de score
GAME_OVER:[score]     # Fim de jogo
```

## 🛠️ Troubleshooting

**Arduino não conecta**
- Verifique cabo USB e drivers
- Teste outras portas COM
- Confirme upload do firmware

**LEDs não funcionam**
- Verifique todas as conexões
- Teste resistores 220Ω
- Confirme alimentação correta

**Código de cliente inválido**
- Verifique se foi digitado corretamente
- Código pode já ter sido usado
- Admins podem gerar novos códigos

**Sem áudio**
- Verifique drivers de áudio do Windows
- Confirme que a aplicação tem permissão de áudio
- Teste com outros aplicativos de som

## 🎯 Recursos Técnicos

- **Avalonia UI 11.3** - Interface moderna e responsiva
- **Performance otimizada** com Compiled Bindings
- **Comunicação serial robusta** com System.IO.Ports
- **Gerenciamento de memória** inteligente
- **Sistema de logs** para debug
- **Arquitetura MVVM** bem estruturada

## 📄 Licença

Este projeto está licenciado sob a MIT License. Consulte o arquivo `LICENSE` para detalhes.

## 📞 Suporte

- **Issues**: Reporte problemas no GitHub
- **Documentação**: Este README e comentários no código
- **Debug**: Use o modo administrador para logs detalhados

---

**🎮 Mini Jogo LEDs - Diversão garantida com Arduino!**