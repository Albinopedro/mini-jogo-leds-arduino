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
  GUITAR_HERO = 5, LIGHTNING_STRIKE = 6, SNIPER_MODE = 7
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
bool pegaLuzJustHit = false;

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
unsigned long ratoMoveInterval = 800;
bool ratoVisible = true;
unsigned long ratoLastBlink = 0;
int captureBlinkCount = 0;
unsigned long gatoRatoTimeLimit = 120000; // 2 minutes
int gatoRatoCapturesRequired = 16; // Need 16 captures to win
int gatoRatoCaptureCount = 0;
bool playerCanMove = true; // Player can only move once per rat movement

// Esquiva Meteoros
int playerPosition = 12;
bool meteoros[NUM_LEDS];
unsigned long meteoroLastSpawn = 0;
unsigned long meteoroSpawnInterval = 1200; // Faster spawning
unsigned long meteoroLastMove = 0;
unsigned long meteoroMoveInterval = 600; // Faster movement
bool meteoroVisible = true;
unsigned long meteoroLastBlink = 0;

// Guitar Hero
struct Note { int column; int row; unsigned long spawnTime; bool active; };
Note notes[8];
unsigned long lastNoteSpawn = 0;
unsigned long noteSpeed = 800; // Faster notes
int noteSpawnInterval = 1500; // More frequent notes



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
unsigned long sniperFlashDuration = 300;
int sniperHitsRequired = 7; // Updated: Need 7 hits to win (challenging but achievable)
int sniperCurrentHits = 0;
unsigned long nextTargetDelay = 0;

// ===== EFEITOS VISUAIS =====
enum AnimationType {
  ANIM_NONE,
  ANIM_STARTUP_SEQUENCE,
  ANIM_CONNECTION_SUCCESS,
  ANIM_GAME_START,
  ANIM_GAME_OVER,
  ANIM_LEVEL_UP,
  ANIM_PERFECT_HIT,
  ANIM_COMBO,
  ANIM_COUNTDOWN,
  ANIM_EXPLOSION,
  ANIM_VICTORY,
  ANIM_DISCONNECT,
  ANIM_WAITING_CONNECTION,
  ANIM_ERROR,
  ANIM_RAINBOW_WAVE,
  ANIM_PULSE_ALL,
  ANIM_SPIRAL_IN,
  ANIM_SPIRAL_OUT,
  ANIM_MATRIX_RAIN,
  ANIM_FIREWORKS
};

struct AnimationState {
  AnimationType currentAnimation;
  unsigned long animationStartTime;
  int animationStep;
  int animationData[4]; // Para parâmetros extras
  bool animationActive;
  bool animationLoop;
} animState;

// ===== DECLARAÇÕES FORWARD DAS FUNÇÕES =====
void playAnimation(AnimationType type, bool loop);
void stopAnimation();
void updateAnimations();
void updateStartupSequence(unsigned long elapsed);
void updateWaitingConnection(unsigned long elapsed);
void updateConnectionSuccess(unsigned long elapsed);
void updateGameStart(unsigned long elapsed);
void updateGameOver(unsigned long elapsed);
void updateLevelUp(unsigned long elapsed);
void updatePerfectHit(unsigned long elapsed);
void updateCombo(unsigned long elapsed);
void updateExplosion(unsigned long elapsed);
void updateVictory(unsigned long elapsed);
void updateDisconnect(unsigned long elapsed);
void updateError(unsigned long elapsed);
void updateRainbowWave(unsigned long elapsed);
void updatePulseAll(unsigned long elapsed);
void updateSpiralIn(unsigned long elapsed);
void updateSpiralOut(unsigned long elapsed);
void updateMatrixRain(unsigned long elapsed);
void updateFireworks(unsigned long elapsed);

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

  // Initialize animation system
  animState.currentAnimation = ANIM_NONE;
  animState.animationActive = false;
  animState.animationLoop = false;

  // Epic startup sequence
  playAnimation(ANIM_STARTUP_SEQUENCE, false);

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

