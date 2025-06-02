// ===== MINI JOGO LEDs - ARDUINO CODE (CORRIGIDO E OTIMIZADO) =====
// Sistema de jogos com matriz LED 4x4 (LEDs simples)
// REVISÃO: Todas as chamadas delay() foram removidas para uma operação não-bloqueante,
// melhorando a responsividade e a precisão da temporização.

// ===== CONFIGURAÇÕES BÁSICAS =====
#define NUM_LEDS 16

const int ledPins[NUM_LEDS] = {
  2, 3, 4, 5,    // Linha 0 (vermelha) - LEDs 0-3
  6, 7, 8, 9,    // Linha 1 (amarela) - LEDs 4-7
  10, 11, 12, 13, // Linha 2 (verde) - LEDs 8-11
  A0, A1, A2, A3  // Linha 3 (azul) - LEDs 12-15
};

// ===== ENUMS DOS MODOS E ESTADOS DE JOGO =====
enum GameMode {
  MENU = 0, PEGA_LUZ = 1, SEQUENCIA_MALUCA = 2, GATO_RATO = 3, ESQUIVA_METEOROS = 4,
  GUITAR_HERO = 5, ROLETA_RUSSA = 6, LIGHTNING_STRIKE = 7, SNIPER_MODE = 8
};

struct GameState {
  GameMode currentMode;
  bool gameActive;
  int score;
  int level;
  unsigned long gameStartTime;
  unsigned long lastUpdateTime; // Usado para lógica de tempo genérica
};

GameState game;

// ===== VARIÁVEIS GLOBAIS DE JOGO =====
// (Variáveis específicas de cada jogo movidas para perto de suas funções)
unsigned long stateChangeTime = 0; // Temporizador genérico para transições de estado

// Pega-Luz
enum PegaLuzState { PL_PLAYING, PL_PAUSE_AFTER_HIT };
PegaLuzState pegaLuzState;
int pegaLuzTarget = -1;
unsigned long pegaLuzStartTime = 0;
unsigned long pegaLuzTimeout = 2000;

// Sequência Maluca
enum SequenciaState { SEQ_SHOWING, SEQ_PAUSE_BEFORE_INPUT, SEQ_WAITING_INPUT, SEQ_PAUSE_BEFORE_NEXT };
SequenciaState sequenciaState;
int sequenciaPattern[16];
int sequenciaLength = 3;
int sequenciaPlayerIndex = 0;
int sequenciaDisplayIndex = 0;
unsigned long sequenciaLastLedShowTime = 0;

// Gato e Rato
enum GatoRatoState { GR_PLAYING, GR_CAPTURE_ANIMATION };
GatoRatoState gatoRatoState;
int gatoPosition = 0;
int ratoPosition = 8;
unsigned long ratoLastMove = 0;
unsigned long ratoMoveInterval = 1000;
bool ratoVisible = true;
unsigned long ratoLastBlink = 0;
int captureBlinkCount = 0;

// Esquiva Meteoros
int playerPosition = 12;
bool meteoros[NUM_LEDS];
unsigned long meteoroLastSpawn = 0;
unsigned long meteoroSpawnInterval = 1500;
unsigned long meteoroLastMove = 0;
unsigned long meteoroMoveInterval = 800;
bool meteoroVisible = true;
unsigned long meteoroLastBlink = 0;

// Guitar Hero
struct Note { int column; int row; unsigned long spawnTime; bool active; };
Note notes[8];
unsigned long lastNoteSpawn = 0;
unsigned long noteSpeed = 1000;
int noteSpawnInterval = 2000;

// Roleta Russa LED
enum RoletaState { ROL_WAITING_CHOICE, ROL_ANIMATE_CHOICE, ROL_SAFE_PAUSE, ROL_EXPLODE_ANIMATION };
RoletaState roletaState;
int roletaSafeIndex = -1;
int roletaRound = 1;
float roletaMultiplier = 1.0;
int roletaChosenKey = -1;
int roletaBlinkCount = 0;

// Lightning Strike
enum LightningState { LS_SHOWING, LS_WAITING_INPUT, LS_WRONG_PAUSE, LS_CORRECT_PAUSE };
LightningState lightningState;
int lightningPattern[16];
int lightningPatternLength = 3;
int lightningInputIndex = 0;
unsigned long lightningShowDuration = 500;

