#include <WiFi.h>
#include <WiFiUdp.h>
#include <Preferences.h>
#include "HX711.h"

// ===== DEFAULT BEÁLLÍTÁSOK =====
// Ezek csak akkor kellenek, ha még nincs mentett Preferences
const char* DEFAULT_WIFI_SSID     = "BagiNet";
const char* DEFAULT_WIFI_PASSWORD = "19920724";
const char* DEFAULT_PC_IP         = "192.168.1.8";
const uint16_t DEFAULT_UDP_PORT   = 4210;
const char* DEFAULT_DEVICE_ID     = "A";

// ===== FUTÁSI KONFIG =====
String wifiSSID;
String wifiPassword;
String pcIPString;
String deviceID;

IPAddress pcIP;
uint16_t udpPort = DEFAULT_UDP_PORT;

Preferences prefs;

// ===== HX711 BEÁLLÍTÁSOK =====
const int HX_DOUT = 22;
const int HX_SCK  = 27;

float hx_offset = -2067.0;
float hx_scale_N_per_count = -1.0709759e-6;

// ===== LED =====
const int LED_PIN = 2;

// ===== MINTAVÉTEL =====
const int SEND_HZ = 80;
const unsigned long SEND_PERIOD_US = 1000000UL / SEND_HZ;

WiFiUDP udp;
HX711 scale;

unsigned long lastSendUs = 0;
unsigned long packetCounter = 0;

// ======================================================
// KONFIGURÁCIÓ BETÖLTÉSE
// ======================================================

void loadConfig()
{
  prefs.begin("newton", true);

  wifiSSID     = prefs.getString("ssid", DEFAULT_WIFI_SSID);
  wifiPassword = prefs.getString("pass", DEFAULT_WIFI_PASSWORD);
  pcIPString   = prefs.getString("pcip", DEFAULT_PC_IP);
  udpPort      = prefs.getUInt("pcport", DEFAULT_UDP_PORT);
  deviceID     = prefs.getString("id", DEFAULT_DEVICE_ID);

  prefs.end();

  if (!pcIP.fromString(pcIPString))
  {
    pcIP.fromString(DEFAULT_PC_IP);
    pcIPString = DEFAULT_PC_IP;
  }

  if (deviceID != "A" && deviceID != "B")
  {
    deviceID = "A";
  }

  Serial.println();
  Serial.println("Mentett konfiguracio:");
  Serial.print("SSID: ");
  Serial.println(wifiSSID);
  Serial.print("PC IP: ");
  Serial.println(pcIPString);
  Serial.print("UDP port: ");
  Serial.println(udpPort);
  Serial.print("ID: ");
  Serial.println(deviceID);
}

// ======================================================
// KONFIGURÁCIÓ MENTÉSE
// ======================================================

void saveConfig()
{
  prefs.begin("newton", false);

  prefs.putString("ssid", wifiSSID);
  prefs.putString("pass", wifiPassword);
  prefs.putString("pcip", pcIPString);
  prefs.putUInt("pcport", udpPort);
  prefs.putString("id", deviceID);

  prefs.end();
}

// ======================================================
// SEGÉDFÜGGVÉNY: kulcs=érték kiolvasás
// ======================================================

String getValue(String data, String key)
{
  String pattern = ";" + key + "=";

  int start = data.indexOf(pattern);

  if (start >= 0)
  {
    start += pattern.length();
  }
  else
  {
    pattern = key + "=";

    if (data.startsWith(pattern))
    {
      start = pattern.length();
    }
    else
    {
      return "";
    }
  }

  int end = data.indexOf(";", start);
  if (end < 0) end = data.length();

  return data.substring(start, end);
}
// ======================================================
// USB-S SOROS PARANCSOK
// ======================================================

