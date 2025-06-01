# Mini Jogo LEDs - Arduino

Um sistema de jogos interativos usando uma matriz 4x4 de LEDs conectada ao Arduino Uno, controlado por uma aplicação desktop em Avalonia UI com 8 jogos diferentes, incluindo modos de alta dificuldade para monetização.

## 🎮 Jogos Disponíveis

### Jogos Clássicos

#### 1. 🎯 Pega-Luz
- **Objetivo**: Reagir rapidamente aos LEDs que acendem
- **Como jogar**: Quando um LED acender, pressione a tecla correspondente o mais rápido possível
- **Pontuação**: +10 pontos por acerto, baseado no tempo de reação
- **Dificuldade**: Timeout diminui de 2s para 0.5s a cada 50 pontos

#### 2. 🧠 Sequência Maluca
- **Objetivo**: Memorizar e repetir sequências de LEDs
- **Como jogar**: Observe a sequência que pisca e repita pressionando as teclas na ordem correta
- **Pontuação**: +1 ponto por sequência completa
- **Dificuldade**: Sequência cresce +1 LED a cada nível

#### 3. 🐱 Gato e Rato
- **Objetivo**: Controlar o "gato" para pegar o "rato"
- **Como jogar**: Use as teclas ou setas para mover o gato (LED fixo) até a posição do rato (LED piscante)
- **Pontuação**: +20 pontos cada vez que pegar o rato
- **Dificuldade**: Rato se move mais rapidamente (-50ms por captura)

#### 4. ☄️ Esquiva Meteoros
- **Objetivo**: Sobreviver aos meteoros que aparecem aleatoriamente
- **Como jogar**: Mova-se para posições seguras evitando os meteoros (LEDs piscantes)
- **Pontuação**: +1 ponto por segundo de sobrevivência
- **Dificuldade**: Meteoros aparecem mais frequentemente

#### 5. 🎸 Guitar Hero
- **Objetivo**: "Tocar" as notas no momento certo
- **Como jogar**: Use as teclas 0-9, A-F para tocar as notas conforme o ritmo
- **Pontuação**: +1 ponto por nota acertada, combos multiplicam pontos
- **Dificuldade**: Velocidade das notas aumenta progressivamente

### Jogos de Alta Dificuldade (Monetização)

#### 6. 🎲 Roleta Russa LED
- **Objetivo**: Jogo de sorte com multiplicadores exponenciais
- **Como jogar**: Escolha um LED de 16 possíveis - 1 é seguro, 15 são "
bombas"
- **Pontuação**: Multiplicadores: 2x, 4x, 8x, 16x... até 256x
- **Risco**: Acertar = continua com multiplicador maior | Errar = perde TODA a pontuação
- **Probabilidade**: 1 em 16 chance de acerto por rodada

#### 7. ⚡ Lightning Strike
- **Objetivo**: Memorização ultra-rápida de padrões que aparecem por milissegundos
- **Como jogar**: Padrão de LEDs pisca rapidamente, reproduza exatamente
- **Pontuação**: +1 ponto por padrão completo
- **Dificuldade**: Tempo de exibição diminui -50ms por nível (mínimo 100ms)
- **Falha**: UM erro = Game Over instantâneo

#### 8. 🎯 Sniper Mode
- **Objetivo**: Precisão impossível - alvos piscam por apenas 0.1 segundo
- **Como jogar**: Pressione a tecla exata enquanto o LED pisca (100ms)
- **Pontuação**: +1 ponto por acerto
- **Meta**: 10 acertos seguidos = VITÓRIA IMPOSSÍVEL
- **Bônus**: Completar = Multiplicador x10 na pontuação final

## 🔧 Hardware Necessário

### Arduino Uno
- 1x Arduino Uno R3
- 16x LEDs simples (organizados por cor)
- 16x Resistores 220Ω
- 1x Protoboard
- Jumpers macho-macho
- Cabo USB para conexão com PC

