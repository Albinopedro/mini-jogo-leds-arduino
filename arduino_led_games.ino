// ===== MINI JOGO LEDs - ARDUINO CODE =====
// Sistema de jogos com matriz LED 4x4 (LEDs simples)

// ===== CONFIGURAÇÕES BÁSICAS =====
#define NUM_LEDS 16

// Pinos dos LEDs (conforme especificação do usuário)
const int ledPins[NUM_LEDS] = {
  2, 3, 4, 5,    // Linha 0 (vermelha) - LEDs 0-3
  6, 7, 8, 9,    // Linha 1 (amarela) - LEDs 4-7
  10, 11, 12, 13, // Linha 2 (verde) - LEDs 8-11
  A0, A1, A2, A3  // Linha 3 (azul) - LEDs 12-15
};

// ===== ENUM DOS MODOS DE JOGO =====
enum GameMode {
  MENU = 0,
  PEGA_LUZ = 1,
  SEQUENCIA_MALUCA = 2,
  GATO_RATO = 3,
  ESQUIVA_METEOROS = 4,
  GUITAR_HERO = 5,
  ROLETA_RUSSA = 6,
  LIGHTNING_STRIKE = 7,
  SNIPER_MODE = 8
};

// ===== ESTRUTURA DO JOGO =====
struct GameState {
  GameMode currentMode;
  bool gameActive;
  int score;
  int level;
  int difficulty;
  unsigned long gameStartTime;
  unsigned long lastUpdateTime;
};

GameState game;

// ===== VARIÁVEIS DOS JOGOS ORIGINAIS =====
// Pega-Luz
int pegaLuzTarget = -1;
unsigned long pegaLuzStartTime = 0;
unsigned long pegaLuzTimeout = 2000;

// Sequência Maluca
int sequenciaPattern[16];
int sequenciaLength = 3;
int sequenciaIndex = 0;
bool sequenciaShowingPattern = false;
bool sequenciaWaitingInput = false;
unsigned long sequenciaLastShow = 0;
int sequenciaDisplayIndex = 0;

// Gato e Rato
int gatoPosition = 0;
int ratoPosition = 8;
unsigned long ratoLastMove = 0;
unsigned long ratoMoveInterval = 1000;
bool ratoVisible = true;
unsigned long ratoLastBlink = 0;

// Esquiva Meteoros
int playerPosition = 12;
bool meteoros[NUM_LEDS];
unsigned long meteoroLastSpawn = 0;
unsigned long meteoroSpawnInterval = 1500;
unsigned long meteoroLastBlink = 0;
bool meteoroVisible = true;

// Guitar Hero
struct Note {
  int column;
  int row;
  unsigned long spawnTime;
  bool active;
};
Note notes[8];
int noteCount = 0;
unsigned long lastNoteSpawn = 0;
unsigned long noteSpeed = 1000;

// ===== VARIÁVEIS DOS NOVOS JOGOS =====
// Roleta Russa LED
int roletaSafeIndex = -1;
int roletaRound = 1;
float roletaMultiplier = 1.0;
bool roletaWaitingChoice = false;
unsigned long roletaChoiceTime = 0;

// Lightning Strike
int lightningPattern[16];
int lightningPatternLength = 3;
int lightningInputIndex = 0;
bool lightningShowingPattern = false;
bool lightningWaitingInput = false;
unsigned long lightningLastShow = 0;
unsigned long lightningShowDuration = 500;

// Sniper Mode
int sniperTarget = -1;
unsigned long sniperTargetTime = 0;
unsigned long sniperFlashDuration = 100;
bool sniperWaitingShot = false;
int sniperHitsRequired = 10;
int sniperCurrentHits = 0;

// ===== SETUP =====
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

  clearAllLEDs();
  randomSeed(analogRead(A4)); // Usar A4 para seed

  // Teste inicial dos LEDs
  testLEDs();

  Serial.println("READY");
}

// ===== FUNÇÕES AUXILIARES =====
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
  // Teste por linha para mostrar as cores
  // Linha vermelha (0-3)
  for (int i = 0; i < 4; i++) {
    setLED(i, true);
  }
  delay(500);
  clearAllLEDs();
  
  // Linha amarela (4-7)
  for (int i = 4; i < 8; i++) {
    setLED(i, true);
  }
  delay(500);
  clearAllLEDs();
  
  // Linha verde (8-11)
  for (int i = 8; i < 12; i++) {
    setLED(i, true);
  }
  delay(500);
  clearAllLEDs();
  
  // Linha azul (12-15)
  for (int i = 12; i < 16; i++) {
    setLED(i, true);
  }
  delay(500);
  clearAllLEDs();
}

