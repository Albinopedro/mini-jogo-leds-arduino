# 🎮 Mini Jogo LEDs - Arduino

Sistema de jogos interativos com matriz 4x4 de LEDs conectada ao Arduino, controlado por aplicação desktop Avalonia UI.

## 🚀 Quick Start

1. **Hardware**: Monte 16 LEDs em matriz 4x4 no Arduino Uno (pinos 2-13, A0-A3)
2. **Arduino**: Upload do arquivo `arduino_led_games.ino`
3. **Software**: Execute `dotnet run` no diretório do projeto
4. **Conectar**: Selecione porta COM e clique "Conectar"
5. **Jogar**: Escolha um jogo e pressione F1!

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
F9: 🎆 Fogos   | F10: ✨ Demo Completa
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
├── Models/GameData.cs        # Dados dos jogos
├── Services/               
│   ├── ArduinoService.cs     # Comunicação serial
│   └── ScoreService.cs       # Sistema de pontuação
└── Views/                    # Janelas de ranking e debug
```

## 📊 Sistema de Pontuação

- **Salvamento Automático**: Todas as partidas são salvas
- **Rankings**: Top 10 global e por jogo
- **Estatísticas**: Tempo de jogo, melhor pontuação, média
- **Exportação**: CSV/TXT para análise

## 🛠️ Troubleshooting

| Problema | Solução |
|----------|---------|
| Arduino não conecta | Verifique porta COM, reinicie Arduino |
| LEDs não acendem | Confira conexões e resistores 220Ω |
| Teclas não respondem | Jogo iniciado? Arduino conectado? |
| Performance lenta | Feche outros programas, use cabo USB direto |
| Efeitos não funcionam | Pressione F6 para resetar, reconecte Arduino |
| Animações travadas | Use F6 (Stop) + F10 (Demo) para testar |

## 🔌 Protocolo de Comunicação

### PC → Arduino
```
START_GAME:[1-8]    # Iniciar jogo
STOP_GAME           # Parar jogo
KEY_PRESS:[0-15]    # Tecla pressionada
KEY_RELEASE:[0-15]  # Tecla solta
```

### Arduino → PC
```
READY                           # Arduino pronto
GAME_EVENT:[tipo]:[dados]       # Eventos do jogo
LED_ON:[index]                  # Acender LED
LED_OFF:[index]                 # Apagar LED
```

## 🎮 Funcionalidades

- ✅ **8 jogos completos** com dificuldades variadas
- ✅ **Interface intuitiva** 1200x800 responsiva
- ✅ **Sistema de debug** em tempo real
- ✅ **Comunicação robusta** Arduino-PC
- ✅ **Rankings persistentes** com estatísticas
- ✅ **20+ efeitos visuais espetaculares** animados
- ✅ **Feedback visual/sonoro** para todos os eventos

## ✨ Sistema de Efeitos Visuais

### 🎭 **Animações Automáticas**
- **🚀 Inicialização**: Sequência épica de 4 segundos com espiral crescente
- **🔗 Conexão**: Explosão de alegria + ondas concêntricas  
- **⚡ Início de Jogo**: Countdown visual 3-2-1-GO com números formados
- **🎯 Acertos**: Explosões do centro para fora baseadas em precisão
- **🆙 Level Up**: Ondas de energia + estrela de vitória
- **💥 Game Over**: Implosão dramática + flash vermelho
- **🏆 Vitórias**: Fogos de artifício + chuva de estrelas
- **👋 Desconexão**: Espiral de despedida suave

### 🎪 **Efeitos Especiais (F5-F10)**
- **F5 - 🌈 Arco-íris**: Ondas coloridas contínuas por linha
- **F6 - ⏹️ Stop**: Para todos os efeitos visuais
- **F7 - 💚 Matrix Rain**: Chuva digital estilo Matrix
- **F8 - 💓 Pulso Universal**: Todos os LEDs pulsam sincronizados
- **F9 - 🎆 Fogos**: Múltiplas explosões sequenciais
- **F10 - ✨ Demo Completa**: Apresentação de 10 segundos

### 🎮 **Efeitos por Jogo**
- **Pega-Luz**: Feedback instantâneo de precisão (perfeito vs normal)
- **Sequência**: Confirmação visual para cada LED correto
- **Roleta Russa**: Explosão massiva quando "explode"
- **Lightning**: Padrões ultra-rápidos impossíveis de acompanhar
- **Sniper**: Vitória legendária com celebração épica
- **Combos**: Ondas laterais convergindo no centro

## 🏆 Jogos Premium

Os últimos 3 jogos são **extremamente difíceis** e ideais para monetização:

- **🎲 Roleta Russa**: Apenas 6.25% chance por rodada + explosão visual épica
- **⚡ Lightning Strike**: Padrões impossíveis + animações ultra-rápidas
- **🎯 Sniper Mode**: 0.000000095% chance + celebração legendária

### 🎬 **Experiência Cinematográfica**
Cada momento do jogo possui animações únicas que transformam a experiência:
- **20+ animações** diferentes para situações específicas
- **Timing perfeito** sincronizado com eventos do jogo  
- **Feedback visual** que recompensa habilidade e precisão
- **Efeitos épicos** para vitórias raras (Sniper 10/10, Roleta 8 rodadas)

## 📄 Licença

MIT License - Open Source

## 🤝 Contribuições

Issues e PRs são bem-vindos! Áreas para melhoria:
- Novos jogos e mecânicas
- Otimizações de performance  
- Sistema de achievements
- Modo multiplayer
- Integração online

---

**Versão 2.0.0** | **Suporte**: Issues no GitHub | **Compatibilidade**: .NET 9.0 + Arduino IDE 2.x