// ===== SISTEMA DE ANIMAÇÕES =====
void playAnimation(AnimationType type, bool loop) {
  animState.currentAnimation = type;
  animState.animationStartTime = millis();
  animState.animationStep = 0;
  animState.animationActive = true;
  animState.animationLoop = loop;
  for (int i = 0; i < 4; i++) animState.animationData[i] = 0;
}

void stopAnimation() {
  animState.animationActive = false;
  animState.currentAnimation = ANIM_NONE;
  clearAllLEDs();
}

void updateAnimations() {
  if (!animState.animationActive) return;

  unsigned long currentTime = millis();
  unsigned long elapsed = currentTime - animState.animationStartTime;

  switch (animState.currentAnimation) {
    case ANIM_STARTUP_SEQUENCE:
      updateStartupSequence(elapsed);
      break;
    case ANIM_CONNECTION_SUCCESS:
      updateConnectionSuccess(elapsed);
      break;
    case ANIM_GAME_START:
      updateGameStart(elapsed);
      break;
    case ANIM_GAME_OVER:
      updateGameOver(elapsed);
      break;
    case ANIM_LEVEL_UP:
      updateLevelUp(elapsed);
      break;
    case ANIM_PERFECT_HIT:
      updatePerfectHit(elapsed);
      break;
    case ANIM_COMBO:
      updateCombo(elapsed);
      break;
    case ANIM_COUNTDOWN:
      updateGameStart(elapsed);
      break;
    case ANIM_EXPLOSION:
      updateExplosion(elapsed);
      break;
    case ANIM_VICTORY:
      updateVictory(elapsed);
      break;
    case ANIM_DISCONNECT:
      updateDisconnect(elapsed);
      break;
    case ANIM_WAITING_CONNECTION:
      updateWaitingConnection(elapsed);
      break;
    case ANIM_ERROR:
      updateError(elapsed);
      break;
    case ANIM_RAINBOW_WAVE:
      updateRainbowWave(elapsed);
      break;
    case ANIM_PULSE_ALL:
      updatePulseAll(elapsed);
      break;
    case ANIM_SPIRAL_IN:
      updateSpiralIn(elapsed);
      break;
    case ANIM_SPIRAL_OUT:
      updateSpiralOut(elapsed);
      break;
    case ANIM_MATRIX_RAIN:
      updateMatrixRain(elapsed);
      break;
    case ANIM_FIREWORKS:
      updateFireworks(elapsed);
      break;
  }
}

// ===== ANIMAÇÕES ESPECÍFICAS =====

void updateStartupSequence(unsigned long elapsed) {
  // Sequência épica de inicialização (4 segundos)
  clearAllLEDs();

  if (elapsed < 500) {
    // Fase 1: Pulse center (LED 5,6,9,10)
    bool on = (elapsed / 100) % 2;
    setLED(5, on); setLED(6, on); setLED(9, on); setLED(10, on);
  }
  else if (elapsed < 1500) {
    // Fase 2: Espiral crescente do centro
    int step = ((elapsed - 500) / 100) % 12;
    int spiral[] = {5, 6, 10, 9, 1, 2, 7, 11, 14, 13, 8, 4, 0, 3, 15, 12};
    for (int i = 0; i <= step && i < 16; i++) {
      setLED(spiral[i], true);
    }
  }
  else if (elapsed < 2500) {
    // Fase 3: Ondas por linha
    int line = ((elapsed - 1500) / 250) % 4;
    for (int i = line * 4; i < (line + 1) * 4; i++) {
      setLED(i, true);
    }
  }
  else if (elapsed < 3500) {
    // Fase 4: Efeito matriz - chuva de LEDs
    for (int col = 0; col < 4; col++) {
      int row = ((elapsed - 2500) / 100 + col) % 8;
      if (row < 4) setLED(row * 4 + col, true);
    }
  }
  else if (elapsed < 4000) {
    // Fase 5: Flash final - todos os LEDs
    bool flash = ((elapsed - 3500) / 100) % 2;
    for (int i = 0; i < NUM_LEDS; i++) {
      setLED(i, flash);
    }
  }
  else {
    // Fim: Iniciar animação de espera
    stopAnimation();
    playAnimation(ANIM_WAITING_CONNECTION, true);
  }
}