void sendGameEvent(String eventType, int value1 = 0, int value2 = 0) {
  Serial.print("GAME_EVENT:");
  Serial.print(eventType);
  Serial.print(":");
  Serial.print(value1);
  if (value2 != 0) {
    Serial.print(",");
    Serial.print(value2);
  }
  Serial.println();
}

// ===== COMUNICAÇÃO SERIAL =====
void processSerialCommands() {
  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim();

    if (command.startsWith("START_GAME:")) {
      int gameMode = command.substring(11).toInt();
      startGame((GameMode)gameMode);
    }
    else if (command == "STOP_GAME") {
      stopGame();
    }
    else if (command.startsWith("KEY_PRESS:")) {
      int key = command.substring(10).toInt();
      handleKeyPress(key);
    }
    else if (command.startsWith("MOVE:")) {
      String direction = command.substring(5);
      handleMovement(direction);
    }
    else if (command == "INIT") {
      Serial.println("READY");
    }
    else if (command == "DISCONNECT") {
      clearAllLEDs();
    }
  }
}

// ===== JOGO 1: PEGA-LUZ =====
void initPegaLuz() {
  pegaLuzTimeout = 2000;
  spawnPegaLuzTarget();
}

void spawnPegaLuzTarget() {
  pegaLuzTarget = random(0, NUM_LEDS);
  pegaLuzStartTime = millis();
  setLED(pegaLuzTarget, true);
  sendGameEvent("LED_ON", pegaLuzTarget);
}

void updatePegaLuz() {
  if (pegaLuzTarget >= 0) {
    if (millis() - pegaLuzStartTime >= pegaLuzTimeout) {
      setLED(pegaLuzTarget, false);
      sendGameEvent("LED_OFF", pegaLuzTarget);
      sendGameEvent("MISS");
      spawnPegaLuzTarget();
    }
  }
}

void handlePegaLuzKey(int key) {
  if (key == pegaLuzTarget) {
    unsigned long reactionTime = millis() - pegaLuzStartTime;
    setLED(pegaLuzTarget, false);
    sendGameEvent("HIT", key, 10);
    game.score += 10;

    // Aumentar dificuldade
    if (game.score % 50 == 0) {
      pegaLuzTimeout = max(pegaLuzTimeout - 100, 500);
      game.level++;
    }

    delay(200);
    spawnPegaLuzTarget();
  }
}

// ===== JOGO 2: SEQUÊNCIA MALUCA =====
void initSequenciaMaluca() {
  sequenciaLength = 3;
  sequenciaIndex = 0;
  sequenciaShowingPattern = false;
  sequenciaWaitingInput = false;
  
  generateSequenciaPattern();
  startSequenciaRound();
}

void generateSequenciaPattern() {
  for (int i = 0; i < sequenciaLength; i++) {
    sequenciaPattern[i] = random(0, NUM_LEDS);
  }
}

void startSequenciaRound() {
  clearAllLEDs();
  sequenciaShowingPattern = true;
  sequenciaDisplayIndex = 0;
  sequenciaLastShow = millis();
  
  sendGameEvent("SEQUENCE_START");
}

void updateSequenciaMaluca() {
  if (sequenciaShowingPattern) {
    if (millis() - sequenciaLastShow >= 600) {
      clearAllLEDs();
      
      if (sequenciaDisplayIndex < sequenciaLength) {
        setLED(sequenciaPattern[sequenciaDisplayIndex], true);
        sequenciaDisplayIndex++;
        sequenciaLastShow = millis();
      } else {
        sequenciaShowingPattern = false;
        sequenciaWaitingInput = true;
        sequenciaIndex = 0;
        clearAllLEDs();
        sendGameEvent("SEQUENCE_REPEAT");
      }
    }
  }
}

void handleSequenciaMalucaKey(int key) {
  if (!sequenciaWaitingInput) return;
  
  if (key == sequenciaPattern[sequenciaIndex]) {
    setLED(key, true);
    delay(100);
    setLED(key, false);
    sequenciaIndex++;
    
    if (sequenciaIndex >= sequenciaLength) {
      game.score++;
      game.level++;
      sequenciaLength++;
      
      sendGameEvent("LEVEL", game.level);
      delay(1000);
      generateSequenciaPattern();
      startSequenciaRound();
    }
  } else {
    sendGameEvent("GAME_OVER", "Sequência errada!");
    stopGame();
  }
}

// ===== JOGO 3: GATO E RATO =====
void initGatoRato() {
  gatoPosition = 0;
  ratoPosition = 8;
  ratoLastMove = millis();
  ratoMoveInterval = 1000;
  ratoVisible = true;
  ratoLastBlink = millis();
}

