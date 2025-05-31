// ===== CONFIGURAÇÕES E DEFINIÇÕES =====
#define NUM_LEDS 16
#define MATRIX_SIZE 4

// Pinos dos LEDs (ajuste conforme sua ligação)
const int ledPins[NUM_LEDS] = {2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, A0, A1, A2, A3};

// Estados dos jogos
enum GameMode {
  MENU = 0,
  PEGA_LUZ = 1,
  SEQUENCIA_MALUCA = 2,
  GATO_RATO = 3,
  ESQUIVA_METEOROS = 4,
  GUITAR_HERO = 5
};

// Estrutura do jogo
struct GameState {
  GameMode currentMode;
  bool gameActive;
  int score;
  int level;
  unsigned long gameStartTime;
  unsigned long lastUpdateTime;
  int difficulty;
};

GameState game;

// ===== VARIÁVEIS ESPECÍFICAS DOS JOGOS =====

// Pega-Luz
int pegaLuzTarget = -1;
unsigned long pegaLuzStartTime = 0;
bool pegaLuzWaitingResponse = false;
unsigned long pegaLuzNextSpawn = 0;

// Sequência Maluca
int sequenciaMaluca[50];
int sequenciaLength = 1;
int sequenciaCurrentStep = 0;
bool sequenciaShowingPattern = false;
bool sequenciaWaitingInput = false;
unsigned long sequenciaLastLed = 0;
int sequenciaDisplayIndex = 0;

// Gato e Rato
int gatoPosition = 0;
int ratoPosition = 1;
unsigned long gatoLastMove = 0;
unsigned long ratoLastMove = 0;
int gatoMoveDelay = 500;
int ratoMoveDelay = 300;

// Esquiva Meteoros
int playerPosition = 0;
bool meteoros[NUM_LEDS];
unsigned long meteorLastUpdate = 0;
int meteorSpeed = 1000;
unsigned long meteorBlinkTime = 0;
bool meteorVisible = true;

// Guitar Hero
struct Note {
  int position;
  unsigned long timing;
  bool active;
};
Note guitarNotes[20];
int guitarNotesCount = 0;
unsigned long guitarLastSpawn = 0;
int guitarSpawnDelay = 1000;
unsigned long guitarStartTime = 0;

void setup() {
  Serial.begin(9600);

  // Inicializar pinos dos LEDs
  for (int i = 0; i < NUM_LEDS; i++) {
    pinMode(ledPins[i], OUTPUT);
    digitalWrite(ledPins[i], LOW);
  }

  // Inicializar estado do jogo
  game.currentMode = MENU;
  game.gameActive = false;
  game.score = 0;
  game.level = 1;
  game.difficulty = 1;

  // Seed para números aleatórios
  randomSeed(analogRead(A5));

  // Teste inicial dos LEDs
  testLEDs();

  Serial.println("ARDUINO_READY");
}

void loop() {
  // Processar comandos seriais
  processSerialCommands();

  // Executar lógica do jogo atual
  if (game.gameActive) {
    switch (game.currentMode) {
      case PEGA_LUZ:
        updatePegaLuz();
        break;
      case SEQUENCIA_MALUCA:
        updateSequenciaMaluca();
        break;
      case GATO_RATO:
        updateGatoRato();
        break;
      case ESQUIVA_METEOROS:
        displayEsquivaMeteoros();
        break;
      case GUITAR_HERO:
        updateGuitarHero();
        break;
    }
  }

  delay(10); // Pequeno delay para estabilidade
}

// ===== FUNÇÕES DE COMUNICAÇÃO SERIAL =====
void processSerialCommands() {
  if (Serial.available()) {
    String command = Serial.readStringUntil('\n');
    command.trim();

    if (command.startsWith("START_GAME:")) {
      int mode = command.substring(11).toInt();
      startGame((GameMode)mode);
    }
    else if (command == "STOP_GAME") {
      stopGame();
    }
    else if (command.startsWith("KEY_PRESS:")) {
      int key = command.substring(10).toInt();
      handleKeyPress(key);
    }
    else if (command == "GET_STATUS") {
      sendGameStatus();
    }
    else if (command == "RESET_SCORE") {
      game.score = 0;
      game.level = 1;
    }
  }
}

void sendGameStatus() {
  Serial.print("STATUS:");
  Serial.print(game.currentMode);
  Serial.print(",");
  Serial.print(game.gameActive ? 1 : 0);
  Serial.print(",");
  Serial.print(game.score);
  Serial.print(",");
  Serial.println(game.level);
}