void updateWaitingConnection(unsigned long elapsed) {
  // Breathing effect - pulso suave
  clearAllLEDs();
  int brightness = (sin((elapsed / 1000.0) * 2 * PI) + 1) * 2; // 0-4

  // Acende LEDs dos cantos com intensidade variável
  bool on = brightness > 2;
  setLED(0, on);  // Canto superior esquerdo
  setLED(3, on);  // Canto superior direito
  setLED(12, on); // Canto inferior esquerdo
  setLED(15, on); // Canto inferior direito

  // Reset a cada 4 segundos
  if (elapsed > 4000) {
    animState.animationStartTime = millis();
  }
}

void updateConnectionSuccess(unsigned long elapsed) {
  // Explosão de alegria (2 segundos)
  clearAllLEDs();

  if (elapsed < 500) {
    // Explosão do centro
    int radius = elapsed / 50;
    int center[] = {5, 6, 9, 10}; // Centro
    int ring1[] = {1, 2, 4, 7, 8, 11, 13, 14}; // Anel 1
    int ring2[] = {0, 3, 12, 15}; // Cantos

    for (int i = 0; i < 4; i++) setLED(center[i], true);
    if (radius > 2) for (int i = 0; i < 8; i++) setLED(ring1[i], true);
    if (radius > 5) for (int i = 0; i < 4; i++) setLED(ring2[i], true);
  }
  else if (elapsed < 1500) {
    // Ondas concêntricas
    int wave = ((elapsed - 500) / 100) % 3;
    for (int i = 0; i < NUM_LEDS; i++) {
      if ((i / 4 + i % 4) % 3 == wave) setLED(i, true);
    }
  }
  else if (elapsed < 2000) {
    // Flash de vitória
    bool flash = ((elapsed - 1500) / 100) % 2;
    for (int i = 0; i < NUM_LEDS; i++) setLED(i, flash);
  }
  else {
    stopAnimation();
  }
}

void updateGameStart(unsigned long elapsed) {
  // Countdown épico (3 segundos)
  clearAllLEDs();

  if (elapsed < 1000) {
    // "3" - Forma número 3
    int three[] = {0, 1, 2, 6, 9, 10, 11, 14, 15};
    for (int i = 0; i < 9; i++) setLED(three[i], true);
  }
  else if (elapsed < 2000) {
    // "2" - Forma número 2
    int two[] = {0, 1, 2, 6, 8, 9, 10, 12, 13, 14, 15};
    for (int i = 0; i < 11; i++) setLED(two[i], true);
  }
  else if (elapsed < 3000) {
    // "1" - Forma número 1
    int one[] = {1, 2, 5, 6, 9, 10, 13, 14};
    for (int i = 0; i < 8; i++) setLED(one[i], true);
  }
  else {
    // GO! - Flash final
    bool flash = ((elapsed - 3000) / 50) % 2;
    for (int i = 0; i < NUM_LEDS; i++) setLED(i, flash);

    if (elapsed > 3500) stopAnimation();
  }
}

void updateGameOver(unsigned long elapsed) {
  // Efeito de morte/explosão (3 segundos)
  clearAllLEDs();

  if (elapsed < 1000) {
    // Implosão - LEDs apagam do exterior para centro
    int step = elapsed / 100;
    int implode[] = {0, 3, 12, 15, 1, 2, 4, 7, 8, 11, 13, 14, 5, 6, 9, 10};
    for (int i = step; i < 16; i++) {
      setLED(implode[i], true);
    }
  }
  else if (elapsed < 2000) {
    // Flash vermelho (simulação de explosão)
    bool flash = ((elapsed - 1000) / 100) % 2;
    // Apenas linha vermelha (0-3)
    for (int i = 0; i < 4; i++) setLED(i, flash);
  }
  else if (elapsed < 3000) {
    // Fade out lento
    int fadeStep = (elapsed - 2000) / 100;
    bool show = fadeStep % 3 != 0; // Pisca mais devagar
    for (int i = 0; i < 4; i++) setLED(i, show);
  }
  else {
    stopAnimation();
  }
}