void updateGatoRato() {
  unsigned long currentTime = millis();
  
  // Mover o rato
  if (currentTime - ratoLastMove >= ratoMoveInterval) {
    int newPos;
    do {
      newPos = random(0, NUM_LEDS);
    } while (newPos == gatoPosition);
    
    ratoPosition = newPos;
    ratoLastMove = currentTime;
    
    // Aumentar velocidade
    if (ratoMoveInterval > 500) {
      ratoMoveInterval -= 50;
    }
  }
  
  // Piscar o rato
  if (currentTime - ratoLastBlink >= 250) {
    ratoVisible = !ratoVisible;
    ratoLastBlink = currentTime;
  }
  
  // Verificar captura
  if (gatoPosition == ratoPosition) {
    game.score += 20;
    sendGameEvent("SCORE", 20);
    
    // Reposicionar
    gatoPosition = random(0, NUM_LEDS);
    do {
      ratoPosition = random(0, NUM_LEDS);
    } while (ratoPosition == gatoPosition);
  }
  
  updateGatoRatoDisplay();
}

void updateGatoRatoDisplay() {
  clearAllLEDs();
  
  // Mostrar gato (sempre visível)
  setLED(gatoPosition, true);
  
  // Mostrar rato (piscando)
  if (ratoVisible && ratoPosition != gatoPosition) {
    setLED(ratoPosition, true);
  }
}

void handleGatoRatoKey(int key) {
  if (key >= 0 && key < NUM_LEDS) {
    gatoPosition = key;
  }
}

// ===== JOGO 6: ROLETA RUSSA LED =====
void initRoletaRussa() {
  roletaRound = 1;
  roletaMultiplier = 1.0;
  roletaWaitingChoice = false;
  clearAllLEDs();
  startRoletaRound();
}

void startRoletaRound() {
  roletaSafeIndex = random(0, NUM_LEDS);
  roletaWaitingChoice = true;
  roletaChoiceTime = millis();

  // Efeito dramático - piscar todos os LEDs
  for(int i = 0; i < 3; i++) {
    for(int j = 0; j < NUM_LEDS; j++) {
      setLED(j, true);
    }
    delay(200);
    clearAllLEDs();
    delay(200);
  }

  roletaMultiplier = pow(2, roletaRound);
  sendGameEvent("ROLETA_ROUND_START", roletaRound, (int)roletaMultiplier);
}

void handleRoletaRussaKey(int key) {
  if (!roletaWaitingChoice) return;

  roletaWaitingChoice = false;

  // Efeito dramático - piscar o LED escolhido
  for(int i = 0; i < 5; i++) {
    setLED(key, true);
    delay(150);
    setLED(key, false);
    delay(150);
  }

  if (key == roletaSafeIndex) {
    // SEGURO! Continua
    game.score += roletaMultiplier;
    roletaRound++;

    setLED(key, true);
    sendGameEvent("ROLETA_SAFE", key, game.score);
    delay(2000);
    setLED(key, false);

    if (roletaRound <= 8) {
      startRoletaRound();
    } else {
      sendGameEvent("ROLETA_MAX_WIN", game.score);
      stopGame();
    }
  } else {
    // BOOM! Perdeu tudo
    setLED(key, true);
    delay(1000);
    setLED(key, false);

    sendGameEvent("ROLETA_EXPLODE", key, 0);
    game.score = 0;
    stopGame();
  }
}

// ===== JOGO 7: LIGHTNING STRIKE =====
void initLightningStrike() {
  lightningPatternLength = 3;
  lightningInputIndex = 0;
  lightningShowingPattern = false;
  lightningWaitingInput = false;
  lightningShowDuration = 500;

  generateLightningPattern();
  startLightningRound();
}

void generateLightningPattern() {
  for(int i = 0; i < lightningPatternLength; i++) {
    lightningPattern[i] = random(0, NUM_LEDS);
  }
}

void startLightningRound() {
  clearAllLEDs();
  lightningShowingPattern = true;
  lightningLastShow = millis();

  // Mostrar padrão completo rapidamente
  for(int i = 0; i < lightningPatternLength; i++) {
    setLED(lightningPattern[i], true);
  }

  sendGameEvent("LIGHTNING_PATTERN_SHOW", lightningPatternLength, lightningShowDuration);
}

void updateLightningStrike() {
  if (lightningShowingPattern) {
    if (millis() - lightningLastShow >= lightningShowDuration) {
      clearAllLEDs();
      lightningShowingPattern = false;
      lightningWaitingInput = true;
      lightningInputIndex = 0;

      sendGameEvent("LIGHTNING_INPUT_START");
    }
  }
}