void handleSerialCommands()
{
  if (!Serial.available()) return;

  String line = Serial.readStringUntil('\n');
  line.trim();

  if (line.length() == 0) return;

  if (line == "GETCFG")
  {
    Serial.print("CFG;");
    Serial.print("ssid=");
    Serial.print(wifiSSID);
    Serial.print(";pcip=");
    Serial.print(pcIPString);
    Serial.print(";pcport=");
    Serial.print(udpPort);
    Serial.print(";id=");
    Serial.println(deviceID);
  }
  else if (line.startsWith("SETCFG;"))
  {
    String newSsid = getValue(line, "ssid");
    String newPass = getValue(line, "pass");
    String newPcIp = getValue(line, "pcip");
    String newPort = getValue(line, "pcport");
    String newId   = getValue(line, "id");

    if (newSsid.length() > 0) wifiSSID = newSsid;
    if (newPass.length() > 0) wifiPassword = newPass;

    if (newPcIp.length() > 0)
    {
      IPAddress testIP;
      if (testIP.fromString(newPcIp))
      {
        pcIP = testIP;
        pcIPString = newPcIp;
      }
      else
      {
        Serial.println("ERR;bad_ip");
        return;
      }
    }

    if (newPort.length() > 0)
    {
      int p = newPort.toInt();
      if (p > 0 && p < 65536)
      {
        udpPort = (uint16_t)p;
      }
      else
      {
        Serial.println("ERR;bad_port");
        return;
      }
    }

    if (newId == "A" || newId == "B")
    {
      deviceID = newId;
    }
    else
    {
      Serial.println("ERR;bad_id");
      return;
    }

    saveConfig();

    Serial.println("OK");
  }
  else if (line == "INFO")
  {
    Serial.print("INFO;");
    Serial.print("ip=");
    Serial.print(WiFi.localIP());
    Serial.print(";rssi=");
    Serial.print(WiFi.RSSI());
    Serial.print(";id=");
    Serial.print(deviceID);
    Serial.print(";ssid=");
    Serial.println(wifiSSID);
  }
  else if (line == "REBOOT")
  {
    Serial.println("REBOOTING");
    delay(300);
    ESP.restart();
  }
  else
  {
    Serial.println("ERR;unknown_command");
  }
}

// ======================================================
// WIFI CSATLAKOZÁS
// ======================================================

void connectWiFi()
{
  pinMode(LED_PIN, OUTPUT);
  digitalWrite(LED_PIN, LOW);

  WiFi.mode(WIFI_STA);
  WiFi.setSleep(false);

  WiFi.begin(wifiSSID.c_str(), wifiPassword.c_str());

  Serial.print("WiFi kapcsolodas");

  while (WiFi.status() != WL_CONNECTED)
  {
    handleSerialCommands();

    digitalWrite(LED_PIN, !digitalRead(LED_PIN));
    delay(300);
    Serial.print(".");
  }

  digitalWrite(LED_PIN, LOW);

  Serial.println();
  Serial.println("WiFi csatlakozva.");
  Serial.print("ESP IP: ");
  Serial.println(WiFi.localIP());
}

// ======================================================
// SETUP
// ======================================================

void setup()
{
  Serial.begin(115200);
  Serial.setTimeout(50);
  delay(500);

  loadConfig();

  connectWiFi();

  scale.begin(HX_DOUT, HX_SCK);

  Serial.println("HX711 inditva.");

  udp.begin(udpPort);

  Serial.print("UDP cel: ");
  Serial.print(pcIP);
  Serial.print(":");
  Serial.println(udpPort);
}

// ======================================================
// LOOP
// ======================================================

void loop()
{
  handleSerialCommands();

  if (WiFi.status() != WL_CONNECTED)
  {
    digitalWrite(LED_PIN, LOW);
    connectWiFi();
  }

  if (scale.is_ready())
  {
    long raw = scale.read();

    float forceN = (raw - hx_offset) * hx_scale_N_per_count;

    packetCounter++;

    char msg[96];

    snprintf(msg, sizeof(msg),
             "%s;%lu;%lu;%.5f",
             deviceID.c_str(),
             packetCounter,
             micros(),
             forceN);

    udp.beginPacket(pcIP, udpPort);
    udp.print(msg);
    udp.endPacket();
  }
}