void sendGameEvent(String event, int value1 = -1, int value2 = -1) {
  Serial.print("EVENT:");
  Serial.print(event);
  if (value1 != -1) {
    Serial.print(",");
    Serial.print(value1);
  }
  if (value2 != -1) {
    Serial.print(",");
    Serial.print(value2);
  }
  Serial.println();
}

// ===== FUNÇÕES DE CONTROLE DOS LEDs =====
void setLED(int index, bool state) {
  if (index >= 0 && index < NUM_LEDS) {
    digitalWrite(ledPins[index], state ? HIGH : LOW);
  }
}

void clearAllLEDs() {
  for (int i = 0; i < NUM_LEDS; i++) {
    setLED(i, false);
  }
}

void testLEDs() {
  // Acender todos os LEDs sequencialmente para teste
  for (int i = 0; i < NUM_LEDS; i++) {
    setLED(i, true);
    delay(50);
    setLED(i, false);
  }
}

// ===== FUNÇÕES DE CONTROLE DO JOGO =====
void startGame(GameMode mode) {
  game.currentMode = mode;
  game.gameActive = true;
  game.score = 0;
  game.level = 1;
  game.difficulty = 1;
  game.gameStartTime = millis();
  game.lastUpdateTime = millis();

  clearAllLEDs();

  // Inicializar jogo específico
  switch (mode) {
    case PEGA_LUZ:
      initPegaLuz();
      break;
    case SEQUENCIA_MALUCA:
      initSequenciaMaluca();
      break;
    case GATO_RATO:
      initGatoRato();
      break;
    case ESQUIVA_METEOROS:
      initEsquivaMeteoros();
      break;
    case GUITAR_HERO:
      initGuitarHero();
      break;
  }

  sendGameEvent("GAME_STARTED", mode);
}

void stopGame() {
  game.gameActive = false;
  clearAllLEDs();
  sendGameEvent("GAME_STOPPED", game.score);
}

void handleKeyPress(int key) {
  if (!game.gameActive) return;

  switch (game.currentMode) {
    case PEGA_LUZ:
      handlePegaLuzKey(key);
      break;
    case SEQUENCIA_MALUCA:
      handleSequenciaMalucaKey(key);
      break;
    case GATO_RATO:
      handleGatoRatoKey(key);
      break;
    case ESQUIVA_METEOROS:
      handleEsquivaMeteoros(key);
      break;
    case GUITAR_HERO:
      handleGuitarHeroKey(key);
      break;
  }
}

// ===== JOGO 1: PEGA-LUZ =====
void initPegaLuz() {
  pegaLuzTarget = -1;
  pegaLuzWaitingResponse = false;
  pegaLuzNextSpawn = millis() + random(500, 1500);
}

void updatePegaLuz() {
  unsigned long currentTime = millis();

  // Verificar timeout se estiver esperando resposta
  if (pegaLuzWaitingResponse && currentTime - pegaLuzStartTime > 2000) {
    // Timeout - jogador não respondeu a tempo
    sendGameEvent("PEGA_LUZ_TIMEOUT");
    if (game.score > 0) game.score--;
    pegaLuzWaitingResponse = false;
    pegaLuzNextSpawn = currentTime + random(500, 1500);
    clearAllLEDs();
  }

  // Spawn novo target se não estiver esperando resposta
  if (!pegaLuzWaitingResponse && currentTime >= pegaLuzNextSpawn) {
    spawnNewPegaLuzTarget();
  }
}

void spawnNewPegaLuzTarget() {
  clearAllLEDs();

  pegaLuzTarget = random(0, NUM_LEDS);
  setLED(pegaLuzTarget, true);
  pegaLuzStartTime = millis();
  pegaLuzWaitingResponse = true;

  sendGameEvent("PEGA_LUZ_TARGET", pegaLuzTarget);
}

void handlePegaLuzKey(int key) {
  if (!pegaLuzWaitingResponse) return;

  if (key == pegaLuzTarget) {
    // Acertou!
    unsigned long reactionTime = millis() - pegaLuzStartTime;
    game.score++;

    // Aumentar dificuldade a cada 5 pontos
    if (game.score % 5 == 0) {
      game.level++;
      game.difficulty++;
    }

    sendGameEvent("PEGA_LUZ_HIT", reactionTime);
    pegaLuzWaitingResponse = false;
    pegaLuzNextSpawn = millis() + random(300, 1000); // Mais rápido com dificuldade
    clearAllLEDs();
  } else {
    // Errou
    sendGameEvent("PEGA_LUZ_MISS");
    if (game.score > 0) game.score--;
  }
}