### Layout Físico da Matriz
```
Linha 0 (VERMELHA):  LED 0-3   → Pinos 2-5
Linha 1 (AMARELA):   LED 4-7   → Pinos 6-9
Linha 2 (VERDE):     LED 8-11  → Pinos 10-13
Linha 3 (AZUL):      LED 12-15 → Pinos A0-A3
```

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
3. Conecte os LEDs conforme o diagrama acima (organizados por cor)
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

### Interface da Aplicação (1200x800)
- **Header**: Nome do jogador e status de conexão
- **Painel Esquerdo**: Controles de jogo, seleção e configurações
- **Painel Central**: Matriz LED visual com cores correspondentes
- **Status Bar**: Jogo atual, pontuação, nível e tempo
- **Quick Actions**: Início rápido, configurações e ajuda

### Controles do Teclado
- **F1**: Iniciar jogo selecionado
- **F2**: Parar jogo atual
- **F3**: Resetar pontuação
- **F4**: Abrir janela de rankings
- **0-9, A-F**: Correspondem aos 16 LEDs da matriz (0-15)
- **↑↓←→**: Movimento (jogos compatíveis)
- **Enter**: Confirmar ação
- **Esc**: Cancelar/Voltar

### Mapeamento dos LEDs
```
Matriz 4x4 com Cores:
[0🔴] [1🔴] [2🔴] [3🔴]     →  Teclas: [0] [1] [2] [3]
[4🟡] [5🟡] [6🟡] [7🟡]     →  Teclas: [4] [5] [6] [7]
[8🟢] [9🟢] [A🟢] [B🟢]     →  Teclas: [8] [9] [A] [B]
[C🔵] [D🔵] [E🔵] [F🔵]     →  Teclas: [C] [D] [E] [F]
```

### Fluxo do Jogo
1. **Definir Nome**: Digite seu nome e clique em 💾
2. **Selecionar Jogo**: Escolha um dos 8 jogos no ComboBox
3. **Conectar Arduino**: Certifique-se que o Arduino está conectado
4. **Iniciar**: Pressione F1 ou clique "▶️ Iniciar"
5. **Jogar**: Use as teclas conforme as instruções específicas do jogo
6. **Parar**: Pressione F2 quando quiser finalizar

### Funcionalidades Avançadas
- **⚡ Início Rápido**: Configuração automática e início imediato
- **📖 Instruções Completas**: Guia detalhado de todos os jogos
- **⚙️ Configurações**: Personalizar nome e porta serial
- **❓ Ajuda**: Manual completo com troubleshooting
- **🔍 Console Debug**: Monitoramento em tempo real da comunicação

## 📊 Sistema de Pontuação e Rankings

### Pontuação Individual
- Cada jogo mantém pontuação e nível separadamente
- Pontuações são salvas automaticamente ao final de cada partida
- Sistema de score específico por jogo (10 pts, 20 pts, multiplicadores)
- Histórico completo disponível na janela de rankings (F4)

### Sistema de Rankings
- **Top 10 Global**: Melhores pontuações de todos os jogos
- **Filtros**: Por jogo específico ou por jogador
- **Estatísticas**: Top 3 jogadores, jogos mais populares, atividade recente
- **Exportação**: CSV e TXT para análise externa
- **Tempo de Jogo**: Duração de cada partida registrada

### Perfil de Dificuldade
1. **Pega-Luz**: ⭐⭐ (Aquecimento)
2. **Sequência Maluca**: ⭐⭐⭐ (Memória)
3. **Gato e Rato**: ⭐⭐ (Estratégia)
4. **Esquiva Meteoros**: ⭐⭐⭐ (Reflexos)
5. **Guitar Hero**: ⭐⭐⭐⭐ (Ritmo)
6. **🎲 Roleta Russa**: ⭐⭐⭐⭐⭐ (Sorte + Coragem)
7.
 **⚡ Lightning Strike**: ⭐⭐⭐⭐⭐ (Memorização Extrema)
8. **🎯 Sniper Mode**: ⭐⭐⭐⭐⭐ (Precisão Impossível)

## 🛠️ Desenvolvimento