void handleLightningStrikeKey(int key) {
  if (!lightningWaitingInput) return;

  if (key == lightningPattern[lightningInputIndex]) {
    setLED(key, true);
    delay(100);
    setLED(key, false);
    lightningInputIndex++;

    if (lightningInputIndex >= lightningPatternLength) {
      game.score++;
      game.level++;

      lightningPatternLength = min(lightningPatternLength + 1, 12);
      lightningShowDuration = max(lightningShowDuration - 50, 100);

      sendGameEvent("LIGHTNING_COMPLETE", game.level, lightningShowDuration);

      delay(1000);
      generateLightningPattern();
      startLightningRound();
    }
  } else {
    // ERRADO! Mostrar padrão correto
    clearAllLEDs();
    for(int i = 0; i < lightningPatternLength; i++) {
      setLED(lightningPattern[i], true);
    }

    sendGameEvent("LIGHTNING_WRONG", key, lightningPattern[lightningInputIndex]);
    delay(2000);
    stopGame();
  }
}

// ===== JOGO 8: SNIPER MODE =====
void initSniperMode() {
  sniperCurrentHits = 0;
  sniperHitsRequired = 10;
  sniperFlashDuration = 100;
  sniperWaitingShot = false;
  clearAllLEDs();

  spawnSniperTarget();
}

void spawnSniperTarget() {
  sniperTarget = random(0, NUM_LEDS);
  sniperTargetTime = millis();
  sniperWaitingShot = true;

  setLED(sniperTarget, true);
  sendGameEvent("SNIPER_TARGET_SPAWN", sniperTarget, sniperFlashDuration);
}

void updateSniperMode() {
  if (sniperWaitingShot) {
    if (millis() - sniperTargetTime >= sniperFlashDuration) {
      setLED(sniperTarget, false);

      if (millis() - sniperTargetTime >= sniperFlashDuration + 200) {
        sendGameEvent("SNIPER_TIMEOUT");
        if (game.score > 0) game.score--;

        sniperWaitingShot = false;
        delay(random(500, 2000));
        spawnSniperTarget();
      }
    }
  }
}

void handleSniperModeKey(int key) {
  if (!sniperWaitingShot) return;

  unsigned long reactionTime = millis() - sniperTargetTime;

  if (key == sniperTarget && reactionTime <= sniperFlashDuration + 50) {
    sniperCurrentHits++;
    game.score++;

    setLED(sniperTarget, false);
    sniperWaitingShot = false;

    sendGameEvent("SNIPER_HIT", sniperCurrentHits, reactionTime);

    if (sniperCurrentHits >= sniperHitsRequired) {
      sendGameEvent("SNIPER_VICTORY", game.score);
      game.score *= 10;
      stopGame();
    } else {
      delay(random(300, 1500));
      spawnSniperTarget();
    }
  } else {
    sendGameEvent("SNIPER_MISS", key, reactionTime);
    if (game.score > 0) game.score--;

    sniperWaitingShot = false;
    setLED(sniperTarget, false);
    delay(1000);
    spawnSniperTarget();
  }
}

// ===== HANDLERS PRINCIPAIS =====
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
    case ROLETA_RUSSA:
      handleRoletaRussaKey(key);
      break;
    case LIGHTNING_STRIKE:
      handleLightningStrikeKey(key);
      break;
    case SNIPER_MODE:
      handleSniperModeKey(key);
      break;
  }
}

void handleMovement(String direction) {
  if (!game.gameActive) return;
  
  // Para jogos que usam movimento (implementar se necessário)
  if (game.currentMode == GATO_RATO) {
    if (direction == "UP" && gatoPosition >= 4) gatoPosition -= 4;
    else if (direction == "DOWN" && gatoPosition < 12) gatoPosition += 4;
    else if (direction == "LEFT" && gatoPosition % 4 > 0) gatoPosition--;
    else if (direction == "RIGHT" && gatoPosition % 4 < 3) gatoPosition++;
  }
}

void startGame(GameMode mode) {
  game.currentMode = mode;
  game.gameActive = true;
  game.score = 0;
  game.level = 1;
  game.difficulty = 1;
  game.gameStartTime = millis();
  game.lastUpdateTime = millis();

  clearAllLEDs();

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
    case ROLETA_RUSSA:
      initRoletaRussa();
      break;
    case LIGHTNING_STRIKE:
      initLightningStrike();
      break;
    case SNIPER_MODE:
      initSniperMode();
      break;
  }

  sendGameEvent("GAME_STARTED", mode);
}

void stopGame() {
  game.gameActive = false;
  clearAllLEDs();
  sendGameEvent("GAME_OVER", game.score);
}

// ===== LOOP PRINCIPAL =====
void loop() {
  processSerialCommands();

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
      case LIGHTNING_STRIKE:
        updateLightningStrike();
        break;
      case SNIPER_MODE:
        updateSniperMode();
        break;
      case ROLETA_RUSSA:
        // Não precisa update contínuo
        break;
    }
  }

  delay(10);
}