// ===== JOGO 2: SEQUÊNCIA MALUCA =====
void initSequenciaMaluca() {
  sequenciaLength = 1;
  sequenciaCurrentStep = 0;
  sequenciaShowingPattern = false;
  sequenciaWaitingInput = false;

  // Gerar primeira sequência
  for (int i = 0; i < 50; i++) {
    sequenciaMaluca[i] = random(0, NUM_LEDS);
  }

  startSequenciaRound();
}

void updateSequenciaMaluca() {
  if (sequenciaShowingPattern) {
    if (millis() - sequenciaLastLed > 600) {
      clearAllLEDs();

      if (sequenciaDisplayIndex < sequenciaLength) {
        setLED(sequenciaMaluca[sequenciaDisplayIndex], true);
        sequenciaDisplayIndex++;
        sequenciaLastLed = millis();
      } else {
        sequenciaShowingPattern = false;
        sequenciaWaitingInput = true;
        sequenciaCurrentStep = 0;
        sendGameEvent("SEQUENCIA_INPUT_START");
      }
    }
  }
}

void startSequenciaRound() {
  clearAllLEDs();
  sequenciaShowingPattern = true;
  sequenciaDisplayIndex = 0;
  sequenciaLastLed = millis();
  sendGameEvent("SEQUENCIA_SHOW_START", sequenciaLength);
}

void handleSequenciaMalucaKey(int key) {
  if (!sequenciaWaitingInput) return;

  if (key == sequenciaMaluca[sequenciaCurrentStep]) {
    sequenciaCurrentStep++;
    setLED(key, true);
    delay(100);
    setLED(key, false);

    if (sequenciaCurrentStep >= sequenciaLength) {
      // Completou a sequência!
      game.score++;
      game.level++;
      sequenciaLength++;
      sequenciaWaitingInput = false;

      sendGameEvent("SEQUENCIA_COMPLETE");
      delay(1000);
      startSequenciaRound();
    }
  } else {
    // Errou a sequência
    sendGameEvent("SEQUENCIA_WRONG");
    if (game.score > 0) game.score--;
    sequenciaWaitingInput = false;
    delay(1000);
    startSequenciaRound();
  }
}

// ===== JOGO 3: GATO E RATO =====
void initGatoRato() {
  gatoPosition = 0;
  ratoPosition = NUM_LEDS / 2;
  gatoLastMove = millis();
  ratoLastMove = millis();
  gatoMoveDelay = 500;
  ratoMoveDelay = max(300 - (game.level * 20), 100); // Rato mais rápido com nível
  updateGatoRatoDisplay();
}

void updateGatoRato() {
  unsigned long currentTime = millis();

  // Mover o rato automaticamente
  if (currentTime - ratoLastMove > ratoMoveDelay) {
    int newRatoPos;
    do {
      newRatoPos = random(0, NUM_LEDS);
    } while (newRatoPos == ratoPosition || newRatoPos == gatoPosition);

    ratoPosition = newRatoPos;
    ratoLastMove = currentTime;
  }

  // Verificar se o gato pegou o rato
  if (gatoPosition == ratoPosition) {
    game.score++;
    game.level++;

    // Aumentar dificuldade
    ratoMoveDelay = max(ratoMoveDelay - 20, 100);

    sendGameEvent("GATO_CAUGHT_RATO");

    // Reposicionar
    gatoPosition = random(0, NUM_LEDS);
    do {
      ratoPosition = random(0, NUM_LEDS);
    } while (ratoPosition == gatoPosition);
  }

  updateGatoRatoDisplay();
}

void handleGatoRatoKey(int key) {
  if (key >= 0 && key < NUM_LEDS) {
    gatoPosition = key;
  }
}

void updateGatoRatoDisplay() {
  clearAllLEDs();
  setLED(gatoPosition, true); // Gato (LED contínuo)

  // Rato (LED piscando)
  static bool ratoVisible = true;
  static unsigned long lastBlink = 0;

  if (millis() - lastBlink > 250) {
    ratoVisible = !ratoVisible;
    lastBlink = millis();
  }

  if (ratoVisible && ratoPosition != gatoPosition) {
    setLED(ratoPosition, true);
  }
}

// ===== JOGO 4: ESQUIVA METEOROS =====
void initEsquivaMeteoros() {
  playerPosition = NUM_LEDS / 2;
  meteorSpeed = 1000;
  meteorLastUpdate = millis();
  meteorBlinkTime = millis();
  meteorVisible = true;

  for (int i = 0; i < NUM_LEDS; i++) {
    meteoros[i] = false;
  }

  updateEsquivaMeteoros();
}

