# Mini Jogo LEDs - Arduino

Um sistema de jogos interativos usando uma matriz 4x4 de LEDs conectada ao Arduino Uno, controlado por uma aplicação desktop em Avalonia UI.

## 🎮 Jogos Disponíveis

### 1. ⚡ Pega-Luz
- **Objetivo**: Reagir rapidamente aos LEDs que acendem
- **Como jogar**: Quando um LED acender, pressione a tecla correspondente o mais rápido possível
- **Pontuação**: Baseada no tempo de reação e acertos consecutivos
- **Dificuldade**: Aumenta a velocidade a cada 5 pontos

### 2. 🧠 Sequência Maluca
- **Objetivo**: Memorizar e repetir sequências de LEDs
- **Como jogar**: Observe a sequência que pisca e repita pressionando as teclas na ordem correta
- **Pontuação**: +1 ponto por sequência completa
- **Dificuldade**: Sequência fica mais longa a cada acerto

### 3. 🐱 Gato e Rato
- **Objetivo**: Controlar o "gato" para pegar o "rato"
- **Como jogar**: Use as teclas para mover o gato (LED fixo) até a posição do rato (LED piscante)
- **Pontuação**: +1 ponto cada vez que pegar o rato
- **Dificuldade**: Rato se move mais rapidamente

### 4. ☄️ Esquiva Meteoros
- **Objetivo**: Sobreviver aos meteoros que aparecem aleatoriamente
- **Como jogar**: Mova-se para posições seguras evitando os meteoros (LEDs piscantes)
- **Pontuação**: +1 ponto por meteoro evitado
- **Dificuldade**: Meteoros aparecem mais frequentemente

### 5. 🎸 Guitar Hero
- **Objetivo**: "Tocar" as notas no momento certo
- **Como jogar**: Use as teclas 0-3 para tocar as notas quando elas chegarem na linha inferior
- **Pontuação**: +1 ponto por nota acertada no tempo correto
- **Dificuldade**: Notas aparecem mais rapidamente

## 🔧 Hardware Necessário

### Arduino Uno
- 1x Arduino Uno R3
- 16x LEDs (qualquer cor)
- 16x Resistores 220Ω
- 1x Protoboard
- Jumpers macho-macho
- Cabo USB para conexão com PC

### Conexões dos LEDs
```
LED 0  (Posição 0,0) → Pino Digital 2  + Resistor 220Ω → GND
LED 1  (Posição 0,1) → Pino Digital 3  + Resistor 220Ω → GND
LED 2  (Posição 0,2) → Pino Digital 4  + Resistor 220Ω → GND
LED 3  (Posição 0,3) → Pino Digital 5  + Resistor 220Ω → GND
LED 4  (Posição 1,0) → Pino Digital 6  + Resistor 220Ω → GND
LED 5  (Posição 1,1) → Pino Digital 7  + Resistor 220Ω → GND
LED 6  (Posição 1,2) → Pino Digital 8  + Resistor 220Ω → GND
LED 7  (Posição 1,3) → Pino Digital 9  + Resistor 220Ω → GND
LED 8  (Posição 2,0) → Pino Digital 10 + Resistor 220Ω → GND
LED 9  (Posição 2,1) → Pino Digital 11 + Resistor 220Ω → GND
LED 10 (Posição 2,2) → Pino Digital 12 + Resistor 220Ω → GND
LED 11 (Posição 2,3) → Pino Digital 13 + Resistor 220Ω → GND
LED 12 (Posição 3,0) → Pino Analógico A0 + Resistor 220Ω → GND
LED 13 (Posição 3,1) → Pino Analógico A1 + Resistor 220Ω → GND
LED 14 (Posição 3,2) → Pino Analógico A2 + Resistor 220Ω → GND
LED 15 (Posição 3,3) → Pino Analógico A3 + Resistor 220Ω → GND
```

## 🚀 Instalação e Configuração

### 1. Preparar o Arduino
1. Abra o Arduino IDE
2. Carregue o arquivo `arduino_led_games.ino` no Arduino Uno
3. Conecte os LEDs conforme o diagrama acima
4. Faça upload do código para o Arduino

### 2. Executar a Aplicação
```bash
# Clone o repositório
git clone [url-do-repositorio]
cd miniJogo

# Restaurar dependências
dotnet restore

# Compilar e executar
dotnet run
```

