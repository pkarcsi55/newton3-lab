/*
  HX711 Force Bluetooth Sender
  ESP32 + HX711 + load cell + 80 mHz órajel

  Cel:
    - csak eromeresi adatkuldes Bluetooth SPP-n
    - Windows alatt virtualis COM portkent jelenik meg
    - idobelyeges sorok: t_us;raw;force_N

  Javasolt hasznalat ket adohoz:
    - az egyik sketchben: DEVICE_NAME = "Newton_FORCE_A"
    - a masik sketchben: DEVICE_NAME = "Newton_FORCE_B"

  Parancsok Serialon vagy Bluetoothon:
    S vagy START  - meres inditasa, ido nullazasa
    X vagy STOP   - meres leallitasa
    Z vagy TARE   - HX711 nullazas
    F10..F100     - mintaveteli frekvencia beallitasa
    CAL <ertek>   - skala beallitasa N/count egysegben
    SPIKE ON/OFF  - ritka kiugras szures be/ki
    SPIKE <cnt>   - nyers count kuszob
    STAT          - allapot
    HELP          - sugo

  Kimenet meres kozben:
    t_us;raw;force_N

  Pelda:
    12500;-2134;-0.00007
*/

#include <Arduino.h>
#include <BluetoothSerial.h>
#include <HX711.h>
#include <stdarg.h>
#include <stdio.h>
#include <string.h>
#include <ctype.h>
#include <math.h>
#include <esp_system.h>

// -------------------- Eszkoznev --------------------
// Ket adonal ezt erdemes atirni:
//   Newton_FORCE_A
//   Newton_FORCE_B
static const char *DEVICE_NAME = "Newton_FORCE_A";

// -------------------- HX711 pinek --------------------
#define HX_DOUT 22
#define HX_SCK  19

// -------------------- Bluetooth / HX711 --------------------
BluetoothSerial SerialBT;
HX711 scale;

// -------------------- Kalibracio --------------------
// Ezek a korabbi bevalt ertekekbol indulnak ki.
// Ha az elojelet forditani kell, a hx_scale_N_per_count elojelet kell megforditani.
static float hx_offset_counts     = -2067.00146f;
static float hx_scale_N_per_count = +1.0709759e-06f;

// -------------------- Mintavetelezes --------------------
static int sampleHz = 80;
static uint32_t samplePeriodUs = 1000000UL / 80UL;
static uint32_t nextSampleUs = 0;
static uint32_t startUs = 0;
static bool measuring = false;

// -------------------- Timeoutok --------------------
static const uint32_t HX_READY_TIMEOUT_MS = 12;
static const uint32_t HX_TARE_TOTAL_MS    = 900;
static const uint32_t INPUT_CMD_GAP_MS    = 120;

// -------------------- Parancspuffer --------------------
static char cmdBuffer[40];
static size_t cmdLen = 0;
static uint32_t lastCmdCharMs = 0;

// -------------------- Statisztika --------------------
static uint32_t hxNotReadyCount = 0;
static uint32_t hxTimeoutCount = 0;
static uint32_t sampleSlipCount = 0;
static uint32_t cmdOverflowCount = 0;
static uint32_t sentCount = 0;

// -------------------- Spike szures --------------------
// Ritka, durva HX711 kiugrasok ellen.
// A szuro az irrealisan nagy egyedi ugrasokat az utolso jo mintaval helyettesiti.
static bool spikeFilterEnabled = true;
static bool haveLastRaw = false;
static long lastGoodRaw = 0;
static uint32_t spikeRejectCount = 0;

// Nyers HX711 count kuszob. Ha valodi utkozesnel is van, novelni kell.
//static long spikeThresholdCounts = 200000;
static long spikeThresholdCounts = 1000000;

// ============================================================
// Kimenet
// ============================================================
void sendOutCStr(const char *s) {
  if (SerialBT.hasClient()) {
    SerialBT.println(s);
  }
  Serial.println(s);
}

void sendOutFmt(const char *fmt, ...) {
  char line[160];
  va_list ap;
  va_start(ap, fmt);
  vsnprintf(line, sizeof(line), fmt, ap);
  va_end(ap);
  sendOutCStr(line);
}

void sendHelp() {
  sendOutCStr("");
  sendOutCStr("HX711 Force Bluetooth Sender");
  sendOutCStr("Parancsok:");
  sendOutCStr("  S / START    - meres inditasa, ido nullazasa");
  sendOutCStr("  X / STOP     - meres leallitasa");
  sendOutCStr("  Z / TARE     - HX711 nullazas");
  sendOutCStr("  F10..F100    - frekvencia Hz-ben");
  sendOutCStr("  CAL <ertek>  - skala N/count, pl. CAL 1.0709759e-06");
  sendOutCStr("  SPIKE ON/OFF - ritka kiugras szures be/ki");
  sendOutCStr("  SPIKE <cnt>  - nyers count kuszob, pl. SPIKE 200000");
  sendOutCStr("  STAT         - allapot");
  sendOutCStr("  HELP         - sugo");
  sendOutCStr("");
  sendOutCStr("Kimenet meres kozben: t_us;raw;force_N");
  sendOutCStr("");
}