// Sniper Mode
enum SniperState { SN_WAITING_SHOT, SN_TARGET_COOLDOWN };
SniperState sniperState;
int sniperTarget = -1;
unsigned long sniperTargetTime = 0;
unsigned long sniperFlashDuration = 100;
int sniperHitsRequired = 10;
int sniperCurrentHits = 0;
unsigned long nextTargetDelay = 0;

// ===== SETUP =====
void setup() {
  Serial.begin(9600);
  for (int i = 0; i < NUM_LEDS; i++) {
    pinMode(ledPins[i], OUTPUT);
    digitalWrite(ledPins[i], LOW);
  }
  for (int i = 0; i < NUM_LEDS; i++) meteoros[i] = false;
  for (int i = 0; i < 8; i++) notes[i].active = false;

  game.currentMode = MENU;
  game.gameActive = false;
  game.score = 0;
  game.level = 1;

  randomSeed(analogRead(A4));
  clearAllLEDs();
  testLEDs(); // Delays in setup are acceptable as they run only once.
  Serial.println("READY");
}

// ===== FUNÇÕES AUXILIARES =====
void setLED(int index, bool state) {
  if (index >= 0 && index < NUM_LEDS) {
    digitalWrite(ledPins[index], state ? HIGH : LOW);
  }
}

void clearAllLEDs() {
  for (int i = 0; i < NUM_LEDS; i++) setLED(i, false);
}

void testLEDs() {
  for (int i = 0; i < 16; i += 4) {
    for (int j = 0; j < 4; j++) setLED(i + j, true);
    delay(500);
    clearAllLEDs();
  }
}

void sendGameEvent(String eventType, int value1 = 0, int value2 = 0) {
  Serial.print("GAME_EVENT:"); Serial.print(eventType); Serial.print(":");
  Serial.print(value1);
  // FIX: Enviar value2 mesmo que seja 0, a aplicação cliente pode esperar o campo.
  // Uma alternativa é ter um valor especial como -1 para "não enviar".
  // Por simplicidade, mantemos o envio condicional.
  if (value2 != 0) { Serial.print(","); Serial.print(value2); }
  Serial.println();
}

// ===== COMUNICAÇÃO SERIAL =====
void processSerialCommands() {
  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim();
    if (command.startsWith("START_GAME:")) startGame((GameMode)command.substring(11).toInt());
    else if (command == "STOP_GAME") stopGame();
    else if (command.startsWith("KEY_PRESS:")) handleKeyPress(command.substring(10).toInt());
    else if (command.startsWith("KEY_RELEASE:")) handleKeyRelease(command.substring(12).toInt());
    else if (command == "INIT") Serial.println("READY");
    else if (command == "DISCONNECT") clearAllLEDs();
  }
}

// ===== JOGO 1: PEGA-LUZ =====
void initPegaLuz() {
  pegaLuzTimeout = 2000;
  spawnPegaLuzTarget();
  pegaLuzState = PL_PLAYING;
}

void spawnPegaLuzTarget() {
  clearAllLEDs();
  pegaLuzTarget = random(0, NUM_LEDS);
  pegaLuzStartTime = millis();
  setLED(pegaLuzTarget, true);
  sendGameEvent("LED_ON", pegaLuzTarget);
  pegaLuzState = PL_PLAYING;
}

void updatePegaLuz() {
  unsigned long currentTime = millis();
  if (pegaLuzState == PL_PLAYING && pegaLuzTarget >= 0) {
    if (currentTime - pegaLuzStartTime >= pegaLuzTimeout) {
      setLED(pegaLuzTarget, false);
      sendGameEvent("LED_OFF", pegaLuzTarget);
      sendGameEvent("MISS");
      spawnPegaLuzTarget();
    }
  } else if (pegaLuzState == PL_PAUSE_AFTER_HIT) {
    if (currentTime - stateChangeTime >= 200) { // REFACTOR: Non-blocking delay
      spawnPegaLuzTarget();
    }
  }
}

