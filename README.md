# 🧙 VRSorcerer

> Aplicación de rehabilitación de manos en realidad virtual mediante reconocimiento de gestos.

![Unity](https://img.shields.io/badge/Unity-6000.0.58f2-black?logo=unity)
![Platform](https://img.shields.io/badge/Platform-Meta%20Quest-blue?logo=meta)
![OpenXR](https://img.shields.io/badge/OpenXR-enabled-green)
![License](https://img.shields.io/badge/License-MIT-yellow)

---

## 📖 Descripción

VRSorcerer es una aplicación de realidad virtual diseñada para la **rehabilitación de manos**. El paciente debe realizar gestos con las manos para "lanzar hechizos", siguiendo las indicaciones del juego. El sistema evalúa en tiempo real la precisión del gesto y registra el progreso de cada sesión.

El juego genera hechizos de forma aleatoria y el jugador debe mantener el gesto correcto durante un tiempo determinado para completarlo. Toda la sesión queda guardada en un archivo JSON para su posterior análisis clínico.

---

## ✨ Características

- 🎯 **5 hechizos** basados en gestos de mano específicos (pellizcos, mano abierta, apuntar...)
- 📊 **Evaluación de similitud en tiempo real** — el sistema mide qué tan cerca está el paciente de hacer el gesto perfecto
- 🤚 **Soporte para mano izquierda, derecha o ambas** — configurable desde la UI
- ⏱️ **Tiempo de mantenimiento configurable** — ajustable en runtime sin parar el juego
- 💾 **Guardado automático de sesión en JSON** — completados, skipeados, mejor y peor gesto
- 📁 **Guardado con nombre personalizado** — para identificar sesiones por paciente
- ⏭️ **Opción de saltar hechizo** — para gestos que el paciente no puede realizar

---

## 🎮 Hechizos

| # | Hechizo | Gesto |
|---|---------|-------|
| 1 | 💜 Purple | Índice y corazón a pulgar |
| 2 | 🔥 Fire | Meñique a pulgar |
| 3 | 💧 Water | Anular a pulgar |
| 4 | ⚡ Electricity | Mano abierta |
| 5 | ❤️ Red | Apuntar con el índice |

---

## 🛠️ Requisitos

- **Unity** 6000.0.58f2 o superior
- **Meta Quest** 2 / 3 / Pro
- **Meta Horizon Link** (para desarrollo via cable)
- Paquetes de Unity:
  - XR Interaction Toolkit
  - XR Hands
  - AR Foundation
  - XR Composition Layers

---

## 🚀 Instalación

### 1. Clonar el repositorio

```bash
git clone https://github.com/tu-usuario/VRSorcerer.git
cd VRSorcerer
```

### 2. Abrir en Unity

Abre Unity Hub → **Add project from disk** → selecciona la carpeta clonada.

> ⚠️ Usa Unity **6000.0.58f2** para evitar incompatibilidades de paquetes.

### 3. Configurar XR Plug-in Management

En **Edit → Project Settings → XR Plug-in Management**:
- Pestaña PC: ✅ OpenXR → Meta Quest feature group → Runtime: Oculus OpenXR
- Pestaña Android: ✅ OpenXR → Meta Quest feature group

### 4. Build para Meta Quest

```
File → Build Profiles → Android → Set Active → Build & Run
```

Con las Quest conectadas por USB y ADB corriendo.

---

## 📁 Estructura del proyecto

```
Assets/
├── HandRecordings/       # Grabaciones .handsbin de poses de mano
├── HandShapes/           # Assets XRHandShape de cada hechizo
├── Materials/            # Materiales y shaders
├── Meshes/               # Meshes de brazos y manos
├── Prefabs/
│   └── Particles/        # Prefabs de partículas por hechizo
├── Resources/            # Recursos cargados en runtime
├── Samples/              # Samples de XR Hands y XR Interaction Toolkit
└── Scripts/
    ├── PlaceObjectAtHandJoint.cs   # Instancia prefabs en joints de la mano
    ├── HandPoseEvaluator.cs        # Evalúa similitud del gesto en tiempo real
    ├── RandomSpellGenerator.cs     # Lógica principal del juego
    ├── ChangeHandManager.cs        # Gestión de mano activa (L/R/Ambas)
    └── SaveInformation.cs          # Guardado de sesión en JSON
```

---

## 📊 Formato del JSON de sesión

El archivo `session.json` se guarda automáticamente en:
- **Quest**: `/sdcard/Android/data/com.TuEmpresa.VRSorcerer/files/`
- **Windows (editor)**: `%APPDATA%\..\LocalLow\[Empresa]\VRSorcerer\`

```json
{
  "sessionDate": "2026-05-07 10:30:00",
  "totalCompleted": 8,
  "totalSkipped": 2,
  "bestGesture": "Fire",
  "bestGestureScore": 0.94,
  "worstGesture": "Water",
  "worstGestureScore": 0.41,
  "spellRecords": [
    {
      "spellName": "Fire",
      "timesCompleted": 3,
      "timesSkipped": 0,
      "bestSimilarity": 0.94,
      "worstSimilarity": 0.72
    }
  ]
}
```

---

## 🎛️ Parámetros configurables en runtime

| Parámetro | Descripción | Por defecto |
|-----------|-------------|-------------|
| Segundos que mantener el gesto | Tiempo que hay que sostener el gesto para completarlo | 3s |
| Umbral similaridad | Precisión exigida para la evaluación (menor = más exigente) | 0.15 |
| Mano activa | Derecha / Izquierda / Ambas | Derecha |

---

## 🤝 Créditos

Desarrollado por **Izan Calvo Alcubierre** — VRSorcererUC3M  
Universidad Carlos III de Madrid

---

## 📄 Licencia

Este proyecto está bajo la licencia MIT. Consulta el archivo [LICENSE](LICENSE) para más detalles.