### Tecnologias Utilizadas
- **Frontend**: Avalonia UI (.NET 9.0) - Interface responsiva 1200x800
- **Hardware**: Arduino Uno com comunicação serial (9600 baud)
- **Persistência**: JSON local para pontuações e configurações
- **Arquitetura**: MVVM com Services para comunicação e dados

### Estrutura do Projeto
```
miniJogo/
├── arduino_led_games.ino           # Código Arduino (8 jogos)
├── MainWindow.axaml                # Interface principal (1200x800)
├── MainWindow.axaml.cs            # Lógica principal e eventos
├── Models/
│   └── GameData.cs                # Modelos (GameScore, PlayerScore)
├── Services/
│   ├── ArduinoService.cs          # Comunicação serial
│   └── ScoreService.cs            # Gerenciamento de pontuações
├── Views/
│   ├── ScoresWindow.axaml         # Interface de rankings
│   ├── ScoresWindow.axaml.cs      # Lógica de rankings
│   └── [...]                     # Outras janelas
├── InstructionsWindow.axaml       # Manual completo dos jogos
├── DebugWindow.axaml              # Console de debug
└── README.md                      # Esta documentação
```

### Protocolo de Comunicação Serial
```
PC → Arduino:
- START_GAME:[1-8]     # Iniciar jogo específico (1-8)
- STOP_GAME            # Parar jogo atual
- KEY_PRESS:[0-15]     # Tecla pressionada (0-9, A-F)
- MOVE:[UP|DOWN|LEFT|RIGHT] # Movimento direcional
- INIT                 # Inicializar Arduino
- DISCONNECT           # Desconectar e limpar

Arduino → PC:
- READY                # Arduino pronto
- GAME_EVENT:[type]:[data] # Eventos específicos de cada jogo
- LED_ON:[index]       # Ligar LED específico
- LED_OFF:[index]      # Desligar LED específico
- ALL_LEDS_OFF         # Desligar todos os LEDs
```

### Eventos Específicos dos Jogos
```
Roleta Russa:
- ROLETA_ROUND_START:[round],[multiplier]
- ROLETA_SAFE:[led],[score]
- ROLETA_EXPLODE:[led],[0]
- ROLETA_MAX_WIN:[score]

Lightning Strike:
- LIGHTNING_PATTERN_SHOW:[length],[duration]
- LIGHTNING_INPUT_START
- LIGHTNING_COMPLETE:[level],[duration]
- LIGHTNING_WRONG:[wrong_key],[correct_key]

Sniper Mode:
- SNIPER_TARGET_SPAWN:[target],[duration]
- SNIPER_HIT:[hits],[reaction_time]
- SNIPER_MISS:[key],[reaction_time]
- SNIPER_TIMEOUT
- SNIPER_VICTORY:[final_score]
```

## 🐛 Sistema de Debug

### Recursos de Debug Implementados
- **Console Debug**: Interface visual em tempo real para monitoramento
- **Logging Completo**: Todos os comandos e eventos são registrados
- **Timestamps**: Cada mensagem tem timestamp preciso
- **Filtros**: Separação entre mensagens normais e debug
- **Exportação**: Salvar logs em arquivo TXT para análise
- **Limpar Console**: Reset do histórico quando necessário

### Uso do Sistema de Debug
1. Execute a aplicação C#
2. Clique em "🔍 Console Debug" no painel de ferramentas
3. Conecte o Arduino - todas as mensagens aparecerão em tempo real
4. Use "Limpar" para resetar o histórico
5. Use "💾 Salvar" para exportar logs

## 🐛 Solução de Problemas

### Arduino não conecta
- Verifique se a porta COM está correta
- Certifique-se que o Arduino está ligado e reconhecido pelo Windows
- Tente diferentes portas COM (COM3, COM4, etc.)
- Reinicie o Arduino desconectando e reconectando o USB
- Verifique se outro programa não está usando a porta (Arduino IDE, etc.)

### LEDs não funcionam
- Confira as conexões conforme o diagrama (especialmente as cores por linha)
- Teste os LEDs individualmente com multímetro
- Verifique se os resistores estão corretos (220Ω, vermelho-vermelho-marrom)
- Confirme se o código foi uploadado corretamente (sem erros de compilação)
- Execute o teste inicial (LEDs acendem por linha: vermelho→amarelo→verde→azul)