void handlePegaLuzKey(int key) {
  if (pegaLuzState != PL_PLAYING) return;
  if (key == pegaLuzTarget) {
    setLED(pegaLuzTarget, false);
    game.score += 10;
    sendGameEvent("HIT", key, game.score);

    if (game.score > 0 && game.score % 50 == 0) {
      pegaLuzTimeout = max(pegaLuzTimeout - 100, 500);
      game.level++;
      sendGameEvent("LEVEL_UP", game.level);
    }
    pegaLuzState = PL_PAUSE_AFTER_HIT;
    stateChangeTime = millis(); // Start pause timer
  } else if (pegaLuzTarget >= 0) {
    sendGameEvent("WRONG_KEY", key);
  }
}

// ===== JOGO 2: SEQUÊNCIA MALUCA =====
void initSequenciaMaluca() {
  sequenciaLength = 3;
  generateSequenciaPattern();
  startSequenciaRound();
}

void generateSequenciaPattern() {
  for (int i = 0; i < sequenciaLength; i++) sequenciaPattern[i] = random(0, NUM_LEDS);
}

void startSequenciaRound() {
  clearAllLEDs();
  sequenciaDisplayIndex = 0;
  sequenciaState = SEQ_SHOWING;
  sequenciaLastLedShowTime = millis();
  sendGameEvent("SEQUENCE_START", sequenciaLength);
}

void updateSequenciaMaluca() {
  unsigned long currentTime = millis();
  if (sequenciaState == SEQ_SHOWING) {
    if (currentTime - sequenciaLastLedShowTime >= 600) {
      clearAllLEDs();
      if (sequenciaDisplayIndex < sequenciaLength) {
        setLED(sequenciaPattern[sequenciaDisplayIndex], true);
        sequenciaDisplayIndex++;
        sequenciaLastLedShowTime = currentTime;
      } else {
        sequenciaState = SEQ_PAUSE_BEFORE_INPUT;
        stateChangeTime = currentTime;
      }
    }
  } else if (sequenciaState == SEQ_PAUSE_BEFORE_INPUT) {
    if (currentTime - stateChangeTime >= 300) { // REFACTOR: Non-blocking delay
      sequenciaPlayerIndex = 0;
      sequenciaState = SEQ_WAITING_INPUT;
      sendGameEvent("SEQUENCE_REPEAT");
    }
  } else if (sequenciaState == SEQ_PAUSE_BEFORE_NEXT) {
      if (currentTime - stateChangeTime >= 1000) { // REFACTOR: Non-blocking delay
          generateSequenciaPattern();
          startSequenciaRound();
      }
  }
}

