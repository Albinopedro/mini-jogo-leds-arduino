// ===== MINI JOGO LEDs - ARDUINO CODE (CORRIGIDO) =====
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
unsigned long meteoroLastMove = 0;
unsigned long meteoroMoveInterval = 800;
bool meteoroVisible = true;
unsigned long meteoroLastBlink = 0;

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
int noteSpawnInterval = 2000;

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

  // Inicializar meteoros array
  for (int i = 0; i < NUM_LEDS; i++) {
    meteoros[i] = false;
  }

  // Inicializar notes array
  for (int i = 0; i < 8; i++) {
    notes[i].active = false;
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
  if (value2 != 0) { // Only print value2 if it's not the default 0
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
  pegaLuzTarget = -1;
  clearAllLEDs();
  spawnPegaLuzTarget();
}

void spawnPegaLuzTarget() {
  clearAllLEDs();
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
  if (key == pegaLuzTarget && pegaLuzTarget >= 0) {
    unsigned long reactionTime = millis() - pegaLuzStartTime;
    setLED(pegaLuzTarget, false);
    sendGameEvent("HIT", key, 10); // Assuming 10 points for a hit
    game.score += 10;

    // Aumentar dificuldade
    if (game.score > 0 && game.score % 50 == 0) { // Every 50 points
      pegaLuzTimeout = max(pegaLuzTimeout - 100, 500); // Decrease timeout, min 500ms
      game.level++;
      sendGameEvent("LEVEL_UP", game.level);
    }

    delay(200); // Short delay before new target
    spawnPegaLuzTarget();
  } else if (pegaLuzTarget >= 0) {
    // Tecla errada pressionada
    sendGameEvent("WRONG_KEY", key);
  }
}

// ===== JOGO 2: SEQUÊNCIA MALUCA =====
void initSequenciaMaluca() {
  sequenciaLength = 3;
  sequenciaIndex = 0;
  sequenciaShowingPattern = false;
  sequenciaWaitingInput = false;
  clearAllLEDs();

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
  sequenciaWaitingInput = false;
  sequenciaDisplayIndex = 0;
  sequenciaLastShow = millis() - 600; // Força início imediato

  sendGameEvent("SEQUENCE_START", sequenciaLength);
}

void updateSequenciaMaluca() {
  if (sequenciaShowingPattern) {
    unsigned long currentTime = millis();

    if (currentTime - sequenciaLastShow >= 600) { // Time to show next LED or finish
      clearAllLEDs(); // Clear previous LED

      if (sequenciaDisplayIndex < sequenciaLength) {
        setLED(sequenciaPattern[sequenciaDisplayIndex], true);
        sequenciaDisplayIndex++;
        sequenciaLastShow = currentTime;
      } else {
        // Pattern finished showing
        delay(300); // Short pause after pattern before clearing
        clearAllLEDs();
        sequenciaShowingPattern = false;
        sequenciaWaitingInput = true;
        sequenciaIndex = 0; // Reset for player input
        sendGameEvent("SEQUENCE_REPEAT");
      }
    }
  }
}

void handleSequenciaMalucaKey(int key) {
  if (!sequenciaWaitingInput || sequenciaShowingPattern) return;

  if (key == sequenciaPattern[sequenciaIndex]) {
    setLED(key, true);
    delay(200); // Visual feedback
    setLED(key, false);
    sequenciaIndex++;

    if (sequenciaIndex >= sequenciaLength) {
      // Sequence complete
      game.score += 10;
      game.level++;
      sequenciaLength = min(sequenciaLength + 1, 12); // Max length 12

      sendGameEvent("LEVEL_UP", game.level, game.score);
      delay(1000); // Pause before next round
      generateSequenciaPattern();
      startSequenciaRound();
    }
  } else {
    // Wrong key
    sendGameEvent("GAME_OVER", game.score);
    stopGame();
  }
}

// ===== JOGO 3: GATO E RATO =====
void initGatoRato() {
  gatoPosition = 0;       // Gato starts at LED 0
  ratoPosition = 8;       // Rato starts at LED 8 (example)
  ratoLastMove = millis();
  ratoMoveInterval = 1000; // Rato moves every 1 second initially
  ratoVisible = true;
  ratoLastBlink = millis();
  clearAllLEDs();
}

void updateGatoRatoDisplay() {
  clearAllLEDs();

  // Mostrar gato (sempre visível)
  setLED(gatoPosition, true);

  // Mostrar rato (piscando)
  if (ratoVisible && ratoPosition != gatoPosition) { // Don't show if caught
    setLED(ratoPosition, true);
  }
}

void updateGatoRato() {
  unsigned long currentTime = millis();

  // Mover o rato
  if (currentTime - ratoLastMove >= ratoMoveInterval) {
    int newPos;
    int attempts = 0; // Prevent infinite loop if gato covers all spots (unlikely)
    do {
      newPos = random(0, NUM_LEDS);
      attempts++;
    } while (newPos == gatoPosition && attempts < 10); // Rato tries not to spawn on Gato

    ratoPosition = newPos;
    ratoLastMove = currentTime;

    // Aumentar velocidade gradualmente
    if (ratoMoveInterval > 300) { // Min interval 300ms
      ratoMoveInterval -= 25;
    }
  }

  // Piscar o rato
  if (currentTime - ratoLastBlink >= 300) { // Blink every 300ms
    ratoVisible = !ratoVisible;
    ratoLastBlink = currentTime;
  }

  // Verificar captura
  if (gatoPosition == ratoPosition) {
    game.score += 20;
    sendGameEvent("SCORE", 20, game.score); // Send points gained and total score

    // Efeito de captura
    for(int i = 0; i < 3; i++) {
      setLED(gatoPosition, true);
      delay(100);
      setLED(gatoPosition, false);
      delay(100);
    }

    // Reposicionar Gato e Rato
    gatoPosition = random(0, NUM_LEDS);
    int attempts = 0;
    do {
      ratoPosition = random(0, NUM_LEDS);
      attempts++;
    } while (ratoPosition == gatoPosition && attempts < 10);

    ratoLastMove = millis(); // Reset timer do movimento do rato
  }

  updateGatoRatoDisplay();
}

void handleGatoRatoKey(int key) {
  // In this version, Gato is moved by KEY_PRESS to a specific LED
  if (key >= 0 && key < NUM_LEDS) {
    gatoPosition = key;
  }
}

// ===== JOGO 4: ESQUIVA METEOROS =====
void initEsquivaMeteoros() {
  playerPosition = 12; // Posição inicial na linha inferior (LEDs 12-15)
  meteoroLastSpawn = millis();
  meteoroSpawnInterval = 1500; // Spawn new meteor every 1.5s
  meteoroLastMove = millis();
  meteoroMoveInterval = 800;  // Meteors move down every 0.8s
  meteoroVisible = true;
  meteoroLastBlink = millis();

  // Limpar meteoros
  for (int i = 0; i < NUM_LEDS; i++) {
    meteoros[i] = false;
  }

  clearAllLEDs();
  setLED(playerPosition, true); // Show player initial position
}

void updateEsquivaMeteorosDisplay() {
  clearAllLEDs();

  // Mostrar player
  setLED(playerPosition, true);

  // Mostrar meteoros (piscando)
  if (meteoroVisible) {
    for (int i = 0; i < NUM_LEDS; i++) {
      if (meteoros[i] && i != playerPosition) { // Don't overwrite player if meteor is on same spot (though collision means game over)
        setLED(i, true);
      }
    }
  }
}

void updateEsquivaMeteoros() {
  unsigned long currentTime = millis();

  // Spawn meteoros na linha superior (LEDs 0-3)
  if (currentTime - meteoroLastSpawn >= meteoroSpawnInterval) {
    int spawnCol = random(0, 4); // Column 0 to 3
    meteoros[spawnCol] = true;   // Spawn in the first row (index = column)
    meteoroLastSpawn = currentTime;

    // Aumentar dificuldade (spawn mais rápido)
    if (meteoroSpawnInterval > 800) { // Min spawn interval 800ms
      meteoroSpawnInterval -= 50;
    }
  }

  // Mover meteoros para baixo
  if (currentTime - meteoroLastMove >= meteoroMoveInterval) {
    // Mover de baixo para cima no array para evitar sobrescrever antes de mover
    for (int row = 3; row >= 0; row--) {
      for (int col = 0; col < 4; col++) {
        int currentPos = row * 4 + col;
        if (meteoros[currentPos]) {
          meteoros[currentPos] = false; // Clear current position
          if (row < 3) { // If not in the last row
            meteoros[currentPos + 4] = true; // Move one row down
          }
          // Meteors that fall off the bottom row (row == 3) just disappear
        }
      }
    }
    meteoroLastMove = currentTime;

    // Aumentar velocidade (movem mais rápido)
    if (meteoroMoveInterval > 300) { // Min move interval 300ms
      meteoroMoveInterval -= 10;
    }
  }

  // Piscar meteoros
  if (currentTime - meteoroLastBlink >= 200) { // Blink every 200ms
    meteoroVisible = !meteoroVisible;
    meteoroLastBlink = currentTime;
  }

  // Verificar colisão
  if (meteoros[playerPosition]) {
    sendGameEvent("METEOR_HIT", playerPosition);
    stopGame();
    return; // Exit update since game is over
  }

  // Pontuar por sobrevivência (aproximadamente a cada segundo)
  // Check if a second has passed since last update for scoring
  if (currentTime - game.lastUpdateTime >= 1000) {
      game.score++;
      game.lastUpdateTime = currentTime; // Update time for next score increment
      // sendGameEvent("SCORE_UPDATE", game.score); // Optional: send score updates periodically
  }

  updateEsquivaMeteorosDisplay(); // Call the renamed display function
}


void handleEsquivaMeteoros(int key) {
  // Player can only move within the bottom row (LEDs 12-15)
  if (key >= 12 && key <= 15) {
    playerPosition = key;
  }
}

// ===== JOGO 5: GUITAR HERO =====
void initGuitarHero() {
  // playerPosition is not directly used for movement here, but for button presses
  lastNoteSpawn = millis();
  noteSpeed = 1000;         // Time for a note to move one row down
  noteSpawnInterval = 2000; // Time between new notes spawning
  noteCount = 0;

  // Limpar notas
  for (int i = 0; i < 8; i++) { // Max 8 notes on screen
    notes[i].active = false;
  }

  clearAllLEDs();
  // Brief flash of the action line (bottom row)
  for (int i = 12; i < 16; i++) {
    setLED(i, true);
  }
  delay(500);
  clearAllLEDs();
}

void spawnNote() {
  // Encontrar slot livre para nova nota
  for (int i = 0; i < 8; i++) {
    if (!notes[i].active) {
      notes[i].column = random(0, 4); // Spawn in one of the 4 columns
      notes[i].row = 0;               // Start at the top row
      notes[i].spawnTime = millis();  // Time it was spawned (or last moved)
      notes[i].active = true;
      // noteCount++; // Not strictly needed if just iterating through active notes
      break; // Spawn one note at a time
    }
  }
}

bool isLEDOnByNote(int pos) { // Helper to check if an LED is occupied by a note for display purposes
  for (int i = 0; i < 8; i++) {
    if (notes[i].active && (notes[i].row * 4 + notes[i].column) == pos) {
      return true;
    }
  }
  return false;
}

void updateGuitarHeroDisplay() {
  clearAllLEDs();

  // Mostrar notas ativas
  for (int i = 0; i < 8; i++) {
    if (notes[i].active) {
      int pos = notes[i].row * 4 + notes[i].column;
      setLED(pos, true);
    }
  }

  // Piscar linha de ação (linha 3 / LEDs 12-15)
  // Only blink if the LED is not already lit by a note
  if (millis() % 500 < 250) { // Blink effect
    for (int i = 12; i < 16; i++) {
      if (!isLEDOnByNote(i)) {
        setLED(i, true);
      }
    }
  }
}


void updateGuitarHero() {
  unsigned long currentTime = millis();

  // Spawn nova nota
  if (currentTime - lastNoteSpawn >= noteSpawnInterval) {
    spawnNote();
    lastNoteSpawn = currentTime;

    // Aumentar dificuldade (notas mais frequentes)
    if (noteSpawnInterval > 1000) { // Min interval 1s
      noteSpawnInterval -= 100;
    }
  }

  // Mover notas
  for (int i = 0; i < 8; i++) {
    if (notes[i].active) {
      if (currentTime - notes[i].spawnTime >= noteSpeed) { // Time to move down
        // Mover nota uma linha para baixo
        if (notes[i].row < 3) { // If not in the last (action) row
          notes[i].row++;
          notes[i].spawnTime = currentTime; // Reset timer for next move
        } else {
          // Nota chegou no final (row 3) e não foi pressionada a tempo
          // This logic means if it reaches row 3 and isn't hit *exactly* when it moves to row 3, it's a miss.
          // A better approach might be to check for hits when a key is pressed while note is in row 3.
          // For now, if it passes row 3 (i.e., was at row 3 and move timer elapsed), it's a miss.
          notes[i].active = false;
          sendGameEvent("NOTE_MISS", notes[i].column);
          // Potentially add a penalty to score here
        }
      }
    }
  }

  updateGuitarHeroDisplay();
}


void handleGuitarHero(int key) {
  if (key < 12 || key > 15) return; // Só aceita botões da linha inferior (12-15)

  int columnPressed = key - 12; // Convert LED index (12-15) to column (0-3)
  bool hitNote = false;

  // Verificar se há nota na coluna pressionada e na linha de ação (row 3)
  for (int i = 0; i < 8; i++) {
    if (notes[i].active && notes[i].column == columnPressed && notes[i].row == 3) {
      // HIT!
      notes[i].active = false; // Deactivate note
      game.score += 10;
      hitNote = true;
      sendGameEvent("NOTE_HIT", columnPressed, game.score);
      // Potentially increase noteSpeed or decrease spawnInterval for difficulty
      if (noteSpeed > 400 && game.score % 50 == 0) noteSpeed -= 50; // Faster notes
      break; // Assume only one note can be hit per key press in that spot
    }
  }

  if (!hitNote) {
    // Missed (pressed when no note was there or wrong timing)
    sendGameEvent("NOTE_MISS", columnPressed);
    if (game.score > 0) game.score = max(0, game.score - 5); // Penalty
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
  roletaSafeIndex = random(0, NUM_LEDS); // One LED is safe
  roletaWaitingChoice = true;
  roletaChoiceTime = millis(); // Timer for choice (not used in this logic yet)

  // Efeito dramático - piscar todos os LEDs
  for(int i = 0; i < 3; i++) {
    for(int j = 0; j < NUM_LEDS; j++) {
      setLED(j, true);
    }
    delay(200);
    clearAllLEDs();
    delay(200);
  }

  roletaMultiplier = pow(2, roletaRound - 1); // Points double each round
  sendGameEvent("ROLETA_ROUND_START", roletaRound, (int)roletaMultiplier);
}

void handleRoletaRussaKey(int key) {
  if (!roletaWaitingChoice) return;

  roletaWaitingChoice = false; // Choice made

  // Efeito dramático - piscar o LED escolhido
  for(int i = 0; i < 5; i++) {
    setLED(key, true);
    delay(150);
    setLED(key, false);
    delay(150);
  }

  if (key == roletaSafeIndex) {
    // SEGURO! Continua
    int points = (int)roletaMultiplier;
    game.score += points;
    roletaRound++;

    setLED(key, true); // Show safe LED
    sendGameEvent("ROLETA_SAFE", key, game.score);
    delay(2000); // Display safe choice for 2 seconds
    setLED(key, false);

    if (roletaRound <= 8) { // Max 8 rounds (example)
      startRoletaRound();
    } else {
      sendGameEvent("ROLETA_MAX_WIN", game.score);
      stopGame();
    }
  } else {
    // BOOM! Perdeu tudo
    setLED(key, true); // Show exploded LED
    delay(1000);
    // Optional: Flash all LEDs red or something
    setLED(key, false);


    sendGameEvent("ROLETA_EXPLODE", key, 0); // Score becomes 0
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
  lightningShowDuration = 500; // How long the pattern is shown

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
  lightningWaitingInput = false;
  lightningLastShow = millis(); // Start timer for showing pattern

  // Mostrar padrão completo rapidamente
  for(int i = 0; i < lightningPatternLength; i++) {
    setLED(lightningPattern[i], true);
  }

  sendGameEvent("LIGHTNING_PATTERN_SHOW", lightningPatternLength, lightningShowDuration);
}

void updateLightningStrike() {
  if (lightningShowingPattern) {
    if (millis() - lightningLastShow >= lightningShowDuration) {
      clearAllLEDs(); // Hide pattern
      lightningShowingPattern = false;
      lightningWaitingInput = true;
      lightningInputIndex = 0; // Reset for player input

      sendGameEvent("LIGHTNING_INPUT_START");
    }
  }
}

void handleLightningStrikeKey(int key) {
  if (!lightningWaitingInput || lightningShowingPattern) return;

  if (key == lightningPattern[lightningInputIndex]) {
    setLED(key, true); // Visual feedback
    delay(100);
    setLED(key, false);
    lightningInputIndex++;

    if (lightningInputIndex >= lightningPatternLength) {
      // Correct sequence entered
      game.score += 10;
      game.level++;

      lightningPatternLength = min(lightningPatternLength + 1, 12); // Max length 12
      lightningShowDuration = max(lightningShowDuration - 50, 100); // Faster display, min 100ms

      sendGameEvent("LIGHTNING_COMPLETE", game.level, lightningShowDuration);

      delay(1000); // Pause before next round
      generateLightningPattern();
      startLightningRound();
    }
  } else {
    // ERRADO! Mostrar padrão correto e fim de jogo
    clearAllLEDs();
    for(int i = 0; i < lightningPatternLength; i++) {
      setLED(lightningPattern[i], true); // Show the correct pattern
    }

    sendGameEvent("LIGHTNING_WRONG", key, lightningPattern[lightningInputIndex]); // Send wrong key and expected key
    delay(2000); // Show correct pattern for 2s
    stopGame();
  }
}

// ===== JOGO 8: SNIPER MODE =====
void initSniperMode() {
  sniperCurrentHits = 0;
  sniperHitsRequired = 10; // Hits needed to win
  sniperFlashDuration = 100; // How long target stays lit
  sniperWaitingShot = false;
  clearAllLEDs();

  spawnSniperTarget();
}

void spawnSniperTarget() {
  clearAllLEDs(); // Clear previous target if any
  sniperTarget = random(0, NUM_LEDS);
  sniperTargetTime = millis();
  sniperWaitingShot = true;

  setLED(sniperTarget, true); // Light up the target
  sendGameEvent("SNIPER_TARGET_SPAWN", sniperTarget, sniperFlashDuration);
}

void updateSniperMode() {
  if (sniperWaitingShot) {
    // Check if target flash duration has passed
    if (millis() - sniperTargetTime >= sniperFlashDuration) {
      setLED(sniperTarget, false); // Turn off target LED

      // Check if player missed (waited too long after flash)
      // Add a small buffer time for reaction after flash ends, e.g., 500ms
      // If no key pressed within sniperFlashDuration + buffer, it's a timeout for that target
      if (millis() - sniperTargetTime >= sniperFlashDuration + 500) {
        sendGameEvent("SNIPER_TIMEOUT"); // Player didn't shoot in time
        if (game.score > 0) game.score = max(0, game.score-1); // Penalty

        sniperWaitingShot = false; // No longer waiting for this specific target
        delay(random(500, 2000)); // Random delay before next target
        spawnSniperTarget();
      }
      // Note: The key press handler will set sniperWaitingShot to false upon a hit or miss.
      // This timeout logic is for when no key is pressed at all for the target.
    }
  }
}

void handleSniperModeKey(int key) {
  if (!sniperWaitingShot) return; // Not waiting for a shot or target already handled

  unsigned long reactionTime = millis() - sniperTargetTime;

  // Check if the correct target was hit AND within the allowed time window
  // Allow a small margin for reaction, e.g., target must be hit while lit or shortly after
  if (key == sniperTarget && reactionTime <= (sniperFlashDuration + 100) ) { // Hit within flash + 100ms grace
    sniperCurrentHits++;
    game.score += 10;

    setLED(sniperTarget, false); // Turn off target if it was still on (unlikely due to updateSniperMode)
    sniperWaitingShot = false;   // Shot processed

    sendGameEvent("SNIPER_HIT", sniperCurrentHits, reactionTime);

    if (sniperCurrentHits >= sniperHitsRequired) {
      sendGameEvent("SNIPER_VICTORY", game.score);
      game.score *= 2; // Bonus for victory
      stopGame();
    } else {
      delay(random(300, 1500)); // Random delay before next target
      spawnSniperTarget();
    }
  } else {
    // Missed (wrong key or too slow)
    sendGameEvent("SNIPER_MISS", key, reactionTime);
    if (game.score > 0) game.score = max(0, game.score-2); // Penalty

    sniperWaitingShot = false; // Shot processed (as a miss)
    setLED(sniperTarget, false); // Ensure target is off
    delay(1000); // Delay after a miss
    spawnSniperTarget();
  }
}


// ===== HANDLERS PRINCIPAIS =====
void handleKeyPress(int key) {
  if (!game.gameActive || key < 0 || key >= NUM_LEDS) return;

  switch (game.currentMode) {
    case PEGA_LUZ:
      handlePegaLuzKey(key);
      break;
    case SEQUENCIA_MALUCA:
      handleSequenciaMalucaKey(key);
      break;
    case GATO_RATO:
      handleGatoRatoKey(key); // Gato is moved by key press to specific LED
      break;
    case ESQUIVA_METEOROS:
      handleEsquivaMeteoros(key); // Player is moved by key press on bottom row
      break;
    case GUITAR_HERO:
      handleGuitarHero(key);
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
    default: // MENU or other modes not handled by direct key press
      break;
  }
}

void handleMovement(String direction) {
  if (!game.gameActive) return;

  // This function is for games that use directional input (UP, DOWN, LEFT, RIGHT)
  // Gato e Rato can use this if you prefer directional movement over direct LED selection.
  // For now, Gato e Rato uses direct KEY_PRESS.
  // Esquiva Meteoros uses KEY_PRESS for left/right on the bottom row.

  // Example for Gato e Rato if using directional:
  /*
  if (game.currentMode == GATO_RATO) {
    int currentRow = gatoPosition / 4;
    int currentCol = gatoPosition % 4;
    if (direction == "UP" && currentRow > 0) gatoPosition -= 4;
    else if (direction == "DOWN" && currentRow < 3) gatoPosition += 4;
    else if (direction == "LEFT" && currentCol > 0) gatoPosition--;
    else if (direction == "RIGHT" && currentCol < 3) gatoPosition++;
  }
  */

  // Example for Esquiva Meteoros if using directional for the player on the bottom row:
  // (Currently it uses handleEsquivaMeteoros with specific key presses 12-15)
  /*
  if (game.currentMode == ESQUIVA_METEOROS) {
    // Player is on bottom row (LEDs 12-15)
    if (direction == "LEFT" && playerPosition > 12) playerPosition--;
    else if (direction == "RIGHT" && playerPosition < 15) playerPosition++;
  }
  */
}

void startGame(GameMode mode) {
  game.currentMode = mode;
  game.gameActive = true;
  game.score = 0;
  game.level = 1;
  game.difficulty = 1; // Can be used to adjust parameters within games
  game.gameStartTime = millis();
  game.lastUpdateTime = millis(); // For timed events like scoring in Esquiva Meteoros

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
    case ESQUIVA_METEOROS:
      initEsquivaMeteoros();
      break;
    case GUITAR_HERO:
      initGuitarHero();
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
    default: // MENU or invalid mode
      game.gameActive = false; // Don't start game if mode is invalid
      return;
  }

  sendGameEvent("GAME_STARTED", (int)mode);
}

void stopGame() {
  game.gameActive = false;
  // Display final score or game over message on LEDs if desired
  // For example, flash all LEDs or show score in binary, etc.
  clearAllLEDs(); // Clear LEDs on game stop
  sendGameEvent("GAME_OVER", game.score); // Send final score
  // game.currentMode = MENU; // Optionally return to menu state
}

// ===== LOOP PRINCIPAL =====
void loop() {
  processSerialCommands(); // Check for commands from PC/controller

  if (game.gameActive) {
    unsigned long currentTime = millis(); // Get current time once per loop iteration

    // Call update functions for the current game mode
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
        updateEsquivaMeteoros();
        break;
      case GUITAR_HERO:
        updateGuitarHero();
        break;
      case LIGHTNING_STRIKE:
        updateLightningStrike();
        break;
      case SNIPER_MODE:
        updateSniperMode();
        break;
      case ROLETA_RUSSA:
        // Roleta Russa is event-driven by key presses, no continuous update needed in loop
        break;
      default:
        // Should not happen if game is active with a valid mode
        break;
    }
    // game.lastUpdateTime = currentTime; // Update for next frame logic if needed globally
  } else {
    // Game is not active, perhaps show a menu pattern or idle animation
    // Example: slowly cycle a light or wait for START_GAME command
  }

  delay(10); // Small delay to prevent overwhelming the processor, adjust as needed
}
