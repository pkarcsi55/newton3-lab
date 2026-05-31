# ESP32 + HX711 Bluetooth erőmérő adatgyűjtő

Ez a dokumentum a [hx711_force_bt_sender.ino](ESP32_code/hx711_force_bt_sender.ino) program alapján készült rövid, szerkeszthető leírás. A modul a Newton 3 Lab projekthez használható Bluetooth-os erőmérő egységként.[ESP32 HX711 

## 1. A mérőegység feladata

Az adatgyűjtő egy ESP32 fejlesztőpanelből, egy HX711 mérőerősítő/A-D átalakítóból és egy erőmérő cellából áll. A rendszer feladata, hogy az erőmérő cella jelét nagy felbontással kiolvassa, majd az adatokat Bluetooth SPP kapcsolaton keresztül elküldje a számítógép felé.

Windows alatt az ESP32 Bluetooth eszközként, majd virtuális COM portként jelenik meg. A Newton 3 Lab C# program ehhez a COM porthoz csatlakozik, és a beérkező adatokat valós időben grafikonon jeleníti meg.

A rendszerből két azonos példány is használható. Ebben az esetben az egyik adó neve lehet például `Newton_FORCE_A`, a másiké `Newton_FORCE_B`. Így két erőmérővel egyszerre vizsgálható az erő és az ellenerő.

## 2. Főbb hardverelemek

- ESP32 fejlesztőpanel
- HX711 24 bites mérőerősítő és A/D átalakító
- erőmérő cella / load cell
- USB vagy akkumulátoros tápellátás
- Bluetooth SPP kapcsolat a számítógép felé

## 3. Lábkiosztás

A feltöltött programban szereplő lábkiosztás:

| Funkció | ESP32 GPIO | Megjegyzés |
|---|---:|---|
| HX711 DOUT | GPIO 22 | HX711 adatvonal |
| HX711 SCK | GPIO 19 | HX711 órajel |
| Bluetooth | beépített | ESP32 Bluetooth SPP kapcsolat |
| USB soros port | beépített | hibakeresésre és parancsadásra is használható |
| Táp | USB / 5 V | a használt ESP32 paneltől függően |
| GND | GND | közös föld |

Megjegyzés: a korábbi kísérleti kódokban előfordulhatott más HX711 SCK láb is, de ebben a konkrét sketchben a `#define HX_SCK 19` beállítás szerepel.

## 4. Bluetooth eszköznév

A program elején állítható be az eszköz Bluetooth neve:

```cpp
static const char *DEVICE_NAME = "Newton_FORCE_A";
```

Két adó használatakor célszerű a második eszköznél ezt átírni például erre:

```cpp
static const char *DEVICE_NAME = "Newton_FORCE_B";
```

Így a két erőmérő a Windows Bluetooth eszközlistájában és a COM portok között is könnyebben megkülönböztethető.

## 5. Adatformátum

Mérés közben az ESP32 soronként küldi az adatokat. Egy adatcsomag formátuma:

```text
t_us;raw;force_N
```

A mezők jelentése:

| Mező | Jelentés |
|---|---|
| `t_us` | a mérés indításától eltelt idő mikroszekundumban |
| `raw` | nyers HX711 mérési érték count egységben |
| `force_N` | kalibráció alapján számított erő newtonban |

Példa:

```text
12500;-2134;-0.000070
```

Ha a HX711 éppen nem olvasható ki, akkor a program ilyen sort is küldhet:

```text
12500;NA;NA
```

Ezt a PC-s programnak érdemes hibás vagy hiányzó mintaként kezelnie.

## 6. Alapértelmezett mintavételezés

A program alapértelmezett mintavételi frekvenciája 80 Hz:

```cpp
static int sampleHz = 80;
static uint32_t samplePeriodUs = 1000000UL / 80UL;
```

A mintavételi frekvencia parancsból állítható 10 és 100 Hz között. Az időzítést a program a `micros()` függvény alapján végzi.

## 7. Kalibráció

A program a nyers HX711 értékből az alábbi összefüggéssel számolja az erőt:

```text
force_N = (raw - hx_offset_counts) * hx_scale_N_per_count
```

A sketchben szereplő alapértékek:

```cpp
static float hx_offset_counts     = -2067.00146f;
static float hx_scale_N_per_count = +1.0709759e-06f;
```

Az `hx_offset_counts` a nullpont, a `hx_scale_N_per_count` pedig az átváltási tényező N/count egységben. Ha az erő előjele fordítva jelenik meg, akkor a skálatényező előjelét kell megfordítani.

Nullázásra a `Z` vagy `TARE` parancs használható. Ilyenkor a program legfeljebb 20 mintát átlagol, és ezekből új nullpontot számít.

## 8. Parancsok

A parancsok USB soros porton vagy Bluetooth kapcsolaton keresztül is elküldhetők.

| Parancs | Funkció |
|---|---|
| `S` vagy `START` | mérés indítása, idő nullázása |
| `X` vagy `STOP` | mérés leállítása |
| `Z` vagy `TARE` | HX711 nullázása |
| `F10` ... `F100` | mintavételi frekvencia beállítása Hz-ben |
| `CAL <érték>` | skálatényező beállítása N/count egységben |
| `SPIKE ON` | kiugró minták szűrésének bekapcsolása |
| `SPIKE OFF` | kiugró minták szűrésének kikapcsolása |
| `SPIKE <count>` | kiugrási küszöb beállítása nyers count egységben |
| `STAT` | állapotinformációk lekérése |
| `HELP` | parancslista kiírása |

Példák:

```text
START
F80
TARE
CAL 1.0709759e-06
SPIKE 1000000
STAT
STOP
```

## 9. Kiugró minták szűrése

A HX711 ritkán adhat irreálisan nagy, egyedi kiugró értékeket. A program ezért tartalmaz egy egyszerű spike szűrőt.

Ha az új nyers mérési érték és az utolsó jónak tekintett érték különbsége nagyobb, mint a beállított küszöb, akkor a program a mintát hibásnak tekinti. Ilyenkor nem engedi tovább a kiugró értéket, hanem az utolsó jó nyers értéket ismétli meg.

A jelenlegi alapértelmezett küszöb:

```cpp
static long spikeThresholdCounts = 1000000;
```

Ez biztonságosabb nagyobb lökések, ütközések esetén, mint a túl alacsony küszöb. Ha a mérésben valódi, gyors erőváltozások is vannak, akkor a küszöböt nem szabad túl kicsire venni.

## 10. A program működése röviden

Indításkor az ESP32:

1. beállítja a CPU frekvenciát 160 MHz-re;
2. elindítja az USB soros kommunikációt;
3. elindítja a Bluetooth SPP kapcsolatot a megadott eszköznévvel;
4. inicializálja a HX711 modult;
5. kiírja az alapállapotot és a súgót.

A fő ciklus két részből áll:

- `pollInputs()` figyeli az USB soros portról és Bluetoothról érkező parancsokat;
- `doMeasurement()` elvégzi az időzített mintavételezést, kiolvassa a HX711-et, kiszámolja az erőt, majd elküldi az adat sort.

A mérés csak `START` parancs után indul el. `STOP` parancsra a mintavételezés leáll, de az eszköz továbbra is fogad parancsokat.

## 11. Oktatási felhasználás

A modul különösen alkalmas Newton III. törvényének szemléltetésére. Két erőmérő használatával egyszerre mérhető két egymásra ható test erőhatása. A mérés során jól megfigyelhető, hogy az erő és az ellenerő nagysága azonos, iránya pedig ellentétes.

Lehetséges kísérletek:

- két erőmérő egymásnak húzása;
- rugós kapcsolat vizsgálata;
- kézi húzás és tolás mérése;
- rövid ütközési impulzusok megfigyelése;
- erőmérés és mobiltelefonos gyorsulásmérés összehasonlítása.

## 12. Kódellenőrzési megjegyzések

A feltöltött kód alapvetően koherens és jól használható. A fontosabb észrevételek:

1. A fájl elején lévő megjegyzésben ez szerepel: `80 mHz órajel`. Ez valószínűleg elírás, helyesen `80 Hz mintavételezés`.

2. A lábkiosztásnál a tényleges SCK láb GPIO 19. Ezt minden dokumentációban így érdemes szerepeltetni.

3. A `doMeasurement()` függvényben ez a rész működik, de formailag érdemes kapcsos zárójelekkel egyértelműbbé tenni:

```cpp
if (SerialBT.hasClient()) 
  SerialBT.println(line);
delayMicroseconds(50);
Serial.println(line);
```

Javasolt forma:

```cpp
if (SerialBT.hasClient()) {
  SerialBT.println(line);
}
delayMicroseconds(50);
Serial.println(line);
```

4. A program minden mintát USB soros portra is kiír. Ez hibakereséshez hasznos, de nagyobb mintavételi frekvencián lassíthatja a rendszert. Ha stabilan működik a Bluetooth adatküldés, később érdemes lehet kapcsolhatóvá tenni a soros debug kimenetet.

5. A `CAL` parancsnál a 0 értéket érvénytelennek tekinti a program, ami helyes. Érdemes arra figyelni, hogy a felhasználó tizedespontot használjon, ne tizedesvesszőt.

6. A `STAT` válasz nagyon hasznos hibakereséshez, mert tartalmazza a küldött minták számát, a HX711 timeoutokat, a mintavételi csúszásokat és a spike szűrő statisztikáját.

Összességében a program dokumentálható és GitHubra feltölthető állapotban van. A legfontosabb javítandó pont inkább dokumentációs jellegű: a `80 mHz` elírás és a pontos lábkiosztás egységesítése.

## 13. Hely a kapcsolási rajznak

Ide kerülhet a végleges kapcsolási rajz vagy bekötési ábra.

Javasolt ábrafelirat:

`1. ábra: Az ESP32 + HX711 Bluetooth-os erőmérő adatgyűjtő bekötése.`