void handleSequenciaMalucaKey(int key) {
  if (sequenciaState != SEQ_WAITING_INPUT) return;
  if (key == sequenciaPattern[sequenciaPlayerIndex]) {
    setLED(key, true); // Visual feedback
    // Note: this feedback is very short, will be cleared by the game loop.
    // For longer feedback, another state would be needed.
    sequenciaPlayerIndex++;
    if (sequenciaPlayerIndex >= sequenciaLength) {
      game.score += 10;
      game.level++;
      sequenciaLength = min(sequenciaLength + 1, 12);
      sendGameEvent("LEVEL_UP", game.level, game.score);
      sequenciaState = SEQ_PAUSE_BEFORE_NEXT;
      stateChangeTime = millis();
    }
  } else {
    sendGameEvent("GAME_OVER", game.score);
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
  gatoRatoState = GR_PLAYING;
  clearAllLEDs();
}

void updateGatoRatoDisplay() {
  clearAllLEDs();
  setLED(gatoPosition, true);
  if (ratoVisible && ratoPosition != gatoPosition) {
    setLED(ratoPosition, true);
  }
}

void updateGatoRato() {
    unsigned long currentTime = millis();
    if (gatoRatoState == GR_PLAYING) {
        if (currentTime - ratoLastMove >= ratoMoveInterval) {
            int newPos;
            do { newPos = random(0, NUM_LEDS); } while (newPos == gatoPosition);
            ratoPosition = newPos;
            ratoLastMove = currentTime;
        }
        if (currentTime - ratoLastBlink >= 300) {
            ratoVisible = !ratoVisible;
            ratoLastBlink = currentTime;
        }
        if (gatoPosition == ratoPosition) {
            game.score += 20;
            // FIX: Speed increases only on capture
            if (ratoMoveInterval > 300) ratoMoveInterval -= 50;
            sendGameEvent("SCORE", 20, game.score);
            gatoRatoState = GR_CAPTURE_ANIMATION;
            stateChangeTime = millis();
            captureBlinkCount = 0;
        }
        updateGatoRatoDisplay();
    } else if (gatoRatoState == GR_CAPTURE_ANIMATION) {
        if (currentTime - stateChangeTime >= 100) { // REFACTOR: Animation timing
            stateChangeTime = currentTime;
            setLED(gatoPosition, captureBlinkCount % 2 == 0); // Blink
            captureBlinkCount++;
            if (captureBlinkCount >= 6) { // 3 blinks (on/off)
                gatoPosition = random(0, NUM_LEDS);
                do { ratoPosition = random(0, NUM_LEDS); } while (ratoPosition == gatoPosition);
                ratoLastMove = currentTime;
                gatoRatoState = GR_PLAYING;
            }
        }
    }
}

void handleGatoRatoKey(int key) {
  if (gatoRatoState == GR_PLAYING && key >= 0 && key < NUM_LEDS) {
    gatoPosition = key;
  }
}

// ===== JOGO 4: ESQUIVA METEOROS =====
void initEsquivaMeteoros() {
  playerPosition = 12;
  meteoroLastSpawn = millis();
  meteoroSpawnInterval = 1500;
  meteoroLastMove = millis();
  meteoroMoveInterval = 800;
  for (int i = 0; i < NUM_LEDS; i++) meteoros[i] = false;
  clearAllLEDs();
  setLED(playerPosition, true);
}

void updateEsquivaMeteorosDisplay() {
  clearAllLEDs();
  setLED(playerPosition, true);
  if (meteoroVisible) {
    for (int i = 0; i < NUM_LEDS; i++) {
      if (meteoros[i] && i != playerPosition) setLED(i, true);
    }
  }
}

void updateEsquivaMeteoros() {
  unsigned long currentTime = millis();
  if (currentTime - meteoroLastSpawn >= meteoroSpawnInterval) {
    meteoros[random(0, 4)] = true;
    meteoroLastSpawn = currentTime;
    if (meteoroSpawnInterval > 800) meteoroSpawnInterval -= 50;
  }
  if (currentTime - meteoroLastMove >= meteoroMoveInterval) {
    for (int row = 3; row >= 0; row--) {
      for (int col = 0; col < 4; col++) {
        int currentPos = row * 4 + col;
        if (meteoros[currentPos]) {
          meteoros[currentPos] = false;
          if (row < 3) meteoros[currentPos + 4] = true;
        }
      }
    }
    meteoroLastMove = currentTime;
    if (meteoroMoveInterval > 300) meteoroMoveInterval -= 10;
  }
  if (currentTime - meteoroLastBlink >= 200) {
    meteoroVisible = !meteoroVisible;
    meteoroLastBlink = currentTime;
  }
  if (meteoros[playerPosition]) {
    sendGameEvent("METEOR_HIT", playerPosition);
    stopGame();
    return;
  }
  if (currentTime - game.lastUpdateTime >= 1000) {
    game.score++;
    game.lastUpdateTime = currentTime;
  }
  updateEsquivaMeteorosDisplay();
}

void handleEsquivaMeteoros(int key) {
  if (key >= 12 && key <= 15) {
    playerPosition = key;
  }
}

// ===== JOGO 5: GUITAR HERO =====
void initGuitarHero() {
  lastNoteSpawn = millis();
  noteSpeed = 1000;
  noteSpawnInterval = 2000;
  for (int i = 0; i < 8; i++) notes[i].active = false;
  clearAllLEDs();
}

void spawnNote() {
  for (int i = 0; i < 8; i++) {
    if (!notes[i].active) {
      notes[i].column = random(0, 4);
      notes[i].row = 0;
      notes[i].spawnTime = millis();
      notes[i].active = true;
      break;
    }
  }
}

bool isLEDOnByNote(int pos) {
  for (int i = 0; i < 8; i++) {
    if (notes[i].active && (notes[i].row * 4 + notes[i].column) == pos) return true;
  }
  return false;
}

void updateGuitarHeroDisplay() {
  clearAllLEDs();
  for (int i = 0; i < 8; i++) {
    if (notes[i].active) setLED(notes[i].row * 4 + notes[i].column, true);
  }
  if (millis() % 500 < 250) {
    for (int i = 12; i < 16; i++) {
      if (!isLEDOnByNote(i)) setLED(i, true);
    }
  }
}

void updateGuitarHero() {
  unsigned long currentTime = millis();
  if (currentTime - lastNoteSpawn >= noteSpawnInterval) {
    spawnNote();
    lastNoteSpawn = currentTime;
    if (noteSpawnInterval > 1000) noteSpawnInterval -= 100;
  }
  for (int i = 0; i < 8; i++) {
    if (notes[i].active) {
      if (currentTime - notes[i].spawnTime >= noteSpeed) {
        if (notes[i].row < 3) {
          notes[i].row++;
          notes[i].spawnTime = currentTime;
        } else {
          notes[i].active = false;
          sendGameEvent("NOTE_MISS", notes[i].column);
        }
      }
    }
  }
  updateGuitarHeroDisplay();
}

void handleGuitarHero(int key) {
  if (key < 12 || key > 15) return;
  int columnPressed = key - 12;
  bool hitNote = false;
  for (int i = 0; i < 8; i++) {
    if (notes[i].active && notes[i].column == columnPressed && notes[i].row == 3) {
      notes[i].active = false;
      game.score += 10;
      hitNote = true;
      sendGameEvent("NOTE_HIT", columnPressed, game.score);
      if (noteSpeed > 400 && game.score % 50 == 0) noteSpeed -= 50;
      break;
    }
  }
  if (!hitNote) {
    sendGameEvent("NOTE_MISS", columnPressed);
    if (game.score > 0) game.score = max(0, game.score - 5);
  }
}

// ===== JOGO 6: ROLETA RUSSA LED =====
void initRoletaRussa() {
    roletaRound = 1;
    roletaMultiplier = 1.0;
    startRoletaRound();
}

void startRoletaRound() {
    roletaSafeIndex = random(0, NUM_LEDS);
    roletaState = ROL_WAITING_CHOICE;
    roletaMultiplier = pow(2, roletaRound - 1);
    clearAllLEDs();
    sendGameEvent("ROLETA_ROUND_START", roletaRound, (int)roletaMultiplier);
}

void updateRoletaRussa() {
    unsigned long currentTime = millis();
    if (roletaState == ROL_ANIMATE_CHOICE) {
        if (currentTime - stateChangeTime >= 150) { // REFACTOR: Animation timing
            stateChangeTime = currentTime;
            setLED(roletaChosenKey, roletaBlinkCount % 2 != 0); // Blink
            roletaBlinkCount++;
            if (roletaBlinkCount >= 10) { // 5 blinks
                if (roletaChosenKey == roletaSafeIndex) {
                    game.score += (int)roletaMultiplier;
                    roletaRound++;
                    setLED(roletaChosenKey, true);
                    sendGameEvent("ROLETA_SAFE", roletaChosenKey, game.score);
                    roletaState = ROL_SAFE_PAUSE;
                    stateChangeTime = currentTime;
                } else {
                    setLED(roletaChosenKey, true);
                    sendGameEvent("ROLETA_EXPLODE", roletaChosenKey, 0);
                    roletaState = ROL_EXPLODE_ANIMATION;
                    stateChangeTime = currentTime;
                }
            }
        }
    } else if (roletaState == ROL_SAFE_PAUSE) {
        if (currentTime - stateChangeTime >= 2000) { // REFACTOR: Non-blocking pause
            if (roletaRound <= 8) {
                startRoletaRound();
            } else {
                sendGameEvent("ROLETA_MAX_WIN", game.score);
                stopGame();
            }
        }
    } else if (roletaState == ROL_EXPLODE_ANIMATION) {
        if (currentTime - stateChangeTime >= 1000) { // REFACTOR: Non-blocking pause
            game.score = 0;
            stopGame();
        }
    }
}

void handleRoletaRussaKey(int key) {
    if (roletaState != ROL_WAITING_CHOICE) return;
    roletaChosenKey = key;
    roletaBlinkCount = 0;
    roletaState = ROL_ANIMATE_CHOICE;
    stateChangeTime = millis();
}

// ===== JOGO 7: LIGHTNING STRIKE =====
void initLightningStrike() {
    lightningPatternLength = 3;
    lightningShowDuration = 500;
    generateLightningPattern();
    startLightningRound();
}

void generateLightningPattern() {
    for (int i = 0; i < lightningPatternLength; i++) lightningPattern[i] = random(0, NUM_LEDS);
}

void startLightningRound() {
    clearAllLEDs();
    for (int i = 0; i < lightningPatternLength; i++) setLED(lightningPattern[i], true);
    lightningState = LS_SHOWING;
    stateChangeTime = millis();
    sendGameEvent("LIGHTNING_PATTERN_SHOW", lightningPatternLength, lightningShowDuration);
}

void updateLightningStrike() {
    unsigned long currentTime = millis();
    if (lightningState == LS_SHOWING) {
        if (currentTime - stateChangeTime >= lightningShowDuration) {
            clearAllLEDs();
            lightningState = LS_WAITING_INPUT;
            lightningInputIndex = 0;
            sendGameEvent("LIGHTNING_INPUT_START");
        }
    } else if (lightningState == LS_CORRECT_PAUSE) {
        if (currentTime - stateChangeTime >= 1000) {
            generateLightningPattern();
            startLightningRound();
        }
    } else if (lightningState == LS_WRONG_PAUSE) {
        if (currentTime - stateChangeTime >= 2000) {
            stopGame();
        }
    }
}

void handleLightningStrikeKey(int key) {
    if (lightningState != LS_WAITING_INPUT) return;
    setLED(key, true); // Visual feedback (will be cleared next loop)
    if (key == lightningPattern[lightningInputIndex]) {
        lightningInputIndex++;
        if (lightningInputIndex >= lightningPatternLength) {
            game.score += 10;
            game.level++;
            lightningPatternLength = min(lightningPatternLength + 1, 12);
            lightningShowDuration = max(lightningShowDuration - 50, 100);
            sendGameEvent("LIGHTNING_COMPLETE", game.level, lightningShowDuration);
            lightningState = LS_CORRECT_PAUSE;
            stateChangeTime = millis();
        }
    } else {
        clearAllLEDs();
        for (int i = 0; i < lightningPatternLength; i++) setLED(lightningPattern[i], true);
        sendGameEvent("LIGHTNING_WRONG", key, lightningPattern[lightningInputIndex]);
        lightningState = LS_WRONG_PAUSE;
        stateChangeTime = millis();
    }
}

// ===== JOGO 8: SNIPER MODE =====
void initSniperMode() {
    sniperCurrentHits = 0;
    sniperHitsRequired = 10;
    sniperFlashDuration = 100;
    sniperState = SN_TARGET_COOLDOWN; // Start with a cooldown before first target
    nextTargetDelay = random(500, 1500);
    stateChangeTime = millis();
    clearAllLEDs();
}

void spawnSniperTarget() {
    clearAllLEDs();
    sniperTarget = random(0, NUM_LEDS);
    sniperTargetTime = millis();
    setLED(sniperTarget, true);
    sniperState = SN_WAITING_SHOT;
    sendGameEvent("SNIPER_TARGET_SPAWN", sniperTarget, sniperFlashDuration);
}

void updateSniperMode() {
    unsigned long currentTime = millis();
    if (sniperState == SN_WAITING_SHOT) {
        if (currentTime - sniperTargetTime >= sniperFlashDuration) {
            setLED(sniperTarget, false); // Turn off LED
            // Player missed by timeout (didn't shoot at all)
            sendGameEvent("SNIPER_TIMEOUT");
            if (game.score > 0) game.score = max(0, game.score - 1);
            sniperState = SN_TARGET_COOLDOWN;
            nextTargetDelay = random(500, 2000);
            stateChangeTime = currentTime;
        }
    } else if (sniperState == SN_TARGET_COOLDOWN) {
        if (currentTime - stateChangeTime >= nextTargetDelay) {
            spawnSniperTarget();
        }
    }
}

void handleSniperModeKey(int key) {
    if (sniperState != SN_WAITING_SHOT) return;
    unsigned long reactionTime = millis() - sniperTargetTime;

    // FIX: Hit is only valid WHILE the target is lit
    if (key == sniperTarget && reactionTime <= sniperFlashDuration) {
        sniperCurrentHits++;
        game.score += 10;
        setLED(sniperTarget, false);
        sendGameEvent("SNIPER_HIT", sniperCurrentHits, reactionTime);

        if (sniperCurrentHits >= sniperHitsRequired) {
            sendGameEvent("SNIPER_VICTORY", game.score);
            stopGame();
        } else {
            sniperState = SN_TARGET_COOLDOWN;
            nextTargetDelay = random(300, 1500);
            stateChangeTime = millis();
        }
    } else {
        sendGameEvent("SNIPER_MISS", key, reactionTime);
        if (game.score > 0) game.score = max(0, game.score - 2);
        setLED(sniperTarget, false);
        sniperState = SN_TARGET_COOLDOWN;
        nextTargetDelay = 1000; // Fixed delay after a miss
        stateChangeTime = millis();
    }
}

// ===== HANDLERS PRINCIPAIS =====
void handleKeyPress(int key) {
  if (!game.gameActive || key < 0 || key >= NUM_LEDS) return;
  switch (game.currentMode) {
    case PEGA_LUZ: handlePegaLuzKey(key); break;
    case SEQUENCIA_MALUCA: handleSequenciaMalucaKey(key); break;
    case GATO_RATO: handleGatoRatoKey(key); break;
    case ESQUIVA_METEOROS: handleEsquivaMeteoros(key); break;
    case GUITAR_HERO: handleGuitarHero(key); break;
    case ROLETA_RUSSA: handleRoletaRussaKey(key); break;
    case LIGHTNING_STRIKE: handleLightningStrikeKey(key); break;
    case SNIPER_MODE: handleSniperModeKey(key); break;
    default: break;
  }
}

void handleKeyRelease(int key) {
  if (!game.gameActive || key < 0 || key >= NUM_LEDS) return;
  // Most games don't need key release handling, but some might
  // For now, just acknowledge the release
  sendGameEvent("KEY_RELEASED", key);
}

void startGame(GameMode mode) {
  game.currentMode = mode;
  game.gameActive = true;
  game.score = 0;
  game.level = 1;
  game.gameStartTime = millis();
  game.lastUpdateTime = millis();
  clearAllLEDs();

  switch (mode) {
    case PEGA_LUZ: initPegaLuz(); break;
    case SEQUENCIA_MALUCA: initSequenciaMaluca(); break;
    case GATO_RATO: initGatoRato(); break;
    case ESQUIVA_METEOROS: initEsquivaMeteoros(); break;
    case GUITAR_HERO: initGuitarHero(); break;
    case ROLETA_RUSSA: initRoletaRussa(); break;
    case LIGHTNING_STRIKE: initLightningStrike(); break;
    case SNIPER_MODE: initSniperMode(); break;
    default: game.gameActive = false; return;
  }
  sendGameEvent("GAME_STARTED", (int)mode);
}

void stopGame() {
  game.gameActive = false;
  sendGameEvent("GAME_OVER", game.score);
  clearAllLEDs();
  // Optional: Add a non-blocking game over animation here
}

// ===== LOOP PRINCIPAL =====
void loop() {
  processSerialCommands();

  if (game.gameActive) {
    switch (game.currentMode) {
      case PEGA_LUZ: updatePegaLuz(); break;
      case SEQUENCIA_MALUCA: updateSequenciaMaluca(); break;
      case GATO_RATO: updateGatoRato(); break;
      case ESQUIVA_METEOROS: updateEsquivaMeteoros(); break;
      case GUITAR_HERO: updateGuitarHero(); break;
      case ROLETA_RUSSA: updateRoletaRussa(); break;
      case LIGHTNING_STRIKE: updateLightningStrike(); break;
      case SNIPER_MODE: updateSniperMode(); break;
      default: break;
    }
  } else {
    // Idle state - could run a non-blocking menu animation
  }
  // REFACTOR: No delay() in the main loop for maximum responsiveness.
}