void displayEsquivaMeteoros() {
  unsigned long currentTime = millis();

  // Atualizar meteoros
  if (currentTime - meteorLastUpdate > meteorSpeed) {
    // Spawn novo meteoro aleatoriamente
    if (random(0, 100) < 30) { // 30% chance
      int meteorPos;
      do {
        meteorPos = random(0, NUM_LEDS);
      } while (meteorPos == playerPosition || meteoros[meteorPos]);

      meteoros[meteorPos] = true;
    }

    // Verificar colisão
    if (meteoros[playerPosition]) {
      sendGameEvent("METEOR_HIT");
      if (game.score > 0) game.score--;
      meteoros[playerPosition] = false;
    }

    // Remover meteoros antigos aleatoriamente
    for (int i = 0; i < NUM_LEDS; i++) {
      if (meteoros[i] && random(0, 100) < 20) { // 20% chance de desaparecer
        meteoros[i] = false;
        if (i != playerPosition) game.score++;
      }
    }

    // Aumentar dificuldade
    if (meteorSpeed > 200) meteorSpeed -= 2;

    meteorLastUpdate = currentTime;
  }

  // Atualizar display
  displayEsquivaMeteoros();
}

void updateEsquivaMeteoros() {
  clearAllLEDs();

  // Mostrar jogador (LED contínuo)
  setLED(playerPosition, true);

  // Mostrar meteoros (LEDs piscando)
  if (millis() - meteorBlinkTime > 150) {
    meteorVisible = !meteorVisible;
    meteorBlinkTime = millis();
  }

  if (meteorVisible) {
    for (int i = 0; i < NUM_LEDS; i++) {
      if (meteoros[i] && i != playerPosition) {
        setLED(i, true);
      }
    }
  }
}

void handleEsquivaMeteoros(int key) {
  if (key >= 0 && key < NUM_LEDS) {
    playerPosition = key;
  }
}

// ===== JOGO 5: GUITAR HERO =====
void initGuitarHero() {
  guitarNotesCount = 0;
  guitarLastSpawn = millis();
  guitarSpawnDelay = 1000;
  guitarStartTime = millis();

  for (int i = 0; i < 20; i++) {
    guitarNotes[i].active = false;
  }
}

void updateGuitarHero() {
  unsigned long currentTime = millis();

  // Spawn novas notas
  if (currentTime - guitarLastSpawn > guitarSpawnDelay) {
    spawnGuitarNote();
    guitarLastSpawn = currentTime;

    // Diminuir delay para aumentar dificuldade
    if (guitarSpawnDelay > 300) guitarSpawnDelay -= 10;
  }

  // Atualizar notas existentes
  for (int i = 0; i < 20; i++) {
    if (guitarNotes[i].active) {
      // Verificar se a nota expirou
      if (currentTime - guitarNotes[i].timing > 3000) {
        guitarNotes[i].active = false;
        sendGameEvent("GUITAR_MISS");
        if (game.score > 0) game.score--;
      }
    }
  }

  updateGuitarHeroDisplay();
}

void spawnGuitarNote() {
  for (int i = 0; i < 20; i++) {
    if (!guitarNotes[i].active) {
      guitarNotes[i].position = random(0, 4); // Apenas 4 posições para simplificar
      guitarNotes[i].timing = millis();
      guitarNotes[i].active = true;
      sendGameEvent("GUITAR_NOTE_SPAWN", guitarNotes[i].position);
      break;
    }
  }
}

void handleGuitarHeroKey(int key) {
  if (key < 0 || key >= 4) return; // Apenas 4 teclas válidas

  unsigned long currentTime = millis();
  bool hitNote = false;

  for (int i = 0; i < 20; i++) {
    if (guitarNotes[i].active && guitarNotes[i].position == key) {
      unsigned long timeDiff = currentTime - guitarNotes[i].timing;

      if (timeDiff < 500) { // Janela de tempo para acertar
        guitarNotes[i].active = false;
        game.score++;
        sendGameEvent("GUITAR_HIT", timeDiff);
        hitNote = true;
        break;
      }
    }
  }

  if (!hitNote) {
    sendGameEvent("GUITAR_WRONG");
    if (game.score > 0) game.score--;
  }
}

void updateGuitarHeroDisplay() {
  clearAllLEDs();

  unsigned long currentTime = millis();

  for (int i = 0; i < 20; i++) {
    if (guitarNotes[i].active) {
      // Calcular posição da nota baseada no tempo
      unsigned long elapsed = currentTime - guitarNotes[i].timing;
      int row = (elapsed / 500) % 4; // Nota desce pela matriz
      int col = guitarNotes[i].position;
      int ledIndex = row * 4 + col;

      if (ledIndex >= 0 && ledIndex < NUM_LEDS) {
        setLED(ledIndex, true);
      }
    }
  }
}