void sendStats() {
  sendOutFmt("STAT:name=%s;Hz=%d;measuring=%d;offset=%.3f;scale=%.10e;sent=%lu;hx_not_ready=%lu;hx_timeout=%lu;sample_slip=%lu;cmd_overflow=%lu;spike_filter=%d;spike_threshold=%ld;spike_reject=%lu",
             DEVICE_NAME,
             sampleHz,
             measuring ? 1 : 0,
             hx_offset_counts,
             hx_scale_N_per_count,
             (unsigned long)sentCount,
             (unsigned long)hxNotReadyCount,
             (unsigned long)hxTimeoutCount,
             (unsigned long)sampleSlipCount,
             (unsigned long)cmdOverflowCount,
             spikeFilterEnabled ? 1 : 0,
             spikeThresholdCounts,
             (unsigned long)spikeRejectCount);
}

// ============================================================
// HX711
// ============================================================
bool hxWaitReady(uint32_t timeoutMs) {
  uint32_t t0 = millis();
  while (!scale.is_ready()) {
    if ((millis() - t0) >= timeoutMs) {
      hxTimeoutCount++;
      return false;
    }
    delay(1);
  }
  return true;
}

bool readHx(long &rawOut, float &forceOut) {
  if (!scale.is_ready()) {
    hxNotReadyCount++;
    if (!hxWaitReady(HX_READY_TIMEOUT_MS)) return false;
  }

  long raw = scale.read();

  if (spikeFilterEnabled && haveLastRaw) {
    long diff = labs(raw - lastGoodRaw);

    if (diff > spikeThresholdCounts) {
      spikeRejectCount++;

      // Hibásnak tekintett mintát nem engedünk tovább,
      // hanem az utolsó jó nyers értéket ismételjük.
      raw = lastGoodRaw;
    } else {
      lastGoodRaw = raw;
    }
  } else {
    lastGoodRaw = raw;
    haveLastRaw = true;
  }

  rawOut = raw;
  forceOut = ((float)raw - hx_offset_counts) * hx_scale_N_per_count;
  return true;
}

void tareHX() {
  const int TARGET_SAMPLES = 20;
  double sum = 0.0;
  int cnt = 0;
  uint32_t t0 = millis();

  bool wasMeasuring = measuring;
  measuring = false;

  while (cnt < TARGET_SAMPLES && (millis() - t0) < HX_TARE_TOTAL_MS) {
    if (scale.is_ready()) {
      long raw = scale.read();
      sum += (double)raw;
      cnt++;
      delay(10);
    } else {
      delay(2);
    }
  }

  if (cnt > 0) {
    hx_offset_counts = (float)(sum / cnt);
    haveLastRaw = false;
    sendOutFmt("OK:TARE offset=%.3f samples=%d", hx_offset_counts, cnt);
  } else {
    hxTimeoutCount++;
    sendOutCStr("ERR:TARE HX711 timeout");
  }

  measuring = wasMeasuring;
  nextSampleUs = micros();
}

// ============================================================
// Vezerles
// ============================================================
void setFrequency(int hz) {
  if (hz < 10 || hz > 100) {
    sendOutCStr("ERR:FREQ csak 10..100 Hz lehet");
    return;
  }
  sampleHz = hz;
  samplePeriodUs = 1000000UL / (uint32_t)sampleHz;
  nextSampleUs = micros();
  sendOutFmt("OK:FREQ %d Hz", sampleHz);
}

void startMeasurement() {
  startUs = micros();
  nextSampleUs = startUs;
  sentCount = 0;
  haveLastRaw = false;
  measuring = true;
  sendOutFmt("OK:START name=%s Hz=%d", DEVICE_NAME, sampleHz);
}

void stopMeasurement() {
  measuring = false;
  sendOutCStr("OK:STOP");
}