void updateLevelUp(unsigned long elapsed) {
  // Celebração de nível (2 segundos)
  clearAllLEDs();

  if (elapsed < 1000) {
    // Ondas de energia subindo
    int wave = (elapsed / 100) % 8;
    for (int row = 0; row < 4; row++) {
      if ((wave - row) % 4 == 0) {
        for (int col = 0; col < 4; col++) {
          setLED(row * 4 + col, true);
        }
      }
    }
  }
  else if (elapsed < 2000) {
    // Estrela de vitória
    bool flash = ((elapsed - 1000) / 150) % 2;
    int star[] = {1, 2, 4, 7, 8, 11, 13, 14}; // Forma de estrela
    for (int i = 0; i < 8; i++) setLED(star[i], flash);
  }
  else {
    stopAnimation();
  }
}

void updatePerfectHit(unsigned long elapsed) {
  // Efeito de acerto perfeito (1 segundo)
  clearAllLEDs();

  // Explosão rápida do centro
  int radius = elapsed / 50;
  if (radius < 8) {
    int center[] = {5, 6, 9, 10};
    int ring1[] = {1, 2, 4, 7, 8, 11, 13, 14};
    int ring2[] = {0, 3, 12, 15};

    for (int i = 0; i < 4; i++) setLED(center[i], true);
    if (radius > 2) for (int i = 0; i < 8; i++) setLED(ring1[i], true);
    if (radius > 4) for (int i = 0; i < 4; i++) setLED(ring2[i], true);
  }

  if (elapsed > 1000) stopAnimation();
}

void updateCombo(unsigned long elapsed) {
  // Efeito de combo (1.5 segundos)
  clearAllLEDs();

  // Ondas laterais convergindo
  int step = (elapsed / 100) % 8;
  for (int i = 0; i < 4; i++) {
    if (step >= i) setLED(i, true);          // Esquerda para direita (linha 0)
    if (step >= i) setLED(4 + i, true);      // Esquerda para direita (linha 1)
    if (step >= i) setLED(8 + i, true);      // Esquerda para direita (linha 2)
    if (step >= i) setLED(12 + i, true);     // Esquerda para direita (linha 3)
  }

  if (elapsed > 1500) stopAnimation();
}

void updateExplosion(unsigned long elapsed) {
  // Explosão massiva (2 segundos)
  clearAllLEDs();

  if (elapsed < 200) {
    // Flash inicial
    for (int i = 0; i < NUM_LEDS; i++) setLED(i, true);
  }
  else if (elapsed < 1500) {
    // Fragmentos voando
    bool flash = ((elapsed - 200) / 50) % 2;
    int fragments[] = {0, 2, 5, 7, 8, 10, 13, 15}; // Padrão de fragmentos
    for (int i = 0; i < 8; i++) setLED(fragments[i], flash);
  }
  else {
    // Fade final
    bool fade = ((elapsed - 1500) / 250) % 2;
    setLED(0, fade); setLED(3, fade); setLED(12, fade); setLED(15, fade);
  }

  if (elapsed > 2000) stopAnimation();
}

void updateVictory(unsigned long elapsed) {
  // Celebração épica de vitória (4 segundos)
  clearAllLEDs();

  if (elapsed < 1000) {
    // Fogos de artifício
    int firework = (elapsed / 100) % 4;
    int patterns[][4] = {{1, 5, 9, 13}, {2, 6, 10, 14}, {0, 4, 8, 12}, {3, 7, 11, 15}};
    for (int i = 0; i < 4; i++) setLED(patterns[firework][i], true);
  }
  else if (elapsed < 3000) {
    // Chuva de estrelas
    int rain = (elapsed - 1000) / 100;
    for (int i = 0; i < NUM_LEDS; i++) {
      bool on = (rain + i) % 3 == 0;
      setLED(i, on);
    }
  }
  else {
    // Grande final - todos piscando
    bool bigFlash = ((elapsed - 3000) / 100) % 2;
    for (int i = 0; i < NUM_LEDS; i++) setLED(i, bigFlash);
  }

  if (elapsed > 4000) stopAnimation();
}