### Jogo não responde às teclas
- Verifique se o jogo foi iniciado (status "🚀 Jogo iniciado!")
- Confirme se o Arduino está conectado (status verde no header)
- Teste as teclas 0-9 e A-F (use o console debug para verificar)
- Certifique-se que a janela do jogo está em foco
- Reinicie o jogo com F2 e F1

### Performance lenta ou travamentos
- Feche outros programas que usam muita CPU
- Verifique se o cabo USB está bem conectado
- Evite usar extensões USB ou hubs
- Monitor a comunicação no console debug para ver delays
- Reinicie a aplicação se necessário

### Problemas de pontuação
- Certifique-se que o nome do jogador foi definido e salvo
- Verifique se o jogo terminou corretamente (não foi forçado)
- Confirme que há espaço em disco para salvar as pontuações
- Use o botão "🔄 Atualizar" na janela de rankings se necessário

## 🎯 Estratégias dos Jogos

### Jogos de Monetização (Dicas Avançadas)

#### 🎲 Roleta Russa
- **Probabilidade**: 6.25% de sucesso por rodada (1/16)
- **Estratégia**: Parar no multiplicador 4x-8x para lucro consistente
- **Risco Calculado**: Multiplicador 16x = 0.39% de chance de sucesso
- **Psicologia**: Sistema vicia pela tensão e possibilidade de grandes ganhos

#### ⚡ Lightning Strike
- **Memória**: Use técnicas de palácio mental para padrões grandes
- **Treino**: Comece com padrões de 3-4 LEDs para treinar
- **Concentração**: Elimine todas as distrações visuais e sonoras
- **Limite**: Padrões de 8+ LEDs são quase impossíveis para humanos

#### 🎯 Sniper Mode
- **Posição**: Mantenha dedos sobre as 16 teclas
- **Foco**: Olhe para o centro da matriz, use visão periférica
- **Antecipação**: Alvos seguem padrões semi-aleatórios
- **Meta**: 10 acertos = 0.000000095% de probabilidade (quase impossível)

## 📈 Potencial de Monetização

### Modelos de Negócio
- **Arcade Premium**: Jogos normais grátis, premium pagos
- **Sistema de Créditos**: Compra créditos para jogos de alto risco
- **Torneios**: Competições pagas com premiações
- **Conquistas Raras**: Recompensas por feitos impossíveis

### Métricas de Engajamento
- **Tempo de Sessão**: Média 15-30 minutos por sessão
- **Taxa de Retorno**: Jogos viciantes com alta retenção
- **Viralidade**: Desafios impossíveis geram compartilhamento
- **Progressão**: Sistema de níveis mantém engajamento

## 📝 Licença

Este projeto é open source e está disponível sob a licença MIT.

## 🤝 Contribuições

Contribuições são bem-vindas! Sinta-se à vontade para:
- Reportar bugs ou problemas de performance
- Sugerir novos jogos ou mecânicas
- Melhorar a documentação e tutoriais
- Adicionar funcionalidades de monetização
- Otimizar o código Arduino ou C#
- Criar testes automatizados

### Roadmap Futuro
- [ ] Sistema de achievements/conquistas
- [ ] Modo multiplayer local
- [ ] Integração com rankings online
- [ ] Suporte para matrizes maiores (8x8)
- [ ] Sistema de moedas virtuais
- [ ] Efeitos sonoros e música
- [ ] Modo treino com tutoriais interativos
- [ ] API para integração com outros sistemas

## 📞 Suporte

Para dúvidas, problemas ou sugestões:
- **Issues**: Abra uma issue no repositório do projeto
- **Debug**: Use o console debug integrado para diagnósticos
- **Documentação**: Este README contém soluções para problemas comuns
- **Email**: [seu-email] para questões críticas

---

**Versão**: 2.0.0  
**Última Atualização**: 2024  
**Compatibilidade**: .NET 9.0, Arduino IDE 2.x, Windows 10/11