// ============================================================
// Parancsfeldolgozas
// ============================================================
void processCommand(const char *cmdRaw) {
  char cmd[40];
  strncpy(cmd, cmdRaw, sizeof(cmd) - 1);
  cmd[sizeof(cmd) - 1] = '\0';

  size_t len = strlen(cmd);
  while (len > 0 && isspace((unsigned char)cmd[len - 1])) cmd[--len] = '\0';

  size_t start = 0;
  while (isspace((unsigned char)cmd[start])) start++;
  if (start > 0) memmove(cmd, cmd + start, strlen(cmd + start) + 1);

  // A CAL parameternel megorizzuk az eredeti szamot, de a parancs elejet nagybetusitjuk.
  char upper[40];
  strncpy(upper, cmd, sizeof(upper) - 1);
  upper[sizeof(upper) - 1] = '\0';
  for (size_t i = 0; upper[i]; i++) upper[i] = (char)toupper((unsigned char)upper[i]);

  if (upper[0] == '\0') return;

  if (strcmp(upper, "HELP") == 0) { sendHelp(); return; }
  if (strcmp(upper, "STAT") == 0) { sendStats(); return; }
  if (strcmp(upper, "S") == 0 || strcmp(upper, "START") == 0) { startMeasurement(); return; }
  if (strcmp(upper, "X") == 0 || strcmp(upper, "STOP") == 0) { stopMeasurement(); return; }
  if (strcmp(upper, "Z") == 0 || strcmp(upper, "TARE") == 0) { tareHX(); return; }

  if (upper[0] == 'F') {
    int hz = atoi(upper + 1);
    setFrequency(hz);
    return;
  }

  if (strncmp(upper, "CAL ", 4) == 0) {
    float v = atof(cmd + 4);
    if (v == 0.0f || isnan(v)) {
      sendOutCStr("ERR:CAL ervenytelen ertek");
      return;
    }
    hx_scale_N_per_count = v;
    sendOutFmt("OK:CAL scale=%.10e", hx_scale_N_per_count);
    return;
  }

  if (strncmp(upper, "SPIKE ", 6) == 0) {
    long v = atol(cmd + 6);
    if (v <= 0) {
      sendOutCStr("ERR:SPIKE ervenytelen kuszob");
      return;
    }
    spikeThresholdCounts = v;
    sendOutFmt("OK:SPIKE threshold=%ld", spikeThresholdCounts);
    return;
  }

  if (strcmp(upper, "SPIKE ON") == 0) {
    spikeFilterEnabled = true;
    haveLastRaw = false;
    sendOutCStr("OK:SPIKE ON");
    return;
  }

  if (strcmp(upper, "SPIKE OFF") == 0) {
    spikeFilterEnabled = false;
    haveLastRaw = false;
    sendOutCStr("OK:SPIKE OFF");
    return;
  }

  sendOutFmt("ERR:UNKNOWN %s", cmd);
}

void commitCmdBuffer() {
  if (cmdLen == 0) return;
  cmdBuffer[cmdLen] = '\0';
  processCommand(cmdBuffer);
  cmdLen = 0;
  cmdBuffer[0] = '\0';
}

void handleIncomingChar(char c) {
  lastCmdCharMs = millis();

  if (c == '\n' || c == '\r') {
    commitCmdBuffer();
    return;
  }

  if ((uint8_t)c < 32 || (uint8_t)c > 126) return;

  if (cmdLen < sizeof(cmdBuffer) - 1) {
    cmdBuffer[cmdLen++] = c;
    cmdBuffer[cmdLen] = '\0';
  } else {
    cmdOverflowCount++;
    cmdLen = 0;
    cmdBuffer[0] = '\0';
    sendOutCStr("ERR:CMD_OVERFLOW");
  }
}

void pollInputs() {
  while (Serial.available()) handleIncomingChar((char)Serial.read());
  while (SerialBT.available()) handleIncomingChar((char)SerialBT.read());

  if (cmdLen > 0 && (millis() - lastCmdCharMs) > INPUT_CMD_GAP_MS) {
    commitCmdBuffer();
  }
}

// ============================================================
// Meresi ciklus
// ============================================================
void doMeasurement() {
  if (!measuring) return;

  uint32_t nowUs = micros();
  if ((int32_t)(nowUs - nextSampleUs) < 0) return;

  if ((uint32_t)(nowUs - nextSampleUs) > samplePeriodUs * 3UL) {
    sampleSlipCount++;
    nextSampleUs = nowUs + samplePeriodUs;
  } else {
    nextSampleUs += samplePeriodUs;
  }

  long raw = 0;
  float forceN = NAN;
  uint32_t tRelUs = nowUs - startUs;

  char line[80];
  if (readHx(raw, forceN)) {
    snprintf(line, sizeof(line), "%lu;%ld;%.6f", (unsigned long)tRelUs, raw, forceN);
  } else {
    snprintf(line, sizeof(line), "%lu;NA;NA", (unsigned long)tRelUs);
  }

  if (SerialBT.hasClient()) 
  SerialBT.println(line);
  delayMicroseconds(50);
  Serial.println(line);
  sentCount++;
}

// ============================================================
// Setup / Loop
// ============================================================
void setup() {
  setCpuFrequencyMhz(80);   //ha nagyon zajos a környezet
  Serial.begin(115200);
  delay(1000);

  SerialBT.begin(DEVICE_NAME);

  scale.begin(HX_DOUT, HX_SCK);

  sendOutCStr("");
  sendOutFmt("Boot: %s", DEVICE_NAME);
  sendOutCStr("ESP32 + HX711 force sender");
  sendOutFmt("BT name: %s", DEVICE_NAME);
  sendOutFmt("Default: %d Hz", sampleHz);
  sendOutFmt("HX offset: %.3f", hx_offset_counts);
  sendOutFmt("HX scale: %.10e N/count", hx_scale_N_per_count);
  sendOutFmt("Spike filter: %s, threshold=%ld counts", spikeFilterEnabled ? "ON" : "OFF", spikeThresholdCounts);

  if (scale.is_ready()) sendOutCStr("HX711: OK");
  else                  sendOutCStr("HX711: not ready");

  sendHelp();
}

void loop() {
  pollInputs();
  doMeasurement();
}