void updateDisconnect(unsigned long elapsed) {
  // Sequência de desconexão (2 segundos)
  clearAllLEDs();

  if (elapsed < 1500) {
    // LEDs apagam em espiral
    int step = elapsed / 100;
    int spiral[] = {0, 1, 2, 3, 7, 11, 15, 14, 13, 12, 8, 4, 5, 6, 10, 9};
    for (int i = step; i < 16; i++) {
      setLED(spiral[i], true);
    }
  }
  else {
    // Flash final de despedida
    bool flash = ((elapsed - 1500) / 250) % 2;
    setLED(5, flash); setLED(6, flash); setLED(9, flash); setLED(10, flash);
  }

  if (elapsed > 2000) {
    stopAnimation();
    clearAllLEDs();
  }
}

void updateError(unsigned long elapsed) {
  // Efeito de erro (1 segundo)
  clearAllLEDs();

  // Flash vermelho rápido
  bool flash = (elapsed / 100) % 2;
  for (int i = 0; i < 4; i++) setLED(i, flash); // Apenas linha vermelha

  if (elapsed > 1000) stopAnimation();
}

void updateRainbowWave(unsigned long elapsed) {
  // Onda arco-íris (contínua se loop ativo)
  clearAllLEDs();

  int wave = (elapsed / 200) % 8;
  for (int i = 0; i < 4; i++) {
    int row = (wave + i) % 4;
    for (int col = 0; col < 4; col++) {
      setLED(row * 4 + col, true);
    }
  }

  if (!animState.animationLoop && elapsed > 2000) stopAnimation();
}

void updatePulseAll(unsigned long elapsed) {
  // Pulso geral (contínuo se loop ativo)
  bool pulse = (elapsed / 500) % 2;
  for (int i = 0; i < NUM_LEDS; i++) setLED(i, pulse);

  if (!animState.animationLoop && elapsed > 2000) stopAnimation();
}

void updateSpiralIn(unsigned long elapsed) {
  // Espiral para dentro
  clearAllLEDs();

  int step = (elapsed / 150) % 16;
  int spiral[] = {0, 1, 2, 3, 7, 11, 15, 14, 13, 12, 8, 4, 5, 6, 10, 9};
  for (int i = 0; i < step; i++) {
    setLED(spiral[i], true);
  }

  if (elapsed > 2400) stopAnimation();
}

void updateSpiralOut(unsigned long elapsed) {
  // Espiral para fora
  clearAllLEDs();

  int step = (elapsed / 150) % 16;
  int spiral[] = {5, 6, 10, 9, 4, 8, 12, 13, 14, 15, 11, 7, 3, 2, 1, 0};
  for (int i = 0; i < step; i++) {
    setLED(spiral[i], true);
  }

  if (elapsed > 2400) stopAnimation();
}

void updateMatrixRain(unsigned long elapsed) {
  // Efeito Matrix - chuva digital
  clearAllLEDs();

  for (int col = 0; col < 4; col++) {
    int dropPos = ((elapsed / 200) + col * 2) % 8;
    if (dropPos < 4) setLED(dropPos * 4 + col, true);
    if (dropPos > 0 && dropPos <= 4) setLED((dropPos - 1) * 4 + col, false);
  }

  if (!animState.animationLoop && elapsed > 3000) stopAnimation();
}