### 3. Conectar o Arduino
1. Conecte o Arduino via USB ao computador
2. Na aplicação, selecione a porta COM correta (geralmente COM3, COM4, etc.)
3. Clique em "🔗 Conectar"
4. Aguarde a confirmação "Arduino pronto para jogar!"

## 🎯 Como Jogar

### Controles do Teclado
- **F1**: Iniciar jogo selecionado
- **F2**: Parar jogo atual
- **F3**: Resetar pontuação
- **F4**: Abrir janela de pontuações
- **0-9, A-F**: Correspondem aos 16 LEDs da matriz (0-15)

### Mapeamento dos LEDs
```
Matriz 4x4:
[0] [1] [2] [3]     →  Teclas: [0] [1] [2] [3]
[4] [5] [6] [7]     →  Teclas: [4] [5] [6] [7]
[8] [9] [A] [B]     →  Teclas: [8] [9] [A] [B]
[C] [D] [E] [F]     →  Teclas: [C] [D] [E] [F]
```

### Fluxo do Jogo
1. **Definir Nome**: Digite seu nome no campo "Jogador"
2. **Selecionar Jogo**: Escolha um dos 5 jogos disponíveis
3. **Conectar Arduino**: Certifique-se que o Arduino está conectado
4. **Iniciar**: Pressione F1 ou clique "▶️ Iniciar Jogo"
5. **Jogar**: Use as teclas 0-9 e A-F conforme as instruções do jogo
6. **Parar**: Pressione F2 quando quiser finalizar

## 📊 Sistema de Pontuação

### Pontuação Individual
- Cada jogo mantém pontuação e nível separadamente
- Pontuações são salvas automaticamente ao final de cada partida
- Histórico completo disponível na janela de pontuações (F4)

### Estatísticas Disponíveis
- **Pontuações Altas**: Top scores por jogo ou geral
- **Estatísticas do Jogador**: Análise detalhada por jogador
- **Jogos Recentes**: Histórico das últimas partidas
- **Exportação**: Dados podem ser exportados para CSV

## 🛠️ Desenvolvimento

### Tecnologias Utilizadas
- **Frontend**: Avalonia UI (.NET 9.0)
- **Hardware**: Arduino Uno com comunicação serial
- **Persistência**: JSON local para pontuações
- **Comunicação**: Serial Port (115200 baud)

### Estrutura do Projeto
```
miniJogo/
├── arduino_led_games.ino    # Código do Arduino
├── MainWindow.axaml         # Interface principal
├── MainWindow.axaml.cs      # Lógica principal
├── Models/
│   └── GameData.cs          # Modelos de dados
├── Services/
│   ├── ArduinoService.cs    # Comunicação serial
│   └── ScoreService.cs      # Gerenciamento de pontuações
└── Views/
    ├── ScoreWindow.axaml    # Interface de pontuações
    └── ScoreWindow.axaml.cs # Lógica de pontuações
```

### Protocolo de Comunicação
```
PC → Arduino:
- START_GAME:[1-5]     # Iniciar jogo específico
- STOP_GAME            # Parar jogo atual
- KEY_PRESS:[0-15]     # Tecla pressionada
- RESET_SCORE          # Resetar pontuação
- GET_STATUS           # Solicitar status

Arduino → PC:
- STATUS:[mode],[active],[score],[level]
- EVENT:[type],[value1],[value2]...
- ARDUINO_READY
```

## 🐛 Solução de Problemas

### Arduino não conecta
- Verifique se a porta COM está correta
- Certifique-se que o Arduino está ligado
- Tente reconectar o cabo USB
- Verifique se outro programa não está usando a porta

### LEDs não funcionam
- Confira as conexões conforme o diagrama
- Teste os LEDs individualmente
- Verifique se os resistores estão corretos (220Ω)
- Confirme se o código foi uploadado corretamente

### Jogo não responde
- Verifique se o jogo foi iniciado (F1)
- Confirme se o Arduino está conectado
- Teste as teclas 0-9 e A-F
- Reinicie a aplicação se necessário

## 📝 Licença

Este projeto é open source e está disponível sob a licença MIT.

## 🤝 Contribuições

Contribuições são bem-vindas! Sinta-se à vontade para:
- Reportar bugs
- Sugerir novos jogos
- Melhorar a documentação
- Adicionar funcionalidades

## 📞 Suporte

Para dúvidas ou problemas, abra uma issue no repositório do projeto.