void updateFireworks(unsigned long elapsed) {
  // Fogos de artifício múltiplos
  clearAllLEDs();

  int phase = (elapsed / 300) % 6;
  switch (phase) {
    case 0: setLED(5, true); break;
    case 1: setLED(1, true); setLED(4, true); setLED(6, true); setLED(9, true); break;
    case 2: setLED(0, true); setLED(2, true); setLED(8, true); setLED(10, true); break;
    case 3: setLED(10, true); break;
    case 4: setLED(6, true); setLED(9, true); setLED(11, true); setLED(14, true); break;
    case 5: setLED(7, true); setLED(13, true); setLED(15, true); break;
  }

  if (elapsed > 1800) stopAnimation();
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
    if (command.startsWith("START_GAME:")) {
      stopAnimation(); // Para qualquer animação atual
      startGame((GameMode)command.substring(11).toInt());
    }
    else if (command == "STOP_GAME") stopGame();
    else if (command.startsWith("KEY_PRESS:")) handleKeyPress(command.substring(10).toInt());
    else if (command.startsWith("KEY_RELEASE:")) handleKeyRelease(command.substring(12).toInt());
    else if (command == "INIT") {
      stopAnimation();
      playAnimation(ANIM_CONNECTION_SUCCESS, false);
      Serial.println("READY");
    }
    else if (command == "DISCONNECT") {
      playAnimation(ANIM_DISCONNECT, false);
      // clearAllLEDs(); // Will be called by animation
    }
    // Comandos especiais para efeitos
    else if (command == "EFFECT_RAINBOW") playAnimation(ANIM_RAINBOW_WAVE, true);
    else if (command == "EFFECT_MATRIX") playAnimation(ANIM_MATRIX_RAIN, true);
    else if (command == "EFFECT_PULSE") playAnimation(ANIM_PULSE_ALL, true);
    else if (command == "EFFECT_FIREWORKS") playAnimation(ANIM_FIREWORKS, false);
    else if (command == "STOP_EFFECTS") stopAnimation();
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
  pegaLuzJustHit = false;
}

void updatePegaLuz() {
  unsigned long currentTime = millis();
  if (pegaLuzState == PL_PLAYING && pegaLuzTarget >= 0 && !pegaLuzJustHit) {
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
    pegaLuzJustHit = true; // Prevent timeout check after hit
    setLED(pegaLuzTarget, false);

    // Efeito visual para acerto
    unsigned long reactionTime = millis() - pegaLuzStartTime;
    if (reactionTime < 150) {
      playAnimation(ANIM_PERFECT_HIT, false); // Acerto perfeito
      sendGameEvent("PERFECT");
    } else {
      playAnimation(ANIM_COMBO, false); // Acerto normal
    }

    game.score += 10;
    sendGameEvent("HIT", key, game.score);

    // Check victory condition: 400 points
    if (game.score >= 400) {
      sendGameEvent("PEGA_LUZ_VICTORY", game.score);
      playAnimation(ANIM_VICTORY, false);
      stopGame();
      return;
    }

    if (game.score > 0 && game.score % 50 == 0) {
      pegaLuzTimeout = max(pegaLuzTimeout - 50, 500);
      game.level++;
      playAnimation(ANIM_LEVEL_UP, false);
      sendGameEvent("LEVEL_UP", game.level);
    }
    pegaLuzState = PL_PAUSE_AFTER_HIT;
    stateChangeTime = millis();
  } else if (pegaLuzTarget >= 0) {
    playAnimation(ANIM_ERROR, false);
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
    setLED(key, true);
    sequenciaPlayerIndex++;

    if (sequenciaPlayerIndex >= sequenciaLength) {
      // Sequência completa!
      playAnimation(ANIM_PERFECT_HIT, false);
      game.score += 10;
      game.level++;
      sequenciaLength = min(sequenciaLength + 1, 12);

      if (game.level % 3 == 0) {
        playAnimation(ANIM_LEVEL_UP, false);
      }

      // Check victory condition: 80 points (8 sequences completed, reaching 10 steps)
      if (game.score >= 80) {
        sendGameEvent("SEQUENCIA_MALUCA_VICTORY", game.score);
        playAnimation(ANIM_VICTORY, false);
        stopGame();
        return;
      }

      sendGameEvent("LEVEL_UP", game.level, game.score);
      sequenciaState = SEQ_PAUSE_BEFORE_NEXT;
      stateChangeTime = millis();
    } else {
      // Acerto parcial - pequeno feedback
      playAnimation(ANIM_COMBO, false);
    }
  } else {
    // Erro - enviar evento de erro primeiro, depois game over
    sendGameEvent("WRONG_KEY", key);
    playAnimation(ANIM_EXPLOSION, false);
    sendGameEvent("GAME_OVER", game.score);
    stopGame();
  }
}

// ===== JOGO 3: GATO E RATO =====
void initGatoRato() {
  gatoPosition = 0;
  ratoPosition = 8;
  ratoLastMove = millis();
  ratoMoveInterval = 800;
  ratoVisible = true;
  ratoLastBlink = millis();
  gatoRatoState = GR_PLAYING;
  gatoRatoCaptureCount = 0;
  playerCanMove = true;
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

    // Check timeout (2 minutes) - end game when time is up
    if (currentTime - game.gameStartTime >= gatoRatoTimeLimit) {
        sendGameEvent("GATO_RATO_TIMEOUT", gatoRatoCaptureCount);
        sendGameEvent("GAME_OVER", game.score);
        stopGame();
        return;
    }

    if (gatoRatoState == GR_PLAYING) {
        if (currentTime - ratoLastMove >= ratoMoveInterval) {
            int newPos;
            do { newPos = random(0, NUM_LEDS); } while (newPos == gatoPosition);
            ratoPosition = newPos;
            ratoLastMove = currentTime;
            playerCanMove = true; // Player can move again after rat moves
        }

        unsigned long blinkInterval = 250;
        if (gatoRatoCaptureCount >= 8) {
            blinkInterval = 150; // Faster blinking after 8 captures
        }

        if (currentTime - ratoLastBlink >= blinkInterval) {
            ratoVisible = !ratoVisible;
            ratoLastBlink = currentTime;
        }
        if (gatoPosition == ratoPosition) {
            game.score += 20;
            gatoRatoCaptureCount++;

            // Increase difficulty: Rato becomes faster with each capture
            if (ratoMoveInterval > 300) {
                ratoMoveInterval -= 30;
            }

            sendGameEvent("SCORE", 20, game.score);

            // Check win condition
            if (gatoRatoCaptureCount >= gatoRatoCapturesRequired) {
                sendGameEvent("GATO_RATO_WIN", game.score);
                playAnimation(ANIM_VICTORY, false);
                stopGame();
                return;
            }

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
  if (gatoRatoState == GR_PLAYING && key >= 0 && key < NUM_LEDS && playerCanMove) {
    gatoPosition = key;
    playerCanMove = false; // Player can only move once per rat movement
  }
}

// ===== JOGO 4: ESQUIVA METEOROS =====
void initEsquivaMeteoros() {
  playerPosition = 12;
  meteoroLastSpawn = millis();
  meteoroSpawnInterval = 1200;
  meteoroLastMove = millis();
  meteoroMoveInterval = 600;
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

  // Increase spawn rate and difficulty over time
  int difficultyLevel = game.score / 10;
  int baseSpawnInterval = meteoroSpawnInterval - (difficultyLevel * 30);
  baseSpawnInterval = max(baseSpawnInterval, 400);

  if (currentTime - meteoroLastSpawn >= baseSpawnInterval) {
    // Spawn more meteors at higher levels
    int meteorCount = 1 + (difficultyLevel / 5);
    meteorCount = min(meteorCount, 3);

    for (int i = 0; i < meteorCount; i++) {
      meteoros[random(0, 4)] = true;
    }
    meteoroLastSpawn = currentTime;
  }

  int baseMoveInterval = meteoroMoveInterval - (difficultyLevel * 15);
  baseMoveInterval = max(baseMoveInterval, 200);

  if (currentTime - meteoroLastMove >= baseMoveInterval) {
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

    // Check victory condition: 180 points (3 minutes survival)
    if (game.score >= 180) {
        sendGameEvent("ESQUIVA_METEOROS_VICTORY", game.score);
        playAnimation(ANIM_VICTORY, false);
        stopGame();
        return;
    }
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
  noteSpeed = 800;
  noteSpawnInterval = 1500;
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

  // Increase difficulty over time
  int difficultyLevel = game.score / 30;
  int currentSpawnInterval = noteSpawnInterval - (difficultyLevel * 50);
  currentSpawnInterval = max(currentSpawnInterval, 600);

  if (currentTime - lastNoteSpawn >= currentSpawnInterval) {
    spawnNote();
    lastNoteSpawn = currentTime;
  }

  int currentNoteSpeed = noteSpeed - (difficultyLevel * 30);
  currentNoteSpeed = max(currentNoteSpeed, 300);

  for (int i = 0; i < 8; i++) {
    if (notes[i].active) {
      if (currentTime - notes[i].spawnTime >= currentNoteSpeed) {
        if (notes[i].row < 3) {
          notes[i].row++;
          notes[i].spawnTime = currentTime;
        } else {
          notes[i].active = false;
          sendGameEvent("NOTE_MISS", notes[i].column);
          // Penalty for missing notes
          if (game.score > 0) game.score = max(0, game.score - 5);
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

      // Check victory condition: 300 points
      if (game.score >= 300) {
        sendGameEvent("GUITAR_HERO_VICTORY", game.score);
        playAnimation(ANIM_VICTORY, false);
        stopGame();
        return;
      }
      break;
    }
  }
  if (!hitNote) {
    sendGameEvent("NOTE_MISS", columnPressed);
    if (game.score > 0) game.score = max(0, game.score - 10);
  }
}



// ===== JOGO 6: LIGHTNING STRIKE =====
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

            // Check victory condition: 70 points (7 successful sequences)
            if (game.score >= 70) {
                sendGameEvent("LIGHTNING_STRIKE_VICTORY", game.score);
                playAnimation(ANIM_VICTORY, false);
                stopGame();
                return;
            }

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

// ===== JOGO 7: SNIPER MODE =====
void initSniperMode() {
    sniperCurrentHits = 0;
    sniperHitsRequired = 8; // Updated: Aligned with user requirements
    sniperFlashDuration = 300;
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
            // VITÓRIA ÉPICA - Muito desafiador!
            playAnimation(ANIM_VICTORY, false);
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
  // Para animações e inicia countdown épico
  stopAnimation();
  playAnimation(ANIM_GAME_START, false);

  // Aguarda fim da animação de início
  while (animState.animationActive) {
    updateAnimations();
    delay(50);
  }

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
    case LIGHTNING_STRIKE: initLightningStrike(); break;
    case SNIPER_MODE: initSniperMode(); break;
    default:
      game.gameActive = false;
      playAnimation(ANIM_ERROR, false);
      return;
  }

  sendGameEvent("GAME_STARTED", (int)mode);
}

void stopGame() {
  game.gameActive = false;

  // Efeito visual baseado na pontuação
  if (game.score == 0) {
    playAnimation(ANIM_GAME_OVER, false); // Morte épica
  } else if (game.score >= 150) {
    playAnimation(ANIM_VICTORY, false);   // Vitória épica
  } else {
    playAnimation(ANIM_EXPLOSION, false); // Explosão normal
  }

  sendGameEvent("GAME_OVER", game.score);

  // Aguarda fim da animação
  while (animState.animationActive) {
    updateAnimations();
    delay(50);
  }

  clearAllLEDs();

  // Volta para animação de espera
  playAnimation(ANIM_WAITING_CONNECTION, true);
}

// ===== LOOP PRINCIPAL =====
void loop() {
  processSerialCommands();

  // Sempre atualiza animações (não bloqueia jogos)
  updateAnimations();

  if (game.gameActive) {
    switch (game.currentMode) {
      case PEGA_LUZ: updatePegaLuz(); break;
      case SEQUENCIA_MALUCA: updateSequenciaMaluca(); break;
      case GATO_RATO: updateGatoRato(); break;
      case ESQUIVA_METEOROS: updateEsquivaMeteoros(); break;
      case GUITAR_HERO: updateGuitarHero(); break;
      case LIGHTNING_STRIKE: updateLightningStrike(); break;
      case SNIPER_MODE: updateSniperMode(); break;
      default: break;
    }
  }
  // Animações rodam automaticamente quando não há jogo ativo
}
